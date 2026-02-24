namespace TinyChat;

/// <summary>
/// Represents a control that can display a chat message.
/// </summary>
public interface IChatMessageControl
{
	/// <summary>
	/// The event that is raised when the size of the control is updated while streaming a message.
	/// </summary>
	public event EventHandler? SizeUpdatedWhileStreaming;

	/// <summary>
	/// Gets or sets the chat message displayed by this control.
	/// </summary>
	IChatMessage? Message { get; set; }

	/// <summary>
	/// Sets whether the control is receiving a stream or not
	/// </summary>
	/// <param name="isReceiving">The flag specifying whether a stream is being received or not</param>
	void SetIsReceivingStream(bool isReceiving);
}
