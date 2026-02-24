namespace TinyChat;

/// <summary>
/// Represents a text input control for sending chat messages.
/// </summary>
public interface IChatInputControl
{
	/// <summary>
	/// The event that is raised when cancellation of a streaming message is requested.
	/// </summary>
	public event EventHandler? CancellationRequested;

	/// <summary>
	/// Occurs when a message is sent from the text box and allows the cancellation of sending.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs> MessageSending;

	/// <summary>
	/// Sets whether the control is receiving a stream or not
	/// </summary>
	/// <param name="isReceiving">The flag specifying whether a stream is being received or not</param>
	/// <param name="allowCancellation">The flag specifying whether the stream can be cancelled or not</param>
	void SetIsReceivingStream(bool isReceiving, bool allowCancellation);
}
