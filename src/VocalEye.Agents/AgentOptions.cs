namespace VocalEye.Agents;

/// <summary>Limits for one conversation turn. Bound from the "Agents" configuration section.</summary>
public class AgentOptions
{
    public const int LowestMaxToolRounds = 1;
    public const int HighestMaxToolRounds = 50;

    /// <summary>
    /// How many times the model may be called in one turn before it is told to answer without tools. A normal turn needs
    /// two (ask for a tool, then answer); a loop that never ends would otherwise hold the request, and the model bill, open.
    /// </summary>
    public int MaxToolRounds { get; set; } = 8;

    /// <exception cref="InvalidOperationException">The limit is outside the allowed range.</exception>
    public void Validate()
    {
        if (MaxToolRounds < LowestMaxToolRounds || MaxToolRounds > HighestMaxToolRounds)
        {
            throw new InvalidOperationException(
                $"Agents:MaxToolRounds must be between {LowestMaxToolRounds} and {HighestMaxToolRounds} (it is {MaxToolRounds}).");
        }
    }
}
