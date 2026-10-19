using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Api.Configuration;

public static class ToolRiskParser
{
    /// <summary>Parses configured risk names. Fails fast at startup with a clear message on an unknown name.</summary>
    public static IReadOnlyList<ToolRiskLevel> Parse(IEnumerable<string>? names)
    {
        List<ToolRiskLevel> risks = new List<ToolRiskLevel>();

        foreach (string name in names ?? Enumerable.Empty<string>())
        {
            if (!Enum.TryParse(name, ignoreCase: true, out ToolRiskLevel risk))
            {
                throw new InvalidOperationException(
                    $"Invalid value '{name}' in Tools:RequireApprovalFor. Allowed: {string.Join(", ", Enum.GetNames<ToolRiskLevel>())}.");
            }

            risks.Add(risk);
        }

        return risks;
    }
}
