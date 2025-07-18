namespace TinyChat;

/// <summary>
/// Provides data for the event when a message was sent
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MessageSentEventArgs"/> class.
/// </remarks>
/// <param name="sender">The sender of the message.</param>
/// <param name="content">The message content being sent.</param>
public class MessageSentEventArgs(ISender sender, IChatMessageContent content) : EventArgs
{
	/// <summary>
	/// Gets the message content being sent.
	/// </summary>
	public IChatMessageContent Content { get; } = content;

	/// <summary>
	/// Gets the sender of the message.
	/// </summary>
	public ISender Sender { get; } = sender;
}