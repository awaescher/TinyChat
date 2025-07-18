using System;
using System.Windows.Forms;
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
	/// Occurs before a message is sent from the text box.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs>? MessageSending;

	private readonly MemoEdit _textBox;

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

		_textBox.KeyPress += TextBox_KeyPress;
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

			var sendArgs = new MessageSendingEventArgs(null! /* we dont know the sender but the ChatControl does */, new StringMessageContent(_textBox.Text));
			MessageSending?.Invoke(this, sendArgs);

			if (!sendArgs.Cancel)
				_textBox.Clear();
		}
	}
}
