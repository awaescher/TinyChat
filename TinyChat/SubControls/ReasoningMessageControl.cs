using System.ComponentModel;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace TinyChat.SubControls;

/// <summary>
/// Displays a thinking text, click-to-expand bubble.
/// </summary>
internal partial class ReasoningMessageControl : Panel, IChatMessageControl, IChatMessageLayoutControl
{
	/// <summary>Icon glyph rendered in the left icon column.</summary>
	private const string IconGlyph = "💭";

	/// <summary>Fixed pixel width reserved for the icon column.</summary>
	private const int IconColumnWidth = 20;

	/// <summary>The chat message whose <see cref="ReasoningMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private Font? _senderFont;

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
	public required IMessageFormatter MessageFormatter { get; set; }

	/// <summary>
	/// Initializes a new instance of <see cref="ReasoningMessageControl"/>, creating and wiring up the
	/// icon, header, and selectable detail editor via the designer-generated <see cref="InitializeComponent"/>.
	/// </summary>
	public ReasoningMessageControl()
	{
		InitializeComponent();

		lblIcon.Font = new Font("Arial", 11);
		ApplyTextFont();
		ApplyColors();
		Disposed += (_, _) =>
		{
			_senderFont?.Dispose();
			_senderFont = null;
		};

		WireMouseDown(tableLayout, lblIcon, lblHeader);
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
		_senderFont = new Font(Font, Font.Style | FontStyle.Bold);
		lblSender.Font = _senderFont;
		lblHeader.Font = Font;
		lblDetail.Font = Font;
		oldSenderFont?.Dispose();
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
	}

	/// <summary>
	/// Gets or sets the chat message to display.
	/// The message's <see cref="IChatMessage.Content"/> must be a
	/// <see cref="ReasoningMessageContent"/> for any content to be rendered.
	/// </summary>
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			if (_message?.Content is ReasoningMessageContent oldReasoningContent)
				oldReasoningContent.PropertyChanged -= OnContentPropertyChanged;

			_message = value;
			lblSender.Text = _message?.Sender?.Name ?? string.Empty;

			if (_message?.Content is ReasoningMessageContent newReasoningContent)
				newReasoningContent.PropertyChanged += OnContentPropertyChanged;

			UpdateDisplay();
		}
	}

	private void OnContentPropertyChanged(object? sender, PropertyChangedEventArgs e)
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

	/// <summary>
	/// Gets or sets the maximum size of this control.
	/// Setting this value also propagates the horizontal constraint to the inner
	/// header and detail controls so that text wraps correctly.
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

	/// <summary>
	/// Toggles the expanded/collapsed state of the detail panel when the user clicks
	/// the header or icon area. The detail text control remains dedicated to text selection.
	/// </summary>
	/// <param name="sender">The object that raised the click event.</param>
	/// <param name="e">Event data (not used).</param>
	private void Toggle(object? sender, EventArgs e)
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		SuspendLayout();
		tableLayout.SuspendLayout();

		_expanded = !_expanded;
		lblDetail.Visible = _expanded;
		UpdateHeader();

		tableLayout.ResumeLayout();
		ResumeLayout();
		AfterLayoutChange?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Rebuilds the single-line header text that shows the icon, header text, and the
	/// expand/collapse arrow indicator. Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is
	/// not a <see cref="ReasoningMessageContent"/> .
	/// </summary>
	private void UpdateHeader()
	{
		if (_message?.Content is ReasoningMessageContent reasoningContent)
			lblHeader.Text = reasoningContent.IsThinking ? "..." : "✔";
	}

	private void UpdateDisplay()
	{
		if (_message?.Content is not ReasoningMessageContent reasoningContent)
		{
			UpdateDetailText(string.Empty);
			lblHeader.Text = string.Empty;
			return;
		}

		UpdateDetailText(MessageFormatter.Format(reasoningContent));
		UpdateHeader();
	}

	private void UpdateDetailText(string text)
	{
		if (lblDetail.Text == text)
			return;

		var selectionStart = lblDetail.IsHandleCreated ? lblDetail.SelectionStart : 0;
		var selectionLength = lblDetail.IsHandleCreated ? lblDetail.SelectionLength : 0;
		lblDetail.Text = text;

		if (!lblDetail.IsHandleCreated)
			return;

		selectionStart = Math.Min(selectionStart, text.Length);
		selectionLength = Math.Min(selectionLength, text.Length - selectionStart);
		lblDetail.Select(selectionStart, selectionLength);
	}

	/// <inheritdoc />
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);

		if (_isReceivingStream)
			SizeUpdatedWhileStreaming?.Invoke(this, EventArgs.Empty);
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

	private void UpdateContentWidthConstraints()
	{
		if (lblSender is null || lblHeader is null || lblDetail is null)
			return;

		var contentWidth = MaximumSize.Width == 0
			? 0
			: Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal);
		var textWidth = contentWidth == 0 ? 0 : Math.Max(0, contentWidth - IconColumnWidth);
		lblSender.MaximumSize = new Size(contentWidth, 0);
		lblHeader.MaximumSize = new Size(textWidth, 0);
		lblDetail.MaximumSize = new Size(textWidth, 0);
	}

	private void ApplyColors()
	{
		if (lblSender is null || tableLayout is null)
			return;

		var backColor = GetOpaqueBackColor();
		tableLayout.BackColor = backColor;
		foreach (var label in new Control[] { lblSender, lblIcon, lblHeader, lblDetail })
		{
			label.BackColor = backColor;
			label.ForeColor = ForeColor;
		}
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
	public override string ToString() => lblDetail.Text;
}
