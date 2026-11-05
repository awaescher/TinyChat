using System.ComponentModel;
using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>
/// Displays a function call and its result in a structured two-column layout.
/// The left column shows Fluent UI icons (see <see cref="FluentIcons"/>); the right column shows
/// the function name (bold), the arguments, and the result.
/// Clicking anywhere on the control toggles the detail rows (arguments + result).
/// </summary>
internal sealed partial class DXFunctionCallMessageControl : PanelControl, IChatMessageLayoutControl, IChatMessageRefreshControl, IFunctionCallMessageControl, IChatMessageTruncationControl, IChatPresentationControl
{
	/// <summary>
	/// Fixed pixel width reserved for the icon column.
	/// </summary>
	private const int ICON_WIDTH = 20;
	private const int ICON_SIZE = 14;
	private const int STATUS_ICON_SIZE = 12;
	private const string STATUS_SUCCESS = "success";
	private const string STATUS_WARNING = "warning";
	private const string STATUS_REJECTED = "rejected";


	private bool? _displaysSkillIcon;

	/// <summary>The chat message whose <see cref="FunctionCallMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;
	private FileAttachmentPreviewPanel? _resultPreview;
	private object? _displayedResult;
	private string _argumentsText = string.Empty;
	private string _resultText = string.Empty;
	private bool _isTruncated;
	private int _maximumDetailTextLength = DetailTextTruncation.DEFAULT_MAXIMUM_LENGTH;
	private int _maximumDetailTextLines = DetailTextTruncation.DEFAULT_MAXIMUM_LINES;
	private ChatDisplayTexts? _displayTexts;
	private ChatControl? _owner;

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
	/// Initialises a new instance of <see cref="DXFunctionCallMessageControl"/>.
	/// </summary>
	public DXFunctionCallMessageControl()
	{
		InitializeComponent();
		LookAndFeel.StyleChanged += (_, _) => statusImages[STATUS_WARNING] = FluentIcons.AttachmentNotice(ChatNoticeSeverity.Error, LookAndFeel);


		WireMouseDown(paddingPanel, tableLayout, lblToolIcon, lblTitle, lblArguments, lblResultIcon, lblResult);
		ApplyAppearance();
		UpdateExpandCursor();
		ApplyPresentation(null);
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

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Padding ContentPadding
	{
		get => Padding;
		set => Padding = value;
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
	/// Refreshes the header and detail labels from the current <see cref="Message"/>.
	/// Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is not
	/// a <see cref="FunctionCallMessageContent"/>.
	/// </summary>
	private void UpdateResultDisplay(object? result)
	{
		var structured = AIMessageContent.FromDisplayResult(result);
		_resultText = structured?.Text ?? result?.ToString() ?? string.Empty;
		if (ReferenceEquals(result, _displayedResult))
			return;
		_displayedResult = result;
		_resultPreview?.Dispose();
		_resultPreview = null;
		if (structured is not null && FileAttachmentPreviewPanel.HasAttachments(structured))
		{
			_resultPreview = new DXFileAttachmentPreviewPanel(structured);
			WireMouseDown(_resultPreview);
			tableLayout.Controls.Add(_resultPreview, 1, 4);
			_resultPreview.Visible = false;
			UpdateContentWidthConstraints();
			if (ChatControl.FindOwner(this) is { } owner)
				_resultPreview.ApplyPresentation(owner);
		}
	}

	private void ApplyDetailTexts()
	{
		var argumentsTruncated = DetailTextTruncation.TryTruncate(_argumentsText, MaximumDetailTextLength, MaximumDetailTextLines, out var arguments);
		var resultTruncated = DetailTextTruncation.TryTruncate(_resultText, MaximumDetailTextLength, MaximumDetailTextLines, out var result);
		lblArguments.Text = arguments;
		lblResult.Text = result;
		_isTruncated = argumentsTruncated || resultTruncated;
	}

	/// <inheritdoc />
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public int MaximumDetailTextLength
	{
		get => _maximumDetailTextLength;
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			if (_maximumDetailTextLength == value)
				return;

			_maximumDetailTextLength = value;
			ApplyDetailTexts();
			ApplyVisibility(force: true);
		}
	}

	/// <inheritdoc />
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public int MaximumDetailTextLines
	{
		get => _maximumDetailTextLines;
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			if (_maximumDetailTextLines == value)
				return;

			_maximumDetailTextLines = value;
			ApplyDetailTexts();
			ApplyVisibility(force: true);
		}
	}

	/// <inheritdoc />
	public void ApplyPresentation(ChatControl? owner)
	{
		_owner = owner;
		_displayTexts = owner?.DisplayTexts;
		var text = _displayTexts?.ShowFullText ?? ChatLocalization.GetString("ShowFullText");
		btnFullText.AccessibleName = text;
		toolTip.Set(btnFullText, string.Empty, text);
	}

