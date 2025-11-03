using System;
using System.Drawing;
using System.Linq;
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
	private readonly StackPanel _stackPanel = new();
	private bool _shouldFollowStreamScroll = true;

	/// <summary>
	/// Gets the maximum vertical scroll value that indicates the bottom of the scrollable area.
	/// </summary>
	private int MaxVerticalScroll => VerticalScroll.Maximum - VerticalScroll.LargeChange;

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
	public void AppendMessageControl(IChatMessageControl messageControl)
	{
		var control = (Control)messageControl;
		_stackPanel.Controls.Add(control);

		// queue this to the UI thread to ensure it runs after the control is added
		// otherwise the sizing of the labels will be incorrect if the scrollbar
		// is not visible
		this.BeginInvoke(() =>
		{
			SetSizeConstraints(control);
			ScrollControlIntoView(control);
			messageControl.SizeUpdatedWhileStreaming += MessageControlStreamingSizeUpdate;
		});
	}

	/// <summary>
	/// Removes all message controls from the history display.
	/// </summary>
	public void ClearMessageControls()
	{
		foreach (var messageControl in _stackPanel.Controls.OfType<IChatMessageControl>())
			messageControl.SizeUpdatedWhileStreaming -= MessageControlStreamingSizeUpdate;

		_stackPanel.Controls.Clear();
	}

	/// <summary>
	/// Removes a message control by a given message
	/// </summary>
	/// <param name="message">The message to remove the control for</param>
	public void RemoveMessageControl(IChatMessage message)
	{
		if (_stackPanel.Controls.OfType<IChatMessageControl>().FirstOrDefault(mc => mc.Message?.Equals(message) ?? false) is { } messageControl)
		{
			messageControl.SizeUpdatedWhileStreaming -= MessageControlStreamingSizeUpdate;
			_stackPanel.Controls.Remove((Control)messageControl);
		}
	}

	/// <summary>
	/// Handles the client size changed event by updating size constraints for all message controls
	/// to ensure they properly fit the new client width.
	/// </summary>
	/// <param name="e">Event arguments containing information about the size change.</param>
	protected override void OnClientSizeChanged(EventArgs e)
	{
		base.OnClientSizeChanged(e);

		if (_stackPanel?.Controls.Count > 0)
		{
			// needs to be done after the control decided whether or not
			// a V scrollbar should be shown
			this.BeginInvoke(() =>
			{
				SuspendLayout();

				foreach (Control control in _stackPanel.Controls)
					SetSizeConstraints(control);

				ResumeLayout();
			});
		}
	}

	/// <summary>
	/// Sets the minimum and maximum size constraints for a control to match the client width.
	/// This ensures message controls span the full width of the container.
	/// </summary>
	/// <param name="control">The control to apply size constraints to.</param>
	private void SetSizeConstraints(Control control)
	{
		control.MinimumSize = new Size(_stackPanel.ClientRectangle.Width, 0);
		control.MaximumSize = new Size(_stackPanel.ClientRectangle.Width, 0);
	}


	/// <inheritdoc />
	protected override void OnScroll(object sender, XtraScrollEventArgs e)
	{
		base.OnScroll(sender, e);

		var didScrollUp = e.ScrollOrientation == DevExpress.XtraEditors.ScrollOrientation.VerticalScroll && e.NewValue < e.OldValue;
		var didScrollToBottom = e.NewValue >= MaxVerticalScroll;

		_shouldFollowStreamScroll = !didScrollUp && didScrollToBottom;
	}

	/// <inheritdoc />
	protected override void OnMouseWheelCore(MouseEventArgs ev)
	{
		base.OnMouseWheelCore(ev);

		var didScrollUp = ev.Delta > 0;
		var didScrollToBottom = VerticalScroll.Value >= MaxVerticalScroll;

		_shouldFollowStreamScroll = !didScrollUp && didScrollToBottom;
	}

	private void MessageControlStreamingSizeUpdate(object? sender, EventArgs args)
	{
		// can't use ScrollControlIntoView() because this will stop scrolling
		// once the message controls gets larger than the flow layout panel
		if (_shouldFollowStreamScroll)
			BeginInvoke(() => VerticalScroll.Value = MaxVerticalScroll);
	}
}
