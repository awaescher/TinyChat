namespace TinyChat;

/// <summary>
/// Identifies the built-in entries of the input menu.
/// </summary>
public enum ChatInputMenuItemKind
{
	/// <summary>A custom entry provided by the application.</summary>
	Custom,

	/// <summary>Opens a file dialog and adds the selected files to the draft.</summary>
	AttachFiles
}

/// <summary>
/// Describes an entry of the menu that opens from the button in the lower left corner of the chat input.
/// </summary>
/// <param name="Kind">The kind of entry, which also selects the built-in icon.</param>
/// <param name="Text">The localized text of the entry.</param>
/// <param name="Execute">The operation that runs when the entry is clicked.</param>
public sealed record ChatInputMenuItem(ChatInputMenuItemKind Kind, string Text, Action Execute)
{
	/// <summary>
	/// Gets the image shown for the entry. Built-in entries use their default icon when no image is set.
	/// </summary>
	public Image? Image { get; init; }

	/// <summary>
	/// Gets whether the entry can be clicked.
	/// </summary>
	public bool Enabled { get; init; } = true;

	/// <summary>
	/// Gets whether a separator is shown above the entry.
	/// </summary>
	public bool BeginGroup { get; init; }
}
