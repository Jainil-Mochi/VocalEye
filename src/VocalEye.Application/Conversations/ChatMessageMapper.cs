using System.Text.Json;
using Microsoft.Extensions.AI;
using VocalEye.Domain.Conversations;

namespace VocalEye.Application.Conversations;

/// <summary>Converts between the framework's provider-neutral messages and what we store.</summary>
public static class ChatMessageMapper
{
    public static string ToJson(ChatMessage message)
    {
        return JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions);
    }

    public static ChatMessage FromJson(string json)
    {
        ChatMessage? message = JsonSerializer.Deserialize<ChatMessage>(json, AIJsonUtilities.DefaultOptions);
        if (message is null)
        {
            throw new InvalidOperationException("A stored chat message could not be read. The conversation history is corrupt.");
        }

        return message;
    }

    public static MessageRole ToRole(ChatRole role)
    {
        if (role == ChatRole.User)
        {
            return MessageRole.User;
        }

        if (role == ChatRole.Assistant)
        {
            return MessageRole.Assistant;
        }

        if (role == ChatRole.Tool)
        {
            return MessageRole.Tool;
        }

        if (role == ChatRole.System)
        {
            return MessageRole.System;
        }

        throw new InvalidOperationException($"Unsupported chat role '{role.Value}'.");
    }
}
