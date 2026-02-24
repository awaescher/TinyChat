using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Provides data and cancellation support for the event before a message gets sent.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MessageSendingEventArgs"/> class.
/// </remarks>
/// <param name="sender">The sender of the message.</param>
/// <param name="content">The message content being sent.</param>
public class MessageSendingEventArgs(ISender sender, IChatMessageContent content) : CancelEventArgs
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
