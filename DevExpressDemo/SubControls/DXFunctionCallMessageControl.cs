using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>
/// Displays a function call and its result as a compact, click-to-expand bubble.
/// Shows a wrench icon and the function name at a glance; click to reveal arguments and result.
/// </summary>
internal sealed class DXFunctionCallMessageControl : PanelControl, IChatMessageControl
{
	/// <summary>The font used to render the expanded arguments and result detail text.</summary>
	private static readonly Font MonospaceFont = new("Consolas", 8f);

	/// <summary>The chat message whose <see cref="FunctionCallMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;

	/// <summary>
	/// Indicates whether the detail panel (arguments and result) is currently visible.
	/// <see langword="true"/> when expanded; <see langword="false"/> when collapsed.
	/// </summary>
	private bool _expanded;

	/// <summary>The label that shows the collapsed one-line summary (icon, name, inline args, and expand arrow).</summary>
	private readonly LabelControl _headerLabel;

	/// <summary>The label that shows the full argument list and function result when the control is expanded.</summary>
	private readonly LabelControl _detailLabel;

	/// <inheritdoc/>
	/// <remarks>Tool call messages are never streamed, so this event is intentionally a no-op.</remarks>
	public event EventHandler? SizeUpdatedWhileStreaming { add { } remove { } }

	/// <inheritdoc/>
	/// <remarks>Tool call messages are never streamed, so this method is intentionally a no-op.</remarks>
	void IChatMessageControl.SetIsReceivingStream(bool isReceiving) { }

	/// <summary>
	/// Initializes a new instance of <see cref="FunctionCallMessageControl"/>, creating and
	/// wiring up the header and detail labels.
	/// </summary>
	public DXFunctionCallMessageControl()
	{
		AutoSize = true;
		Padding = new Padding(8);
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		Cursor = Cursors.Hand;

		var borderPanel = new PanelControl
		{
			AutoSize = true,
			Dock = DockStyle.Fill,
			Padding = new Padding(8)
		};

		_headerLabel = new LabelControl
		{
			AutoSize = true,
			Font = MonospaceFont,
			UseMnemonic = false,
			Dock = DockStyle.Top,
			Cursor = Cursors.Hand,
		};

		_detailLabel = new LabelControl
		{
			AutoSize = true,
			Font = MonospaceFont,
			UseMnemonic = false,
			Dock = DockStyle.Fill,
			Visible = false,
			Padding = new Padding(14, 4, 0, 0),
		};

		borderPanel.Controls.Add(_detailLabel);
		borderPanel.Controls.Add(_headerLabel);
		Controls.Add(borderPanel);
		_headerLabel.BringToFront();
		_detailLabel.BringToFront();

		borderPanel.Click += Toggle;
		_headerLabel.Click += Toggle;
		_detailLabel.Click += Toggle;
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

			_detailLabel.DataBindings.Clear();
			_headerLabel.DataBindings.Clear();
			if (Message is not null && Message.Content is FunctionCallMessageContent fc)
			{
				var binding = _detailLabel.DataBindings.Add(nameof(_detailLabel.Text), Message.Content, nameof(Message.Content.Content));
				binding.Format += (_, e) =>
				{
					var maxArgKeyLength = fc.Arguments?.Any() ?? false ? fc.Arguments.Keys.Max(k => k.Length) : 0;
					var args = fc.Arguments?.Count > 0
						? string.Join("\n", fc.Arguments.Select(kv => $"{(kv.Key + ":").PadRight(maxArgKeyLength + 1)} {kv.Value}"))
						: "";

					var result = fc.Result is not null ? $"\n\n🡪 {fc.Result}" : "";

					e.Value = (args + result).TrimStart('\n');
				};


				binding = _headerLabel.DataBindings.Add(nameof(_headerLabel.Text), Message.Content, nameof(FunctionCallMessageContent.IsFunctionExecuting));
				binding.Format += (_, e) =>
				{
					var bullet = _expanded ? "-" : "+";
					var value = $"{bullet} {fc.Name}";

					if (fc.IsFunctionExecuting)
						value += " (working)";

					e.Value = value;
				};

			}
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
	/// Rebuilds the single-line header text that shows the wrench icon, function name,
	/// inline argument summary, and the expand/collapse arrow indicator.
	/// Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is not
	/// a <see cref="FunctionCallMessageContent"/>.
	/// </summary>
	private void UpdateHeader()
	{
		if (_headerLabel.DataBindings.Count > 0)
			_headerLabel.DataBindings[0].ReadValue();
	}

}
