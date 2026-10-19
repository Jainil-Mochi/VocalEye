# Architecture

## Style
**Modular monolith, Clean Architecture.** One deployable API plus a Worker, split into feature modules with enforced boundaries. This gives microservice-like seams (each module can be extracted later) without microservice cost today.

## Layers and allowed references
```
Domain            → (nothing)
Contracts         → (nothing)
Application       → Domain
Agents/Tools/Rag  → Application (+ Domain)         [never each other]
Infrastructure    → Application (+ Domain)
Api / Worker      → everything (composition roots)
```

## Core runtime flow (one user turn)
1. Client sends **typed text or transcribed speech** (same field, `InputMode` metadata) plus an optional **model choice** → `POST /api/chat`, or `POST /api/chat/stream` for server-sent events.
2. `IAgentOrchestrator` resolves the model: request → conversation's last model → catalog default.
3. `IChatClientFactory.Create(selection)` returns the cached `IChatClient`; the orchestrator builds an Agent Framework `AIAgent` with instructions + tools (`IAgentTool` adapted to `AIFunction`) and the conversation history loaded from Postgres.
4. The agent runs. For tools the policy marks as needing approval, the run **pauses** and returns `PendingApprovals`; the client shows Approve/Deny and calls again with `Approvals`. Otherwise the tool runs and its result goes back to the model (loop, with a max-step guard).
5. Stream the final text; persist messages with the model used; emit traces/metrics (tokens, latency, cost per model).

## How each growth axis is handled
| Need | Mechanism |
|---|---|
| New LLM provider / model | Config block under `Llm:Providers`; new provider *type* = one builder in `Infrastructure/Llm/Providers`. Users pick at runtime (ADR-0005). |
| New input channel | Anything that yields text (typing, STT, web form, Slack) calls the same endpoint. |
| New tool | Add `IAgentTool` in `Tools/`. Later: external tools via MCP (`Tools/Mcp`). |
| New agent / multi-agent | Add a definition in `Agents/Definitions`; orchestrator routes. Built on Microsoft Agent Framework orchestration patterns (sequential, concurrent, handoff, group chat) (ADR-0002). |
| RAG store swap | `IVectorStore` behind Application; start pgvector, can move to Qdrant/Azure AI Search. |
| More load | API is stateless (state in Postgres/Redis) → scale replicas. Heavy jobs go to Worker via queue. |
| Extract a service | Module already has an interface boundary + its own DI extension; move it to its own host. |
| New client | Consumes `Contracts` only. |

## Cross-cutting
- **Observability:** OpenTelemetry traces/metrics/logs → OTLP (Aspire dashboard locally). One `ActivitySource` per module; spans for each LLM call and tool call, with token counts.
- **Resilience:** `Microsoft.Extensions.Http.Resilience` (retry, timeout, circuit breaker) on all outbound HTTP/LLM calls.
- **Security:** secrets via user-secrets/Key Vault; tool approval policy; auth (OIDC/JWT) added in Phase 4; rate limiting per user; PII-safe logging.
- **Config:** typed `IOptions<T>`, validated on start.
- **Caching:** Redis for embeddings cache, session state, semantic cache (later).
- **Messaging:** start with in-process `Channel<T>`; interface in `Application/Abstractions/Messaging` so RabbitMQ/Service Bus can replace it.

## RAG pipeline
Ingest: load (PDF/DOCX/MD/TXT) → clean → chunk (recursive, overlap) → embed → upsert (vector + metadata + source).
Query: rewrite query → hybrid search (vector + keyword) → rerank → context assembly with citations → generation → groundedness check.
Quality is measured in `tests/VocalEye.AgentEvals` (recall@k, faithfulness).

## Data
Postgres (EF Core): conversations, messages, documents, chunks (pgvector column), tool-call audit log. Redis: cache/sessions.

## Deployment path
Local: docker-compose infra + `dotnet run`. Next: container images (`deploy/docker`). Then: Aspire AppHost or Kubernetes manifests (`deploy/k8s`), CI pipeline, staged environments.
