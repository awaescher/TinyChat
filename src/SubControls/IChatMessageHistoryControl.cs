namespace TinyChat;

/// <summary>
/// Provides functionality for managing chat message history controls.
/// </summary>
public interface IChatMessageHistoryControl
{
	/// <summary>
	/// Appends a chat message control to the history.
	/// </summary>
	/// <param name="messageControl">The chat message control to append.</param>
	void AppendMessageControl(IChatMessageControl messageControl);

	/// <summary>
	/// Clears all message controls from the history.
	/// </summary>
	void ClearMessageControls();
}