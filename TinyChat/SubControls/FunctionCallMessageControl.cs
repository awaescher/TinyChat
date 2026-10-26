using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Displays a function call and its result in a structured two-column layout.
/// The left column shows icon glyphs (see <see cref="ChatIcons"/>); the right column shows
/// the function name (bold), the arguments, and the result.
/// Clicking the header or icon areas toggles the detail rows (arguments + result).
/// The detail text controls remain dedicated to text selection.
/// </summary>
internal sealed partial class FunctionCallMessageControl : Panel, IChatMessageLayoutControl, IChatMessageRefreshControl, IFunctionCallMessageControl
{
	/// <summary>Fixed pixel width reserved for the icon column.</summary>
	private const int ICON_WIDTH = 20;

	/// <summary>The chat message whose <see cref="FunctionCallMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;
	private Font? _senderFont;
	private Font? _iconFont;
	private int _iconColumnWidth = ICON_WIDTH;

	/// <summary>
	/// Whether the detail panel (arguments + result) is currently visible.
	/// Starts collapsed.
	/// </summary>
	private bool _expanded;
	private bool _allowExpand;

	/// <inheritdoc/>
	/// <remarks>Tool call messages are never streamed, so this event is intentionally a no-op.</remarks>
	public event EventHandler? SizeUpdatedWhileStreaming { add { } remove { } }

	/// <inheritdoc/>
	public event EventHandler? BeforeLayoutChange;

	/// <inheritdoc/>
	public event EventHandler? AfterLayoutChange;

	/// <inheritdoc/>
	/// <remarks>Tool call messages are never streamed, so this method is intentionally a no-op.</remarks>
	void IChatMessageControl.SetIsReceivingStream(bool isReceiving) { }

	/// <inheritdoc/>
	void IChatMessageControl.ShowSenderHeader(bool show)
	{
		lblSender.Visible = show;
	}

	/// <summary>
	/// Initialises a new instance of <see cref="FunctionCallMessageControl"/>.
	/// </summary>
	public FunctionCallMessageControl()
	{
		InitializeComponent();

		lblArgs.ContentHeightChanging += SelectableText_ContentHeightChanging;
		lblArgs.ContentHeightChanged += SelectableText_ContentHeightChanged;
		lblResult.ContentHeightChanging += SelectableText_ContentHeightChanging;
		lblResult.ContentHeightChanged += SelectableText_ContentHeightChanged;
		ApplyTextFont();
		ApplyColors();
		Disposed += (_, _) =>
		{
			_senderFont?.Dispose();
			_senderFont = null;
			_iconFont?.Dispose();
			_iconFont = null;
		};

		WireMouseDown(tableLayout, lblIcon, lblTitle, lblResultIcon);
		UpdateExpandCursor();
	}

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Padding ContentPadding
	{
		get => Padding;
		set => Padding = value;
	}

