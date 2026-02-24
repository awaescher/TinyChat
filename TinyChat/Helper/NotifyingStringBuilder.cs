using System.ComponentModel;
using System.Text;

namespace TinyChat;

/// <summary>
/// A string builder decorator that notifies with INotifyPropertyChanged when its content changes.
/// </summary>
public class NotifyingStringBuilder : INotifyPropertyChanged
{
	private readonly StringBuilder _inner = new();

	/// <summary>
	/// Occurs when the string builder content changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Gets the length of the current StringBuilder.
	/// </summary>
	public int Length => _inner.Length;

	/// <summary>
	/// Gets or sets the capacity of the StringBuilder.
	/// </summary>
	public int Capacity
	{
		get => _inner.Capacity;
		set => _inner.Capacity = value;
	}

	/// <summary>
	/// Gets or sets the character at the specified index.
	/// </summary>
	public char this[int index]
	{
		get => _inner[index];
		set
		{
			_inner[index] = value;
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// Appends a string to the StringBuilder.
	/// </summary>
	public NotifyingStringBuilder Append(string? value)
	{
		_inner.Append(value);
		OnPropertyChanged();
		return this;
	}

	/// <summary>
	/// Appends a character to the StringBuilder.
	/// </summary>
	public NotifyingStringBuilder Append(char value)
	{
		_inner.Append(value);
		OnPropertyChanged();
		return this;
	}

	/// <summary>
	/// Appends an object's string representation to the StringBuilder.
	/// </summary>
	public NotifyingStringBuilder Append(object? value)
	{
		_inner.Append(value);
		OnPropertyChanged();
		return this;
	}

	/// <summary>
	/// Appends a line to the StringBuilder.
	/// </summary>
	public NotifyingStringBuilder AppendLine()
	{
		_inner.AppendLine();
		OnPropertyChanged();
		return this;
	}

	/// <summary>
	/// Appends a line with the specified string to the StringBuilder.
	/// </summary>
	public NotifyingStringBuilder AppendLine(string? value)
	{
		_inner.AppendLine(value);
		OnPropertyChanged();
		return this;
	}

	/// <summary>
	/// Inserts a string at the specified index.
	/// </summary>
	public NotifyingStringBuilder Insert(int index, string? value)
	{
		_inner.Insert(index, value);
		OnPropertyChanged();
		return this;
	}

	/// <summary>
	/// Removes characters from the StringBuilder.
	/// </summary>
	public NotifyingStringBuilder Remove(int startIndex, int length)
	{
		_inner.Remove(startIndex, length);
		OnPropertyChanged();
		return this;
	}

	/// <summary>
	/// Clears the StringBuilder.
	/// </summary>
	public NotifyingStringBuilder Clear()
	{
		_inner.Clear();
		OnPropertyChanged();
		return this;
	}

	/// <summary>
	/// Returns the string representation of the StringBuilder.
	/// </summary>
	public override string ToString() => _inner.ToString();

	/// <summary>
	/// Raises the PropertyChanged event.
	/// </summary>
	protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}