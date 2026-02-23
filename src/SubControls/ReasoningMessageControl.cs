using System.ComponentModel;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace TinyChat.SubControls;

/// <summary>
/// Displays a thinking text, click-to-expand bubble.
/// </summary>
internal class ReasoningMessageControl : Panel, IChatMessageControl
{
	/// <summary>The font used to render the detail text.</summary>
	private static readonly Font MonospaceFont = new("Consolas", 8f);

	/// <summary>The chat message whose <see cref="ReasoningMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;
	private bool _isReceivingStream;

	/// <summary>
	/// Indicates whether the detail panel (arguments and result) is currently visible.
	/// <see langword="true"/> when expanded; <see langword="false"/> when collapsed.
	/// </summary>
	private bool _expanded;

	/// <summary>The label that shows the collapsed one-line summary (icon, name and expand arrow).</summary>
	private readonly Label _headerLabel;

	/// <summary>The label that shows the full text when the control is expanded.</summary>
	private readonly Label _detailLabel;

	/// <inheritdoc/>
	public event EventHandler? SizeUpdatedWhileStreaming;

	/// <summary>
	/// Gets or sets the formatter that converts message content into displayable strings.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public required IMessageFormatter MessageFormatter { get; set; }

	/// <summary>
	/// Initializes a new instance of <see cref="ReasoningMessageControl"/> , creating and wiring up the header and detail
	/// labels.
	/// </summary>
	public ReasoningMessageControl()
	{
		AutoSize = true;
		BorderStyle = BorderStyle.FixedSingle;
		Padding = new Padding(8);
		Margin = new Padding(12, 0, 6, 0);
		Cursor = Cursors.Hand;

		_headerLabel = new Label
		{
			AutoSize = true,
			Font = MonospaceFont,
			UseMnemonic = false,
			Dock = DockStyle.Top,
			Cursor = Cursors.Hand,
		};

		_detailLabel = new Label
		{
			AutoSize = true,
			Font = MonospaceFont,
			UseMnemonic = false,
			Dock = DockStyle.Fill,
			Visible = false,
			Padding = new Padding(14, 4, 0, 0),
		};

		Controls.Add(_detailLabel);
		Controls.Add(_headerLabel);
		_headerLabel.BringToFront();
		_detailLabel.BringToFront();
		AutoSize = true;

		_headerLabel.Click += Toggle;
		_detailLabel.Click += Toggle;
		Click += Toggle;
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
			_message = value;

			_detailLabel.DataBindings.Clear();
			_headerLabel.DataBindings.Clear();
			if (Message is not null)
			{
				var binding = _detailLabel.DataBindings.Add(nameof(_detailLabel.Text), Message.Content, nameof(Message.Content.Content));
				binding.Format += (_, e) => e.Value = MessageFormatter.Format(Message.Content);

				if (Message.Content is ReasoningMessageContent rc)
				{
					binding = _headerLabel.DataBindings.Add(nameof(_headerLabel.Text), Message.Content, nameof(ReasoningMessageContent.IsThinking));
					binding.Format += (_, e) =>
					{
						var bullet = _expanded ? "-" : "+";
						if (rc.IsThinking)
							e.Value = $"{bullet} Thinking...";
						else
							e.Value = $"{bullet} Thoughts";
					};
				}
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
	/// Rebuilds the single-line header text that shows the icon, header text, and the
	/// expand/collapse arrow indicator. Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is
	/// not a <see cref="ReasoningMessageContent"/> .
	/// </summary>
	private void UpdateHeader()
	{
		if (_headerLabel.DataBindings.Count > 0)
			_headerLabel.DataBindings[0].ReadValue();
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
}
