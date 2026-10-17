namespace VocalEye.Application.Abstractions.Tools;

/// <summary>Drives the approval policy: Destructive tools never run without explicit user confirmation.</summary>
public enum ToolRiskLevel { ReadOnly, Write, Destructive }
