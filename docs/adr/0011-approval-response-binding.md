# ADR-0011: Approval-response binding is disabled; the server binds approvals

Status: Accepted

**Context.** Microsoft Agent Framework (1.24+) adds `ApprovalResponseBindingChatClient`. It accepts a tool-approval response only if the framework itself recorded the matching request in the run's `AgentSession` state, and silently drops any other response ("so a forged approval cannot execute"). Our agent is stateless per turn (ADR-0007): we rebuild it for every request and pass the stored history, with no session. The recorded-request list is therefore always empty, every approval response was dropped, and the next step failed with
`ToolApprovalRequestContent found with FunctionCall.CallId(s) '...' that have no matching ToolApprovalResponseContent.`

**Decision.** Build the agent with `ChatClientAgentOptions.DisableApprovalResponseBinding = true`, and enforce the same guarantee ourselves:
- The client sends only `{ approvalId, approved }`. It cannot send tool names or arguments.
- `ApprovalMessageBuilder` creates the response from the `ToolApprovalRequestContent` found in the conversation history **we stored** (Postgres). An id with no stored request is rejected with 400.
- A tool call that already has a result is never run again (the framework skips responses whose call already has a result).

**Rejected alternative.** Persist and restore `AgentSession` state per conversation. It stores framework-internal state we do not control, couples us to the framework's session format, and fights the stateless design.

**Revisit** if we add multi-user accounts: approvals must then also check that the caller owns the conversation.
