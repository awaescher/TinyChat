using System.ComponentModel;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// A user control that provides a chat interface with message display and text input functionality.
/// </summary>
public partial class ChatControl : UserControl
{
	private const string ROBOT_WELCOME = "●\n┌─┴─┐\n◉‿◉\n└───┘\n\nGreetings human.\nHow can I help you today?";

	private List<IChatMessage> _messages = [];

	/// <summary>
	/// Occurs when a message is sent from the text box and allows the cancellation of sending.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs>? MessageSending;

	/// <summary>
	/// Occurs when a message has been sent from the user interface.
	/// </summary>
	public event EventHandler<MessageSentEventArgs>? MessageSent;
	/// <summary>
	/// Gets the control that manages and displays the chat message history.
	/// </summary>
	/// <value>
	/// The control responsible for displaying chat messages, or <see langword="null"/> if not initialized.
	/// </value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Control? MessageHistoryControl { get; private set; }
	/// <summary>
	/// Gets the control that displays the welcome message when no chat messages are present.
	/// </summary>
	/// <value>
	/// The welcome message control, or <see langword="null"/> if not initialized.
	/// </value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Control? WelcomeControl { get; private set; }
	/// <summary>
	/// Gets the control that provides the chat input interface for sending messages.
	/// </summary>
	/// <value>
	/// The input control for entering and sending chat messages, or <see langword="null"/> if not initialized.
	/// </value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Control? InputControl { get; private set; }
	/// <summary>
	/// Gets the split container control that divides the chat history panel from the input panel.
	/// </summary>
	/// <value>
	/// The split container control managing the layout of history and input areas, or <see langword="null"/> if not initialized.
	/// </value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Control? SplitContainerControl { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatControl"/> class.
	/// </summary>
	public ChatControl() => InitializeComponent();

	/// <summary>
	/// Gets or sets the message history displayed in the chat control.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IEnumerable<IChatMessage> Messages
	{
		get => _messages.AsReadOnly();
		set
		{
			_messages = value is null ? [] : [.. value];
			PopulateMessages();
		}
	}

