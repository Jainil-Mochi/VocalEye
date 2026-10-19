namespace VocalEye.Application.Abstractions.Agents;

/// <summary>One item of a streamed turn: text as it is produced, then a final completed reply.</summary>
public sealed class AgentStreamEvent
{
    private AgentStreamEvent(AgentStreamEventKind kind)
    {
        Kind = kind;
    }

    public AgentStreamEventKind Kind { get; }

    /// <summary>Set when <see cref="Kind"/> is <see cref="AgentStreamEventKind.TextDelta"/>.</summary>
    public string? Text { get; private init; }

    /// <summary>Set when <see cref="Kind"/> is <see cref="AgentStreamEventKind.Completed"/>.</summary>
    public AgentReply? Reply { get; private init; }

    public static AgentStreamEvent ForText(string text)
    {
        return new AgentStreamEvent(AgentStreamEventKind.TextDelta) { Text = text };
    }

    public static AgentStreamEvent ForCompleted(AgentReply reply)
    {
        return new AgentStreamEvent(AgentStreamEventKind.Completed) { Reply = reply };
    }
}
