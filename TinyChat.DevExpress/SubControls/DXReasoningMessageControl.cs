using System.ComponentModel;
using DevExpress.XtraEditors;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// Displays a thinking text, click-to-expand bubble.
/// </summary>
internal sealed partial class DXReasoningMessageControl : PanelControl, IChatMessageControl, IChatMessageLayoutControl
{
	/// <summary>Fixed pixel width reserved for the icon column.</summary>
	private const int ICON_COLUMN_WIDTH = 20;

	/// <summary>SVG for the think/idea icon.</summary>
	private const string THINK_SVG = """
		<svg viewBox="0 0 32 32">
			<style type="text/css">.Black{fill:#727272;}.Yellow{fill:#FFB115;}</style>
			<path d="M13,24h6c0.6,0,1-0.5,1-1s-0.4-1-1-1h-6c-0.6,0-1,0.5-1,1S12.4,24,13,24z" class="Black" />
			<path d="M19,26h-6c-0.6,0-1,0.5-1,1s0.4,1,1,1h1c0,1.1,0.9,2,2,2s2-0.9,2-2h1c0.6,0,1-0.5,1-1S19.6,26,19,26z" class="Black" />
			<path d="M16,2c-4.4,0-8,3.6-8,8c0,5,4,6,4,10c2,0,6,0,8,0c0-4,4-5,4-10C24,5.6,20.4,2,16,2z" class="Yellow" />
		</svg>
		""";

	/// <summary>The chat message whose <see cref="ReasoningMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private IMessageFormatter _messageFormatter = new PlainTextMessageFormatter();

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
			_messageFormatter = value ?? throw new ArgumentNullException(nameof(value));
			UpdateDisplay();
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
	/// icon, header and selectable detail editor via the designer-generated <see cref="InitializeComponent"/>.
	/// </summary>
	public DXReasoningMessageControl()
	{
		InitializeComponent();

		txtDetail.ContentHeightChanging += SelectableText_ContentHeightChanging;
		txtDetail.ContentHeightChanged += SelectableText_ContentHeightChanged;
		WireMouseDown(paddingPanel, tableLayout, lblIcon, lblTitle);
		ApplyAppearance();
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
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		SuspendLayout();
		tableLayout.SuspendLayout();

		_expanded = !_expanded;
		txtDetail.Visible = _expanded;
		UpdateHeader();

		tableLayout.ResumeLayout();
		ResumeLayout();
		AfterLayoutChange?.Invoke(this, EventArgs.Empty);
	}

	private void UpdateHeader()
	{
		if (Message?.Content is ReasoningMessageContent reasoningContent)
			lblTitle.Text = reasoningContent.IsThinking ? "..." : "\u2714";
		else
			lblTitle.Text = string.Empty;
	}

	private void UpdateDisplay()
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		try
		{
			lblSender.Text = Message?.Sender?.Name ?? string.Empty;
			UpdateHeader();

			if (Message is null)
			{
				txtDetail.SetTextPreservingSelection(string.Empty);
				txtDetail.Visible = false;
				return;
			}

			var formattedText = MessageFormatter.Format(Message.Content);
			txtDetail.SetFormattedTextPreservingSelection(formattedText);
			txtDetail.Visible = _expanded;
			txtDetail.UpdateContentHeight();
		}
		finally
		{
			AfterLayoutChange?.Invoke(this, EventArgs.Empty);
		}
	}

	private void MessageContent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (IsDisposed || Disposing)
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
		_isReceivingStream = isReceiving;
	}

	/// <inheritdoc/>
	void IChatMessageControl.ShowSenderHeader(bool show)
	{
		lblSender.Visible = show;
	}

	private void ApplyAppearance()
	{
		if (lblSender is null)
			return;

		ApplyLabelAppearance(lblSender, FontStyle.Bold);
		ApplyLabelAppearance(lblIcon);
		ApplyLabelAppearance(lblTitle);
		txtDetail.ApplyTextAppearance(Font, ForeColor, BackColor);

		paddingPanel.BackColor = BackColor;
		tableLayout.BackColor = BackColor;
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
		if (lblTitle is null || txtDetail is null)
			return;

		var textWidth = MaximumSize.Width == 0
			? 0
			: Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal - ICON_COLUMN_WIDTH);
		lblTitle.MaximumSize = new Size(textWidth, 0);
		txtDetail.MaximumSize = new Size(textWidth, 0);
		txtDetail.UpdateContentHeight();
	}

	private void SelectableText_ContentHeightChanging(object? sender, EventArgs e)
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
	}

	private void SelectableText_ContentHeightChanged(object? sender, EventArgs e)
	{
		AfterLayoutChange?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc/>
	public override string ToString() => txtDetail.Text;
}
