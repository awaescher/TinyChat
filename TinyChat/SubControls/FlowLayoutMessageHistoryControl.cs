namespace TinyChat;

/// <summary>
/// A flow layout panel control that manages and displays chat message history with automatic scrolling and width management.
/// </summary>
public class FlowLayoutMessageHistoryControl : FlowLayoutPanel, IChatMessageHistoryControl, IChatMessageHistoryViewport
{
	private bool _shouldFollowStreamScroll = true;
	private bool _streamScrollPending;
	private IChatMessageControl? _selectedMessageControl;

	/// <inheritdoc />
	public event EventHandler? ViewportChanged;

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
		control.MouseDown += OnMessageControlMouseDown;
		control.VisibleChanged += MessageControlVisibleChanged;
		Controls.Add(control);
		control.MarginChanged += MessageControlMarginChanged;

		SetSizeConstraints(control);
		ScrollControlIntoView(control);

		messageControl.SizeUpdatedWhileStreaming += MessageControlStreamingSizeUpdate;
		messageControl.BeforeLayoutChange += BeforeMessageLayoutChange;
		messageControl.AfterLayoutChange += AfterMessageLayoutChange;
	}

	/// <summary>
	/// Clears all message controls from the chat history.
	/// </summary>
	public void ClearMessageControls()
	{
		_selectedMessageControl = null;

		var messageControls = Controls.OfType<IChatMessageControl>().ToArray();
		foreach (var messageControl in messageControls)
		{
			messageControl.SizeUpdatedWhileStreaming -= MessageControlStreamingSizeUpdate;
			messageControl.BeforeLayoutChange -= BeforeMessageLayoutChange;
			messageControl.AfterLayoutChange -= AfterMessageLayoutChange;

			var control = (Control)messageControl;
			control.MouseDown -= OnMessageControlMouseDown;
			control.VisibleChanged -= MessageControlVisibleChanged;
			control.MarginChanged -= MessageControlMarginChanged;
		}

		Controls.Clear();
		foreach (var messageControl in messageControls)
		{
			messageControl.Message = null;
			((Control)messageControl).Dispose();
		}
	}

	/// <summary>
	/// Removes the message control associated with the specified chat message from the history.
	/// </summary>
	/// <param name="message">The chat message whose control should be removed.</param>
	public void RemoveMessageControl(IChatMessage message)
	{
		var messageControls = Controls.OfType<IChatMessageControl>();
		var messageControl = messageControls.FirstOrDefault(
			control => ReferenceEquals(control.Message, message)) ??
			messageControls.FirstOrDefault(control => control.Message?.Equals(message) ?? false);
		if (messageControl is not null)
		{
			var control = (Control)messageControl;
			messageControl.SizeUpdatedWhileStreaming -= MessageControlStreamingSizeUpdate;
			messageControl.BeforeLayoutChange -= BeforeMessageLayoutChange;
			messageControl.AfterLayoutChange -= AfterMessageLayoutChange;
			control.MouseDown -= OnMessageControlMouseDown;
			control.VisibleChanged -= MessageControlVisibleChanged;
			control.MarginChanged -= MessageControlMarginChanged;
			Controls.Remove(control);
			messageControl.Message = null;
			control.Dispose();

			if (ReferenceEquals(_selectedMessageControl, messageControl))
				_selectedMessageControl = null;
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
		ViewportChanged?.Invoke(this, EventArgs.Empty);

		SuspendLayout();

		foreach (Control control in Controls)
			SetSizeConstraints(control);

		ResumeLayout(performLayout: false);
		PerformLayout(); // to hide the H-scrollbar that pops up from time to time
	}

	/// <inheritdoc />
	protected override void OnPaddingChanged(EventArgs e)
	{
		base.OnPaddingChanged(e);
		ViewportChanged?.Invoke(this, EventArgs.Empty);

		SuspendLayout();
		foreach (Control control in Controls)
			SetSizeConstraints(control);
		ResumeLayout(performLayout: false);
		PerformLayout();
	}

	/// <summary>
	/// Sets the minimum and maximum width of a control to the available history width while
	/// accounting for the vertical scrollbar and control margins.
	/// </summary>
	/// <param name="control">The control whose width should be adjusted.</param>
	private void SetSizeConstraints(Control control)
	{
		var availableWidth = Math.Max(
			0,
			ClientRectangle.Width - Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - control.Margin.Horizontal);
		var constraint = new Size(availableWidth, 0);
		if (control.MinimumSize != constraint)
			control.MinimumSize = constraint;
		if (control.MaximumSize != constraint)
			control.MaximumSize = constraint;
	}

	private void MessageControlMarginChanged(object? sender, EventArgs e)
	{
		if (sender is not Control control || !Controls.Contains(control))
			return;

		SuspendLayout();
		SetSizeConstraints(control);
		ResumeLayout(performLayout: false);
		PerformLayout();
	}

	/// <inheritdoc />
	protected override void OnScroll(ScrollEventArgs se)
	{
		base.OnScroll(se);
		ViewportChanged?.Invoke(this, EventArgs.Empty);

		var didScrollUp = se.ScrollOrientation == ScrollOrientation.VerticalScroll && se.NewValue < se.OldValue;
		var didScrollToBottom = se.NewValue >= MaxVerticalScroll;

		_shouldFollowStreamScroll = !didScrollUp && didScrollToBottom;
	}

	/// <inheritdoc />
	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		ViewportChanged?.Invoke(this, EventArgs.Empty);

		var didScrollUp = e.Delta > 0;
		var didScrollToBottom = VerticalScroll.Value >= MaxVerticalScroll;

		_shouldFollowStreamScroll = !didScrollUp && didScrollToBottom;
	}

	/// <inheritdoc />
	protected override void OnHandleDestroyed(EventArgs e)
	{
		_streamScrollPending = false;
		base.OnHandleDestroyed(e);
	}

	/// <inheritdoc/>
	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == (Keys.Control | Keys.C) && _selectedMessageControl is not null)
		{
			try
			{
				var text = _selectedMessageControl.ToString();
				if (!string.IsNullOrEmpty(text))
					Clipboard.SetText(text);
			}
			catch { }
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	private void OnMessageControlMouseDown(object? sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			_selectedMessageControl = sender as IChatMessageControl;
			Focus(); // required to use ProcessCmdKey()
		}
	}

	private void MessageControlVisibleChanged(object? sender, EventArgs e)
	{
		if (sender is Control { Visible: false } control &&
			ReferenceEquals(_selectedMessageControl, control))
		{
			_selectedMessageControl = null;
		}
	}

	private void MessageControlStreamingSizeUpdate(object? sender, EventArgs args)
	{
		// can't use ScrollControlIntoView() because this will stop scrolling
		// once the message controls gets larger than the flow layout panel
		if (!_shouldFollowStreamScroll || _streamScrollPending || !IsHandleCreated)
			return;

		_streamScrollPending = true;
		try
		{
			BeginInvoke(ApplyPendingStreamScroll);
		}
		catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
		{
			_streamScrollPending = false;
		}
	}

	private void ApplyPendingStreamScroll()
	{
		_streamScrollPending = false;
		if (IsDisposed || Disposing || !_shouldFollowStreamScroll)
			return;

		var scrollValue = MaxVerticalScroll;
		if (VerticalScroll.Value != scrollValue)
			VerticalScroll.Value = scrollValue;
	}

	private void BeforeMessageLayoutChange(object? sender, EventArgs e)
	{
		SuspendLayout();
	}

	private void AfterMessageLayoutChange(object? sender, EventArgs e)
	{
		ResumeLayout();
	}
}
