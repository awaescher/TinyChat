using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Represents text-based message content.
/// </summary>
public class StringMessageContent : IChatMessageContent
{
	private readonly string? _value;

	/// <summary>
	/// Occurs then the value of the message content changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;


	/// <summary>
	/// Initializes a new instance of the <see cref="StringMessageContent"/> class.
	/// </summary>
	/// <param name="value">The string value of the message content.</param>
	public StringMessageContent(string? value)
	{
		if (string.Equals(_value, value))
			return;

		_value = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_value)));
	}

	/// <summary>
	/// Renders the content as a string for display.
	/// </summary>
	/// <returns>The string value of the content.</returns>
	public string? Render() => _value;

	/// <inheritdoc />
	public object? Content => _value;
}
