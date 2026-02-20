using TinyChat;

namespace TinyChat;

/// <summary>
/// Displays a function call and its result as a compact, click-to-expand bubble.
/// Shows a wrench icon and the function name at a glance; click to reveal arguments and result.
/// </summary>
internal sealed class ToolCallMessageControl : Panel, IChatMessageControl
{
	private static readonly Color BubbleBack = Color.FromArgb(243, 244, 250);
	private static readonly Font HeaderFont = new("Segoe UI", 8.5f);
	private static readonly Font DetailFont = new("Consolas", 8f);

	private IChatMessage? _message;
	private bool _expanded;

	private readonly Label _headerLabel;
	private readonly Label _detailLabel;

	// IChatMessageControl — tool calls are never streamed
	public event EventHandler? SizeUpdatedWhileStreaming { add { } remove { } }
	void IChatMessageControl.SetIsReceivingStream(bool isReceiving) { }

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

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		using var pen = new Pen(Color.FromArgb(180, 185, 215));
		e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
	}

	private void Toggle(object? sender, EventArgs e)
	{
		_expanded = !_expanded;
		_detailLabel.Visible = _expanded;
		UpdateHeader();
	}

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
