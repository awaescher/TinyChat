namespace TinyChat;

/// <summary>
/// Provides data for the event when a message was sent
/// </summary>
public class MessageSentEventArgs : EventArgs
{
	/// <summary>
	/// Gets the message content being sent.
	/// </summary>
	public IChatMessageContent Content { get; }

	/// <summary>
	/// Gets the sender of the message.
	/// </summary>
	public ISender Sender { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSendEventArgs"/> class.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The message content being sent.</param>
	public MessageSentEventArgs(ISender sender, IChatMessageContent content)
	{
		Content = content;
		Sender = sender;
	}
}