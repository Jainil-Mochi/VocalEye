# ADR-0005: Choose the AI model at runtime
Status: Accepted

## Decision
- Models are declared in config (`Llm:Providers:*:Models`) → `IModelCatalog`. The user picks one in the client; it is sent as `ChatRequest.Model` and may change on every message. Omitted = conversation's last model, else `Llm:Default`.
- `IChatClientFactory.Create(ModelSelection)` returns a cached, telemetry-wrapped `IChatClient`. The orchestrator builds the agent for that client per turn (cheap) and reuses the stored conversation history.
- Provider kinds: OpenAI, AzureOpenAI, OpenAICompatible (Gemini, Groq, OpenRouter, LM Studio, vLLM), Anthropic, Ollama. Adding one = one builder + one config block.
- Each `ModelInfo` carries capability flags (tools, streaming, vision). If a model lacks tool support, the agent runs without tools and tells the user, instead of failing.

## Consequences / watch-outs
- Switching provider mid-conversation works on normalized messages, but provider-specific content (e.g. reasoning blocks) may not carry over; strip or ignore it when replaying history.
- Tool-calling quality varies a lot by model; `AgentEvals` runs the same tool-selection dataset against each model.
- API keys: user-secrets/env/Key Vault now. Per-user "bring your own key" (encrypted at rest) is a Phase 4 option.
- Record the model used on every assistant message, trace span and cost metric.
