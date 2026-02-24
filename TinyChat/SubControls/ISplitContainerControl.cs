namespace TinyChat;

/// <summary>
/// Defines the contract for a split container control that manages chat history and input panels.
/// </summary>
public interface ISplitContainerControl
{
	/// <summary>
	/// Gets the control that displays the chat history.
	/// </summary>
	/// <value>
	/// The control containing the chat history display, or <see langword="null"/> if not available.
	/// </value>
	Control? HistoryPanel { get; }

	/// <summary>
	/// Gets the control that contains the chat input interface.
	/// </summary>
	/// <value>
	/// The control containing the chat input interface, or <see langword="null"/> if not available.
	/// </value>
	Control? ChatInputPanel { get; }

	/// <summary>
	/// Gets or sets the position of the splitter between the history and input panels.
	/// </summary>
	/// <value>
	/// The position of the splitter in pixels from the top or left edge, depending on the split orientation.
	/// </value>
	int SplitterPosition { get; set; }
}