	/// <inheritdoc />
	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		ApplyColors();
	}

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);

		if (lblSender is not null)
			ApplyTextFont();
	}

	/// <inheritdoc />
	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		ApplyColors();
	}

	/// <inheritdoc />
	protected override void OnPaddingChanged(EventArgs e)
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		try
		{
			base.OnPaddingChanged(e);
			UpdateContentWidthConstraints();
			PerformLayout();
		}
		finally
		{
			AfterLayoutChange?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <inheritdoc />
	protected override void OnParentBackColorChanged(EventArgs e)
	{
		base.OnParentBackColorChanged(e);
		ApplyColors();
	}

	private void ApplyTextFont()
	{
		var oldSenderFont = _senderFont;
		var oldIconFont = _iconFont;
		_senderFont = new Font(Font, Font.Style | FontStyle.Bold);
		_iconFont = ChatIcons.CreateFont(Math.Max(1f, Math.Min(Font.SizeInPoints, 9f)));
		lblSender.Font = _senderFont;
		lblTitle.Font = Font;
		lblArgs.Font = Font;
		lblResult.Font = Font;
		btnApprove.Font = Font;
		btnReject.Font = Font;
		lblIcon.Font = _iconFont;
		lblResultIcon.Font = _iconFont;
		UpdateIconLayout();
		oldSenderFont?.Dispose();
		oldIconFont?.Dispose();
	}

	/// <inheritdoc />
	protected override void OnDpiChangedAfterParent(EventArgs e)
	{
		base.OnDpiChangedAfterParent(e);
		UpdateIconLayout();
	}

	/// <inheritdoc/>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Category("Chat")]
	public bool AllowExpand
	{
		get => _allowExpand;
		set
		{
			if (_allowExpand == value)
				return;

			_allowExpand = value;
			UpdateExpandCursor();

			if (!_allowExpand && _expanded)
			{
				_expanded = false;
				ApplyVisibility();
			}
		}
	}

	/// <summary>
	/// Gets or sets the chat message to display.
	/// The message's <see cref="IChatMessage.Content"/> must be a
	/// <see cref="FunctionCallMessageContent"/> for any content to be rendered.
	/// </summary>
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			// Unsubscribe from the previous content's change notifications.
			if (_message?.Content is FunctionCallMessageContent oldFc)
				oldFc.PropertyChanged -= OnContentPropertyChanged;

			_message = value;
			lblSender.Text = _message?.Sender?.Name ?? string.Empty;

			// Subscribe to the new content so we redraw when IsFunctionExecuting
			// or Result changes (raised by FunctionCallMessageContent.SetResult).
			if (_message?.Content is FunctionCallMessageContent newFc)
				newFc.PropertyChanged += OnContentPropertyChanged;

			UpdateDisplay();
		}
	}

	/// <summary>
	/// Handles <see cref="FunctionCallMessageContent.PropertyChanged"/> so the
	/// control updates as soon as <see cref="FunctionCallMessageContent.IsFunctionExecuting"/>
	/// or <see cref="FunctionCallMessageContent.Result"/> changes.
	/// </summary>
	private void OnContentPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (IsDisposed || Disposing)
			return;
		if (e.PropertyName == nameof(FunctionCallMessageContent.IsFunctionExecuting))
			return;

		if (InvokeRequired)
		{
			if (IsHandleCreated)
			{
				try
				{
					BeginInvoke(UpdateDisplay);
				}
				catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
				{
					// The message was removed while the cross-thread update was being queued.
				}
			}
			return;
		}

		UpdateDisplay();
	}

	/// <summary>
	/// Gets or sets the maximum size of this control.
	/// The horizontal constraint is forwarded to the text controls so that long lines wrap.
	/// </summary>
	public override Size MaximumSize
	{
		get => base.MaximumSize;
		set
		{
			if (base.MaximumSize == value)
				return;

			base.MaximumSize = value;
			UpdateContentWidthConstraints();

			// Details shown before the control was added to the history were measured without a width;
			// re-show them like a toggle does so the table picks up their real height.
			if (lblArgs is not null && _message?.Content is FunctionCallMessageContent { ApprovalState: ToolApprovalState.Pending })
			{
				lblArgs.Visible = false;
				approvalPanel.Visible = false;
				ApplyVisibility();
			}
		}
	}

	/// <summary>
	/// Toggles the expanded/collapsed state and refreshes visibility.
	/// </summary>
	private void Toggle(object? sender, EventArgs e)
	{
		if (AllowExpand)
		{
			_expanded = !_expanded;
			ApplyVisibility();
		}
	}

	/// <summary>
	/// Rebuilds the header and selectable detail controls from the current <see cref="Message"/> and applies visibility.
	/// Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is not
	/// a <see cref="FunctionCallMessageContent"/>.
	/// </summary>
	private void UpdateDisplay()
	{
		if (CanUpdateCollapsedContentWithoutLayout())
		{
			UpdateDisplayCore();
			return;
		}

		ApplyLayoutUpdate(UpdateDisplayCore);
	}

	private bool CanUpdateCollapsedContentWithoutLayout()
	{
		return !_expanded &&
			!lblArgs.Visible &&
			!lblResultIcon.Visible &&
			!lblResult.Visible &&
			_message?.Content is FunctionCallMessageContent functionCall &&
			approvalPanel.Visible == (functionCall.ApprovalState == ToolApprovalState.Pending) &&
			string.Equals(lblSender.Text, Message?.Sender?.Name ?? string.Empty, StringComparison.Ordinal) &&
			lblTitle.Text.StartsWith(FunctionCallDisplay.GetTitle(functionCall), StringComparison.Ordinal);
	}

	private void UpdateDisplayCore()
	{
		lblSender.Text = Message?.Sender?.Name ?? string.Empty;

		if (_message?.Content is not FunctionCallMessageContent fc)
		{
			lblTitle.Text = string.Empty;
			lblArgs.Text = string.Empty;
			lblResult.Text = string.Empty;
			_expanded = false;
			ApplyVisibilityCore();
			return;
		}

		lblIcon.Text = FunctionCallDisplay.GetIcon(fc);
		ApplyIconColors();
		lblTitle.Text = FunctionCallDisplay.GetHeader(fc);

		var argumentsText = string.Empty;
		if (fc.Arguments?.Count > 0)
		{
			var maxKeyLen = fc.Arguments.Keys.Max(k => k.Length);
			argumentsText = string.Join(Environment.NewLine, fc.Arguments.Select(kv => $"{(kv.Key + ":").PadRight(maxKeyLen + 1)} {kv.Value}"));
		}
		lblArgs.Text = argumentsText;

		lblResult.Text = fc.Result is null ? string.Empty : fc.Result + " ";
		ApplyVisibilityCore();
	}

	/// <summary>
	/// Shows or hides the argument and result rows based on <see cref="_expanded"/>
	/// and whether the data is actually present, and updates the chevron glyph.
	/// </summary>
	private void ApplyVisibility()
	{
		ApplyLayoutUpdate(ApplyVisibilityCore);
	}

	private void ApplyVisibilityCore()
	{
		var hasArgs = _message?.Content is FunctionCallMessageContent { Arguments.Count: > 0 };
		var hasResult = _message?.Content is FunctionCallMessageContent { Result: not null };
		var isPending = _message?.Content is FunctionCallMessageContent { ApprovalState: ToolApprovalState.Pending };

		lblArgs.Visible = (_expanded || isPending) && hasArgs;
		lblResultIcon.Visible = _expanded && hasResult;
		lblResult.Visible = _expanded && hasResult;
		approvalPanel.Visible = isPending;
	}

	private void BtnApprove_Click(object? sender, EventArgs e)
	{
		(_message?.Content as FunctionCallMessageContent)?.Approve();
	}

	private void BtnReject_Click(object? sender, EventArgs e)
	{
		(_message?.Content as FunctionCallMessageContent)?.Reject();
	}

	private void ApplyLayoutUpdate(Action updateControls)
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		SuspendLayout();
		tableLayout.SuspendLayout();
		try
		{
			updateControls();
		}
		finally
		{
			tableLayout.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
			tableLayout.PerformLayout();
			PerformLayout();

			if (lblArgs.Visible)
				lblArgs.RefreshContentHeight();
			if (lblResult.Visible)
				lblResult.RefreshContentHeight();

			tableLayout.PerformLayout();
			PerformLayout();
			if (lblArgs.Visible)
				lblArgs.Invalidate();
			if (lblResult.Visible)
				lblResult.Invalidate();

			try
			{
				AfterLayoutChange?.Invoke(this, EventArgs.Empty);
			}
			finally
			{
				Parent?.PerformLayout(this, nameof(PreferredSize));
			}
		}
	}

	/// <inheritdoc />
	protected override void OnLayout(LayoutEventArgs levent)
	{
		// Compute the width the argument/result text will occupy (client width minus padding and the
		// icon column) and re-measure their heights *before* the base layout positions the table rows.
		// This keeps the rows stacked with the correct height in a single pass; measuring afterwards
		// would re-flow re-entrantly and leave the rows overlapping until another layout pass.
		var contentWidth = Math.Max(0, ClientSize.Width - ContentPadding.Horizontal);
		var textWidth = Math.Max(0, contentWidth - _iconColumnWidth);
		if (lblArgs.Visible)
			lblArgs.EnsureContentHeightForWidth(textWidth);
		if (lblResult.Visible)
			lblResult.EnsureContentHeightForWidth(textWidth);
		SyncArgumentsRowHeight();

		base.OnLayout(levent);
	}

	/// <summary>
	/// The auto-size row does not always pick up the measured argument height (for example when the
	/// arguments are shown immediately for a pending approval), so the row is sized explicitly.
	/// </summary>
	private void SyncArgumentsRowHeight()
	{
		if (tableLayout.RowStyles.Count < 2)
			return;

		var rowStyle = tableLayout.RowStyles[1];
		var height = lblArgs.Visible ? lblArgs.Height + lblArgs.Margin.Vertical : 0f;
		var sizeType = lblArgs.Visible ? SizeType.Absolute : SizeType.AutoSize;
		if (rowStyle.SizeType != sizeType || Math.Abs(rowStyle.Height - height) > 0.5f)
		{
			rowStyle.SizeType = sizeType;
			rowStyle.Height = height;
		}
	}

	private void SelectableText_ContentHeightChanging(object? sender, EventArgs e)
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
	}

	private void SelectableText_ContentHeightChanged(object? sender, EventArgs e)
	{
		AfterLayoutChange?.Invoke(this, EventArgs.Empty);
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
	}

	private void UpdateExpandCursor()
	{
		var cursor = AllowExpand ? Cursors.Hand : Cursors.Default;
		Cursor = cursor;
		tableLayout.Cursor = cursor;
		lblTitle.Cursor = cursor;
		lblIcon.Cursor = cursor;
		lblResultIcon.Cursor = cursor;
	}

	private void UpdateIconLayout()
	{
		if (lblIcon is null || lblResultIcon is null || lblTitle is null || lblResult is null)
			return;

		_iconColumnWidth = ScaleLogicalPixels(ICON_WIDTH, DeviceDpi);
		if (tableLayout.ColumnStyles.Count > 0)
			tableLayout.ColumnStyles[0].Width = _iconColumnWidth;
		lblIcon.Width = _iconColumnWidth;
		lblResultIcon.Width = _iconColumnWidth;
		ApplyIconLayout(lblIcon, lblTitle);
		ApplyIconLayout(lblResultIcon, lblResult);
		UpdateContentWidthConstraints();
		tableLayout.PerformLayout();
		PerformLayout();
	}

	private static void ApplyIconLayout(Label icon, Control textControl)
	{
		icon.Margin = new Padding(0, textControl.Margin.Top, 0, textControl.Margin.Bottom);
		icon.Height = Math.Max(
			1,
			(int)Math.Ceiling(textControl.Font.GetHeight(textControl.DeviceDpi)));
		icon.TextAlign = ContentAlignment.MiddleCenter;
	}

	private static int ScaleLogicalPixels(int logicalPixels, int deviceDpi)
	{
		return Math.Max(1, (int)Math.Round(logicalPixels * deviceDpi / 96f));
	}

	private void UpdateContentWidthConstraints()
	{
		if (lblSender is null || lblTitle is null || lblArgs is null || lblResult is null)
			return;

		var contentWidth = MaximumSize.Width == 0
			? 0
			: Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal);
		var textWidth = contentWidth == 0 ? 0 : Math.Max(0, contentWidth - _iconColumnWidth);
		lblSender.MaximumSize = new Size(contentWidth, 0);
		lblTitle.MaximumSize = new Size(textWidth, 0);
		lblArgs.MaximumSize = new Size(textWidth, 0);
		lblResult.MaximumSize = new Size(textWidth, 0);
	}

	private void ApplyColors()
	{
		if (lblSender is null || tableLayout is null)
			return;

		var backColor = GetOpaqueBackColor();
		tableLayout.BackColor = backColor;
		approvalPanel.BackColor = backColor;
		foreach (var label in new Control[] { lblSender, lblIcon, lblTitle, lblArgs, lblResultIcon, lblResult })
		{
			label.BackColor = backColor;
			label.ForeColor = ForeColor;
		}

		ApplyIconColors();
	}

	private void ApplyIconColors()
	{
		var accentColor = _message?.Content is FunctionCallMessageContent { Skill: not null } ? ChatIcons.SkillColor : ChatIcons.ToolColor;
		lblIcon.ForeColor = ChatIcons.GetIconColor(accentColor, ForeColor);
		lblResultIcon.ForeColor = ChatIcons.GetIconColor(ChatIcons.ResultColor, ForeColor);
	}

	/// <inheritdoc />
	void IChatMessageRefreshControl.RefreshMessageDisplay()
	{
		UpdateDisplay();
	}

	private Color GetOpaqueBackColor()
	{
		for (Control? current = this; current is not null; current = current.Parent)
		{
			if (current.BackColor.A == byte.MaxValue)
				return current.BackColor;
		}

		return SystemColors.Control;
	}

	/// <inheritdoc/>
	public override string ToString() => $"{lblTitle.Text}{Environment.NewLine}{lblArgs.Text}{Environment.NewLine}{lblResult.Text}";
}
