namespace VocalEye.Contracts.Chat;

/// <summary>Payload of the "text" server-sent event.</summary>
public class ChatStreamTextDto
{
    public string Text { get; set; } = string.Empty;
}
