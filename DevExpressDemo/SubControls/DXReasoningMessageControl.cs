using System;
using System.ComponentModel;
using System.Drawing;
using DevExpress.XtraEditors;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// Displays a thinking text, click-to-expand bubble.
/// </summary>
internal sealed partial class DXReasoningMessageControl : PanelControl, IChatMessageControl
{
	/// <summary>Fixed pixel width reserved for the icon column.</summary>
	private const int IconColumnWidth = 20;

	/// <summary>The chat message whose <see cref="ReasoningMessageContent"/> is being displayed.</summary>
	private IChatMessage? _message;
	private bool _isReceivingStream;

	/// <summary>
	/// Indicates whether the detail panel (arguments and result) is currently visible.
	/// <see langword="true"/> when expanded; <see langword="false"/> when collapsed.
	/// </summary>
	private bool _expanded;

	/// <inheritdoc/>
	public event EventHandler? SizeUpdatedWhileStreaming;

	/// <summary>
	/// Gets or sets the formatter that converts message content into displayable strings.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public required IMessageFormatter MessageFormatter { get; set; }

	/// <summary>
	/// Initializes a new instance of <see cref="DXReasoningMessageControl"/>, creating and wiring up the
	/// icon, header and detail labels via the designer-generated <see cref="InitializeComponent"/>.
	/// </summary>
	public DXReasoningMessageControl()
	{
		InitializeComponent();

	}

	/// <summary>
	/// Gets or sets the chat message to display. The message's <see cref="IChatMessage.Content"/> must be a
	/// <see cref="ReasoningMessageContent"/> for any content to be rendered.
	/// </summary>
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			_message = value;

			lblDetail.DataBindings.Clear();
			lblTitle.DataBindings.Clear();
			if (Message is not null)
			{
				var binding = lblDetail.DataBindings.Add(nameof(lblDetail.Text), Message.Content, nameof(Message.Content.Content));
				binding.Format += (_, e) => e.Value = MessageFormatter.Format(Message.Content);

				if (Message.Content is ReasoningMessageContent rc)
				{
					binding = lblTitle.DataBindings.Add(nameof(lblTitle.Text), Message.Content, nameof(ReasoningMessageContent.IsThinking));
					binding.Format += (_, e) =>
					{
						if (rc.IsThinking)
							e.Value = "...";
						else
							e.Value = "✔ "; // keep extra space for to prevent capping the char
					};
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets the maximum size of this control.
	/// Setting this value also propagates the horizontal constraint to the inner
	/// <see cref="lblTitle"/> and <see cref="lblDetail"/> so that text wraps correctly.
	/// </summary>
	public override Size MaximumSize
	{
		get => base.MaximumSize;
		set
		{
			base.MaximumSize = value;
			lblTitle.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
			lblDetail.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
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
		lblDetail.Visible = _expanded;
		UpdateHeader();
	}

	/// <summary>
	/// Rebuilds the single-line header text that shows the wrench icon, function name, inline argument summary, and the
	/// expand/collapse arrow indicator. Does nothing if <see cref="Message"/> is <see langword="null"/> or its content is
	/// not a <see cref="ReasoningMessageContent"/> .
	/// </summary>
	private void UpdateHeader()
	{
		if (lblTitle.DataBindings.Count > 0)
			lblTitle.DataBindings[0].ReadValue();
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
