# Roadmap (tick boxes as you go; work one phase at a time)

## Phase 0: Scaffold ✅
- [x] Solution layout, build props, central package management, compose, Dockerfile, AGENTS.md
- [ ] Run `setup.sh` locally and fix whatever it reports (package names `Anthropic`, `OllamaSharp`, `Microsoft.Agents.AI` were not verified against NuGet from the sandbox)

## Phase 1: Parity with the Python app (headless, via API)
- [ ] Options + secrets (Gemini key, SMTP) with `ValidateOnStart`
- [ ] `ModelCatalog` + `ChatClientFactory` + provider builders (Gemini via OpenAI-compatible, OpenAI, Ollama first; Azure OpenAI/Anthropic next) + `GET /api/models`
- [ ] Agent Framework orchestrator: build `AIAgent` from the chosen `IChatClient`, `AgentToolAdapter`, approval flow (`PendingApprovals` ↔ `Approvals`), streaming, max-step guard
- [ ] Conversation history in Postgres in provider-neutral form; model switchable between turns; record model per message
- [ ] Text endpoint accepts `InputMode` text|voice (identical handling); optional `POST /api/transcribe`
- [ ] Port tools: file generation (txt/pdf/docx/pptx), email (MailKit), desktop command (allow-listed, structured), code builder (sandboxed output dir)
- [ ] `POST /api/chat`, SignalR streaming, health checks
- [ ] Unit tests for each tool; integration test for chat endpoint with a fake `IChatClient`

## Phase 2: RAG
- [ ] Postgres + pgvector, EF migrations
- [ ] Loaders (PDF/DOCX/MD), chunker, embeddings, ingestion job in Worker
- [ ] Retriever with citations; `search_knowledge_base` tool
- [ ] Hybrid search + reranking
- [ ] Eval dataset + recall@k / faithfulness evals, run per model (compare providers)

## Phase 3: Advanced agents
- [ ] Persistent memory (short-term window + long-term summaries/facts)
- [ ] Multi-agent with Agent Framework patterns (sequential/concurrent/handoff/group chat); per-agent model choice
- [ ] MCP client (consume external tools) and MCP server (expose ours)
- [ ] Guardrails: prompt-injection checks, output validation, cost/step budgets

## Phase 4: Production hardening
- [ ] AuthN/AuthZ (OIDC/JWT), per-user rate limits, audit log
- [ ] OpenTelemetry end to end; dashboards + alerts; token/cost metrics
- [ ] CI/CD (build, test, container scan, deploy), environments
- [ ] Load test; caching; background queue durability

## Phase 5: Clients and scale
- [ ] Desktop client (ADR-0004), voice (STT/TTS)
- [ ] Web client
- [ ] Aspire AppHost or Kubernetes; horizontal scaling; extract Worker/Rag if needed
