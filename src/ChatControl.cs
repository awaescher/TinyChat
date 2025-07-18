using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// A user control that provides a chat interface with message display and text input functionality.
/// </summary>
public partial class ChatControl : UserControl
{
	private Control? _messageHistoryControl;
	private Control? _textBox;

	public event EventHandler<MessageSendingEventArgs> MessageSending;

	public event EventHandler<MessageSentEventArgs> MessageSent;


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

		var splitter = CreateSplitContainerControl();
		var splitterControl = (Control)splitter;
		Controls.Add(splitterControl);
		LayoutSplitContainerControl(splitterControl);

		_messageHistoryControl = (Control)CreateMessageHistoryControl();
		splitter.HistoryPanel.Controls.Add(_messageHistoryControl);
		LayoutMessageHistoryControl(_messageHistoryControl);

		var textBox = CreateChatInputControl();
		textBox.MessageSending += (_, args) => InvokeSendMessage(args);
		_textBox = (Control)textBox;

		splitter.ChatInputPanel.Controls.Add(_textBox);
		LayoutChatInputControl(_textBox);
	}

	/// <summary>
	/// Raises the <see cref="Control.DataContextChanged"/> event and refreshes the displayed messages.
	/// </summary>
	/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		((IChatMessageHistoryControl)_messageHistoryControl)?.ClearMessageControls();

		foreach (var message in DataContext as IEnumerable<IChatMessage> ?? [])
			AppendMessage(message);
	}

	/// <summary>
	/// Appends a chat message to the message container.
	/// </summary>
	/// <param name="message">The chat message to append.</param>
	protected virtual IChatMessageControl AppendMessage(IChatMessage message)
	{
		var messageControl = CreateMessageControl(message);
		messageControl.Message = message;
		var control = (Control)messageControl;
		LayoutMessageControl(_messageHistoryControl, control);
		((IChatMessageHistoryControl)_messageHistoryControl).AppendMessage(messageControl);
		return messageControl;
	}

	protected virtual ISplitContainerControl CreateSplitContainerControl() => new ChatSplitContainerControl();

	protected virtual void LayoutSplitContainerControl(Control splitter)
	{
		splitter.Dock = DockStyle.Fill;
		((ISplitContainerControl)splitter).SplitterPosition = 100;
	}

	/// <summary>
	/// Creates the container control that will hold all chat messages.
	/// </summary>
	/// <returns>A <see cref="Control"/> that serves as the messages container.</returns>
	protected virtual IChatMessageHistoryControl CreateMessageHistoryControl() => new TableLayoutMessageHistoryControl();

	/// <summary>
	/// Creates a message control for displaying a specific chat message.
	/// </summary>
	/// <param name="message">The chat message to create a control for.</param>
	/// <returns>An <see cref="IChatMessageControl"/> instance for the message.</returns>
	protected virtual IChatMessageControl CreateMessageControl(IChatMessage message) => new ChatMessageControl();

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
	protected virtual IChatInputControl CreateChatInputControl() => new ChatInputControl();

	/// <summary>
	/// Applies layout settings to the text input control.
	/// </summary>
	/// <param name="textBox">The text box control to layout.</param>
	protected virtual void LayoutChatInputControl(Control textBox) => textBox.Dock = DockStyle.Fill;

	/// <summary>
	/// Invokes the events before and after a message is sent
	/// </summary>
	/// <param name="content">The content of the message to send.</param>
	protected virtual void InvokeSendMessage(MessageSendingEventArgs args)
	{
		var sender = args.Sender ?? Sender;

		MessageSending?.Invoke(this, args);

		if (!args.Cancel)
		{
			AppendMessage(CreateChatMessage(sender, args.Content));
			MessageSent?.Invoke(this, new MessageSentEventArgs(sender, args.Content));
		}
	}

	/// <summary>
	/// Creates a new chat message instance.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The content of the message.</param>
	/// <returns>A new <see cref="IChatMessage"/> instance.</returns>
	protected virtual IChatMessage CreateChatMessage(ISender sender, IChatMessageContent content) => new ChatMessage(sender, content);

	/// <summary>
	/// Adds a chat message to the message history control.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The content of the message.</param>
	/// <returns></returns>
	public IChatMessageControl AddMessage(ISender sender, IChatMessageContent content)
	{
		var message = CreateChatMessage(sender, content);
		return AppendMessage(message);
	}

	/// <summary>
	/// Adds a chat message with with support of streaming input, like when an AI assistant is streaming tokens
	/// </summary>
	/// <param name="sender">The sender of the streaming message.</param>
	/// <param name="stream">The stream of the tokens.</param>
	/// <param name="synchronizationContext">An optional synchronization context. Only required if the applications does not provide a default synchronization context.</param>
	/// <param name="cancellationToken">The token to cancel the operation with.</param>
	/// <returns></returns>
	public IChatMessageControl AddStreamingMessage(ISender sender, IAsyncEnumerable<string> stream, SynchronizationContext? synchronizationContext = default, CancellationToken cancellationToken = default)
	{
		var stringBuilder = new NotifyingStringBuilder();
		var content = new ChangingMessageContent(stringBuilder);
		var message = CreateChatMessage(sender, content);

		var context = (synchronizationContext ?? SynchronizationContext.Current) ?? throw new InvalidOperationException("No synchronization context available. Please make sure a the default SynchronizationContext is available or pass in an SynchronizationContext as argument!");

		// loop through the stream in a background thread and append the chunks to the string builder
		context.Post(async (_) =>
		{
			await foreach (var chunk in stream.ConfigureAwait(true).WithCancellation(cancellationToken))
				stringBuilder.Append(chunk);
		}, state: null);

		return AppendMessage(message);
	}

	/// <summary>
	/// Gets or sets the sender for messages sent from this chat control.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ISender Sender { get; set; } = new NamedSender(Environment.UserName);
}

/// <summary>
/// Provides data and cancellation support for the event before a message gets sent.
/// </summary>
public class MessageSendingEventArgs : CancelEventArgs
{
	/// <summary>
	/// Gets the message content being sent.
	/// </summary>
	public IChatMessageContent Content { get; }

	/// <summary>
	/// Gets the sender of the message.
	/// </summary>
	public ISender Sender { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSendEventArgs"/> class.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The message content being sent.</param>
	public MessageSendingEventArgs(ISender sender, IChatMessageContent content)
	{
		Content = content;
		Sender = sender;
	}
}

/// <summary>
/// Provides data for the event when a message was sent
/// </summary>
public class MessageSentEventArgs : EventArgs
{
	/// <summary>
	/// Gets the message content being sent.
	/// </summary>
	public IChatMessageContent Content { get; }

	/// <summary>
	/// Gets the sender of the message.
	/// </summary>
	public ISender Sender { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSendEventArgs"/> class.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The message content being sent.</param>
	public MessageSentEventArgs(ISender sender, IChatMessageContent content)
	{
		Content = content;
		Sender = sender;
	}
}