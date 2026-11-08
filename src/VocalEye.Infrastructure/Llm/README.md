# Infrastructure/Llm
Implements `IModelCatalog` and `IChatClientFactory` (Application/Abstractions/Llm).

- `ModelCatalog`: builds the list from `Llm:Providers:*` config. A provider with no ApiKey/Endpoint is omitted.
- `ChatClientFactory`: maps provider `Type` → an `IChatClient`, caches one per (provider, model), then applies the
  standard pipeline (OpenTelemetry, logging, resilience). Function invocation is done by the Agent Framework, not here.
- `Providers/`: one small builder per `Type`:
  - `OpenAI` / `AzureOpenAI`: official OpenAI client → `.AsIChatClient()`
  - `OpenAICompatible`: same OpenAI client with a custom endpoint. Covers **Gemini** (no official .NET provider in the
    framework), Groq, OpenRouter, LM Studio, vLLM
  - Anthropic: currently via its OpenAI-compatible endpoint (`Type: OpenAICompatible`, `https://api.anthropic.com/v1/`). Swap to a native `IChatClient` adapter later if needed (some Claude-specific features are not available through the compat layer)
  - `Ollama`: OllamaSharp (implements `IChatClient`)
Adding a provider = add one builder + one config block. No other code changes.

## Gemini 3 and tool calls
Gemini 3 returns a "thought signature" with each function call and rejects the next request if it is missing
(`400 Function call is missing a thought_signature`). The OpenAI client drops that field, so
`Providers/GeminiThoughtSignatureHandler` adds Google's placeholder (`skip_thought_signature_validator`) to the first tool
call of each assistant message that has no signature. It applies only to `generativelanguage.googleapis.com`.
Config (optional): `Llm:Providers:gemini:ThoughtSignatureFallback` - another placeholder value, or `""` to switch it off.
