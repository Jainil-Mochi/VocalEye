# VocalEye (.NET)

Production-grade agentic AI platform in .NET 10 built on **Microsoft Agent Framework**: tool-calling agents, RAG, text + voice input, and **any LLM chosen at runtime** (Gemini, OpenAI, Azure OpenAI, Anthropic, Ollama, any OpenAI-compatible endpoint). This is a port and expansion of the original Python prototype
(`legacy/python-voice-assistant`, a Gemini voice assistant with Tkinter UI).

> **Working with an AI coding tool (Codex, Cursor, Claude Code)?** Read [`AGENTS.md`](AGENTS.md) first. Code must follow the binding [coding standards](docs/coding-standards.md).

## Quick start
```bash
./setup.sh            # Windows: ./setup.ps1   (adds NuGet packages, builds, runs unit tests)
docker compose up -d  # Postgres+pgvector, Redis, OpenTelemetry dashboard (http://localhost:18888)
dotnet user-secrets init --project src/VocalEye.Api
dotnet user-secrets set "Llm:Providers:gemini:ApiKey" "<key>" --project src/VocalEye.Api   # add any providers you like
# Conversation history needs Postgres (docker compose above) and this in src/VocalEye.Api/appsettings.json:
#   "Persistence": { "Provider": "Postgres" },
#   "ConnectionStrings": { "Postgres": "Host=localhost;Port=5432;Database=vocaleye;Username=vocaleye;Password=vocaleye" }
# No Docker? Use  "Persistence": { "Provider": "InMemory" }  (history is lost on restart).
dotnet run --project src/VocalEye.Api      # GET /health, GET /api/tools (Phase 1: GET /api/models, POST /api/chat, POST /api/chat/stream, GET /api/conversations)
```

## Architecture in one picture
```
 Clients (Desktop, Web, Mobile)         ← own mic/speaker, approval prompts
        │ HTTP / SignalR
        ▼
   VocalEye.Api ──────────┐       VocalEye.Worker (ingestion, long agent jobs)
        │                 │                │
        ▼                 ▼                ▼
   VocalEye.Agents   VocalEye.Rag     VocalEye.Tools        ← feature modules
        └────────────┬────┴────────────────┘
                     ▼
            VocalEye.Application   ← interfaces (ports) + use cases
                     ▼
              VocalEye.Domain      ← entities, no dependencies
   VocalEye.Infrastructure implements the ports: LLMs, Postgres/pgvector, Redis, OTel
```
Dependencies point inward only.

### Three design promises
- **Any model, decided at runtime.** The client lists models (`GET /api/models`) and sends the choice with each message; switching mid-conversation is supported (ADR-0005).
- **Microsoft Agent Framework** runs the agents (ADR-0002). Its types stay inside `VocalEye.Agents`.
- **Text and voice are both inputs.** Voice is transcribed on the client and sent as text; typing always works (ADR-0006). Full detail: [`docs/architecture.md`](docs/architecture.md).

## Repository layout
| Path | Purpose |
|---|---|
| `src/VocalEye.Domain` | Entities and rules. No packages. |
| `src/VocalEye.Contracts` | Public DTOs shared with clients. Versioned carefully. |
| `src/VocalEye.Application` | Ports (`IAgentTool`, `IRetriever`, `IAgentOrchestrator`, voice) and use cases. |
| `src/VocalEye.Agents` | Orchestration loop, agent definitions, planning, memory, guardrails, prompts. |
| `src/VocalEye.Tools` | Agent tools, one folder each. Ported from the Python tools. |
| `src/VocalEye.Rag` | Ingestion (loaders, chunking), embeddings, retrieval, reranking, eval. |
| `src/VocalEye.Infrastructure` | All external systems: model catalog + provider clients, EF Core, vector store, cache, telemetry, secrets. |
| `src/VocalEye.Api` | ASP.NET Core host (REST + SignalR). Composition root. |
| `src/VocalEye.Worker` | Background host for ingestion and long-running jobs. |
| `src/VocalEye.Client.Desktop` | Future UI (replaces Tkinter). Not scaffolded yet. |
| `tests/` | Unit, integration (Testcontainers), and agent/RAG evals. |
| `evals/datasets` | Golden datasets for evals. |
| `deploy/` | Dockerfiles, Kubernetes manifests. |
| `docs/` | Architecture, roadmap, Python→.NET mapping, ADRs. |
| `legacy/` | Original Python project, reference only. Delete once ported. |

## Roadmap
See [`docs/roadmap.md`](docs/roadmap.md). Current phase: **0 – Scaffold done; next is Phase 1 – port the 4 Python tools and the agent loop.**

## Status
Scaffold was generated without a local .NET SDK, so run `./setup.sh` first; fix any package or compile error it reports before building features.
