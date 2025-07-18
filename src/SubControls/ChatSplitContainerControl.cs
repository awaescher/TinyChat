using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// A specialized split container control designed for chat applications with a horizontal layout.
/// The top panel is typically used for chat history and the bottom panel for input controls.
/// </summary>
public class ChatSplitContainerControl : SplitContainer, ISplitContainerControl
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSplitContainerControl"/> class.
	/// Sets up horizontal orientation with the bottom panel (Panel2) as the fixed panel.
	/// </summary>
	public ChatSplitContainerControl()
	{
		Orientation = Orientation.Horizontal;
		FixedPanel = FixedPanel.Panel2;
	}

	/// <summary>
	/// Gets the top panel of the split container, typically used for displaying chat history.
	/// </summary>
	public Control? HistoryPanel => Panel1;

	/// <summary>
	/// Gets the bottom panel of the split container, typically used for chat input controls.
	/// </summary>
	public Control? ChatInputPanel => Panel2;

	/// <summary>
	/// Gets or sets the splitter position measured from the bottom of the container.
	/// This property provides an alternative to <see cref="SplitContainer.SplitterDistance"/> 
	/// by measuring from the bottom instead of the top, making it easier to work with 
	/// fixed bottom panels.
	/// </summary>
	/// <value>The distance in pixels from the bottom of the container to the splitter.</value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public int SplitterPosition
	{
		get => Height - SplitterDistance;
		set => SplitterDistance = Height - value;
	}
}