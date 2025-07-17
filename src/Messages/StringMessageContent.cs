namespace TinyChat.Messages;

/// <summary>
/// Represents text-based message content.
/// </summary>
public class StringMessageContent : IChatMessageContent
{
	/// <summary>
	/// Gets the string value of the message content.
	/// </summary>
	public string? Value { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="StringMessageContent"/> class.
	/// </summary>
	/// <param name="value">The string value of the message content.</param>
	public StringMessageContent(string? value)
	{
		Value = value;
	}

	/// <summary>
	/// Renders the content as a string for display.
	/// </summary>
	/// <returns>The string value of the content.</returns>
	public string? Render() => Value;
}
