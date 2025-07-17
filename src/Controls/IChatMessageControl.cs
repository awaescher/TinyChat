using TinyChat.Messages;

namespace TinyChat.Controls;

/// <summary>
/// Represents a control that can display a chat message.
/// </summary>
public interface IChatMessageControl
{
	/// <summary>
	/// Gets or sets the chat message displayed by this control.
	/// </summary>
	IChatMessage Message { get; set; }
}
