using VocalEye.Domain.Common;

namespace VocalEye.Domain.Conversations;

/// <summary>
/// One stored chat message. <see cref="ContentJson"/> holds the provider-neutral message
/// (text, tool calls, tool results, approval requests) serialized by the Application layer.
/// </summary>
public class Message : Entity
{
    private Message()
    {
    }

    internal Message(Guid conversationId, int sequence, MessageRole role, string contentJson)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        Sequence = sequence;
        Role = role;
        ContentJson = contentJson;
    }

    public Guid ConversationId { get; private set; }

    public int Sequence { get; private set; }

    public MessageRole Role { get; private set; }

    public string ContentJson { get; private set; } = string.Empty;
}