	/// <summary>
	/// Gets or sets the welcome message displayed when no messages are present in the chat history.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets the welcome message displayed when no messages are present in the chat history.")]
	[DefaultValue(ROBOT_WELCOME)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public string WelcomeMessage { get; set; } = ROBOT_WELCOME;

	/// <summary>
	/// Gets or sets the splitter position dividing the chat message history from the chat input box below.
	/// </summary>
	[Category("Chat")]
	[DefaultValue(60)]
	[Description("Gets or sets the splitter position dividing the chat message history from the chat input box below.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public int SplitterPosition
	{
		get => (SplitContainerControl as ISplitContainerControl)?.SplitterPosition ?? 0;
		set
		{
			if (SplitContainerControl is ISplitContainerControl splitContainer)
				splitContainer.SplitterPosition = value;
		}
	}

	/// <summary>
	/// Gets or sets the sender for messages sent from this chat control.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ISender Sender { get; set; } = new NamedSender(Environment.UserName);

	/// <summary>
	/// Gets or sets the formatter that converts message content into displayable strings.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IMessageFormatter MessageFormatter { get; set; } = new PlainTextMessageFormatter();

	/// <summary>
	/// Updates the visibility of the welcome control based on the current message history.
	/// </summary>
	protected virtual void UpdateWelcomeControlVisibility()
	{
		if (WelcomeControl is not null)
			WelcomeControl.Visible = ShouldShowWelcomeControl();
	}

	/// <summary>
	/// Determines whether the welcome control should be displayed based on the current message history.
	/// </summary>
	protected virtual bool ShouldShowWelcomeControl() => _messages.Count == 0;


	/// <inheritdoc/>
	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);

		MessageFormatter = CreateDefaultMessageFormatter() ?? MessageFormatter;
	}

	/// <summary>
	/// Creates the message formatter that is used to display chat messages contents in the chat user interface
	/// </summary>
	/// <returns></returns>
	protected virtual IMessageFormatter? CreateDefaultMessageFormatter() => null;

	/// <summary>
	/// Adds a chat message to the message history control.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The content of the message.</param>
	/// <returns></returns>
	public virtual IChatMessageControl AddMessage(ISender sender, IChatMessageContent content)
	{
		var message = AddChatMessage(sender, content);
		UpdateWelcomeControlVisibility();
		return AppendMessageControl(message);
	}

	/// <summary>
	/// Adds a chat message with with support of streaming input, like when an AI assistant is streaming tokens
	/// </summary>
	/// <param name="sender">The sender of the streaming message.</param>
	/// <param name="stream">The stream of the tokens.</param>
	/// <param name="completionCallback">An optional callback that can be used to process the streamed messages after it was received completely.</param>
	/// <param name="exceptionCallback">An optional callback that can be used to process exceptions that occured during the processing of the stream.</param>
	/// <param name="synchronizationContext">An optional synchronization context. Only required if the applications does not provide a default synchronization context.</param>
	/// <param name="cancellationToken">The token to cancel the operation with.</param>
	/// <returns></returns>
	public virtual IChatMessageControl AddStreamingMessage(
		ISender sender,
		IAsyncEnumerable<string> stream,
		SynchronizationContext? synchronizationContext = default,
		Action<string>? completionCallback = default,
		Action<Exception>? exceptionCallback = default,
		CancellationToken cancellationToken = default)
	{
		var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		void InputControl_CancellationRequested(object? sender, EventArgs e) => cancellationSource.Cancel();

		var stringBuilder = new NotifyingStringBuilder();
		var content = new ChangingMessageContent(stringBuilder);
		var message = AddChatMessage(sender, content);

		var context = (synchronizationContext ?? SynchronizationContext.Current) ?? throw new InvalidOperationException("No synchronization context available. Please make sure a the default SynchronizationContext is available or pass in an SynchronizationContext as argument!");

		UpdateWelcomeControlVisibility();
		var messageControl = AppendMessageControl(message);

		var inputControl = InputControl as IChatInputControl;

		// loop through the stream in a background thread and append the chunks to the string builder
		context.Post(async (_) =>
		{
			try
			{
				if (inputControl != null)
					inputControl.CancellationRequested += InputControl_CancellationRequested;

				messageControl.SetIsReceivingStream(true);
				inputControl?.SetIsReceivingStream(true, allowCancellation: cancellationToken.CanBeCanceled);

				await foreach (var chunk in stream.ConfigureAwait(true).WithCancellation(cancellationSource.Token))
				{
					stringBuilder.Append(chunk);

					// leave the chat if cancellation was requested, the stream might or might not support cancellation.
					if (cancellationToken.IsCancellationRequested)
						break;
				}
			}
			catch (Exception ex)
			{
				exceptionCallback?.Invoke(ex);
				if (!cancellationSource.Token.IsCancellationRequested)
					throw;
			}
			finally
			{
				inputControl?.SetIsReceivingStream(false, allowCancellation: false);
				messageControl.SetIsReceivingStream(false);

				if (inputControl != null)
					inputControl.CancellationRequested -= InputControl_CancellationRequested;
			}

			completionCallback?.Invoke(stringBuilder.ToString());
		}, state: null);

		return messageControl;
	}

	/// <summary>
	/// Removes a given message from the chat
	/// </summary>
	/// <param name="message"></param>
	public virtual void RemoveMessage(IChatMessage message)
	{
		_messages.Remove(message);

		if (MessageHistoryControl is IChatMessageHistoryControl casted)
			casted.RemoveMessageControl(message);

		UpdateWelcomeControlVisibility();
	}

	/// <summary>
	/// Raises the <see cref="Control.CreateControl"/> event and initializes the chat control layout.
	/// </summary>
	protected override void OnCreateControl()
	{
		base.OnCreateControl();

		var splitContainer = CreateSplitContainerControl();
		SplitContainerControl = (Control)splitContainer;
		Controls.Add(SplitContainerControl);
		LayoutSplitContainerControl(SplitContainerControl);

		MessageHistoryControl = (Control)CreateMessageHistoryControl();
		splitContainer?.HistoryPanel?.Controls.Add(MessageHistoryControl);
		LayoutMessageHistoryControl(MessageHistoryControl);

		WelcomeControl = CreateWelcomeControl();
		splitContainer?.HistoryPanel?.Controls.Add(WelcomeControl);
		LayoutWelcomeControl(WelcomeControl);

		var inputControl = CreateChatInputControl();
		inputControl.MessageSending += (_, e) => SendMessage(e);
		InputControl = (Control)inputControl;

		splitContainer?.ChatInputPanel?.Controls.Add(InputControl);
		LayoutChatInputControl(InputControl);

		PopulateMessages();
	}

	/// <summary>
	/// Adds the messages to the controls
	/// </summary>
	private void PopulateMessages()
	{
		if (MessageHistoryControl is IChatMessageHistoryControl casted)
			casted.ClearMessageControls();

		foreach (var message in _messages)
			AppendMessageControl(message);

		UpdateWelcomeControlVisibility();
	}

	/// <summary>
	/// Appends a chat message to the message container.
	/// </summary>
	/// <param name="message">The chat message to append.</param>
	protected virtual IChatMessageControl AppendMessageControl(IChatMessage message)
	{
		var messageControl = CreateMessageControl(message);
		messageControl.Message = message;
		var control = (Control)messageControl;

		if (MessageHistoryControl is IChatMessageHistoryControl casted)
		{
			LayoutMessageControl(MessageHistoryControl, control);
			casted.AppendMessageControl(messageControl);
		}

		return messageControl;
	}

	/// <summary>
	/// Creates the container control that will hold all chat messages.
	/// </summary>
	/// <returns>A <see cref="Control"/> that serves as the messages container.</returns>
	protected virtual IChatMessageHistoryControl CreateMessageHistoryControl() => new FlowLayoutMessageHistoryControl();

	/// <summary>
	/// Applies layout settings to the messages container control.
	/// </summary>
	/// <param name="control">The control to layout.</param>
	protected virtual void LayoutMessageHistoryControl(Control control)
	{
		control.Dock = DockStyle.Fill;
	}

	/// <summary>
	/// Creates the container control that will hold all chat messages.
	/// </summary>
	/// <returns>A <see cref="Control"/> that serves as the messages container.</returns>
	protected virtual Control CreateWelcomeControl()
	{
		var label = new Label { Text = WelcomeMessage, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font("Tahoma", 14f), UseMnemonic = false };
		var panel = new Panel();
		panel.Controls.Add(label);
		return panel;
	}

	/// <summary>
	/// Applies layout settings to the messages container control.
	/// </summary>
	/// <param name="control">The control to layout.</param>
	protected virtual void LayoutWelcomeControl(Control control)
	{
		control.Dock = DockStyle.Fill;
		control.BringToFront();
	}

	/// <summary>
	/// Creates a message control for displaying a specific chat message.
	/// </summary>
	/// <param name="message">The chat message to create a control for.</param>
	/// <returns>An <see cref="IChatMessageControl"/> instance for the message.</returns>
	protected virtual IChatMessageControl CreateMessageControl(IChatMessage message) => new ChatMessageControl() { MessageFormatter = MessageFormatter, Message = message };

	/// <summary>
	/// Applies layout settings to a chat message control and adds it to the container.
	/// </summary>
	/// <param name="container">The container to add the message control to.</param>
	/// <param name="chatMessageControl">The chat message control to layout and add.</param>
	protected virtual void LayoutMessageControl(Control container, Control chatMessageControl)
	{
		chatMessageControl.Dock = DockStyle.Fill;
	}

	/// <summary>
	/// Creates the split container control that holds the message history and input controls.
	/// </summary>
	/// <returns></returns>
	protected virtual ISplitContainerControl CreateSplitContainerControl() => new ChatSplitContainerControl();

	/// <summary>
	/// Applies layout settings to the split container control.
	/// </summary>
	/// <param name="splitter"></param>
	protected virtual void LayoutSplitContainerControl(Control splitter)
	{
		splitter.Dock = DockStyle.Fill;
		((ISplitContainerControl)splitter).SplitterPosition = 60;
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
	/// Sends a message from the current sender with the specified text content.
	/// </summary>
	/// <param name="message">The text content of the message to send.</param>
	/// <returns>
	/// <see langword="true"/> if the message was sent successfully; 
	/// <see langword="false"/> if the message sending was cancelled.
	/// </returns>
	/// <remarks>
	/// This method creates a <see cref="StringMessageContent"/> wrapper around the provided text
	/// and uses the control's default <see cref="Sender"/> property for the message sender.
	/// The message sending can be cancelled by handling the <see cref="MessageSending"/> event
	/// and setting the MessageSendingEventArgs.Cancel property to <see langword="true"/>.
	/// </remarks>
	public bool SendMessage(string message)
	{
		var args = new MessageSendingEventArgs(Sender, new StringMessageContent(message));
		SendMessage(args);
		return !args.Cancel;
	}

	/// <summary>
	/// Sends a message from the specified sender with the given content.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The content of the message to send.</param>
	/// <returns>
	/// <see langword="true"/> if the message was sent successfully; 
	/// <see langword="false"/> if the message sending was cancelled.
	/// </returns>
	/// <remarks>
	/// This method allows specifying both the sender and content of the message explicitly.
	/// The message sending can be cancelled by handling the <see cref="MessageSending"/> event
	/// and setting the MessageSendingEventArgs.Cancel property to <see langword="true"/>.
	/// </remarks>
	public bool SendMessage(ISender sender, IChatMessageContent content)
	{
		var args = new MessageSendingEventArgs(sender, content);
		SendMessage(args);
		return !args.Cancel;
	}

	/// <summary>
	/// Sends a message using the provided event arguments, handling the complete message sending workflow.
	/// </summary>
	/// <param name="e">The event arguments containing the sender, content, and cancellation state.</param>
	/// <remarks>
	/// This method orchestrates the complete message sending process:
	/// <list type="number">
	/// <item><description>Determines the effective sender (uses <paramref name="e"/>.Sender if provided, otherwise falls back to the control's <see cref="Sender"/>)</description></item>
	/// <item><description>Raises the <see cref="MessageSending"/> event to allow subscribers to inspect or cancel the operation</description></item>
	/// <item><description>If not cancelled, adds the message to the chat history and displays it</description></item>
	/// <item><description>Raises the <see cref="MessageSent"/> event to notify subscribers that the message was successfully sent</description></item>
	/// </list>
	/// The message sending can be cancelled by setting the MessageSendingEventArgs.Cancel property to <see langword="true"/> 
	/// in the <see cref="MessageSending"/> event handler.
	/// </remarks>
	public virtual void SendMessage(MessageSendingEventArgs e)
	{
		var sender = e.Sender ?? Sender;

		MessageSending?.Invoke(this, e);

		if (!e.Cancel)
		{
			AppendMessageControl(AddChatMessage(sender, e.Content));
			MessageSent?.Invoke(this, new MessageSentEventArgs(sender, e.Content));
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
	/// Creates a new chat message and adds it to the message history.
	/// </summary>
	/// <param name="sender">The sender of the message.</param>
	/// <param name="content">The content of the message.</param>
	/// <returns>A new <see cref="IChatMessage"/> instance.</returns>
	protected virtual IChatMessage AddChatMessage(ISender sender, IChatMessageContent content)
	{
		var message = CreateChatMessage(sender, content);
		_messages.Add(message);
		return message;
	}
}