namespace VocalEye.Contracts.Chat;

/// <summary>Body of PATCH /api/conversations/{id}.</summary>
public class RenameConversationDto
{
    /// <summary>1 to 100 characters; whitespace is trimmed and line breaks become spaces.</summary>
    public string Title { get; set; } = string.Empty;
}
