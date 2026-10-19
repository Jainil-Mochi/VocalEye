namespace VocalEye.Application.Abstractions.Agents;

public interface IAgentOrchestrator
{
    /// <exception cref="ArgumentException">Nothing to do, unknown approval id, or unknown/unavailable model.</exception>
    Task<AgentReply> RunAsync(AgentRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Same turn as <see cref="RunAsync"/>, but the answer arrives piece by piece and ends with one
    /// <see cref="AgentStreamEventKind.Completed"/> event. Validation errors are thrown on the first MoveNext.
    /// </summary>
    IAsyncEnumerable<AgentStreamEvent> RunStreamingAsync(AgentRequest request, CancellationToken cancellationToken);
}
