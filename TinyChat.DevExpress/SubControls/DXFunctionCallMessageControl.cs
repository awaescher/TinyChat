using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>
/// Displays a function call and its result in a structured two-column layout.
/// The left column shows icon glyphs from Segoe MDL2 Assets; the right column shows
/// the function name (bold), the arguments, and the result.
/// Clicking anywhere on the control toggles the detail rows (arguments + result).
/// </summary>
internal sealed partial class DXFunctionCallMessageControl : PanelControl, IChatMessageControl
{
	/// <summary>
	/// Fixed pixel width reserved for the icon column.
	/// </summary>
	private const int ICON_WIDTH = 20;

	/// <summary>SVG for the tool/lightning icon.</summary>
	private const string ToolSvg = """
		<svg viewBox="0 0 32 32">
			<style type="text/css">.Yellow{fill:#FFB115;}</style>
			<polygon points="22,2 14,2 6,16 14,16 8,30 26,12 16.3,12" class="Yellow" />
		</svg>
		""";

	/// <summary>SVG for the result/arrow icon.</summary>
	private const string ResultSvg = """
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

	/// <inheritdoc/>
	/// <remarks>Tool call messages are never streamed, so this event is intentionally a no-op.</remarks>
	public event EventHandler? SizeUpdatedWhileStreaming { add { } remove { } }

	/// <inheritdoc/>
	/// <remarks>Tool call messages are never streamed, so this method is intentionally a no-op.</remarks>
	void IChatMessageControl.SetIsReceivingStream(bool isReceiving) { }

	/// <summary>
	/// Initialises a new instance of <see cref="DXFunctionCallMessageControl"/>.
	/// </summary>
	public DXFunctionCallMessageControl()
	{
		InitializeComponent();

		lblToolIcon.Font = new Font(lblTitle.Font.FontFamily, lblTitle.Font.Size);
		lblResultIcon.Font = lblToolIcon.Font;

		lblTitle.Font = new Font("Consolas", lblTitle.Font.Size);
		lblArguments.Font = new Font(lblTitle.Font.FontFamily, lblArguments.Font.Size - 1);
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
	/// The horizontal constraint is forwarded to the text labels so that long lines wrap.
	/// </summary>
	public override Size MaximumSize
	{
		get => base.MaximumSize;
		set
		{
			base.MaximumSize = value;
			// Account for outer padding and icon column.
			var textWidth = Math.Max(0, value.Width - Padding.Horizontal - ICON_WIDTH);
			lblTitle.MaximumSize = new Size(textWidth, 0);
			lblArguments.MaximumSize = new Size(textWidth, 0);
			lblResult.MaximumSize = new Size(textWidth, 0);
		}
	}

	/// <summary>
	/// Toggles the expanded/collapsed state and refreshes visibility.
	/// </summary>
	private void Toggle(object? sender, EventArgs e)
	{
		_expanded = !_expanded;
		ApplyVisibility();
	}

	/// <summary>
	/// Refreshes both the header and detail labels from the current <see cref="Message"/>.
	/// Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is not
	/// a <see cref="FunctionCallMessageContent"/>.
	/// </summary>
	private void UpdateDisplay()
	{
		if (_message?.Content is not FunctionCallMessageContent fc)
			return;

		lblTitle.Text = fc.IsFunctionExecuting
			? fc.Name + " ..."
			: fc.Name + " ✔ "; // keep extra space for to prevent capping the char;

		if (fc.Arguments?.Count > 0)
		{
			var maxKeyLen = fc.Arguments.Keys.Max(k => k.Length);
			lblArguments.Text = string.Join("\n",
				fc.Arguments.Select(kv => $"{(kv.Key + ":").PadRight(maxKeyLen + 1)} {kv.Value}"));
		}

		if (fc.Result is not null)
			lblResult.Text = fc.Result.ToString();

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

		var hasArgs = fc.Arguments?.Count > 0;
		var hasResult = fc.Result is not null;

		lblArguments.Visible = _expanded && hasArgs;
		lblResultIcon.Visible = _expanded && hasResult;
		lblResult.Visible = _expanded && hasResult;
	}
}
