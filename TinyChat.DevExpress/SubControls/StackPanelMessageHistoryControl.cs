using DevExpress.Utils.Layout;
using DevExpress.XtraEditors;

namespace TinyChat;


/// <summary>
/// A scrollable control that displays chat message history using a vertical stack panel layout.
/// Implements the IChatMessageHistoryControl interface to provide message management functionality.
/// </summary>
public class StackPanelMessageHistoryControl : XtraScrollableControl, IChatMessageHistoryControl, IChatMessageHistoryViewport
{
	private readonly StackPanel _stackPanel = new();
	private readonly List<Control> _pendingAppendedControls = [];
	private bool _appendLayoutPending;
	private int _messageLayoutSuspendCount;
	private bool _resizeLayoutPending;
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
		control.MouseDown += OnMessageControlMouseDown;
		control.MarginChanged += MessageControlMarginChanged;
		control.VisibleChanged += MessageControlVisibleChanged;
		_stackPanel.Controls.Add(control);
		SetSizeConstraints(control);
		messageControl.SizeUpdatedWhileStreaming += MessageControlStreamingSizeUpdate;
		messageControl.BeforeLayoutChange += BeforeMessageLayoutChange;
		messageControl.AfterLayoutChange += AfterMessageLayoutChange;

		// Size the controls once the history decided whether it needs a vertical scrollbar; otherwise the
		// labels would be measured too wide. Controls appended together are sized and scrolled in one pass.
		_pendingAppendedControls.Add(control);
		if (_appendLayoutPending)
			return;

		_appendLayoutPending = true;
		if (!IsHandleCreated)
			return;

