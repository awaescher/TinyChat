namespace TinyChat.Messages;

/// <summary>
/// Represents a chat message with a sender and content.
/// </summary>
/// <param name="Sender">The sender of the message.</param>
/// <param name="Content">The content of the message.</param>
public record ChatMessage(ISender Sender, IChatMessageContent Content) : IChatMessage;
