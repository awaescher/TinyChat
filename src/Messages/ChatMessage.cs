namespace TinyChat;

/// <summary>
/// Represents a chat message with a sender and content.
/// </summary>
/// <param name="Sender">The sender of the message.</param>
/// <param name="Content">The content of the message.</param>
[System.Diagnostics.DebuggerDisplay("{Sender.Name}: {Content.Content}")]
public record ChatMessage(ISender Sender, IChatMessageContent Content) : IChatMessage;
