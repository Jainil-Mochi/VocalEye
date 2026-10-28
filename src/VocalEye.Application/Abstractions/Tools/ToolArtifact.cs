namespace VocalEye.Application.Abstractions.Tools;

/// <summary>Something a tool produced that a client can offer to open: a file, a folder or a web address.</summary>
/// <param name="Kind">One of <see cref="ToolArtifactKinds"/>.</param>
/// <param name="Value">The full path, or the URL.</param>
public sealed record ToolArtifact(string Kind, string Value);
