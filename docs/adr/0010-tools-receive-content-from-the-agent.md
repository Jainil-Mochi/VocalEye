# ADR-0010: Tools receive their content from the agent

Status: Accepted

**Context.** The Python tools each called Gemini a second time to write what they needed (file text, email body, shell command, project manifest). That ties every tool to one model and one API key, doubles latency and cost, and hides the content from the agent that has to explain the result to the user.

**Decision.** The agent's own model writes the content and passes it in the tool arguments. A tool only validates, saves and reports. This works with any model chosen at runtime (ADR-0005) and keeps tools small and testable without a model.

**Consequences.**
- Tool descriptions and `SystemPrompts.Assistant` tell the model to send the *complete* content.
- Tools validate everything (size limits, supported types) and return `ToolResult.Fail` with a message the model can act on.
- File-writing tools write only below `Tools:Output:OutputDirectory` (default `Documents\VocalEye`) through `SafePath`, never overwrite, and reject `..`, absolute paths and drive letters.
- Tools do not open the file or speak. Audio and "open the result" belong to the client (ADR-0004).

**file_generator (slice 5a).** txt, md, pdf (QuestPDF, Community licence) and docx (Open XML SDK). pptx is not supported yet and the tool says so; add it as its own small slice.

**code_builder (slice 5b).** The model sends the project as a list of `{ path, content }` files. The tool validates all of them first (at most 60 files, 256 KB per file, 2 MB total, no duplicate paths, no path that is both a file and a folder, no `..` or absolute paths) and then writes them into a new folder `projects/<name>` (numbered if the name exists). It never runs the project. The Python tool also executed the manifest's `run_command` after a substring blocklist; a blocklist is not a defence against prompt injection, so execution is out of scope. If running generated code is added later it must be a separate, approved, sandboxed tool.

**desktop_command (slice 5c).** The Python tool asked a model to write a terminal command and ran it with `shell=True`. The port exposes two structured actions instead: `open_url` (http/https only) and `open_app` (the model names an app; the executable comes from the `Tools:Desktop:AllowedApps` list, never from the model, and no arguments are passed). It starts processes through the OS shell-execute call, not a command line, so nothing the model writes is ever parsed as a command. It acts on the machine running the API; on a server set `Tools:Desktop:ExecuteLocally` to false and the tool refuses. A later desktop client (ADR-0004) will receive these actions and perform them on the user's own machine.

**send_email (slice 5d).** The agent writes the subject and body; the tool validates and sends one plain-text message through `IEmailSender` (MailKit). Rules: exactly one recipient (no cc/bcc), subject on one line, body at most 20,000 characters, optional `Tools:Email:AllowedRecipientDomains` allow-list, risk level Destructive so it always pauses for approval under the default policy. SMTP settings come from `Tools:Email`; the password lives in user-secrets (the Python version kept it in a config file). If email is not configured the tool description says so, so the model tells the user instead of trying.
