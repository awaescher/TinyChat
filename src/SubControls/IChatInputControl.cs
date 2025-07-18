namespace TinyChat;

/// <summary>
/// Represents a text input control for sending chat messages.
/// </summary>
public interface IChatInputControl
{
	/// <summary>
	/// Occurs when a message is sent from the text box and allows the cancellation of sending.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs> MessageSending;
}
