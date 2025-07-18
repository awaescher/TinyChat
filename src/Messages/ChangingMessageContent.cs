using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Represents text-based message content.
/// </summary>
public class ChangingMessageContent : IChatMessageContent
{
	private readonly INotifyPropertyChanged? _value;

	/// <summary>
	/// Occures when the value of the message content gets changed.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="StringMessageContent"/> class.
	/// </summary>
	/// <param name="value">The string value of the message content.</param>
	public ChangingMessageContent(INotifyPropertyChanged? value)
	{
		if (value == _value)
			return;

		if (_value is not null)
			_value.PropertyChanged -= NotifyChangedValue;

		_value = value;
		NotifyChangedValue(null, null);

		if (value is not null)
			value.PropertyChanged += NotifyChangedValue;
	}

	/// <summary>
	/// Renders the content as a string for display.
	/// </summary>
	/// <returns>The string value of the content.</returns>
	public string Render() => _value?.ToString() ?? string.Empty;

	/// <inheritdoc />
	public object? Content => _value;

	private void NotifyChangedValue(object? _, PropertyChangedEventArgs? __)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
	}
}
