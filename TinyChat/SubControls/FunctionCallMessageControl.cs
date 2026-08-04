using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Displays a function call and its result in a structured two-column layout.
/// The left column shows icon glyphs from Segoe MDL2 Assets; the right column shows
/// the function name (bold), the arguments, and the result.
/// Clicking the header or icon areas toggles the detail rows (arguments + result).
/// The detail text controls remain dedicated to text selection.
/// </summary>
internal sealed partial class FunctionCallMessageControl : Panel, IFunctionCallMessageControl, IChatMessageLayoutControl
{
	/// <summary>
	/// Icon used to mark the function-call row.
	/// </summary>
	private const string TOOL_CALL_ICON = "\U0001f9f0";

	/// <summary>
	/// Icon glyph used to mark the result row.
	/// </summary>
	private const string RESULT_ICON = "🡪";

	/// <summary>Fixed pixel width reserved for the icon column.</summary>
	private const int ICON_WIDTH = 20;

	/// <summary>The chat message whose <see cref="FunctionCallMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;
	private Font? _senderFont;

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

		lblIcon.Font = new Font("Arial", 11);
		lblResultIcon.Font = lblIcon.Font;
		ApplyTextFont();
		ApplyColors();
		Disposed += (_, _) =>
		{
			_senderFont?.Dispose();
			_senderFont = null;
		};

		WireMouseDown(tableLayout, lblIcon, lblTitle, lblResultIcon);
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
		lblTitle.Font = Font;
		lblArgs.Font = Font;
		lblResult.Font = Font;
		oldSenderFont?.Dispose();
	}

	/// <inheritdoc/>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Category("Chat")]
	public bool AllowExpand
	{
		get => _allowExpand;
		set
		{
			_allowExpand = value;

			var cursor = _allowExpand ? Cursors.Hand : Cursors.Default;
			tableLayout.Cursor = cursor;
			lblTitle.Cursor = cursor;
			lblIcon.Cursor = cursor;
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
		if (InvokeRequired)
			BeginInvoke(UpdateDisplay);
		else
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
			base.MaximumSize = value;
			UpdateContentWidthConstraints();
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
		if (_message?.Content is not FunctionCallMessageContent fc)
			return;

		lblTitle.Text = fc.IsFunctionExecuting
			? fc.Name + " ..."
			: fc.Name + " ✔";

		if (fc.Arguments?.Count > 0)
		{
			var maxKeyLen = fc.Arguments.Keys.Max(k => k.Length);
			lblArgs.Text = string.Join(Environment.NewLine, fc.Arguments.Select(kv => $"{(kv.Key + ":").PadRight(maxKeyLen + 1)} {kv.Value}"));
		}

		if (fc.Result is not null)
			lblResult.Text = fc.Result.ToString() + " ";

		ApplyVisibility();
	}

	/// <summary>
	/// Shows or hides the argument and result rows based on <see cref="_expanded"/>
	/// and whether the data is actually present, and updates the chevron glyph.
	/// </summary>
	private void ApplyVisibility()
	{
		if (_message?.Content is not FunctionCallMessageContent fc)
			return;

		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		SuspendLayout();
		tableLayout.SuspendLayout();

		var hasArgs = fc.Arguments?.Count > 0;
		var hasResult = fc.Result is not null;

		lblArgs.Visible = _expanded && hasArgs;
		lblResultIcon.Visible = _expanded && hasResult;

		lblResult.Visible = _expanded && hasResult;

		tableLayout.ResumeLayout();
		ResumeLayout();
		AfterLayoutChange?.Invoke(this, EventArgs.Empty);
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
	}

	private void UpdateContentWidthConstraints()
	{
		if (lblSender is null || lblTitle is null || lblArgs is null || lblResult is null)
			return;

		var contentWidth = MaximumSize.Width == 0
			? 0
			: Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal);
		var textWidth = contentWidth == 0 ? 0 : Math.Max(0, contentWidth - ICON_WIDTH);
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
		foreach (var label in new Control[] { lblSender, lblIcon, lblTitle, lblArgs, lblResultIcon, lblResult })
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
	public override string ToString() => $"{lblTitle.Text}{Environment.NewLine}{lblArgs.Text}{Environment.NewLine}{lblResult.Text}";
}
