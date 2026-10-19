namespace VocalEye.Api.Streaming;

/// <summary>Outcome of advancing the event stream by one item.</summary>
internal sealed class StreamStep
{
    public bool HasNext { get; init; }

    public Exception? Error { get; init; }
}
