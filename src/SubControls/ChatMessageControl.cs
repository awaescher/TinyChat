using System.Collections.Generic;
using System.ComponentModel;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// A panel control that displays a chat message with sender name and content.
/// </summary>
public class ChatMessageControl : Panel, IChatMessageControl
{
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private readonly Label _senderLabel;
	private readonly TableLayoutPanel _messageContentPanel;
	private readonly List<ThinkSectionControl> _thinkSectionControls = [];
	private IChatMessageContent? _boundContent;
	private IMessageFormatter? _messageFormatter;
	private int _contentMaxWidth;

	/// <summary>
	/// The event that is raised when the size of the control is updated while streaming a message.
	/// </summary>
	public event EventHandler? SizeUpdatedWhileStreaming;

	/// <summary>
	/// Gets or sets the formatter that converts message content into displayable strings.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public required IMessageFormatter MessageFormatter
	{
		get => _messageFormatter ?? throw new InvalidOperationException("Message formatter not initialized.");
		set
		{
			_messageFormatter = value ?? throw new ArgumentNullException(nameof(value));
			RenderMessageContent();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatMessageControl"/> class.
	/// </summary>
	public ChatMessageControl()
	{
		_senderLabel = new Label() { Dock = DockStyle.Top, AutoSize = true, Font = new Font(Font, FontStyle.Bold), UseMnemonic = false };
		_messageContentPanel = new TableLayoutPanel()
		{
			Dock = DockStyle.Fill,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			ColumnCount = 1,
			RowCount = 0,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		Controls.Add(_senderLabel);
		Controls.Add(_messageContentPanel);

		_messageContentPanel.BringToFront();

		AutoSize = true;
		Padding = new Padding(8);
	}

	/// <summary>
	/// Gets or sets the chat message displayed by this control.
	/// When set, the control updates to display the sender's name and message content.
	/// If the message is null, both the sender and content labels will display empty strings.
	/// </summary>
	/// <value>
	/// The <see cref="IChatMessage"/> instance to display, or null to clear the display.
	/// </value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			if (_message == value)
				return;

			DetachFromContent();
			_message = value;
			_senderLabel.Text = _message?.Sender?.Name ?? string.Empty;
			AttachToContent(_message?.Content);
		}
	}

	/// <inheritdoc />
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override Size MaximumSize
	{
		get => base.MaximumSize;
		set
		{
			base.MaximumSize = value;
			var contentWidth = Math.Max(0, value.Width - Padding.Horizontal);
			_senderLabel.MaximumSize = new Size(contentWidth, 0);
			_contentMaxWidth = contentWidth;
			_messageContentPanel.MaximumSize = new Size(contentWidth, 0);
			UpdateContentWidths();
		}
	}

	/// <inheritdoc />
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);

