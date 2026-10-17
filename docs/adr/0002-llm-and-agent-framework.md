# ADR-0002: Microsoft Agent Framework on top of Microsoft.Extensions.AI
Status: Accepted (supersedes the earlier "start custom" note)

## Decision
- Agent runtime: **Microsoft Agent Framework** (`Microsoft.Agents.AI`; reported 1.0/GA in April 2026; re-check the package's stable/prerelease state when restoring).
- LLM abstraction: **`Microsoft.Extensions.AI` `IChatClient`**. Any provider that exposes an `IChatClient` can back an agent, which is what makes runtime model switching possible (ADR-0005).
- Framework types (`AIAgent`, `AgentSession`, `AIFunction`, `ToolApprovalRequestContent`) are confined to `VocalEye.Agents`. Everything else sees only our interfaces (`IAgentOrchestrator`, `IAgentTool`).

## Why
We want to learn and use the framework Microsoft is investing in, and it already provides: tool calling, sessions/memory, streaming, human approval, multi-agent patterns (sequential, concurrent, handoff, group chat), MCP and A2A. Writing our own loop would duplicate that.

## Consequences
- `IAgentTool` → `AIFunction` adapter in `Agents/Tooling`; risky tools wrapped with `ApprovalRequiredAIFunction`.
- Conversation history is stored by us (Postgres) in provider-neutral `ChatMessage` form, so the model can change between turns.
- Gemini has no official .NET provider in the framework: use an OpenAI-compatible endpoint or a community `IChatClient` (Infrastructure/Llm/Providers).
- API shapes were checked against Microsoft Learn on 2026-10-17 (`AsAIAgent`, `RunAsync`, `CreateSessionAsync`, `ApprovalRequiredAIFunction`). The exact `IChatClient` → agent call must be confirmed against the installed version in Phase 1. Fallback if the framework blocks us: a small custom loop behind the same `IAgentOrchestrator`.
