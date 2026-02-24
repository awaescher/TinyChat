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

	/// <inheritdoc />
	public object? Content => _value;

	/// <inheritdoc />
	public override string ToString() => _value?.ToString() ?? string.Empty;

	private void NotifyChangedValue(object? _, PropertyChangedEventArgs? __)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
	}
}
