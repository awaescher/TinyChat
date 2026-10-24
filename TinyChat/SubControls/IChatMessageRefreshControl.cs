namespace TinyChat;

/// <summary>
/// Allows an existing chat message control to refresh its display without being recreated.
/// </summary>
public interface IChatMessageRefreshControl
{
	/// <summary>
	/// Refreshes the visible sender and message content from the current message.
	/// </summary>
	void RefreshMessageDisplay();
}
