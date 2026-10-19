using VocalEye.Domain.Common;

namespace VocalEye.Domain.Conversations;

/// <summary>Aggregate root: a conversation and its ordered messages.</summary>
public class Conversation : Entity
{
    private readonly List<Message> _messages = new List<Message>();

    private Conversation()
    {
    }

    public string? ModelProvider { get; private set; }

    public string? ModelName { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<Message> Messages => _messages;

    public static Conversation Create(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A conversation id must not be empty.", nameof(id));
        }

        return new Conversation { Id = id };
    }

    public Message AddMessage(MessageRole role, string contentJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentJson);

        Message message = new Message(Id, _messages.Count, role, contentJson);
        _messages.Add(message);
        UpdatedAt = DateTimeOffset.UtcNow;
        return message;
    }

    public void SetModel(string provider, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        ModelProvider = provider;
        ModelName = model;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