	internal string GetFullText()
	{
		var title = _message?.Content is FunctionCallMessageContent content ? FunctionCallDisplay.GetTitle(content) : string.Empty;
		var argumentsHeading = _displayTexts?.FunctionArguments ?? ChatLocalization.GetString("FunctionArguments");
		var resultHeading = _displayTexts?.FunctionResult ?? ChatLocalization.GetString("FunctionResult");
		var sections = new List<string> { title };
		if (_argumentsText.Length > 0)
			sections.Add($"{argumentsHeading}:{Environment.NewLine}{_argumentsText}");
		if (_resultText.Length > 0)
			sections.Add($"{resultHeading}:{Environment.NewLine}{_resultText}");
		return string.Join(Environment.NewLine + Environment.NewLine, sections);
	}

	private void BtnFullText_Click(object? sender, EventArgs e)
	{
		var title = _message?.Content is FunctionCallMessageContent content ? FunctionCallDisplay.GetTitle(content) : string.Empty;
		if ((_owner ?? ChatControl.FindOwner(this)) is { } owner)
		{
			owner.OpenFullText(title, GetFullText());
			return;
		}

		using var dialog = new DXFullTextDialog(title, GetFullText(), Font);
		dialog.ShowDialog(FindForm());
	}

	private void UpdateDisplay()
	{
		lblSender.Text = Message?.Sender?.Name ?? string.Empty;

		if (_message?.Content is not FunctionCallMessageContent fc)
		{
			lblTitle.Text = string.Empty;
			_argumentsText = string.Empty;
			_resultText = string.Empty;
			UpdateResultDisplay(null);
			ApplyDetailTexts();
			_expanded = false;
			ApplyVisibility();
			return;
		}

		lblTitle.Text = GetHeaderHtml(fc);
		UpdateToolIcon(fc.Skill is not null);

		var argumentsText = string.Empty;
		if (fc.Arguments?.Count > 0)
		{
			var maxKeyLen = fc.Arguments.Keys.Max(k => k.Length);
			argumentsText = string.Join(Environment.NewLine, fc.Arguments.Select(kv => $"{(kv.Key + ":").PadRight(maxKeyLen + 1)} {kv.Value}"));
		}
		_argumentsText = argumentsText;
		UpdateResultDisplay(fc.Result);
		ApplyDetailTexts();

		ApplyVisibility();
	}

