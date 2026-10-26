namespace TinyChat;

/// <summary>
/// Opts a chat input control into the layout settings exposed by <see cref="ChatControl"/>.
/// </summary>
public interface IChatInputLayoutControl
{
	/// <summary>
	/// Gets or sets the padding between the input surface and its editor content.
	/// </summary>
	Padding ContentPadding { get; set; }
}
