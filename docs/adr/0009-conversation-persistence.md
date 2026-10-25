# ADR-0009: Conversation persistence (Postgres, SQLite, EF Core)
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

**SQLite (desktop, slice B1).** `Persistence:Provider = Sqlite` stores history in one file, by default `%LocalAppData%\VocalEye\vocaleye.db` (override with `ConnectionStrings:Sqlite`), so the desktop app needs no database server. It uses the same EF model as Postgres, with two differences: `DateTimeOffset` columns are stored as UTC ticks (SQLite cannot sort a native DateTimeOffset), and the file is switched to write-ahead logging so reads are not blocked while a reply is saved. The schema is created on start in every environment, because the file belongs to the user's machine; Postgres is still created only in Development. Needs the package `Microsoft.EntityFrameworkCore.Sqlite` in Infrastructure. Moving existing Postgres history into SQLite is not supported.

**Titles, rename and delete (slice B4).** `Conversation.Title` (max 100 characters; the database column allows 200) is set from the first user message (shortened to 60 characters) unless the user renames the conversation, and a conversation saved without a title gets one on its next save. Rename and delete go through `IConversationManager` and run as single SQL statements (`ExecuteUpdate`, `ExecuteDelete`), so they do not load the messages. Delete removes the messages first, in one transaction.

**Adding a column before migrations exist.** `EnsureCreated` never alters an existing table, so `PersistenceInitializer` adds the `Title` column to older SQLite and Postgres databases (`ALTER TABLE ... ADD COLUMN`, safe to run twice). This is a stop-gap: switch to real EF migrations before the first desktop release, because users will upgrade with data. The Postgres upgrade runs only when the initializer runs (Development); other environments must add the column themselves.
