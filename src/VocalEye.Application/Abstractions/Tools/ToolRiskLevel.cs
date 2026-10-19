namespace VocalEye.Application.Abstractions.Tools;

/// <summary>Drives the approval policy.</summary>
public enum ToolRiskLevel
{
    ReadOnly,
    Write,
    Destructive
}
