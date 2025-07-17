namespace TinyChat.Messages;

/// <summary>
/// Represents the content of a chat message.
/// </summary>
public interface IChatMessageContent
{
	/// <summary>
	/// Renders the content as a string for display.
	/// </summary>
	/// <returns>The rendered content as a string, or null if no content.</returns>
	string? Render();
}
