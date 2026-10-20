using VocalEye.Domain.Conversations;

namespace VocalEye.Application.Conversations;

/// <summary>One stored message as read back for display. <see cref="ContentJson"/> is decoded by <see cref="ChatMessageMapper"/>.</summary>
public class StoredMessageDto
{
    public int Sequence { get; set; }

    public MessageRole Role { get; set; }

    public string ContentJson { get; set; } = string.Empty;
}
