using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// Represents a chat message control whose formatter can be changed after the control was created.
/// </summary>
public interface IChatMessageFormattingControl
{
	/// <summary>
	/// Gets or sets the formatter used to render the current message content.
	/// </summary>
	IMessageFormatter MessageFormatter { get; set; }
}
