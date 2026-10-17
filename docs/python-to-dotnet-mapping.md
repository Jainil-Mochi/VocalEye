# Python prototype → .NET mapping

| Python (legacy) | What it does | .NET target |
|---|---|---|
| `main.py`, `tools/first_run_config.py` | Entry, first-run key prompt | `Api/Program.cs`; options + user-secrets; client onboarding UI later |
| `config/settings.py` (env → keyring → config.ini) | Secrets/config | `IOptions<T>` + user-secrets/Key Vault. Drop `config.ini` |
| `modules/llm_processor.py` (Gemini function calling, one global chat) | Agent loop | `Agents/Orchestration` + `IChatClient`; per-conversation memory, not global state |
| `llm_tools/create_file.py` (reportlab, python-docx, python-pptx) | File generator tool | `Tools/FileGeneration` (QuestPDF, OpenXml) |
| `llm_tools/email_sender.py` (smtplib, Gmail) | Email tool | `Tools/Email` (MailKit), `Destructive` risk, approval required |
| `llm_tools/generate_os_command.py` + `command_executor.py` | LLM writes shell command, runs it with `shell=True` | `Tools/DesktopCommand`: structured actions (open_url, open_app) from an allow-list, executed on the **client**. No arbitrary shell |
| `llm_tools/code_builder.py` (LLM JSON manifest → files → run) | Project generator | `Tools/CodeBuilder`: write only inside a sandbox dir; running code is opt-in, approved, containerized |
| `speech_to_text.py` (SpeechRecognition + Google) | STT | `ISpeechToText` implemented in client (Windows Speech / Azure Speech / Whisper) |
| `text_to_speech.py` (pyttsx3) | TTS | `ITextToSpeech` in client |
| `tools/app_ui.py` (Tkinter) | UI | `Client.Desktop` (ADR-0004) |

## Problems in the prototype to fix in the port
1. `shell=True` + LLM-generated commands = remote-code-execution risk (prompt injection → arbitrary commands). The substring blocklist in `code_builder.py` is not a real defense.
2. Tools call `speak()` directly: business logic coupled to audio. Tools must return results; the client speaks.
3. Global mutable `chat` session and module-level `genai.configure` → not multi-user, not testable.
4. Errors swallowed (`return "Error sending email"`) → no diagnosis. Use structured `ToolResult` + logs.
5. Email password in app config. Use SMTP app-password in secret store; later OAuth.
6. `file_generator` ignores `save_path` for converted files and always writes `.txt` first.
7. Single tool call per turn; results are not fed back to the model. The new loop returns tool results to the model so it can chain steps.
