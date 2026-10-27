using System.ComponentModel;
using DevExpress.XtraEditors;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// Displays a thinking text, click-to-expand bubble.
/// </summary>
internal sealed partial class DXReasoningMessageControl : PanelControl, IChatMessageFormattingControl, IChatMessageLayoutControl, IChatMessageRefreshControl, IReasoningMessageControl
{
	/// <summary>Fixed pixel width reserved for the icon column.</summary>
	private const int ICON_COLUMN_WIDTH = 20;
	private const int ICON_SIZE = 14;

	/// <summary>The chat message whose <see cref="ReasoningMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private bool _allowExpand = true;
	private bool _detailTextDirty = true;
	private IMessageFormatter _messageFormatter = new PlainTextMessageFormatter();
	private readonly CoalescedUiUpdate _streamingDisplayUpdate;

	/// <summary>
	/// Indicates whether the detail panel (arguments and result) is currently visible.
	/// <see langword="true"/> when expanded; <see langword="false"/> when collapsed.
	/// </summary>
	private bool _expanded;

	/// <inheritdoc/>
	public event EventHandler? SizeUpdatedWhileStreaming;

	/// <inheritdoc/>
	public event EventHandler? BeforeLayoutChange;

	/// <inheritdoc/>
	public event EventHandler? AfterLayoutChange;

