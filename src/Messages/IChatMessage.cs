namespace WinFormsChat.Messages;

/// <summary>
/// Represents a chat message with a sender and content.
/// </summary>
public interface IChatMessage
{
	/// <summary>
	/// Gets the sender of the message.
	/// </summary>
	ISender Sender { get; }

	/// <summary>
	/// Gets the content of the message.
	/// </summary>
	IChatMessageContent Content { get; }
}
