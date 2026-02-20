namespace TinyChat;

/// <summary>
/// Displays a function call and its result as a compact, click-to-expand bubble.
/// Shows a wrench icon and the function name at a glance; click to reveal arguments and result.
/// </summary>
internal sealed class ToolCallMessageControl : Panel, IChatMessageControl
{
	/// <summary>The background color used for the bubble panel.</summary>
	private static readonly Color BubbleBack = Color.FromArgb(243, 244, 250);

	/// <summary>The font used to render the collapsed header line (function name and arrow).</summary>
	private static readonly Font HeaderFont = new("Segoe UI", 8.5f);

	/// <summary>The font used to render the expanded arguments and result detail text.</summary>
	private static readonly Font DetailFont = new("Consolas", 8f);

	/// <summary>The chat message whose <see cref="FunctionCallMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;

	/// <summary>
	/// Indicates whether the detail panel (arguments and result) is currently visible.
	/// <see langword="true"/> when expanded; <see langword="false"/> when collapsed.
	/// </summary>
	private bool _expanded;

	/// <summary>The label that shows the collapsed one-line summary (icon, name, inline args, and expand arrow).</summary>
	private readonly Label _headerLabel;

	/// <summary>The label that shows the full argument list and function result when the control is expanded.</summary>
	private readonly Label _detailLabel;

	/// <inheritdoc/>
	/// <remarks>Tool call messages are never streamed, so this event is intentionally a no-op.</remarks>
	public event EventHandler? SizeUpdatedWhileStreaming { add { } remove { } }

	/// <inheritdoc/>
	/// <remarks>Tool call messages are never streamed, so this method is intentionally a no-op.</remarks>
	void IChatMessageControl.SetIsReceivingStream(bool isReceiving) { }

	/// <summary>
	/// Initializes a new instance of <see cref="ToolCallMessageControl"/>, creating and
	/// wiring up the header and detail labels.
	/// </summary>
	public ToolCallMessageControl()
	{
		AutoSize = true;
		Padding = new Padding(6, 4, 6, 6);
		BackColor = BubbleBack;
		Cursor = Cursors.Hand;

		_headerLabel = new Label
		{
			AutoSize = true,
			Font = HeaderFont,
			ForeColor = Color.FromArgb(70, 70, 110),
			UseMnemonic = false,
			Dock = DockStyle.Top,
			Cursor = Cursors.Hand,
		};

		_detailLabel = new Label
		{
			AutoSize = true,
			Font = DetailFont,
			ForeColor = Color.FromArgb(80, 80, 100),
			UseMnemonic = false,
			Dock = DockStyle.Fill,
			Visible = false,
			Padding = new Padding(14, 4, 0, 0),
		};

		Controls.Add(_detailLabel);
		Controls.Add(_headerLabel);
		_headerLabel.BringToFront();

		_headerLabel.Click += Toggle;
		Click += Toggle;
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
			_message = value;
			UpdateDisplay();
		}
	}

	/// <summary>
	/// Gets or sets the maximum size of this control.
	/// Setting this value also propagates the horizontal constraint to the inner
	/// <see cref="_headerLabel"/> and <see cref="_detailLabel"/> so that text wraps correctly.
	/// </summary>
	public override Size MaximumSize
	{
		get => base.MaximumSize;
		set
		{
			base.MaximumSize = value;
			_headerLabel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
			_detailLabel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
		}
	}

	/// <summary>
	/// Paints the control, then draws a one-pixel border rectangle around the bubble.
	/// </summary>
	/// <param name="e">Paint event data, including the <see cref="Graphics"/> context.</param>
	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		using var pen = new Pen(Color.FromArgb(180, 185, 215));
		e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
	}

	/// <summary>
	/// Toggles the expanded/collapsed state of the detail panel when the user clicks
	/// anywhere on the control.
	/// </summary>
	/// <param name="sender">The object that raised the click event.</param>
	/// <param name="e">Event data (not used).</param>
	private void Toggle(object? sender, EventArgs e)
	{
		_expanded = !_expanded;
		_detailLabel.Visible = _expanded;
		UpdateHeader();
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

		UpdateHeader();

		var args = fc.Arguments?.Count > 0
			? string.Join("\n", fc.Arguments.Select(kv => $"  {kv.Key}: {kv.Value}"))
			: "  (no arguments)";

		_detailLabel.Text = $"Arguments:\n{args}\n\nResult:\n  {fc.Result ?? "(no result)"}";
	}

	/// <summary>
	/// Rebuilds the single-line header text that shows the wrench icon, function name,
	/// inline argument summary, and the expand/collapse arrow indicator.
	/// Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is not
	/// a <see cref="FunctionCallMessageContent"/>.
	/// </summary>
	private void UpdateHeader()
	{
		if (_message?.Content is not FunctionCallMessageContent fc)
			return;

		var arrow = _expanded ? "▼" : "▶";
		var argsSummary = fc.Arguments?.Count > 0
			? string.Join(", ", fc.Arguments.Select(kv => $"{kv.Key}: {kv.Value}"))
			: "";

		_headerLabel.Text = $"\U0001f527 {fc.Name}({argsSummary})  {arrow}";
	}
}
