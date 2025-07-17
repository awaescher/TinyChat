using System.ComponentModel;
using WinFormsChat.Controls;
using WinFormsChat.Messages;

namespace WinFormsChat;

/// <summary>
/// A user control that provides a chat interface with message display and text input functionality.
/// </summary>
public partial class ChatControl : UserControl
{
	private Control? _messageHistoryControl;
	private Control? _textBox;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatControl"/> class.
	/// </summary>
	public ChatControl()
	{
		InitializeComponent();
	}

	/// <summary>
	/// Raises the <see cref="Control.CreateControl"/> event and initializes the chat control layout.
	/// </summary>
	protected override void OnCreateControl()
	{
		base.OnCreateControl();

		_messageHistoryControl = (Control)CreateMessageHistoryControl();
		Controls.Add(_messageHistoryControl);
		LayoutMessageHistoryControl(_messageHistoryControl);

		var textBox = CreateTextBox();
		textBox.Send += (sender, content) => SendMessage(content);
		_textBox = (Control)textBox;

		Controls.Add(_textBox);
		LayoutTextBox(_textBox);
	}

	/// <summary>
	/// Raises the <see cref="Control.DataContextChanged"/> event and refreshes the displayed messages.
	/// </summary>
	/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		_messageHistoryControl?.Controls.Clear();

		foreach (var message in DataContext as IEnumerable<IChatMessage> ?? [])
			AppendMessage(message);
	}

	/// <summary>
	/// Appends a chat message to the message container.
	/// </summary>
	/// <param name="message">The chat message to append.</param>
	protected virtual void AppendMessage(IChatMessage message)
	{
		var messageControl = CreateMessageControl(message);
		messageControl.Message = message;
		LayoutMessageControl(_messageHistoryControl, (Control)messageControl);
		((IChatMessageHistoryControl)_messageHistoryControl).AppendMessage(messageControl);
	}

	/// <summary>
	/// Creates the container control that will hold all chat messages.
	/// </summary>
	/// <returns>A <see cref="Control"/> that serves as the messages container.</returns>
	protected virtual IChatMessageHistoryControl CreateMessageHistoryControl() => new TableLayoutMessageHistoryControl { Visible = true };

	/// <summary>
	/// Creates a message control for displaying a specific chat message.
	/// </summary>
	/// <param name="message">The chat message to create a control for.</param>
	/// <returns>An <see cref="IChatMessageControl"/> instance for the message.</returns>
	protected virtual IChatMessageControl CreateMessageControl(IChatMessage message) => new ChatMessageControl() { Visible = true };

	/// <summary>
	/// Applies layout settings to the messages container control.
	/// </summary>
	/// <param name="container">The container control to layout.</param>
	protected virtual void LayoutMessageHistoryControl(Control container)
	{
		container.Dock = DockStyle.Fill;
	}

	/// <summary>
	/// Applies layout settings to a chat message control and adds it to the container.
	/// </summary>
	/// <param name="container">The container to add the message control to.</param>
	/// <param name="chatMessageControl">The chat message control to layout and add.</param>
	protected virtual void LayoutMessageControl(Control? container, Control? chatMessageControl)
	{
		chatMessageControl.Dock = DockStyle.Fill;
	}

	/// <summary>
	/// Creates the text input control for sending new messages.
	/// </summary>
	/// <returns>An <see cref="IChatInputControl"/> instance for message input.</returns>
	protected virtual IChatInputControl CreateTextBox() => new ChatInputControl { Visible = true };

	/// <summary>
	/// Applies layout settings to the text input control.
	/// </summary>
	/// <param name="textBox">The text box control to layout.</param>
	protected virtual void LayoutTextBox(Control textBox) => textBox.Dock = DockStyle.Bottom;

	/// <summary>
	/// Sends a new message by appending it to the chat.
	/// </summary>
	/// <param name="content">The content of the message to send.</param>
	protected virtual void SendMessage(IChatMessageContent content)
	{
		AppendMessage(CreateChatMessage(Sender, content));
	}

	/// <summary>
	/// Creates a new chat message instance.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The content of the message.</param>
	/// <returns>A new <see cref="IChatMessage"/> instance.</returns>
	protected virtual IChatMessage CreateChatMessage(ISender sender, IChatMessageContent content) => new ChatMessage(sender, content);

	/// <summary>
	/// Gets or sets the sender for messages sent from this chat control.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ISender Sender { get; set; } = new NamedSender(Environment.UserName);
}
