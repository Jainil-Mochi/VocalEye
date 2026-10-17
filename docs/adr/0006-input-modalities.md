# ADR-0006: Text and voice are both first-class input; the server only sees text
Status: Accepted

## Decision
- The API accepts text only. Voice is converted to text **in the client** (`ISpeechToText`), then sent exactly like typed text, with `InputMode = "voice"` as metadata.
- Reply is always text. Speaking it (`ITextToSpeech`) is a client-side toggle, independent of how the user asked.
- Every client shows a text box; the mic is an addition, never a replacement.
- Optional later: `POST /api/transcribe` (server-side Whisper-style STT) for thin clients such as web/mobile.

## Why
One code path for the agent, simple tests (no audio), works on any client, and voice stays a swappable adapter. The Python prototype coupled tools to `speak()`; that coupling is removed.

## Consequences
Evals include voice-style inputs (short, no punctuation, mis-transcriptions) alongside typed ones.