		try
		{
			BeginInvoke(ApplyPendingAppendLayout);
		}
		catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
		{
			_appendLayoutPending = false;
		}
	}

	private void ApplyPendingAppendLayout()
	{
		_appendLayoutPending = false;
		var controls = _pendingAppendedControls.Where(control => !control.IsDisposed && ReferenceEquals(control.Parent, _stackPanel)).ToArray();
		_pendingAppendedControls.Clear();
		if (IsDisposed || Disposing || controls.Length == 0)
			return;

		SuspendLayout();
		_stackPanel.SuspendLayout();
		try
		{
			foreach (var control in controls)
				SetSizeConstraints(control);
		}
		finally
		{
			_stackPanel.ResumeLayout(performLayout: true);
			ResumeLayout(performLayout: true);
		}

		if (_shouldFollowStreamScroll)
			ScrollControlIntoView(controls[^1]);
	}

	/// <inheritdoc />
	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		if (_appendLayoutPending)
			BeginInvoke(ApplyPendingAppendLayout);
	}

	/// <summary>
	/// Removes all message controls from the history display.
	/// </summary>
	public void ClearMessageControls()
	{
		var messageControls = _stackPanel.Controls.OfType<IChatMessageControl>().ToArray();
		foreach (var messageControl in messageControls)
		{
			messageControl.SizeUpdatedWhileStreaming -= MessageControlStreamingSizeUpdate;
			messageControl.BeforeLayoutChange -= BeforeMessageLayoutChange;
			messageControl.AfterLayoutChange -= AfterMessageLayoutChange;

			var control = (Control)messageControl;
			control.MouseDown -= OnMessageControlMouseDown;
			control.MarginChanged -= MessageControlMarginChanged;
			control.VisibleChanged -= MessageControlVisibleChanged;
		}

		_stackPanel.Controls.Clear();
		_pendingAppendedControls.Clear();
		_selectedMessageControl = null;
		foreach (var messageControl in messageControls)
		{
			messageControl.Message = null;
			((Control)messageControl).Dispose();
		}
	}

	/// <summary>
	/// Removes a message control by a given message
	/// </summary>
	/// <param name="message">The message to remove the control for</param>
	public void RemoveMessageControl(IChatMessage message)
	{
		var messageControls = _stackPanel.Controls.OfType<IChatMessageControl>();
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
			control.MarginChanged -= MessageControlMarginChanged;
			control.VisibleChanged -= MessageControlVisibleChanged;
			_stackPanel.Controls.Remove(control);
			messageControl.Message = null;
			control.Dispose();

			if (ReferenceEquals(_selectedMessageControl, messageControl))
				_selectedMessageControl = null;
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
		ViewportChanged?.Invoke(this, EventArgs.Empty);

		if (_stackPanel?.Controls.Count == 0 || _resizeLayoutPending || !IsHandleCreated)
			return;

		// Run after the control decided whether a vertical scrollbar is required.
		_resizeLayoutPending = true;
		try
		{
			BeginInvoke(ApplyPendingResizeLayout);
		}
		catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
		{
			_resizeLayoutPending = false;
		}
	}

	private void ApplyPendingResizeLayout()
	{
		_resizeLayoutPending = false;
		if (IsDisposed || Disposing)
			return;

		SuspendLayout();
		_stackPanel.SuspendLayout();
		try
		{
			foreach (Control control in _stackPanel.Controls)
				SetSizeConstraints(control);
		}
		finally
		{
			_stackPanel.ResumeLayout(performLayout: true);
			ResumeLayout(performLayout: true);
		}
	}

	/// <summary>
	/// Sets the minimum and maximum size constraints for a control to match the client width.
	/// This ensures message controls span the full width of the container.
	/// </summary>
	/// <param name="control">The control to apply size constraints to.</param>
	private void SetSizeConstraints(Control control)
	{
		var availableWidth = Math.Max(0, _stackPanel.ClientRectangle.Width - control.Margin.Horizontal);
		var constraint = new Size(availableWidth, 0);
		if (control.MinimumSize != constraint)
			control.MinimumSize = constraint;
		if (control.MaximumSize != constraint)
			control.MaximumSize = constraint;
	}


	/// <inheritdoc />
	protected override void OnScroll(object sender, XtraScrollEventArgs e)
	{
		base.OnScroll(sender, e);
		ViewportChanged?.Invoke(this, EventArgs.Empty);

		var didScrollUp = e.ScrollOrientation == DevExpress.XtraEditors.ScrollOrientation.VerticalScroll && e.NewValue < e.OldValue;
		var didScrollToBottom = e.NewValue >= MaxVerticalScroll;

		_shouldFollowStreamScroll = !didScrollUp && didScrollToBottom;
	}

	/// <inheritdoc />
	protected override void OnMouseWheelCore(MouseEventArgs ev)
	{
		base.OnMouseWheelCore(ev);
		ViewportChanged?.Invoke(this, EventArgs.Empty);

		var didScrollUp = ev.Delta > 0;
		var didScrollToBottom = VerticalScroll.Value >= MaxVerticalScroll;

		_shouldFollowStreamScroll = !didScrollUp && didScrollToBottom;
	}

	/// <inheritdoc />
	protected override void OnHandleDestroyed(EventArgs e)
	{
		_resizeLayoutPending = false;
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
				ClipboardText.SetText(text ?? string.Empty);
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

	private void MessageControlMarginChanged(object? sender, EventArgs e)
	{
		if (sender is not Control control || !ReferenceEquals(control.Parent, _stackPanel))
			return;

		SetSizeConstraints(control);
		_stackPanel.PerformLayout();
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

	/// <inheritdoc />
	public bool IsFollowingEnd => _shouldFollowStreamScroll;

	/// <inheritdoc />
	public void ScrollToEnd()
	{
		_shouldFollowStreamScroll = true;
		if (!IsHandleCreated)
			return;

		// The appended controls are sized after the history decided about its scrollbar, so scroll afterwards.
		try
		{
			BeginInvoke(() =>
			{
				if (IsDisposed || Disposing || !_shouldFollowStreamScroll)
					return;

				if (_appendLayoutPending)
					ApplyPendingAppendLayout();
				if (_stackPanel.Controls.Count > 0)
					ScrollControlIntoView(_stackPanel.Controls[_stackPanel.Controls.Count - 1]);
				if (VerticalScroll.Value != MaxVerticalScroll)
					VerticalScroll.Value = Math.Max(0, MaxVerticalScroll);
				ViewportChanged?.Invoke(this, EventArgs.Empty);
			});
		}
		catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
		{
		}
	}

	private void BeforeMessageLayoutChange(object? sender, EventArgs e)
	{
		_messageLayoutSuspendCount++;
		if (_messageLayoutSuspendCount > 1)
			return;

		SuspendLayout();
		_stackPanel.SuspendLayout();
	}

	private void AfterMessageLayoutChange(object? sender, EventArgs e)
	{
		if (_messageLayoutSuspendCount == 0)
			return;

		_messageLayoutSuspendCount--;
		if (_messageLayoutSuspendCount > 0)
			return;

		_stackPanel.ResumeLayout(performLayout: true);
		ResumeLayout(performLayout: true);
	}
}
