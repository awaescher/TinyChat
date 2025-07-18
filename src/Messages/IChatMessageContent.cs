using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Represents the content of a chat message.
/// </summary>
public interface IChatMessageContent : INotifyPropertyChanged
{
	/// <summary>
	/// Renders the content as a string for display.
	/// </summary>
	/// <returns>The rendered content as a string, or null if no content.</returns>
	string? Render();

	/// <summary>
	/// Gets the content of the message.
	/// </summary>
	object? Content { get; }
}
