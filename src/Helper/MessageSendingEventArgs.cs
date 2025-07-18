using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Provides data and cancellation support for the event before a message gets sent.
/// </summary>
public class MessageSendingEventArgs : CancelEventArgs
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
	public MessageSendingEventArgs(ISender sender, IChatMessageContent content)
	{
		Content = content;
		Sender = sender;
	}
}
