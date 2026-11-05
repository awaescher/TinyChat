namespace TinyChat.Helper;

/// <summary>
/// Provides data for the <see cref="TinyChat.ChatControl.ErrorOccurred"/> event.
/// </summary>
public class ChatErrorEventArgs : EventArgs
{
	/// <summary>
	/// Gets the exception that was thrown.
	/// </summary>
	public Exception Exception { get; }

	/// <summary>
	/// Gets or sets a value indicating whether the error has been handled.
	/// When set to <see langword="true"/>, the default diagnostic icon will not be added to the chat history
	/// and any pending re-throw will be suppressed.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Initializes a new instance of <see cref="ChatErrorEventArgs"/>.
	/// </summary>
	/// <param name="exception">The exception that occurred.</param>
	public ChatErrorEventArgs(Exception exception)
	{
		Exception = exception;
	}
}
