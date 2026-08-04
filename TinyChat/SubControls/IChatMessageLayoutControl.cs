namespace TinyChat;

/// <summary>
/// Opts a chat message control into the message padding and margin settings exposed by
/// <see cref="ChatControl"/>.
/// </summary>
public interface IChatMessageLayoutControl
{
	/// <summary>
	/// Gets or sets the padding around the visible message content.
	/// </summary>
	Padding ContentPadding { get; set; }
}
