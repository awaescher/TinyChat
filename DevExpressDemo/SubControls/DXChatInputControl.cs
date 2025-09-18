using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;
using TinyChat;

namespace DevExpressDemo;

/// <summary>
/// Provides a DevExpress-based chat input control that allows users to enter and send messages.
/// Implements the <see cref="IChatInputControl"/> interface for chat input functionality.
/// </summary>
public class DXChatInputControl : Control, IChatInputControl
{
	/// <summary>
	/// Gets the SVG code for the send icon (taken and minimized from the DevExpress SVG library)
	/// </summary>
	private const string SEND_SVG = """
		<svg viewBox="0 0 32 32">
			<style type="text/css">.Black{fill:#727272;}</style>
			<path d="M16,2C8.3,2,2,8.3,2,16s6.3,14,14,14s14-6.3,14-14S23.7,2,16,2z M18,24h-4v-8H8l8-8l8,8h-6V24z" class="Black" />
		</svg>
		""";

	/// <summary>
	/// Gets the SVG code for the stop icon (taken and minimized from the DevExpress SVG library)
	/// </summary>s
	private const string STOP_SVG = """
		<svg viewBox="0 0 32 32">
			<style type="text/css">.Black{fill:#727272;}</style>
			<path d="M25,26H7c-0.6,0-1-0.5-1-1V7c0-0.6,0.4-1,1-1h18c0.5,0,1,0.4,1,1v18C26,25.5,25.5,26,25,26z" class="Black" />
		</svg>
		""";

	/// <summary>
	/// Occurs before a message is sent from the text box.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs>? MessageSending;

	/// <summary>
	/// The event that is raised when cancellation of a streaming message is requested.
	/// </summary>
	public event EventHandler? CancellationRequested;

	private readonly MemoEdit _textBox;
	private readonly SimpleButton _sendButton;
	private bool _isReceivingStream;

	private SvgImage SendImage { get; } = SvgImage.FromStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(SEND_SVG)));

	private SvgImage StopImage { get; } = SvgImage.FromStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(STOP_SVG)));

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInputControl"/> class.
	/// </summary>
	public DXChatInputControl()
	{
		_textBox = new MemoEdit { Visible = true, Dock = DockStyle.Fill };
		_textBox.Properties.ScrollBars = ScrollBars.None;
		var panel = new PanelControl { Padding = new Padding(8), Dock = DockStyle.Fill, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
		Controls.Add(panel);
		panel.Controls.Add(_textBox);

		var size = new Size(24, 24);
		_sendButton = new SimpleButton { MaximumSize = size, MinimumSize = size, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
		_sendButton.ImageOptions.SvgImage = SendImage;
		_sendButton.ImageOptions.SvgImageSize = new Size(18, 18);
		_sendButton.ImageOptions.ImageToTextAlignment = ImageAlignToText.TopCenter;
		_sendButton.Left = ClientRectangle.Width - _sendButton.Width - panel.Padding.Right / 2 * 3;
		_sendButton.Top = ClientRectangle.Height - _sendButton.Height - panel.Padding.Bottom / 2 * 3;
		Controls.Add(_sendButton);
		_sendButton.BringToFront();

		_sendButton.Click += (s, e) => SendOrStop();
		_textBox.KeyPress += TextBox_KeyPress;
	}

	/// <inheritdoc />
	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		_textBox.Focus();
	}

	/// <summary>
	/// Handles the KeyPress event of the internal text box to send messages on Enter key.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="KeyPressEventArgs"/> that contains the event data.</param>
	private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
	{
		if (e.KeyChar == (char)Keys.Enter)
		{
			e.Handled = true;
			_sendButton.PerformClick();
		}
	}

	private void SendOrStop()
	{
		if (_isReceivingStream)
			Stop();
		else
			Send();
	}

	private void Send()
	{
		var sendArgs = new MessageSendingEventArgs(null! /* we dont know the sender but the ChatControl does */, new StringMessageContent(_textBox.Text));
		MessageSending?.Invoke(this, sendArgs);

		if (!sendArgs.Cancel)
			_textBox.Clear();
	}

	private void Stop()
	{
		if (_sendButton.Enabled)
			CancellationRequested?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	void IChatInputControl.SetIsReceivingStream(bool isReceiving, bool allowCancellation)
	{
		_isReceivingStream = isReceiving;
		BeginInvoke(() =>
		{
			_sendButton.ImageOptions.SvgImage = isReceiving && allowCancellation ? StopImage : SendImage;
			_sendButton.Enabled = !isReceiving || allowCancellation;
		});
	}
}