	/// <summary>
	/// Gets or sets the formatter that converts message content into displayable strings.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public required IMessageFormatter MessageFormatter
	{
		get => _messageFormatter;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			if (ReferenceEquals(_messageFormatter, value))
				return;

			_messageFormatter = value;
			_detailTextDirty = true;
			if (Message is not null)
				UpdateDisplay();
		}
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Padding ContentPadding
	{
		get => Padding;
		set => Padding = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DXReasoningMessageControl"/>, creating and wiring up the
	/// icon, header and detail labels via the designer-generated <see cref="InitializeComponent"/>.
	/// </summary>
	public DXReasoningMessageControl()
	{
		_streamingDisplayUpdate = new CoalescedUiUpdate(this, UpdateDisplay);
		InitializeComponent();

		WireMouseDown(paddingPanel, tableLayout, lblIcon, lblTitle, lblDetail);
		ApplyAppearance();
		UpdateExpandCursor();
		Disposed += (_, _) => _streamingDisplayUpdate.Cancel();
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
	}

	/// <summary>
	/// Gets or sets the chat message to display. The message's <see cref="IChatMessage.Content"/> must be a
	/// <see cref="ReasoningMessageContent"/> for any content to be rendered.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			if (_message?.Content is { } previousContent)
				previousContent.PropertyChanged -= MessageContent_PropertyChanged;

			_message = value;
			_detailTextDirty = true;
			if (_message?.Content is { } currentContent)
				currentContent.PropertyChanged += MessageContent_PropertyChanged;

			UpdateDisplay();
		}
	}

	/// <summary>
	/// Gets or sets the maximum size of this control.
	/// Setting this value also propagates the horizontal constraint to the inner
	/// title and detail controls so that text wraps correctly.
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
		}
	}

	/// <inheritdoc />
	protected override void OnPaddingChanged(EventArgs e)
	{
		base.OnPaddingChanged(e);
		UpdateContentWidthConstraints();
		PerformLayout();
	}

	/// <summary>
	/// Toggles the expanded/collapsed state of the detail panel.
	/// </summary>
	/// <param name="sender">The object that raised the click event.</param>
	/// <param name="e">Event data (not used).</param>
	private void Toggle(object? sender, EventArgs e)
	{
		if (!AllowExpand)
			return;

		_expanded = !_expanded;
		ApplyVisibility();
	}

	private void ApplyVisibility()
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		SuspendLayout();
		tableLayout.SuspendLayout();
		try
		{
			if (_expanded)
				EnsureDetailTextCurrent();
			lblDetail.Visible = _expanded && Message is not null;
		}
		finally
		{
			tableLayout.ResumeLayout(performLayout: false);
			tableLayout.PerformLayout();
			ResumeLayout(performLayout: false);
			PerformLayout();

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

	private void UpdateExpandCursor()
	{
		var cursor = AllowExpand ? Cursors.Hand : Cursors.Default;
		Cursor = cursor;
		paddingPanel.Cursor = cursor;
		tableLayout.Cursor = cursor;
		lblIcon.Cursor = cursor;
		lblTitle.Cursor = cursor;
		lblDetail.Cursor = cursor;
	}

	private string GetHeaderText()
	{
		return Message?.Content is ReasoningMessageContent reasoningContent
			? reasoningContent.IsThinking ? "..." : "\u2714"
			: string.Empty;
	}

	private void UpdateDisplay()
	{
		var senderText = Message?.Sender?.Name ?? string.Empty;
		var headerText = GetHeaderText();
		var detailVisible = _expanded && Message is not null;
		var metadataChanged =
			!string.Equals(lblSender.Text, senderText, StringComparison.Ordinal) ||
			!string.Equals(lblTitle.Text, headerText, StringComparison.Ordinal) ||
			lblDetail.Visible != detailVisible;
		if (!lblDetail.Visible && !metadataChanged)
			return;

		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		try
		{
			lblSender.Text = senderText;
			lblTitle.Text = headerText;
			if (detailVisible)
				EnsureDetailTextCurrent();
			lblDetail.Visible = detailVisible;
		}
		finally
		{
			AfterLayoutChange?.Invoke(this, EventArgs.Empty);
		}
	}

	private void EnsureDetailTextCurrent()
	{
		if (!_detailTextDirty)
			return;

		lblDetail.Text = Message is null
			? string.Empty
			: MessageFormatter.Format(Message.Content);
		_detailTextDirty = false;
	}

	private void MessageContent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (IsDisposed || Disposing)
			return;
		_detailTextDirty = true;
		if (_isReceivingStream)
		{
			_streamingDisplayUpdate.Queue();
			return;
		}

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

	/// <inheritdoc />
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);

		if (_isReceivingStream)
			SizeUpdatedWhileStreaming?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		ApplyAppearance();
	}

	/// <inheritdoc />
	protected override void OnDpiChangedAfterParent(EventArgs e)
	{
		base.OnDpiChangedAfterParent(e);
		UpdateIconLayout();
		UpdateContentWidthConstraints();
		PerformLayout();
	}

	/// <inheritdoc />
	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		ApplyAppearance();
	}

	/// <inheritdoc />
	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		ApplyAppearance();
	}

	/// <inheritdoc />
	void IChatMessageControl.SetIsReceivingStream(bool isReceiving)
	{
		if (isReceiving)
		{
			_isReceivingStream = true;
			return;
		}

		try
		{
			_streamingDisplayUpdate.Flush();
		}
		finally
		{
			_isReceivingStream = false;
		}
	}

	/// <inheritdoc/>
	void IChatMessageControl.ShowSenderHeader(bool show)
	{
		lblSender.Visible = show;
	}

	/// <inheritdoc />
	void IChatMessageRefreshControl.RefreshMessageDisplay()
	{
		UpdateDisplay();
	}

	private void ApplyAppearance()
	{
		if (lblSender is null)
			return;

		ApplyLabelAppearance(lblSender, FontStyle.Bold);
		ApplyLabelAppearance(lblIcon);
		ApplyLabelAppearance(lblTitle);
		ApplyLabelAppearance(lblDetail);
		UpdateIconLayout();

		paddingPanel.BackColor = BackColor;
		tableLayout.BackColor = BackColor;
	}

	private void UpdateIconLayout()
	{
		if (lblIcon is null || lblTitle is null)
			return;

		var scaledIconSize = ScaleLogicalPixels(ICON_SIZE);
		var scaledIconColumnWidth = ScaleLogicalPixels(ICON_COLUMN_WIDTH);
		tableLayout.ColumnStyles[0].Width = scaledIconColumnWidth;
		lblIcon.Margin = new Padding(0, lblTitle.Margin.Top, 0, lblTitle.Margin.Bottom);
		lblIcon.Width = scaledIconColumnWidth;
		lblIcon.ImageOptions.SvgImageSize = new Size(scaledIconSize, scaledIconSize);
		lblIcon.Appearance.ImageAlign = ContentAlignment.MiddleCenter;
		lblIcon.Appearance.Options.UseImage = true;
	}

	private void ApplyLabelAppearance(LabelControl label, FontStyle fontStyle = FontStyle.Regular)
	{
		label.Appearance.Font = Font;
		label.Appearance.FontStyleDelta = fontStyle;
		label.Appearance.ForeColor = ForeColor;
		label.Appearance.BackColor = BackColor;
		label.Appearance.Options.UseFont = true;
		label.Appearance.Options.UseForeColor = true;
		label.Appearance.Options.UseBackColor = true;
	}

	private void UpdateContentWidthConstraints()
	{
		if (lblTitle is null || lblDetail is null)
			return;

		var textWidth = MaximumSize.Width == 0
			? 0
			: Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal - ScaleLogicalPixels(ICON_COLUMN_WIDTH));
		lblTitle.MaximumSize = new Size(textWidth, 0);
		lblDetail.MaximumSize = new Size(textWidth, 0);
	}

	private int ScaleLogicalPixels(int logicalPixels)
	{
		return Math.Max(1, (int)Math.Round(logicalPixels * DeviceDpi / 96f));
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		EnsureDetailTextCurrent();
		return DXFormattedText.ToPlainText(lblDetail.Text);
	}
}
