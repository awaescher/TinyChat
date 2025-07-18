using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Utils.Layout;
using DevExpress.XtraEditors;
using TinyChat;

namespace DevExpressDemo;

/// <summary>
/// A scrollable control that displays chat message history using a vertical stack panel layout.
/// Implements the IChatMessageHistoryControl interface to provide message management functionality.
/// </summary>
public class StackPanelMessageHistoryControl : XtraScrollableControl, IChatMessageHistoryControl
{
	/// <summary>
	/// The internal stack panel that contains and arranges the message controls vertically.
	/// </summary>
	private readonly StackPanel _stackPanel = new();

	/// <summary>
	/// Initializes a new instance of the StackPanelMessageHistoryControl class.
	/// Sets up the stack panel with top-down layout direction and enables auto-scrolling.
	/// </summary>
	public StackPanelMessageHistoryControl()
	{
		AutoScroll = true;

		_stackPanel.LayoutDirection = StackPanelLayoutDirection.TopDown;
		_stackPanel.AutoSize = true;
		_stackPanel.Visible = true;
		_stackPanel.Dock = DockStyle.Top;
		Controls.Add(_stackPanel);
	}

	/// <summary>
	/// Appends a new message control to the bottom of the message history.
	/// The control is automatically sized to fit the client width and scrolled into view.
	/// </summary>
	/// <param name="messageControl">The chat message control to add to the history.</param>
	public void AppendMessage(IChatMessageControl messageControl)
	{
		var control = (Control)messageControl;
		_stackPanel.Controls.Add(control);
		SetSizeContraints(control);
		ScrollControlIntoView(control);
	}

	/// <summary>
	/// Removes all message controls from the history display.
	/// </summary>
	public void ClearMessageControls()
	{
		_stackPanel.Controls.Clear();
	}

	/// <summary>
	/// Handles the client size changed event by updating size constraints for all message controls
	/// to ensure they properly fit the new client width.
	/// </summary>
	/// <param name="e">Event arguments containing information about the size change.</param>
	protected override void OnClientSizeChanged(EventArgs e)
	{
		base.OnClientSizeChanged(e);

		SuspendLayout();

		foreach (Control control in _stackPanel.Controls)
			SetSizeContraints(control);

		ResumeLayout();
	}

	/// <summary>
	/// Sets the minimum and maximum size constraints for a control to match the client width.
	/// This ensures message controls span the full width of the container.
	/// </summary>
	/// <param name="control">The control to apply size constraints to.</param>
	private void SetSizeContraints(Control control)
	{
		control.MinimumSize = new Size(ClientRectangle.Width, 0);
		control.MaximumSize = new Size(ClientRectangle.Width, 0);
	}
}
