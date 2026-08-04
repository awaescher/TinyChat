using System.ComponentModel;
using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>
/// Displays a function call and its result in a structured two-column layout.
/// The left column shows icon glyphs from Segoe MDL2 Assets; the right column shows
/// the function name (bold), the arguments, and the result.
/// Clicking the header or icon areas toggles the detail rows (arguments + result).
/// The detail editors remain dedicated to text selection.
/// </summary>
internal sealed partial class DXFunctionCallMessageControl : PanelControl, IFunctionCallMessageControl, IChatMessageLayoutControl
{
	/// <summary>
	/// Fixed pixel width reserved for the icon column.
	/// </summary>
	private const int ICON_WIDTH = 20;

	/// <summary>SVG for the tool/lightning icon.</summary>
	private const string TOOL_SVG = """
		<svg viewBox="0 0 32 32">
			<style type="text/css">.Yellow{fill:#FFB115;}</style>
			<polygon points="22,2 14,2 6,16 14,16 8,30 26,12 16.3,12" class="Yellow" />
		</svg>
		""";

	/// <summary>SVG for the result/arrow icon.</summary>
	private const string RESULT_SVG = """
		<svg viewBox="0 0 32 32">
			<style type="text/css">.Green{fill:#039C23;}</style>
			<polygon points="18,6 12.3,6 20.3,14 4,14 4,18 20.3,18 12.3,26 18,26 28,16" class="Green" />
		</svg>
		""";

	/// <summary>The chat message whose <see cref="FunctionCallMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;

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

		txtArguments.ContentHeightChanging += SelectableText_ContentHeightChanging;
		txtArguments.ContentHeightChanged += SelectableText_ContentHeightChanged;
		txtResult.ContentHeightChanging += SelectableText_ContentHeightChanging;
		txtResult.ContentHeightChanged += SelectableText_ContentHeightChanged;
		WireMouseDown(paddingPanel, tableLayout, lblToolIcon, lblTitle, lblResultIcon);
		ApplyAppearance();
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
			lblToolIcon.Cursor = cursor;
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
	/// Refreshes the header and selectable detail editors from the current <see cref="Message"/>.
	/// Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is not
	/// a <see cref="FunctionCallMessageContent"/>.
	/// </summary>
	private void UpdateDisplay()
	{
		if (_message?.Content is not FunctionCallMessageContent fc)
			return;

		lblTitle.Text = $"{fc.Name} {(fc.IsFunctionExecuting ? "..." : "\u2714")}";

		var argumentsText = string.Empty;
		if (fc.Arguments?.Count > 0)
		{
			var maxKeyLen = fc.Arguments.Keys.Max(k => k.Length);
			argumentsText = string.Join(Environment.NewLine, fc.Arguments.Select(kv => $"{(kv.Key + ":").PadRight(maxKeyLen + 1)} {kv.Value}"));
		}
		txtArguments.SetTextPreservingSelection(argumentsText);

		txtResult.SetTextPreservingSelection(fc.Result?.ToString());

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

		txtArguments.Visible = _expanded && hasArgs;
		lblResultIcon.Visible = _expanded && hasResult;
		txtResult.Visible = _expanded && hasResult;

		tableLayout.ResumeLayout();
		ResumeLayout();
		AfterLayoutChange?.Invoke(this, EventArgs.Empty);
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

	private void ApplyAppearance()
	{
		if (lblSender is null)
			return;

		ApplyLabelAppearance(lblSender, FontStyle.Bold);
		ApplyLabelAppearance(lblToolIcon);
		ApplyLabelAppearance(lblTitle);
		ApplyLabelAppearance(lblResultIcon);
		txtArguments.ApplyTextAppearance(Font, ForeColor, BackColor);
		txtResult.ApplyTextAppearance(Font, ForeColor, BackColor);

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
		if (lblTitle is null || txtArguments is null || txtResult is null)
			return;

		var textWidth = MaximumSize.Width == 0
			? 0
			: Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal - ICON_WIDTH);
		lblTitle.MaximumSize = new Size(textWidth, 0);
		txtArguments.MaximumSize = new Size(textWidth, 0);
		txtResult.MaximumSize = new Size(textWidth, 0);
		txtArguments.UpdateContentHeight();
		txtResult.UpdateContentHeight();
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
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
	public override string ToString() => $"{lblTitle.Text}{Environment.NewLine}{txtArguments.Text}{Environment.NewLine}{txtResult.Text}";
}
