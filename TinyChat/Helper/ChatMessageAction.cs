namespace TinyChat;

/// <summary>
/// Identifies the built-in actions offered for a chat message.
/// </summary>
public enum ChatMessageActionKind
{
	/// <summary>A custom action provided by the application.</summary>
	Custom,

	/// <summary>Copies the message as plain text.</summary>
	Copy,

	/// <summary>Copies the message including its formatting.</summary>
	CopyFormatted,

	/// <summary>Reverts the conversation to the message.</summary>
	Revert
}

/// <summary>
/// Describes an icon button of the action bar that appears when the pointer rests over a chat message.
/// </summary>
/// <param name="Kind">The kind of action, which also selects the built-in icon.</param>
/// <param name="Execute">The operation that runs when the button is clicked.</param>
public sealed record ChatMessageAction(ChatMessageActionKind Kind, Action Execute)
{
	/// <summary>
	/// Gets or sets the image shown for a <see cref="ChatMessageActionKind.Custom"/> action.
	/// </summary>
	public Image? Image { get; init; }
}
