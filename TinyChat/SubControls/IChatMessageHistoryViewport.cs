namespace TinyChat;

/// <summary>
/// Provides notifications when the visible area of a message history changes.
/// </summary>
public interface IChatMessageHistoryViewport
{
	/// <summary>
	/// Occurs when scrolling or resizing changes the visible message area.
	/// </summary>
	event EventHandler? ViewportChanged;
}
