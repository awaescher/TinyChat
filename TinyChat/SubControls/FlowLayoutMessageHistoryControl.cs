namespace TinyChat;

/// <summary>
/// A flow layout panel control that manages and displays chat message history with automatic scrolling and width management.
/// </summary>
public class FlowLayoutMessageHistoryControl : FlowLayoutPanel, IChatMessageHistoryControl
{
	private bool _shouldFollowStreamScroll = true;

	/// <summary>
	/// Gets the maximum vertical scroll value that indicates the bottom of the scrollable area.
	/// </summary>
	private int MaxVerticalScroll => VerticalScroll.Maximum - VerticalScroll.LargeChange;

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

		messageControl.SizeUpdatedWhileStreaming += MessageControlStreamingSizeUpdate;
	}

	/// <summary>
	/// Clears all message controls from the chat history.
	/// </summary>
	public void ClearMessageControls()
	{
		foreach (var messageControl in Controls.OfType<IChatMessageControl>())
			messageControl.SizeUpdatedWhileStreaming -= MessageControlStreamingSizeUpdate;

		Controls.Clear();
	}

	/// <summary>
	/// Removes the message control associated with the specified chat message from the history.
	/// </summary>
	/// <param name="message">The chat message whose control should be removed.</param>
	public void RemoveMessageControl(IChatMessage message)
	{
		if (Controls.OfType<IChatMessageControl>().FirstOrDefault(mc => mc.Message?.Equals(message) ?? false) is { } messageControl)
		{
			messageControl.SizeUpdatedWhileStreaming -= MessageControlStreamingSizeUpdate;
			Controls.Remove((Control)messageControl);
		}
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
		PerformLayout(); // to hide the H-scrollbar that pops up from time to time
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


	/// <inheritdoc />
	protected override void OnScroll(ScrollEventArgs se)
	{
		base.OnScroll(se);

		var didScrollUp = se.ScrollOrientation == ScrollOrientation.VerticalScroll && se.NewValue < se.OldValue;
		var didScrollToBottom = se.NewValue >= MaxVerticalScroll;

		_shouldFollowStreamScroll = !didScrollUp && didScrollToBottom;
	}

	/// <inheritdoc />
	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);

		var didScrollUp = e.Delta > 0;
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