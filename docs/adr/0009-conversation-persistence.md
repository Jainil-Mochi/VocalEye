# ADR-0009: Conversation persistence (Postgres, EF Core)
Status: Accepted

**Decision.** Conversation history is stored in Postgres through EF Core. `Persistence:Provider` selects `Postgres` (default) or `InMemory` (no database; used by tests and quick demos).

**Shape.**
- Domain: `Conversation` (aggregate root) owns ordered `Message`s; `IConversationRepository` is the only write-side abstraction.
- Application: `IConversationStore` is what the agent uses. `ConversationStore` (Infrastructure) implements it on top of the repository. `IConversationQueries` is the read side and returns DTOs.
- Writes load the aggregate (tracked) and append. Reads use `AsNoTracking()` and project straight into DTOs (coding standards section 6).
- History is append-only: a save writes only messages that are not stored yet. A unique index on (ConversationId, Sequence) rejects two concurrent writers to one conversation.

**Why messages are stored as JSON text.** The agent's history contains tool calls, tool results and approval requests. We store each `ChatMessage` as JSON produced by `AIJsonUtilities.DefaultOptions`, so the model can be switched between turns and approvals survive a restart. The column is `text`, not `jsonb`: jsonb reorders keys, and the polymorphic `$type` marker must stay first to deserialize.

**Lifetimes.** DbContext, repository, store and queries are scoped, so `AgentOrchestrator` is scoped too.

**Schema creation.** In Development, `EnsureCreated` makes the tables on first start. There are no EF migrations yet. Add them before the first deployment (`dotnet ef migrations add Initial`) and replace `EnsureCreated` with `Migrate`.

**Not yet.** Per-message model recorded (only the conversation's last model is kept), conversation delete/rename, pgvector (RAG phase), optimistic retry on concurrent writes.
