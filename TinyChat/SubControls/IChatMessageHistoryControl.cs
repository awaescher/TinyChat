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

	/// <summary>
	/// Removes a message control by a given message
	/// </summary>
	/// <param name="message">The message to remove the control for</param>
	void RemoveMessageControl(IChatMessage message);

	/// <summary>
	/// Scrolls to the last message, for example after a whole conversation was loaded at once.
	/// Following a streamed answer starts again from there.
	/// </summary>
	void ScrollToEnd() { }

	/// <summary>
	/// Gets whether the history follows new content, because the user has not scrolled away from its end.
	/// </summary>
	bool IsFollowingEnd => true;
}