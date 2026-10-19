using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Application.Abstractions.Agents;

public sealed record ToolInvocation(string Name, string ArgumentsJson, ToolResult Result);
