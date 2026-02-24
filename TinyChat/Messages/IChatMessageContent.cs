using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Represents the content of a chat message.
/// </summary>
public interface IChatMessageContent : INotifyPropertyChanged
{
	/// <summary>
	/// Gets the content of the message.
	/// </summary>
	object? Content { get; }
}
