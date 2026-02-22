using System.ComponentModel;
using System.Text;

namespace TinyChat.Messages;

/// <summary>
/// Represents text-based message content for the reasoning.
/// </summary>
public class ReasoningMessageContent : IChatMessageContent
{
	private readonly StringBuilder _value;

	/// <summary>
	/// Occurs then the value of the message content changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="StringMessageContent"/> class.
	/// </summary>
	/// <param name="value">The string value of the message content.</param>
	public ReasoningMessageContent(string? value)
	{
		_value = new StringBuilder(value);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
	}

	/// <inheritdoc />
	public object? Content => ToString();

	/// <inheritdoc />
	public override string ToString() => _value?.ToString() ?? string.Empty;

	/// <summary>
	/// Append the text to the content
	/// </summary>
	/// <param name="text"></param>
	public void AppendText(string text)
	{
		_value.Append(text);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
	}
}
