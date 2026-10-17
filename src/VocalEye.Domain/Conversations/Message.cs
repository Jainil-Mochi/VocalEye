using VocalEye.Domain.Common;

namespace VocalEye.Domain.Conversations;

public sealed class Message : Entity
{
    public Message(MessageRole role, string content)
    {
        Role = role;
        Content = content;
    }

    public MessageRole Role { get; }
    public string Content { get; }
}