		if (_isReceivingStream)
			SizeUpdatedWhileStreaming?.Invoke(this, EventArgs.Empty);
	}

	private void AttachToContent(IChatMessageContent? content)
	{
		_boundContent = content;
		if (_boundContent is not null)
			_boundContent.PropertyChanged += OnContentChanged;

		RenderMessageContent();
	}

	private void DetachFromContent()
	{
		if (_boundContent is not null)
			_boundContent.PropertyChanged -= OnContentChanged;

		_boundContent = null;
	}

	private void OnContentChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (!IsHandleCreated || IsDisposed)
			return;

		if (InvokeRequired)
		{
			try
			{
				BeginInvoke(new MethodInvoker(RenderMessageContent));
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			return;
		}

		RenderMessageContent();
	}

	private void RenderMessageContent()
	{
		if (_messageFormatter is null)
			return;

		var rawContent = ExtractRawContent();
		var segments = ThinkTagParser.Split(rawContent);

		_messageContentPanel.SuspendLayout();
		_messageContentPanel.Controls.Clear();
		_thinkSectionControls.Clear();

		if (segments.Count == 0)
		{
			AddTextSegment(rawContent);
		}
		else
		{
			foreach (var segment in segments)
			{
				if (segment.IsThinkSegment)
					AddThinkSegment(segment.Content);
				else
					AddTextSegment(segment.Content);
			}
		}

		_messageContentPanel.ResumeLayout();
		UpdateContentWidths();
	}

	private void UpdateContentWidths()
	{
		if (_contentMaxWidth <= 0)
			return;

		foreach (Control control in _messageContentPanel.Controls)
		{
			control.MaximumSize = new Size(_contentMaxWidth, 0);
			if (control is ThinkSectionControl thinkSection)
				thinkSection.UpdateMaxWidth(_contentMaxWidth);
		}
	}

	private void AddTextSegment(string rawSegment)
	{
		if (_messageFormatter is null)
			return;

		var formatted = _messageFormatter.Format(rawSegment ?? string.Empty);
		if (string.IsNullOrEmpty(formatted))
			return;

		var label = new Label()
		{
			AutoSize = true,
			UseMnemonic = false,
			Margin = new Padding(0, _messageContentPanel.Controls.Count > 0 ? 4 : 0, 0, 0),
			MaximumSize = new Size(_contentMaxWidth, 0),
			Text = formatted
		};

		_messageContentPanel.Controls.Add(label);
	}

	private void AddThinkSegment(string rawSegment)
	{
		if (_messageFormatter is null)
			return;

		var formatted = _messageFormatter.Format(rawSegment ?? string.Empty);
		var thinkControl = new ThinkSectionControl(formatted)
		{
			Margin = new Padding(0, _messageContentPanel.Controls.Count > 0 ? 4 : 0, 0, 0)
		};

		_thinkSectionControls.Add(thinkControl);
		_messageContentPanel.Controls.Add(thinkControl);
	}

	private string ExtractRawContent()
	{
		if (_boundContent is null)
			return string.Empty;

		var source = _boundContent.Content;
		return source?.ToString() ?? _boundContent.ToString() ?? string.Empty;
	}

	/// <inheritdoc />
	void IChatMessageControl.SetIsReceivingStream(bool isReceiving)
	{
		_isReceivingStream = isReceiving;
	}

	private sealed class ThinkSectionControl : Panel
	{
		private readonly Label _toggleLabel;
		private readonly Label _contentLabel;
		private bool _isExpanded;

		public ThinkSectionControl(string formattedContent)
		{
			AutoSize = true;
			AutoSizeMode = AutoSizeMode.GrowAndShrink;
			Padding = new Padding(8);
			Margin = Padding.Empty;
			BackColor = SystemColors.ControlLightLight;
			BorderStyle = BorderStyle.FixedSingle;
			Cursor = Cursors.Hand;

			_toggleLabel = new Label()
			{
				AutoSize = true,
				UseMnemonic = false,
				Font = new Font(SystemFonts.DefaultFont, FontStyle.Italic),
				ForeColor = SystemColors.GrayText,
				Text = "Thinking ... (click to expand)"
			};

			_contentLabel = new Label()
			{
				AutoSize = true,
				UseMnemonic = false,
				Visible = false,
				Text = formattedContent,
				Margin = new Padding(0, 6, 0, 0)
			};

			Controls.Add(_contentLabel);
			Controls.Add(_toggleLabel);

			_toggleLabel.Click += Toggle;
			_contentLabel.Click += Toggle;
			Click += Toggle;
		}

		public void UpdateMaxWidth(int width)
		{
			var usableWidth = Math.Max(0, width - Padding.Horizontal);
			MaximumSize = new Size(width, 0);
			_toggleLabel.MaximumSize = new Size(usableWidth, 0);
			_contentLabel.MaximumSize = new Size(usableWidth, 0);
		}

		private void Toggle(object? sender, EventArgs e)
		{
			_isExpanded = !_isExpanded;
			_contentLabel.Visible = _isExpanded;
			_toggleLabel.Text = _isExpanded ? "Thinking (click to collapse)" : "Thinking ... (click to expand)";
		}
	}
}
