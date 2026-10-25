using VocalEye.Domain.Common;

namespace VocalEye.Domain.Conversations;

/// <summary>Aggregate root: a conversation and its ordered messages.</summary>
public class Conversation : Entity
{
    public const int MaximumTitleLength = 100;
    private const int AutomaticTitleLength = 60;

    private readonly List<Message> _messages = new List<Message>();

    private Conversation()
    {
    }

    public string? Title { get; private set; }

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

    /// <summary>The user's chosen title, trimmed and on one line.</summary>
    /// <exception cref="ArgumentException">The title is blank or longer than <see cref="MaximumTitleLength"/>.</exception>
    public static string NormalizeTitle(string title)
    {
        string cleaned = CollapseWhitespace(title ?? string.Empty);
        if (cleaned.Length == 0)
        {
            throw new ArgumentException("A conversation title must not be blank.", nameof(title));
        }

        if (cleaned.Length > MaximumTitleLength)
        {
            throw new ArgumentException($"A conversation title can have at most {MaximumTitleLength} characters.", nameof(title));
        }

        return cleaned;
    }

    public void Rename(string title)
    {
        Title = NormalizeTitle(title);
    }

    /// <summary>Gives the conversation a title taken from the user's first message, unless it already has one.</summary>
    public void SetTitleIfMissing(string? firstUserText)
    {
        if (!string.IsNullOrWhiteSpace(Title))
        {
            return;
        }

        string cleaned = CollapseWhitespace(firstUserText ?? string.Empty);
        if (cleaned.Length == 0)
        {
            return;
        }

        Title = cleaned.Length <= AutomaticTitleLength
            ? cleaned
            : cleaned[..(AutomaticTitleLength - 1)].TrimEnd() + "…";
    }

    private static string CollapseWhitespace(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
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
