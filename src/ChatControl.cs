using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
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
	/// Occurs before a request is sent to the <see cref="IChatClient"/>, allowing the developer to define or modify <see cref="Microsoft.Extensions.AI.ChatOptions"/>.
	/// </summary>
	public event EventHandler<ChatOptionsRequestedEventArgs>? ChatOptionsRequested;
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
	public ChatControl()
	{
		InitializeComponent();
	}

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
	/// Gets or sets the service provider used to resolve the <see cref="IChatClient"/> instance.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IServiceProvider? ServiceProvider { get; set; }

	/// <summary>
	/// Gets or sets the service key used to resolve a keyed <see cref="IChatClient"/> registration.
	/// When null, the default <see cref="IChatClient"/> registration is used.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets the service key used to resolve a keyed IChatClient registration.")]
	[DefaultValue(null)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public string? ChatClientServiceKey { get; set; }

	/// <summary>
	/// Gets or sets whether streaming should be used when communicating with the <see cref="IChatClient"/>.
	/// When true (default), responses will be streamed in real-time. When false, the complete response is awaited before displaying.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether streaming should be used when communicating with the IChatClient.")]
	[DefaultValue(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool UseStreaming { get; set; } = true;

	/// <summary>
	/// Gets or sets whether function call and function result content should be included in the streaming visualization.
	/// When true, function calls and their results will be displayed alongside text content during streaming.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether function call and function result content should be included in the streaming visualization.")]
	[DefaultValue(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool IncludeFunctionCalls { get; set; } = false;
  
	/// Gets or sets the <see cref="Microsoft.Extensions.AI.ChatOptions"/> passed to every <see cref="IChatClient"/> request.
	/// When set, these options are used as the default for each request. They can also be overridden per-request by handling the <see cref="ChatOptionsRequested"/> event.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ChatOptions? ChatOptions { get; set; }

	/// <summary>
	/// Gets or sets the sender name used for assistant responses when using <see cref="IChatClient"/>.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets the sender name used for assistant responses when using IChatClient.")]
	[DefaultValue("Assistant")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public string AssistantSenderName { get; set; } = "Assistant";

	private CancellationTokenSource? _chatClientCancellationTokenSource;

	/// <summary>
	/// Gets the cancellation token source for the current IChatClient operation, if any.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public CancellationTokenSource? ChatClientCancellationTokenSource => _chatClientCancellationTokenSource;

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
	/// Adds a chat message with support of streaming input, handling different kinds of content
	/// such as text and function calls.
	/// </summary>
	/// <param name="sender">The sender of the streaming message.</param>
	/// <param name="stream">The stream of content items.</param>
	/// <param name="synchronizationContext">An optional synchronization context. Only required if the applications does not provide a default synchronization context.</param>
	/// <param name="completionCallback">An optional callback that can be used to process the streamed messages after it was received completely.</param>
	/// <param name="exceptionCallback">An optional callback that can be used to process exceptions that occurred during the processing of the stream.</param>
	/// <param name="cancellationToken">The token to cancel the operation with.</param>
	/// <returns>An <see cref="IChatMessageControl"/> instance representing the added streaming message.</returns>
	public virtual IChatMessageControl AddStreamingMessage(
		ISender sender,
		IAsyncEnumerable<IChatMessageContent> stream,
		SynchronizationContext? synchronizationContext = default,
		Action<string>? completionCallback = default,
		Action<Exception>? exceptionCallback = default,
		CancellationToken cancellationToken = default)
	{
		async IAsyncEnumerable<string> ToStringStream()
		{
			await foreach (var content in stream.ConfigureAwait(false))
			{
				var text = content.ToString();
				if (!string.IsNullOrEmpty(text))
					yield return text;
			}
		}

		return AddStreamingMessage(sender, ToStringStream(), synchronizationContext, completionCallback, exceptionCallback, cancellationToken);
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
	protected virtual IChatMessageControl CreateMessageControl(IChatMessage message) => new ChatMessageControl() { Message = message, MessageFormatter = MessageFormatter };

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
			SendMessageByChatClient();
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

	/// <summary>
	/// Handles the MessageSent event to automatically call IChatClient if configured.
	/// </summary>
	private async void SendMessageByChatClient()
	{
		try
		{
			// Only proceed if we have a service provider
			if (ServiceProvider is null)
				return;

			// Try to resolve the IChatClient
			var chatClient = ResolveChatClient();
			if (chatClient is null)
				return;

			// Cancel any existing IChatClient operation
			_chatClientCancellationTokenSource?.Cancel();
			_chatClientCancellationTokenSource?.Dispose();
			_chatClientCancellationTokenSource = new CancellationTokenSource();

			// Convert message history to Microsoft.Extensions.AI format
			var chatMessages = ConvertToChatMessages();

			// Resolve chat options, allowing subscribers to override them via event
			var chatOptionsArgs = new ChatOptionsRequestedEventArgs(ChatOptions);
			ChatOptionsRequested?.Invoke(this, chatOptionsArgs);
			var chatOptions = chatOptionsArgs.ChatOptions;

			try
			{
				var assistantSender = new NamedSender(AssistantSenderName);

				if (UseStreaming)
				{
					// Use streaming response
					var streamingResponse = chatClient.GetStreamingResponseAsync(chatMessages, chatOptions, cancellationToken: _chatClientCancellationTokenSource.Token);
					await HandleStreamingResponseAsync(assistantSender, streamingResponse).ConfigureAwait(true);
				}
				else
				{
					// Use non-streaming response
					var response = await chatClient.GetResponseAsync(chatMessages, chatOptions, cancellationToken: _chatClientCancellationTokenSource.Token).ConfigureAwait(true);
					HandleNonStreamingResponse(assistantSender, response);
				}
			}
			catch (OperationCanceledException)
			{
				// Operation was cancelled, this is expected behavior
			}
			catch (Exception ex)
			{
				// Display error message in chat
				AddMessage(new NamedSender("System"), new StringMessageContent($"Error: {ex.Message}"));
			}
			finally
			{
				_chatClientCancellationTokenSource?.Dispose();
				_chatClientCancellationTokenSource = null;
			}
		}
		catch (Exception ex)
		{
			// Catch any unexpected exceptions to prevent application crash
			// Since this is an async void method, unhandled exceptions would terminate the application
			try
			{
				AddMessage(new NamedSender("System"), new StringMessageContent($"Unexpected error: {ex.Message}"));
			}
			catch
			{
				// If we can't even add an error message, just ignore it to prevent further issues
			}
		}
	}

	/// <summary>
	/// Resolves the IChatClient from the service provider, using the ChatClientServiceKey if configured.
	/// </summary>
	private IChatClient? ResolveChatClient()
	{
		if (ServiceProvider is null)
			return null;

		try
		{
			if (string.IsNullOrEmpty(ChatClientServiceKey))
			{
				// Resolve default IChatClient
				return ServiceProvider.GetService<IChatClient>();
			}
			else
			{
				// Resolve keyed IChatClient
				return ServiceProvider.GetKeyedService<IChatClient>(ChatClientServiceKey);
			}
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Converts the current message history to Microsoft.Extensions.AI.ChatMessage format.
	/// </summary>
	/// <remarks>
	/// This method determines the chat role based on the sender name:
	/// - Messages from the current <see cref="Sender"/> or the environment username are treated as User messages
	/// - Messages from the <see cref="AssistantSenderName"/> or containing "Assistant" are treated as Assistant messages
	/// - All other messages are treated as Assistant messages by default
	/// Override this method to customize role determination logic.
	/// </remarks>
	protected virtual List<Microsoft.Extensions.AI.ChatMessage> ConvertToChatMessages()
	{
		var result = new List<Microsoft.Extensions.AI.ChatMessage>();

		foreach (var message in _messages)
		{
			var content = message.Content?.Content?.ToString() ?? string.Empty;
			var senderName = message.Sender?.Name ?? "User";

			// Determine the role based on sender name
			var role = DetermineChatRole(senderName);

			result.Add(new Microsoft.Extensions.AI.ChatMessage(role, content));
		}

		return result;
	}

	/// <summary>
	/// Determines the ChatRole for a given sender name.
	/// </summary>
	/// <param name="senderName">The name of the sender.</param>
	/// <returns>The appropriate ChatRole for the sender.</returns>
	/// <remarks>
	/// Override this method to customize how sender names are mapped to chat roles.
	/// </remarks>
	protected virtual ChatRole DetermineChatRole(string senderName)
	{
		// Check if this is the current user
		if (senderName == Sender.Name || senderName == Environment.UserName)
			return ChatRole.User;

		// Check if this is an assistant
		if (senderName == AssistantSenderName ||
			senderName.Contains("Assistant", StringComparison.OrdinalIgnoreCase) ||
			senderName.Contains("AI", StringComparison.OrdinalIgnoreCase) ||
			senderName.Contains("Bot", StringComparison.OrdinalIgnoreCase))
			return ChatRole.Assistant;

		// Check for system messages
		if (senderName.Equals("System", StringComparison.OrdinalIgnoreCase))
			return ChatRole.System;

		// Default to Assistant for all other senders
		return ChatRole.Assistant;
	}

	/// <summary>
	/// Handles a streaming response from the IChatClient.
	/// </summary>
	private async Task HandleStreamingResponseAsync(ISender sender, IAsyncEnumerable<ChatResponseUpdate> stream)
	{
		// Create an async enumerable that yields typed content items
		async IAsyncEnumerable<IChatMessageContent> ContentStream()
		{
			await foreach (var update in stream.ConfigureAwait(false))
			{
				foreach (var content in update.Contents)
				{
					if (content is Microsoft.Extensions.AI.TextContent textContent)
					{
						if (!string.IsNullOrEmpty(textContent.Text))
							yield return new StringMessageContent(textContent.Text);
					}
					else if (IncludeFunctionCalls && content is Microsoft.Extensions.AI.FunctionCallContent functionCall)
					{
						yield return new FunctionCallMessageContent(
							functionCall.CallId ?? string.Empty,
							functionCall.Name ?? string.Empty,
							functionCall.Arguments);
					}
					else if (IncludeFunctionCalls && content is Microsoft.Extensions.AI.FunctionResultContent functionResult)
					{
						yield return new FunctionResultMessageContent(
							functionResult.CallId ?? string.Empty,
							functionResult.Result);
					}
				}
			}
		}

		// Use the AddStreamingMessage overload that handles different content types
		AddStreamingMessage(sender, ContentStream());
	}

	/// <summary>
	/// Handles a non-streaming response from the IChatClient.
	/// </summary>
	private void HandleNonStreamingResponse(ISender sender, ChatResponse response)
	{
		var content = response.Text ?? string.Empty;
		AddMessage(sender, new StringMessageContent(content));
	}
}