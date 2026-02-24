namespace TinyChat;

/// <summary>
/// Displays a function call and its result in a structured two-column layout.
/// The left column shows icon glyphs from Segoe MDL2 Assets; the right column shows
/// the function name (bold), the arguments, and the result.
/// Clicking anywhere on the control toggles the detail rows (arguments + result).
/// </summary>
internal sealed partial class FunctionCallMessageControl : Panel, IChatMessageControl
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
	/// Initialises a new instance of <see cref="FunctionCallMessageControl"/>.
	/// </summary>
	public FunctionCallMessageControl()
	{
		InitializeComponent();

		_callIconLabel.Font = new Font("Arial", 11);
		_resultIconLabel.Font = _callIconLabel.Font;

		_callTitleLabel.Font = new Font("Consolas", _callTitleLabel.Font.Size - 1);
		_argsLabel.Font = new Font(_callTitleLabel.Font.FontFamily, _callTitleLabel.Font.Size - 1);
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
			var textWidth = Math.Max(0, value.Width - Padding.Horizontal - ICON_WIDTH);
			_callTitleLabel.MaximumSize = new Size(textWidth, 0);
			_argsLabel.MaximumSize = new Size(textWidth, 0);
			_resultLabel.MaximumSize = new Size(textWidth, 0);
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
	/// Rebuilds all labels from the current <see cref="Message"/> and applies visibility.
	/// Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is not
	/// a <see cref="FunctionCallMessageContent"/>.
	/// </summary>
	private void UpdateDisplay()
	{
		if (_message?.Content is not FunctionCallMessageContent fc)
			return;

		_callTitleLabel.Text = fc.IsFunctionExecuting
			? fc.Name + " ..."
			: fc.Name + " ✔ "; // keep extra space for to prevent capping the char;

		if (fc.Arguments?.Count > 0)
		{
			var maxKeyLen = fc.Arguments.Keys.Max(k => k.Length);
			_argsLabel.Text = string.Join("\n",
			fc.Arguments.Select(kv => $"{(kv.Key + ":").PadRight(maxKeyLen + 1)} {kv.Value}"));
		}

		if (fc.Result is not null)
			_resultLabel.Text = fc.Result.ToString() + " ";

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

		_argsLabel.Visible = _expanded && hasArgs;
		_resultIconLabel.Visible = _expanded && hasResult;
		_resultLabel.Visible = _expanded && hasResult;
	}
}