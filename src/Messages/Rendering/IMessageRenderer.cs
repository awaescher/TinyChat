namespace TinyChat.Messages.Rendering;

/// <summary>
/// Interface to render chat message content into a displayable string format.
/// </summary>
public interface IMessageRenderer
{
	/// <summary>
	/// Renders a given chat message content to a display string
	/// </summary>
	/// <param name="content">The chat messag content to render</param>
	/// <returns></returns>
	string Render(IChatMessageContent content);

	/// <summary>
	/// Renders a given string content to a display string
	/// </summary>
	/// <param name="content">The string content to render</param>
	/// <returns></returns>
	string Render(string content);
}