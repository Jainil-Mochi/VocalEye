namespace VocalEye.Contracts.Chat;

public class ConversationMessageDto
{
    public int Sequence { get; set; }

    public string Role { get; set; } = string.Empty;

    /// <summary>The message text. Empty for messages that only contain tool calls or tool results.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Names of the tools the assistant called in this message, if any.</summary>
    public List<string> ToolNames { get; set; } = new List<string>();

    /// <summary>For a tool message: what each tool call returned, with its name, arguments and any files it made.</summary>
    public List<ToolCallDto> ToolResults { get; set; } = new List<ToolCallDto>();
}
