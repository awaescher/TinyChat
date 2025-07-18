using System.Windows.Forms;
using DevExpress.XtraEditors;
using TinyChat;

namespace DevExpressDemo;

/// <summary>
/// A DevExpress-based split container control specifically designed for chat interfaces.
/// Provides a vertical layout with a fixed bottom panel for chat input and a resizable top panel for chat history.
/// </summary>
public class DXChatSplitContainerControl : SplitContainerControl, ISplitContainerControl
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DXChatSplitContainerControl"/> class.
	/// Configures the control for vertical splitting with a fixed bottom panel.
	/// </summary>
	public DXChatSplitContainerControl()
	{
		Horizontal = false;
		FixedPanel = SplitFixedPanel.Panel2;
	}

	/// <summary>
	/// Gets the panel that contains the chat history display.
	/// This is the top panel (Panel1) in the split container.
	/// </summary>
	public Control HistoryPanel => Panel1;

	/// <summary>
	/// Gets the panel that contains the chat input controls.
	/// This is the bottom panel (Panel2) in the split container and is fixed in size.
	/// </summary>
	public Control ChatInputPanel => Panel2;
}