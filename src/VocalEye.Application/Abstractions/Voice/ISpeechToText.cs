namespace VocalEye.Application.Abstractions.Voice;

/// <summary>Implemented by the client (desktop/mobile/web), never by the server core.</summary>
public interface ISpeechToText
{
    Task<string?> ListenAsync(CancellationToken cancellationToken);
}
