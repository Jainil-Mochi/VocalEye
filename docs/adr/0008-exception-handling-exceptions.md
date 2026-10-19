# ADR-0008: Where local exception handling is allowed
Status: Accepted

The coding standards say domain and application classes bubble raw faults to one global exception middleware. Three places legitimately need a local `catch`; each is narrow and documented in code:

1. **Tool boundary** (`AgentToolAdapter`). A failing tool must become text the model can read ("Error: ..."), otherwise one bad tool call aborts the entire agent run and the user gets nothing.
2. **Vendor-exception translation** (`ErrorDetailChatClient`, Infrastructure). Provider SDK exceptions hide the real reason in the response body. The adapter rethrows an `HttpRequestException` that names the model and includes the body. It does not swallow anything; the middleware still produces the response (502).
3. **Streaming responses** (`ChatStreamWriter`). After an SSE response has started, the status code can no longer change and middleware cannot write a problem body. The writer catches the fault, logs it, and sends a final `error` event. Faults that occur **before** the first byte (validation, model unavailable, model refused the request) are surfaced by pulling the first event before starting the response, so the middleware still handles them.

Everything else bubbles up.
