namespace VocalEye.Contracts.Chat;

/// <summary>Payload of the "error" server-sent event (errors after the stream has started).</summary>
public class ChatStreamErrorDto
{
    public string Title { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;
}
