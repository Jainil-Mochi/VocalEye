# Agents/Tooling
`AgentToolAdapter`: wraps our provider-neutral `IAgentTool` as an `AIFunction` for the Agent Framework, and wraps it in
`ApprovalRequiredAIFunction` when `IToolApprovalPolicy.RequiresApproval(tool)` is true.
Keeps `Tools/` free of any Agent Framework reference (tools stay unit-testable and reusable by an MCP server).
