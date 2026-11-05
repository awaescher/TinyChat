namespace TinyChat;

/// <summary>
/// Represents a message control that shortens long details, such as reasoning or tool results,
/// and offers the complete text in a dialog.
/// </summary>
public interface IChatMessageTruncationControl
{
	/// <summary>
	/// Gets or sets the maximum number of characters shown in the conversation, or zero for no limit.
	/// </summary>
	int MaximumDetailTextLength { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of lines shown in the conversation, or zero for no limit.
	/// </summary>
	int MaximumDetailTextLines { get; set; }
}

/// <summary>
/// Lets a message control take over the localized texts of the owning <see cref="ChatControl"/>.
/// </summary>
internal interface IChatPresentationControl
{
	void ApplyPresentation(ChatControl? owner);
}
