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
	/// Gets the application-provided bitmap or other <see cref="System.Drawing.Image"/>.
	/// DevExpress gives <see cref="SvgIcon"/> priority when both are set.
	/// <see href="https://learn.microsoft.com/windows/apps/design/iconography/segoe-fluent-icons-font">Microsoft catalogue</see>
	/// </summary>
	public Image? Image { get; init; }

	/// <summary>
	/// Gets the self-contained SVG markup for DevExpress, where it takes precedence over
	/// <see cref="Image"/> and <see cref="SegoeFluentIcon"/>. WinForms uses those properties as fallbacks.
	/// </summary>
	public string? SvgIcon { get; init; }

	/// <summary>
	/// Gets a Unicode glyph from the Segoe Fluent Icons font, such as <see cref="SegoeFluentIcons.Bookmarks"/>.
	/// It is used when no <see cref="Image"/> or SVG supported by the control is set.
	/// </summary>
	public string? SegoeFluentIcon { get; init; }

	/// <summary>
	/// Gets the localized text used for the tooltip and the accessible name of the button.
	/// </summary>
	public string? ToolTipText { get; init; }
}
