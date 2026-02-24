namespace TinyChat.Messages.Formatting;

/// <summary>
/// Interface to format chat message content into a displayable string format.
/// </summary>
public interface IMessageFormatter
{
	/// <summary>
	/// Renders a given chat message content to a display string
	/// </summary>
	/// <param name="content">The chat messag content to format</param>
	/// <returns></returns>
	string Format(IChatMessageContent content);

	/// <summary>
	/// Renders a given string content to a display string
	/// </summary>
	/// <param name="content">The string content to format</param>
	/// <returns></returns>
	string Format(string content);
}