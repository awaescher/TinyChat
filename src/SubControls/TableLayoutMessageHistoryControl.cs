namespace TinyChat;

/// <summary>
/// A flow layout panel control that manages and displays chat message history with automatic scrolling and width management.
/// </summary>
public class FlowLayoutMessageHistoryControl : FlowLayoutPanel, IChatMessageHistoryControl
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FlowLayoutMessageHistoryControl"/> class
	/// with top-down flow direction, auto-scroll enabled, and content wrapping disabled.
	/// </summary>
	public FlowLayoutMessageHistoryControl()
	{
		FlowDirection = FlowDirection.TopDown;
		AutoScroll = true;
		WrapContents = false;
	}

	/// <summary>
	/// Appends a chat message control to the history and automatically scrolls to show the new message.
	/// </summary>
	/// <param name="messageControl">The chat message control to append to the history.</param>
	public void AppendMessageControl(IChatMessageControl messageControl)
	{
		var control = (Control)messageControl;
		Controls.Add(control);
		SetMaxWidthToPreventHorizontalScrollbar(control);
		ScrollControlIntoView(control);
	}

	/// <summary>
	/// Clears all message controls from the chat history.
	/// </summary>
	public void ClearMessageControls()
	{
		Controls.Clear();
	}

	/// <summary>
	/// Removes the message control associated with the specified chat message from the history.
	/// </summary>
	/// <param name="message">The chat message whose control should be removed.</param>
	public void RemoveMessageControl(IChatMessage message)
	{
		if (Controls.OfType<IChatMessageControl>().FirstOrDefault(mc => mc.Message?.Equals(message) ?? false) is Control control)
			Controls.Remove(control);
	}

	/// <summary>
	/// Handles the client size changed event by updating the maximum width of all child controls
	/// to prevent horizontal scrollbars from appearing.
	/// </summary>
	/// <param name="e">The event arguments containing information about the size change.</param>
	protected override void OnClientSizeChanged(EventArgs e)
	{
		base.OnClientSizeChanged(e);

		SuspendLayout();

		foreach (Control control in Controls)
			SetMaxWidthToPreventHorizontalScrollbar(control);

		ResumeLayout();
		PerformLayout();
	}

	/// <summary>
	/// Sets the maximum width of a control to prevent horizontal scrollbars by accounting for
	/// the vertical scrollbar width when present.
	/// </summary>
	/// <param name="control">The control whose maximum width should be adjusted.</param>
	private void SetMaxWidthToPreventHorizontalScrollbar(Control control)
	{
		control.MaximumSize = new Size(ClientRectangle.Width - SystemInformation.VerticalScrollBarWidth, 0);
	}
}