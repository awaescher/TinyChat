using System.ComponentModel;
using System.Text;

namespace TinyChat.Messages;

/// <summary>
/// Represents text-based message content for the reasoning.
/// </summary>
public class ReasoningMessageContent : IChatMessageContent
{
	private StringBuilder? _builder;
	private string? _message;

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
		_builder = new StringBuilder(value);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
		IsThinking = true;
	}

	/// <summary>
	/// Gets or sets whether the reasoning is still ongoing.
	/// </summary>
	public bool IsThinking { get; private set; }

	/// <inheritdoc />
	public object? Content => ToString();

	/// <inheritdoc />
	public override string ToString() => _builder?.ToString() ?? (_message ?? string.Empty);

	/// <summary>
	/// Append the text to the content
	/// </summary>
	/// <param name="text"></param>
	public void AppendText(string text)
	{
		_builder?.Append(text);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
	}

	/// <summary>
	/// Sets the reasoning for completed
	/// </summary>
	public void SetDone()
	{
		// Convert the string builder to a fixed string
		_message = _builder?.ToString();
		_builder = null;

		IsThinking = false;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsThinking)));
	}
}
