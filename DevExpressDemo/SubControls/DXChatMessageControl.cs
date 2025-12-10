using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using TinyChat;
using TinyChat.Messages.Formatting;

namespace DevExpressDemo;

/// <summary>
/// A DevExpress-based chat message control that displays a chat message with sender and content.
/// Implements the IChatMessageControl interface and inherits from PanelControl.
/// </summary>
public class DXChatMessageControl : PanelControl, IChatMessageControl
{
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private readonly LabelControl _senderLabel;
	private readonly LabelControl _messageLabel;
	private readonly DevExpress.XtraEditors.PanelControl _thinkingPanel;

	/// <summary>
	/// The event that is raised when the size of the control is updated while streaming a message.
	/// </summary>
	public event EventHandler? SizeUpdatedWhileStreaming;

	/// <summary>
	/// Gets or sets the formatter that converts message content into displayable strings.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public required IMessageFormatter MessageFormatter { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="DXChatMessageControl"/> class.
	/// Sets up the layout with sender and message labels, configures styling and sizing behavior.
	/// </summary>
	public DXChatMessageControl()
	{
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		AutoSize = true;

		_senderLabel = new LabelControl() { AllowHtmlString = true, Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical, Font = new Font(Font, FontStyle.Bold), UseMnemonic = false };
		_thinkingPanel = new DevExpress.XtraEditors.PanelControl() { Dock = DockStyle.Top, AutoSize = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
		_messageLabel = new LabelControl() { AllowHtmlString = true, Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical, UseMnemonic = false };

		Controls.Add(_senderLabel);
		Controls.Add(_thinkingPanel);
		Controls.Add(_messageLabel);

		_messageLabel.BringToFront();
		_thinkingPanel.BringToFront();

		AutoSize = true;
		Padding = new Padding(8);
	}

	/// <summary>
	/// Gets or sets the chat message displayed by this control.
	/// When set, updates the sender and content labels with the message data.
	/// </summary>
	/// <value>
	/// The <see cref="IChatMessage"/> instance to display, or null if no message is set.
	/// </value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			_message = value;
			_senderLabel.Text = Message?.Sender?.Name ?? string.Empty;

			_messageLabel.DataBindings.Clear();
			if (Message is not null)
			{
				var binding = _messageLabel.DataBindings.Add(nameof(_messageLabel.Text), Message.Content, nameof(Message.Content.Content));
				binding.Format += (_, e) =>
				{
					var formattedText = MessageFormatter.Format(e.Value?.ToString() ?? string.Empty);

					// Check for thinking placeholders and extract them
					if (DXThinkingControl.HasThinkPlaceholders(formattedText))
					{
						// Clear existing thinking controls
						_thinkingPanel.Controls.Clear();

						// Create thinking controls
						var thinkingControls = DXThinkingControl.CreateThinkingControls(formattedText).ToList();
						for (int i = 0; i < thinkingControls.Count; i++)
						{
							var control = thinkingControls[i];
							control.Dock = DockStyle.Top;
							_thinkingPanel.Controls.Add(control);

							// Bring to front in reverse order so they appear in correct order
							if (i == 0)
								control.BringToFront();
						}

						// Remove thinking placeholders from the main text
						e.Value = DXThinkingControl.ExtractTextWithoutThinkPlaceholders(formattedText);
					}
					else
					{
						// Clear thinking panel if no thinking placeholders
						_thinkingPanel.Controls.Clear();
						e.Value = formattedText;
					}
				};
			}
		}
	}

	/// <summary>
	/// Gets or sets the maximum size of the control.
	/// When set, also updates the maximum size of the internal labels to account for padding.
	/// </summary>
	/// <value>
	/// The maximum <see cref="Size"/> that this control can occupy.
	/// </value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override Size MaximumSize
	{
		get => base.MaximumSize;
		set
		{
			base.MaximumSize = value;
			_senderLabel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
			_thinkingPanel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
			_messageLabel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
		}
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
