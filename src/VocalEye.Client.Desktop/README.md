# Desktop client (not scaffolded yet)
Decision pending (ADR-0004): Avalonia vs WPF vs MAUI.

## Inputs: text AND voice (ADR-0006)
One chat box that always accepts typing; a mic button adds speech. Both produce a string that is sent as
`ChatRequest { Message, InputMode = "text" | "voice" }`. Speech-to-text runs in the client (`ISpeechToText`);
the server never needs audio. Replies are always shown as text; speaking them (`ITextToSpeech`) is a client toggle,
so a typed message can still be read aloud and a spoken one can be answered silently.

## Also owns
Model picker (from `GET /api/models`, switchable per message), approval prompts for tool calls
(`PendingApprovals` → Approve/Deny → `ChatRequest.Approvals`), local-only actions (open app/URL).
