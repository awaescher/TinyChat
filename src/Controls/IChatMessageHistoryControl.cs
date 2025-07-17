namespace TinyChat;

public interface IChatMessageHistoryControl
{
	/// <summary>
	/// Appends a chat message control to the history.
	/// </summary>
	/// <param name="messageControl">The chat message control to append.</param>
	void AppendMessage(IChatMessageControl messageControl);
	void ClearMessageControls();
}