	/// <summary>
	/// Shows or hides the argument and result rows based on <see cref="_expanded"/>
	/// and whether the data is actually present, and updates the chevron glyph.
	/// </summary>
	private void ApplyVisibility(bool force = false)
	{
		var hasArgs = _message?.Content is FunctionCallMessageContent { Arguments.Count: > 0 };
		var hasResult = _message?.Content is FunctionCallMessageContent { Result: not null };
		var isPending = _message?.Content is FunctionCallMessageContent { ApprovalState: ToolApprovalState.Pending };
		var showArguments = (_expanded || isPending) && hasArgs;
		var showResult = _expanded && hasResult && _resultText.Length > 0;
		// Images and files returned by the tool stay visible while collapsed, like attachments of a message.
		var showPreview = _resultPreview is not null && hasResult;
		var showFullText = _isTruncated && (showArguments || showResult);
		if (!force &&
			lblArguments.Visible == showArguments &&
			lblResultIcon.Visible == showResult &&
			lblResult.Visible == showResult &&
			(_resultPreview is null || _resultPreview.Visible == showPreview) &&
			btnFullText.Visible == showFullText &&
			approvalPanel.Visible == isPending)
		{
			return;
		}

		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		SuspendLayout();
		tableLayout.SuspendLayout();
		try
		{
			lblArguments.Visible = showArguments;
			lblResultIcon.Visible = showResult;
			lblResult.Visible = showResult;
			if (_resultPreview is not null)
				_resultPreview.Visible = showPreview;
			btnFullText.Visible = showFullText;
			approvalPanel.Visible = isPending;
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

	private string GetHeaderHtml(FunctionCallMessageContent content)
	{
		var title = System.Net.WebUtility.HtmlEncode(FunctionCallDisplay.GetTitle(content));
		var statusImage = FunctionCallDisplay.GetStatusKind(content) switch
		{
			FunctionCallStatus.Succeeded => STATUS_SUCCESS,
			FunctionCallStatus.Failed => STATUS_WARNING,
			FunctionCallStatus.Rejected => STATUS_REJECTED,
			_ => null
		};

		if (statusImage is null)
			return $"{title} {FunctionCallDisplay.GetStatus(content)}";

		var size = ScaleLogicalPixels(STATUS_ICON_SIZE);
		return $"{title} <image={statusImage};size={size},{size};align=center>";
	}

	private void UpdateToolIcon(bool isSkill)
	{
		if (_displaysSkillIcon == isSkill)
			return;

		_displaysSkillIcon = isSkill;
		lblToolIcon.ImageOptions.SvgImage = isSkill ? FluentIcons.Skill : FluentIcons.Tool;
	}


	private void BtnApprove_Click(object? sender, EventArgs e)
	{
		(_message?.Content as FunctionCallMessageContent)?.Approve();
	}

	private void BtnReject_Click(object? sender, EventArgs e)
	{
		(_message?.Content as FunctionCallMessageContent)?.Reject();
	}

	private void UpdateExpandCursor()
	{
		var cursor = AllowExpand ? Cursors.Hand : Cursors.Default;
		Cursor = cursor;
		paddingPanel.Cursor = cursor;
		tableLayout.Cursor = cursor;
		lblToolIcon.Cursor = cursor;
		lblTitle.Cursor = cursor;
		lblArguments.Cursor = cursor;
		lblResultIcon.Cursor = cursor;
		lblResult.Cursor = cursor;
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
		if (_message?.Content is FunctionCallMessageContent content)
			lblTitle.Text = GetHeaderHtml(content);
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

	private void ApplyAppearance()
	{
		if (lblSender is null)
			return;

		ApplyLabelAppearance(lblSender, FontStyle.Bold);
		ApplyLabelAppearance(lblToolIcon);
		ApplyLabelAppearance(lblTitle);
		ApplyLabelAppearance(lblArguments);
		ApplyLabelAppearance(lblResultIcon);
		ApplyLabelAppearance(lblResult);
		UpdateIconLayout();

		paddingPanel.BackColor = BackColor;
		tableLayout.BackColor = BackColor;
		approvalPanel.BackColor = BackColor;
		btnFullText.Appearance.BackColor = BackColor;
		btnFullText.Appearance.Options.UseBackColor = true;
		btnApprove.Appearance.Font = Font;
		btnApprove.Appearance.Options.UseFont = true;
		btnReject.Appearance.Font = Font;
		btnReject.Appearance.Options.UseFont = true;
	}

	private void UpdateIconLayout()
	{
		if (lblToolIcon is null || lblResultIcon is null)
			return;

		var scaledIconSize = ScaleLogicalPixels(ICON_SIZE);
		var scaledIconColumnWidth = ScaleLogicalPixels(ICON_WIDTH);
		var svgSize = new Size(scaledIconSize, scaledIconSize);
		tableLayout.ColumnStyles[0].Width = scaledIconColumnWidth;

		ApplyIconLayout(
			lblToolIcon,
			lblTitle.Margin,
			Math.Max(lblTitle.Appearance.Font.Height, scaledIconSize),
			scaledIconColumnWidth,
			svgSize);
		ApplyIconLayout(
			lblResultIcon,
			lblResult.Margin,
			Math.Max(lblResult.Appearance.Font.Height, scaledIconSize),
			scaledIconColumnWidth,
			svgSize);
	}

	private static void ApplyIconLayout(
		LabelControl icon,
		Padding textMargin,
		int firstLineHeight,
		int columnWidth,
		Size svgSize)
	{
		icon.Margin = new Padding(0, textMargin.Top, 0, textMargin.Bottom);
		icon.Width = columnWidth;
		icon.Height = firstLineHeight;
		icon.ImageOptions.SvgImageSize = svgSize;
		icon.Appearance.ImageAlign = ContentAlignment.MiddleCenter;
		icon.Appearance.Options.UseImage = true;
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
		if (lblTitle is null || lblArguments is null || lblResult is null)
			return;

		var textWidth = MaximumSize.Width == 0
			? 0
			: Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal - ScaleLogicalPixels(ICON_WIDTH));
		lblTitle.MaximumSize = new Size(textWidth, 0);
		lblArguments.MaximumSize = new Size(textWidth, 0);
		lblResult.MaximumSize = new Size(textWidth, 0);
		if (_resultPreview is not null)
			_resultPreview.MaximumSize = new Size(textWidth, 0);
	}

	private int ScaleLogicalPixels(int logicalPixels)
	{
		return Math.Max(1, (int)Math.Round(logicalPixels * DeviceDpi / 96f));
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
	}

	/// <inheritdoc />
	void IChatMessageRefreshControl.RefreshMessageDisplay()
	{
		UpdateDisplay();
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		var header = _message?.Content is FunctionCallMessageContent content ? FunctionCallDisplay.GetHeader(content) : string.Empty;
		return $"{header}{Environment.NewLine}{_argumentsText}{Environment.NewLine}{_resultText}";
	}
}
