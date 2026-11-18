namespace TinyChat;

/// <summary>
/// Opts a chat input control into the menu button shown in the lower left corner of the editor.
/// </summary>
public interface IChatInputMenuControl
{
	/// <summary>
	/// Occurs when the user clicks the menu button.
	/// </summary>
	event EventHandler? MenuRequested;

	/// <summary>
	/// Gets or sets whether the menu button is visible.
	/// </summary>
	bool ShowMenuButton { get; set; }

	/// <summary>
	/// Gets or sets the tooltip and accessible name of the menu button.
	/// </summary>
	string MenuButtonText { get; set; }

	/// <summary>
	/// Shows the menu with the specified entries next to the menu button.
	/// </summary>
	/// <param name="items">The entries to show.</param>
	void ShowMenu(IReadOnlyList<ChatInputMenuItem> items);
}
