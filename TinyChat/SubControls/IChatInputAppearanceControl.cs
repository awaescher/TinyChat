namespace TinyChat;

/// <summary>
/// Allows a chat input implementation to receive editor-specific appearance settings
/// without forcing the surrounding chat surface colours onto the editable text area.
/// </summary>
public interface IChatInputAppearanceControl
{
	/// <summary>
	/// Gets or sets the editor background colour.
	/// <see cref="Color.Empty"/> restores the platform or skin default.
	/// </summary>
	Color EditorBackColor { get; set; }

	/// <summary>
	/// Gets or sets the editor foreground colour.
	/// <see cref="Color.Empty"/> restores the platform or skin default.
	/// </summary>
	Color EditorForeColor { get; set; }

	/// <summary>
	/// Gets or sets the logical editor border style.
	/// DevExpress input controls map this value to the corresponding skin-aware border.
	/// </summary>
	BorderStyle EditorBorderStyle { get; set; }

	/// <summary>
	/// Gets or sets the placeholder displayed while the editor is empty.
	/// </summary>
	string PlaceholderText { get; set; }
}
