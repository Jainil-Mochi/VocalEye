# ADR-0003: Tool safety model
Status: Accepted
Decision: Every tool declares `ToolRiskLevel`. `Destructive` tools require approval via `IToolApprovalPolicy` (interactive confirm on client, or pre-approved policy for headless). Tools accept structured args only, never raw shell strings. All calls are audit-logged.
