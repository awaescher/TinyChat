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
	private readonly List<DXCollapsibleThinkPanel> _thinkPanels = [];
	private readonly PanelControl _contentContainer;

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
		_messageLabel = new LabelControl() { AllowHtmlString = true, Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical, UseMnemonic = false };

		_contentContainer = new PanelControl() 
		{ 
			Dock = DockStyle.Fill, 
			AutoSize = true,
			BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
		};
		_contentContainer.Controls.Add(_messageLabel);

		Controls.Add(_senderLabel);
		Controls.Add(_contentContainer);

		_contentContainer.BringToFront();

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
			ClearThinkPanels();
			
			if (Message is not null)
			{
				// Subscribe to content changes to detect think tags
				if (Message.Content is INotifyPropertyChanged notifyPropertyChanged)
				{
					notifyPropertyChanged.PropertyChanged += OnContentPropertyChanged;
				}
				
				// Initial update
				UpdateContentWithThinkTags(Message.Content?.Content?.ToString() ?? string.Empty);
			}
		}
	}

	private void OnContentPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (Message?.Content?.Content is string content)
		{
			UpdateContentWithThinkTags(content);
		}
	}

	private void UpdateContentWithThinkTags(string rawContent)
	{
		var extraction = ThinkTagHelper.ExtractThinkSections(rawContent);
		
		// Clear existing think panels
		ClearThinkPanels();
		
		// Update the message label with content without think tags
		var formattedContent = MessageFormatter.Format(extraction.ContentWithoutThink);
		_messageLabel.Text = formattedContent;
		
		// Add think panels for each think section
		if (extraction.ThinkSections.Count > 0)
		{
			_contentContainer.SuspendLayout();
			
			// Insert think panels at the top (before the message)
			foreach (var section in extraction.ThinkSections.Reverse())
			{
				var thinkPanel = new DXCollapsibleThinkPanel
				{
					ThinkContent = section.Content,
					Dock = DockStyle.Top,
					MaximumSize = new Size(Math.Max(0, MaximumSize.Width - Padding.Horizontal), 0)
				};
				thinkPanel.ExpandedChanged += (_, _) => 
				{
					if (_isReceivingStream)
						SizeUpdatedWhileStreaming?.Invoke(this, EventArgs.Empty);
				};
				
				_thinkPanels.Add(thinkPanel);
				_contentContainer.Controls.Add(thinkPanel);
				thinkPanel.BringToFront();
			}
			
			// Make sure message label is at the bottom
			_messageLabel.SendToBack();
			
			_contentContainer.ResumeLayout();
		}
	}

	private void ClearThinkPanels()
	{
		foreach (var panel in _thinkPanels)
		{
			_contentContainer.Controls.Remove(panel);
			panel.Dispose();
		}
		_thinkPanels.Clear();
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
			_messageLabel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
			
			foreach (var thinkPanel in _thinkPanels)
			{
				thinkPanel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
			}
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
