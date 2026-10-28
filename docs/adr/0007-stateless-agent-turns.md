# ADR-0007: We own conversation history; the agent is rebuilt every turn
Status: Accepted

## Decision
Conversation history is stored by us (`IConversationStore`, provider-neutral `ChatMessage` list, plus the last model used). Each turn the orchestrator builds a `ChatClientAgent` for the chosen model and passes the full history to `RunAsync`. No Agent Framework session is persisted.

## Why
- Model switching between turns (ADR-0005) needs history that is not tied to one provider or one agent instance.
- A stateless API scales horizontally; any replica can serve any turn.
- We can list, search, export and delete conversations in our own database.

## Approvals
A paused tool call is stored in history as the framework's approval-request content. The client answers with `ApprovalDecision`s; the orchestrator turns them into the framework's approval response message and replays history, which resumes the run. Approval id = the tool call id.

## Costs / watch-outs
- The whole history is sent each turn; add a window/summary strategy when conversations get long (Phase 3 memory).
- Provider-specific content (e.g. reasoning blocks) may not replay on another provider; strip when found.
- Streaming and Postgres persistence come in slice 3.

**Tool-round limit (slice B7).** `ToolRoundLimitChatClient` wraps the model client for one turn and counts model calls. After `Agents:MaxToolRounds` calls (default 8, allowed 1 to 50) it stops sending the tool list and adds a short notice asking the model to answer with what it has, so a model that loops on tools ends with a normal reply. It sits below the agent's own tool loop, so it does not depend on framework settings. Approval pauses do not count: the turn ends there and the next request starts a new count.
