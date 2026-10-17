using VocalEye.Domain.Common;

namespace VocalEye.Domain.Conversations;

public sealed class Conversation : Entity
{
    private readonly List<Message> _messages = [];

    public IReadOnlyList<Message> Messages => _messages;

    public Message Add(MessageRole role, string content)
    {
        var message = new Message(role, content);
        _messages.Add(message);
        return message;
    }
}
