using System.ComponentModel;
using System.Drawing.Imaging;
using System.Threading.Channels;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;
using TinyChat.SubControls;

namespace TinyChat;

/// <summary>
/// A user control that provides a chat interface with message display and text input functionality.
/// </summary>
public partial class ChatControl : UserControl
{
	private const string ROBOT_WELCOME = "●\n┌─┴─┐\n◉‿◉\n└───┘\n\nGreetings human.\nHow can I help you today?";

	private const long DEFAULT_MAXIMUM_ATTACHMENT_FILE_SIZE = 20 * 1024 * 1024;

	private List<IChatMessage> _messages = [];
	private readonly object _cancellationLock = new();
	private readonly HashSet<Control> _fileDropTargets = [];
	private CancellationTokenSource? _currentCancellationTokenSource;
	private Control? _messageCopyButton;
	private IChatMessageControl? _messageCopyTarget;
	private System.Windows.Forms.Timer? _messageCopyHoverTimer;
	private System.Windows.Forms.Timer? _messageCopyHideTimer;
	private bool _attachmentDraftVisible;
	private bool _suppressMessageCopyButton;

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
	/// Occurs when an error is thrown during message processing, either from the <see cref="IChatClient"/>
	/// or from a streaming operation started via AddStreamingMessage.
	/// Set <see cref="Helper.ChatErrorEventArgs.Handled"/> to <see langword="true"/> to suppress the default
	/// "System" error message and any pending re-throw.
	/// </summary>
	public event EventHandler<Helper.ChatErrorEventArgs>? ErrorOccurred;

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
	/// Gets or sets whether the user is allowed to expand function call messages by clicking on them.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Category("Chat")]
	public bool AllowExpandFunctionCalls
	{
		get => _allowExpandFunctionCalls;
		set
		{
			_allowExpandFunctionCalls = value;

			foreach (var functionCallMessageControl in _functionCallMessageControls)
				functionCallMessageControl.AllowExpand = value;
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

	/// <summary>
	/// Gets or sets whether reasoning content should be included in the streaming visualization.
	/// When true, reasoning text will be displayed alongside text content during streaming.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether reasoning content should be included in the streaming visualization.")]
	[DefaultValue(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool IncludeReasoning { get; set; } = false;

	/// <summary>
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

	/// <summary>
	/// Gets or sets whether files can be dropped onto the chat and sent as attachments.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether files can be dropped onto the chat and sent as attachments.")]
	[DefaultValue(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool AllowFileAttachments { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum size of a single dropped attachment in bytes.
	/// Set to zero to allow files of any size.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets the maximum size of a single dropped attachment in bytes. Set to zero for no limit.")]
	[DefaultValue(DEFAULT_MAXIMUM_ATTACHMENT_FILE_SIZE)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public long MaximumAttachmentFileSize { get; set; } = DEFAULT_MAXIMUM_ATTACHMENT_FILE_SIZE;

	/// <summary>
	/// Gets or sets whether a copy button is shown when the pointer rests over a message.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether a delayed copy button is shown when the pointer rests over a message.")]
	[DefaultValue(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool ShowCopyButton { get; set; } = true;

	/// <summary>
	/// Gets or sets the delay in milliseconds before the message copy button is shown.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets the hover delay in milliseconds before the message copy button is shown.")]
	[DefaultValue(600)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public int CopyButtonHoverDelay { get; set; } = 600;

	private List<IFunctionCallMessageControl> _functionCallMessageControls = new();
	private bool _allowExpandFunctionCalls = true;

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

	/// <inheritdoc />
	protected override void OnHandleDestroyed(EventArgs e)
	{
		if (Disposing)
		{
			CancelCurrentOperation();
			DisposeMessageCopyButton();
		}

		base.OnHandleDestroyed(e);
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
		var ownsCurrentOperation = RegisterCancellationSource(cancellationSource, cancellationToken);

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
				messageControl.SetIsReceivingStream(true);
				if (ownsCurrentOperation)
					inputControl?.SetIsReceivingStream(true, allowCancellation: true);

				await foreach (var chunk in EnumerateWithCancellation(stream, cancellationSource.Token).ConfigureAwait(true))
				{
					stringBuilder.Append(chunk);
				}
			}
			catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
			{
				// Cancellation is expected and must not be reported as an error.
			}
			catch (Exception ex)
			{
				var errorArgs = new Helper.ChatErrorEventArgs(ex);
				ErrorOccurred?.Invoke(this, errorArgs);
				exceptionCallback?.Invoke(ex);
				if (!errorArgs.Handled && !cancellationSource.Token.IsCancellationRequested)
					throw;
			}
			finally
			{
				try
				{
					if (ownsCurrentOperation)
						inputControl?.SetIsReceivingStream(false, allowCancellation: false);
					messageControl.SetIsReceivingStream(false);
				}
				finally
				{
					if (ownsCurrentOperation)
						ClearCancellationSource(cancellationSource);
					cancellationSource.Dispose();
				}
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
	/// <param name="synchronizationContext">An optional synchronization context. Only required if the application does not provide a default synchronization context.</param>
	/// <param name="completionCallback">An optional callback that can be used to process the streamed messages after they have been received completely.</param>
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
		async IAsyncEnumerable<string> ToStringStream([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
		{
			await foreach (var content in stream.WithCancellation(ct).ConfigureAwait(false))
			{
				var text = content.ToString();
				if (!string.IsNullOrEmpty(text))
					yield return text;
			}
		}

		return AddStreamingMessage(sender, ToStringStream(cancellationToken), synchronizationContext, completionCallback, exceptionCallback, cancellationToken);
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
		inputControl.CancellationRequested += (_, _) => CancelCurrentOperation();
		if (inputControl is IChatAttachmentInputControl attachmentInputControl)
		{
			attachmentInputControl.AttachmentPasteRequested += AttachmentInputControl_AttachmentPasteRequested;
			attachmentInputControl.AttachmentsChanged += AttachmentInputControl_AttachmentsChanged;
		}
		InputControl = (Control)inputControl;

		splitContainer?.ChatInputPanel?.Controls.Add(InputControl);
		LayoutChatInputControl(InputControl);

		ConfigureFileDropTarget(this);

		PopulateMessages();
	}

	/// <summary>
	/// Adds the messages to the controls
	/// </summary>
	private void PopulateMessages()
	{
		_functionCallMessageControls.Clear();

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
		IChatMessageControl messageControl;

		if (message.Content is FunctionCallMessageContent)
		{
			var functionCallMessageControl = CreateFunctionCallMessageControl(message);
			messageControl = functionCallMessageControl;
			_functionCallMessageControls.Add(functionCallMessageControl);
		}
		else if (message.Content is ReasoningMessageContent)
		{
			messageControl = CreateReasoningMessageControl(message);
		}
		else
		{
			messageControl = CreateMessageControl(message);
		}

		messageControl.Message = message;

		if (IsContinuationMessage(message))
			messageControl.ShowSenderHeader(false);

		var control = (Control)messageControl;

		if (MessageHistoryControl is IChatMessageHistoryControl casted)
		{
			LayoutMessageControl(MessageHistoryControl, control);
			casted.AppendMessageControl(messageControl);
		}

		if (ShowCopyButton && control.Parent is not null)
			AttachCopyButton(messageControl);

		return messageControl;
	}

	/// <summary>
	/// Determines whether the specified message is a continuation of the immediately
	/// preceding message from the same sender. Continuation messages should not display
	/// the sender header, regardless of content type.
	/// </summary>
	/// <param name="message">The message to evaluate.</param>
	/// <returns><see langword="true"/> if the message is a continuation; otherwise, <see langword="false"/>.</returns>
	protected virtual bool IsContinuationMessage(IChatMessage message)
	{
		var index = _messages.IndexOf(message);
		if (index <= 0)
			return false;

		var prev = _messages[index - 1];
		return string.Equals(prev.Sender?.Name, message.Sender?.Name, StringComparison.Ordinal);
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
	/// Creates the button shown after hovering over a message.
	/// </summary>
	/// <returns>The copy button control.</returns>
	protected virtual Control CreateMessageCopyButton()
	{
		var button = new Button
		{
			Cursor = Cursors.Hand,
			FlatStyle = FlatStyle.Flat,
			Size = new Size(26, 26),
			TabStop = false,
			Text = "⧉"
		};
		button.FlatAppearance.BorderSize = 0;
		return button;
	}

	/// <summary>
	/// Creates a control for displaying a tool call with its result
	/// </summary>
	/// <param name="message">The chat message to create a control for.</param>
	/// <returns>An <see cref="IChatMessageControl"/> instance for the message.</returns>
	protected virtual IFunctionCallMessageControl CreateFunctionCallMessageControl(IChatMessage message) => new FunctionCallMessageControl { Message = message, AllowExpand = AllowExpandFunctionCalls };

	/// <summary>
	/// Creates a control for displaying reasoning message
	/// </summary>
	/// <param name="message">The chat message to create a control for.</param>
	/// <returns>An <see cref="IChatMessageControl"/> instance for the message.</returns>
	protected virtual IChatMessageControl CreateReasoningMessageControl(IChatMessage message) => new ReasoningMessageControl { MessageFormatter = MessageFormatter, Message = message };

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
	/// Loads files from disk and adds them to the attachment draft in the chat input.
	/// </summary>
	/// <param name="filePaths">The files to add.</param>
	/// <param name="cancellationToken">The token used to cancel file loading.</param>
	/// <returns>The number of files added to the draft.</returns>
	public async Task<int> AddFilesToInputAsync(
		IEnumerable<string> filePaths,
		CancellationToken cancellationToken = default)
	{
		var attachmentInput = GetAttachmentInputControl();
		var attachments = await LoadAttachmentsAsync(filePaths, cancellationToken).ConfigureAwait(true);
		attachmentInput.AddAttachments(attachments);
		return attachments.Count;
	}

	/// <summary>
	/// Adds supported clipboard data to the attachment draft in the chat input.
	/// </summary>
	/// <remarks>
	/// File-drop data, such as files copied in Windows Explorer, and bitmap data are supported.
	/// Bitmap data is stored as a PNG attachment.
	/// </remarks>
	/// <param name="data">The clipboard data to add.</param>
	/// <param name="cancellationToken">The token used to cancel file loading.</param>
	/// <returns>The number of attachments added to the draft.</returns>
	public async Task<int> AddClipboardContentToInputAsync(
		IDataObject data,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(data);

		if (data.GetDataPresent(DataFormats.FileDrop) && data.GetData(DataFormats.FileDrop) is string[] filePaths)
			return await AddFilesToInputAsync(filePaths, cancellationToken).ConfigureAwait(true);

		if (!data.GetDataPresent(DataFormats.Bitmap) || data.GetData(DataFormats.Bitmap) is not Image image)
			return 0;

		cancellationToken.ThrowIfCancellationRequested();
		using var stream = new MemoryStream();
		image.Save(stream, ImageFormat.Png);
		var fileName = $"image-{DateTime.Now:yyyyMMdd-HHmmssfff}.png";
		if (MaximumAttachmentFileSize > 0 && stream.Length > MaximumAttachmentFileSize)
			throw new IOException($"The image exceeds the maximum attachment size of {MaximumAttachmentFileSize:N0} bytes.");

		var attachment = new ChatFileAttachment(fileName, "image/png", stream.ToArray());
		GetAttachmentInputControl().AddAttachments([attachment]);
		return 1;
	}

	/// <summary>
	/// Loads files from disk and sends them together as a chat message.
	/// </summary>
	/// <param name="filePaths">The files to attach.</param>
	/// <param name="message">Optional text accompanying the files.</param>
	/// <param name="cancellationToken">The token used to cancel file loading.</param>
	/// <returns><see langword="true"/> when the message was sent; otherwise, <see langword="false"/>.</returns>
	public async Task<bool> SendFilesAsync(
		IEnumerable<string> filePaths,
		string? message = null,
		CancellationToken cancellationToken = default)
	{
		var attachments = await LoadAttachmentsAsync(filePaths, cancellationToken).ConfigureAwait(true);

		if (attachments.Count == 0)
			return false;

		return SendMessage(Sender, new FileAttachmentMessageContent(attachments, message));
	}

	private async Task<List<ChatFileAttachment>> LoadAttachmentsAsync(
		IEnumerable<string> filePaths,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		var maximumFileSize = MaximumAttachmentFileSize > 0 ? MaximumAttachmentFileSize : (long?)null;
		var attachments = new List<ChatFileAttachment>();
		foreach (var filePath in filePaths.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			cancellationToken.ThrowIfCancellationRequested();
			attachments.Add(await ChatFileAttachment.LoadAsync(filePath, maximumFileSize, cancellationToken));
		}

		return attachments;
	}

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
			AddMessage(sender, e.Content);
			SendMessageByChatClient();
			MessageSent?.Invoke(this, new MessageSentEventArgs(sender, e.Content));
		}
	}

	/// <summary>
	/// Cancels the current chat client or streaming operation, if one is running.
	/// </summary>
	public virtual void CancelCurrentOperation()
	{
		CancellationTokenSource? cancellationSource;
		lock (_cancellationLock)
			cancellationSource = _currentCancellationTokenSource;

		TryCancel(cancellationSource);
	}

	private bool RegisterCancellationSource(CancellationTokenSource cancellationSource, CancellationToken parentToken = default)
	{
		CancellationTokenSource? previousSource;
		lock (_cancellationLock)
		{
			if (_currentCancellationTokenSource is { } currentSource &&
				parentToken.CanBeCanceled &&
				parentToken == currentSource.Token)
			{
				return false;
			}

			previousSource = _currentCancellationTokenSource;
			_currentCancellationTokenSource = cancellationSource;
		}

		if (!ReferenceEquals(previousSource, cancellationSource))
			TryCancel(previousSource);

		return true;
	}

	private void ClearCancellationSource(CancellationTokenSource cancellationSource)
	{
		lock (_cancellationLock)
		{
			if (ReferenceEquals(_currentCancellationTokenSource, cancellationSource))
				_currentCancellationTokenSource = null;
		}
	}

	private static void TryCancel(CancellationTokenSource? cancellationSource)
	{
		try
		{
			cancellationSource?.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// The owning operation completed between reading and cancelling the source.
		}
	}

	private void ConfigureFileDropTarget(Control control)
	{
		if (!_fileDropTargets.Add(control))
			return;

		control.AllowDrop = true;
		control.DragEnter += FileDropTarget_DragEnter;
		control.DragDrop += FileDropTarget_DragDrop;
		control.ControlAdded += FileDropTarget_ControlAdded;
		control.Disposed += (_, _) => _fileDropTargets.Remove(control);

		foreach (Control child in control.Controls)
			ConfigureFileDropTarget(child);
	}

	private void FileDropTarget_ControlAdded(object? sender, ControlEventArgs e)
	{
		if (e.Control is not null)
			ConfigureFileDropTarget(e.Control);
	}

	private void FileDropTarget_DragEnter(object? sender, DragEventArgs e)
	{
		e.Effect = AllowFileAttachments && e.Data?.GetDataPresent(DataFormats.FileDrop) == true
			? DragDropEffects.Copy
			: DragDropEffects.None;
	}

	private async void FileDropTarget_DragDrop(object? sender, DragEventArgs e)
	{
		try
		{
			if (!AllowFileAttachments ||
				e.Data?.GetDataPresent(DataFormats.FileDrop) != true ||
				e.Data.GetData(DataFormats.FileDrop) is not string[] filePaths)
			{
				return;
			}

			await AddFilesToInputAsync(filePaths).ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			// File loading was cancelled; nothing was added to the draft.
		}
		catch (Exception ex)
		{
			ReportAttachmentError(ex);
		}
	}

	private async void AttachmentInputControl_AttachmentPasteRequested(object? sender, AttachmentPasteRequestedEventArgs e)
	{
		try
		{
			await AddClipboardContentToInputAsync(e.Data).ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			// Clipboard attachment processing was cancelled; nothing was added to the draft.
		}
		catch (Exception ex)
		{
			ReportAttachmentError(ex);
		}
	}

	private void AttachmentInputControl_AttachmentsChanged(object? sender, EventArgs e)
	{
		if (sender is not IChatAttachmentInputControl attachmentInput)
			return;

		var hasAttachments = attachmentInput.PendingAttachments.Count > 0;
		if (hasAttachments == _attachmentDraftVisible)
			return;

		try
		{
			var heightChange = hasAttachments
				? attachmentInput.AttachmentDisplayHeight
				: -attachmentInput.AttachmentDisplayHeight;
			SplitterPosition = Math.Max(1, SplitterPosition + heightChange);
		}
		catch (ArgumentOutOfRangeException)
		{
			// Very small host controls may not have enough room to expand the input panel.
		}
		catch (InvalidOperationException)
		{
			// The splitter may still be completing layout while attachments are being added.
		}

		_attachmentDraftVisible = hasAttachments;
	}

	private IChatAttachmentInputControl GetAttachmentInputControl()
	{
		return InputControl as IChatAttachmentInputControl
			?? throw new InvalidOperationException("The configured chat input control does not support attachment drafts.");
	}

	private void ReportAttachmentError(Exception exception)
	{
		try
		{
			var errorArgs = new Helper.ChatErrorEventArgs(exception);
			ErrorOccurred?.Invoke(this, errorArgs);
			if (!errorArgs.Handled)
				AddMessage(new NamedSender("System"), new StringMessageContent($"Could not attach file: {exception.Message}"));
		}
		catch
		{
			// Prevent an exception in error reporting from escaping an async event handler.
		}
	}

	private void AttachCopyButton(IChatMessageControl messageControl)
	{
		EnsureMessageCopyButton();

		void WireHoverEvents(Control control)
		{
			control.MouseEnter += (_, _) => StartMessageCopyHover(messageControl);
			control.MouseMove += (_, _) => StartMessageCopyHover(messageControl);
			control.MouseLeave += (_, _) => StartMessageCopyHideDelay();
			control.ControlAdded += (_, e) =>
			{
				if (e.Control is not null)
					WireHoverEvents(e.Control);
			};

			foreach (Control child in control.Controls)
				WireHoverEvents(child);
		}

		WireHoverEvents((Control)messageControl);
	}

	private void EnsureMessageCopyButton()
	{
		if (_messageCopyButton is not null)
			return;

		_messageCopyHoverTimer = new System.Windows.Forms.Timer { Interval = Math.Max(1, CopyButtonHoverDelay) };
		_messageCopyHideTimer = new System.Windows.Forms.Timer { Interval = 75 };
		_messageCopyButton = CreateMessageCopyButton();
		_messageCopyButton.Visible = false;
		_messageCopyButton.Click += MessageCopyButton_Click;
		_messageCopyButton.MouseEnter += (_, _) => _messageCopyHideTimer.Stop();
		_messageCopyButton.MouseLeave += (_, _) => StartMessageCopyHideDelay();
		_messageCopyHoverTimer.Tick += (_, _) =>
		{
			_messageCopyHoverTimer.Stop();
			ShowMessageCopyButton();
		};
		_messageCopyHideTimer.Tick += (_, _) =>
		{
			_messageCopyHideTimer.Stop();
			if (!IsPointerInsideMessageCopyArea())
			{
				_suppressMessageCopyButton = false;
				HideMessageCopyButton(clearTarget: true);
			}
		};

		Controls.Add(_messageCopyButton);
		_messageCopyButton.BringToFront();

		if (MessageHistoryControl is IChatMessageHistoryViewport viewport)
			viewport.ViewportChanged += MessageHistoryViewport_Changed;

		if (MessageHistoryControl is not null)
		{
			MessageHistoryControl.SizeChanged += MessageHistoryViewport_Changed;
			MessageHistoryControl.VisibleChanged += MessageHistoryViewport_Changed;
		}
	}

	private void StartMessageCopyHover(IChatMessageControl messageControl)
	{
		if (_messageCopyButton is null || _messageCopyHoverTimer is null || _messageCopyHideTimer is null)
			return;

		_messageCopyHideTimer.Stop();
		if (!ReferenceEquals(_messageCopyTarget, messageControl))
		{
			HideMessageCopyButton(clearTarget: false);
			_messageCopyTarget = messageControl;
			_suppressMessageCopyButton = false;
		}

		if (_suppressMessageCopyButton || _messageCopyButton.Visible || _messageCopyHoverTimer.Enabled)
			return;

		_messageCopyHoverTimer.Interval = Math.Max(1, CopyButtonHoverDelay);
		_messageCopyHoverTimer.Start();
	}

	private void StartMessageCopyHideDelay()
	{
		if (_messageCopyHideTimer is null)
			return;

		_messageCopyHideTimer.Stop();
		_messageCopyHideTimer.Start();
	}

	private void ShowMessageCopyButton()
	{
		if (_messageCopyButton is null ||
			_messageCopyTarget is not Control messageControl ||
			MessageHistoryControl is null ||
			messageControl.IsDisposed ||
			!messageControl.Visible ||
			!IsHandleCreated ||
			!messageControl.IsHandleCreated ||
			!MessageHistoryControl.IsHandleCreated ||
			!IsPointerInsideMessageCopyArea())
		{
			return;
		}

		var messageBounds = RectangleToClient(messageControl.RectangleToScreen(messageControl.ClientRectangle));
		var historyBounds = RectangleToClient(MessageHistoryControl.RectangleToScreen(MessageHistoryControl.ClientRectangle));
		historyBounds.Intersect(ClientRectangle);
		var visibleMessageBounds = Rectangle.Intersect(messageBounds, historyBounds);
		if (visibleMessageBounds.Width < _messageCopyButton.Width || visibleMessageBounds.Height < _messageCopyButton.Height)
			return;

		var preferredX = messageBounds.Right - messageControl.Padding.Right - _messageCopyButton.Width;
		var maximumX = visibleMessageBounds.Right - _messageCopyButton.Width;
		var preferredY = messageBounds.Top + messageControl.Padding.Top;
		var maximumY = visibleMessageBounds.Bottom - _messageCopyButton.Height;
		var location = new Point(
			Math.Clamp(preferredX, visibleMessageBounds.Left, maximumX),
			Math.Clamp(preferredY, visibleMessageBounds.Top, maximumY));

		_messageCopyButton.Location = location;
		_messageCopyButton.BringToFront();
		_messageCopyButton.Visible = true;
	}

	private bool IsPointerInsideMessageCopyArea()
	{
		var pointerPosition = Cursor.Position;
		var insideMessage = _messageCopyTarget is Control messageControl &&
			messageControl.IsHandleCreated &&
			messageControl.RectangleToScreen(messageControl.ClientRectangle).Contains(pointerPosition);
		var insideButton = _messageCopyButton is { Visible: true, IsHandleCreated: true } &&
			_messageCopyButton.RectangleToScreen(_messageCopyButton.ClientRectangle).Contains(pointerPosition);
		return insideMessage || insideButton;
	}

	private void HideMessageCopyButton(bool clearTarget)
	{
		_messageCopyHoverTimer?.Stop();
		_messageCopyHideTimer?.Stop();
		if (_messageCopyButton is { IsDisposed: false })
			_messageCopyButton.Visible = false;

		if (clearTarget)
			_messageCopyTarget = null;
	}

	private void MessageCopyButton_Click(object? sender, EventArgs e)
	{
		try
		{
			var text = _messageCopyTarget?.Message?.Content?.ToString() ?? _messageCopyTarget?.ToString();
			if (!string.IsNullOrEmpty(text))
				Clipboard.SetText(text);
		}
		catch
		{
			// The clipboard can temporarily be unavailable; keep the chat responsive.
		}
		finally
		{
			_suppressMessageCopyButton = true;
			HideMessageCopyButton(clearTarget: false);
		}
	}

	private void MessageHistoryViewport_Changed(object? sender, EventArgs e)
	{
		_suppressMessageCopyButton = false;
		HideMessageCopyButton(clearTarget: true);
	}

	private void DisposeMessageCopyButton()
	{
		if (MessageHistoryControl is IChatMessageHistoryViewport viewport)
			viewport.ViewportChanged -= MessageHistoryViewport_Changed;

		if (MessageHistoryControl is not null)
		{
			MessageHistoryControl.SizeChanged -= MessageHistoryViewport_Changed;
			MessageHistoryControl.VisibleChanged -= MessageHistoryViewport_Changed;
		}

		_messageCopyHoverTimer?.Dispose();
		_messageCopyHideTimer?.Dispose();
		_messageCopyButton?.Dispose();
		_messageCopyHoverTimer = null;
		_messageCopyHideTimer = null;
		_messageCopyButton = null;
		_messageCopyTarget = null;
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
			if (ServiceProvider is null)
				return;

			var chatClient = ResolveChatClient();
			if (chatClient is null)
				return;

			var cancellationSource = new CancellationTokenSource();
			RegisterCancellationSource(cancellationSource);
			var inputControl = InputControl as IChatInputControl;

			try
			{
				inputControl?.SetIsReceivingStream(true, allowCancellation: true);

				var chatMessages = ConvertToChatMessages();

				var chatOptionsArgs = new ChatOptionsRequestedEventArgs(ChatOptions);
				ChatOptionsRequested?.Invoke(this, chatOptionsArgs);
				var chatOptions = chatOptionsArgs.ChatOptions;

				var assistantSender = new NamedSender(AssistantSenderName);

				if (UseStreaming)
				{
					// Use streaming response
					var streamingResponse = chatClient.GetStreamingResponseAsync(chatMessages, chatOptions, cancellationToken: cancellationSource.Token);
					await HandleStreamingResponseAsync(assistantSender, streamingResponse, cancellationSource.Token).ConfigureAwait(true);
				}
				else
				{
					// Use non-streaming response
					var response = await chatClient
						.GetResponseAsync(chatMessages, chatOptions, cancellationToken: cancellationSource.Token)
						.WaitAsync(cancellationSource.Token)
						.ConfigureAwait(true);
					HandleNonStreamingResponse(assistantSender, response);
				}
			}
			catch (OperationCanceledException)
			{
				// Operation was cancelled, this is expected behavior
			}
			catch (Exception ex)
			{
				var errorArgs = new Helper.ChatErrorEventArgs(ex);
				ErrorOccurred?.Invoke(this, errorArgs);
				if (!errorArgs.Handled)
					AddMessage(new NamedSender("System"), new StringMessageContent($"Error: {ex.Message}"));
			}
			finally
			{
				try
				{
					inputControl?.SetIsReceivingStream(false, allowCancellation: false);
				}
				finally
				{
					ClearCancellationSource(cancellationSource);
					cancellationSource.Dispose();
				}
			}
		}
		catch (Exception ex)
		{
			// Catch any unexpected exceptions to prevent application crash
			// Since this is an async void method, unhandled exceptions would terminate the application
			try
			{
				var errorArgs = new Helper.ChatErrorEventArgs(ex);
				ErrorOccurred?.Invoke(this, errorArgs);
				if (!errorArgs.Handled)
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
	/// Function call messages are converted to structured FunctionCallContent and FunctionResultContent.
	/// Override this method to customize role determination logic.
	/// </remarks>
	protected virtual List<Microsoft.Extensions.AI.ChatMessage> ConvertToChatMessages()
	{
		var result = new List<Microsoft.Extensions.AI.ChatMessage>();

		foreach (var message in _messages)
		{
			var senderName = message.Sender?.Name ?? "User";

			// Handle function call messages separately to preserve structured tool call data
			if (message.Content is FunctionCallMessageContent funcCallContent)
			{
				// Convert IReadOnlyDictionary to IDictionary for FunctionCallContent
				IDictionary<string, object?>? arguments = funcCallContent.Arguments is not null
					? new Dictionary<string, object?>(funcCallContent.Arguments)
					: null;

				// Add the function call from the assistant
				var functionCallMessage = new Microsoft.Extensions.AI.ChatMessage(
					ChatRole.Assistant,
					[new FunctionCallContent(funcCallContent.CallId, funcCallContent.Name, arguments)]
				);
				result.Add(functionCallMessage);

				// If there's a result, add it as a tool response
				// This ensures exceptions and errors are properly communicated back to the model
				if (funcCallContent.Result is not null)
				{
					var functionResultMessage = new Microsoft.Extensions.AI.ChatMessage(
						ChatRole.Tool,
						[new FunctionResultContent(funcCallContent.CallId, funcCallContent.Result)]
					);
					result.Add(functionResultMessage);
				}
			}
			else if (message.Content is FileAttachmentMessageContent attachmentContent)
			{
				var contents = new List<AIContent>();
				if (!string.IsNullOrWhiteSpace(attachmentContent.Text))
					contents.Add(new TextContent(attachmentContent.Text));

				foreach (var attachment in attachmentContent.Attachments)
					contents.AddRange(ConvertFileAttachment(attachment));

				var role = DetermineChatRole(senderName);
				result.Add(new Microsoft.Extensions.AI.ChatMessage(role, contents));
			}
			else
			{
				// Handle regular text messages
				var content = message.Content?.Content?.ToString() ?? string.Empty;
				var role = DetermineChatRole(senderName);

				result.Add(new Microsoft.Extensions.AI.ChatMessage(role, content));
			}
		}

		return result;
	}

	/// <summary>
	/// Converts an attached file into one or more Microsoft.Extensions.AI content items.
	/// </summary>
	/// <remarks>
	/// Text formats are decoded and sent as named text sections. Other formats are sent as typed binary data.
	/// Override this method to extract PDF or Office text, upload files to provider-specific storage, or replace
	/// large documents with RAG references.
	/// </remarks>
	/// <param name="attachment">The attachment to convert.</param>
	/// <returns>The content items passed to the chat client.</returns>
	protected virtual IEnumerable<AIContent> ConvertFileAttachment(ChatFileAttachment attachment)
	{
		if (attachment.IsText)
		{
			var lineBreak = Environment.NewLine;
			var text = attachment.GetText();
			yield return new TextContent(
				$"--- BEGIN ATTACHED FILE: {attachment.Name} ({attachment.MediaType}) ---{lineBreak}" +
				$"{text}{lineBreak}" +
				$"--- END ATTACHED FILE: {attachment.Name} ---");
			yield break;
		}

		if (!attachment.IsImage)
		{
			yield return new TextContent(
				$"An attached file named '{attachment.Name}' with media type '{attachment.MediaType}' follows as binary content. " +
				"Read it if this model and provider support that document format.");
		}

		yield return attachment.ToDataContent();
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
	/// Function calls are added as separate messages before the text response stream starts.
	/// </summary>
	private async Task HandleStreamingResponseAsync(ISender sender, IAsyncEnumerable<ChatResponseUpdate> stream, CancellationToken cancellationToken)
	{
		var pendingCalls = new Dictionary<string, FunctionCallMessageContent>();
		var textChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
		var textStreamStarted = false;
		var hadNonTextContentSinceLastText = false;

		try
		{
			ReasoningMessageContent? reasoningMessageContent = null;

			// Iterate without ConfigureAwait(false) so continuations stay on the UI thread,
			// allowing direct AddMessage / AddStreamingMessage calls.
			await foreach (var update in EnumerateWithCancellation(stream, cancellationToken).ConfigureAwait(true))
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				foreach (var item in update.Contents)
				{
					if (cancellationToken.IsCancellationRequested)
						break;

					if (item is FunctionCallContent funcCall && IncludeFunctionCalls)
					{
						var content = new FunctionCallMessageContent(funcCall.CallId, funcCall.Name ?? string.Empty, funcCall.Arguments) { IsFunctionExecuting = true };
						AddMessage(sender, content);

						pendingCalls[funcCall.CallId] = content;

						hadNonTextContentSinceLastText = true;

						// reset the reasoning to be able to start a new control
						reasoningMessageContent?.SetDone();
						reasoningMessageContent = null;
					}
					else if (item is FunctionResultContent funcResult && IncludeFunctionCalls)
					{
						if (pendingCalls.TryGetValue(funcResult.CallId, out var content))
						{
							pendingCalls.Remove(funcResult.CallId);
							content.SetResult(funcResult.Result);
						}

						hadNonTextContentSinceLastText = true;

						// reset the reasoning to be able to start a new control
						reasoningMessageContent?.SetDone();
						reasoningMessageContent = null;
					}
					else if (item is TextReasoningContent reasoningContent && IncludeReasoning)
					{
						if (!string.IsNullOrEmpty(reasoningContent.Text))
						{
							if (reasoningMessageContent == null)
							{
								reasoningMessageContent = new ReasoningMessageContent(reasoningContent.Text);
								AddMessage(sender, reasoningMessageContent);
							}
							else
							{
								reasoningMessageContent.AppendText(reasoningContent.Text);
							}

							hadNonTextContentSinceLastText = true;
						}
					}
					else
					{
						// reset the reasoning to be able to start a new control for a new thinking
						reasoningMessageContent?.SetDone();
						reasoningMessageContent = null;
					}
				}

				if (!string.IsNullOrEmpty(update.Text))
				{
					// When text arrives after non-text content (tool calls, reasoning),
					// complete the current text channel and start a new one so the
					// continuation text appears below the non-text controls.
					if (textStreamStarted && hadNonTextContentSinceLastText)
					{
						textChannel.Writer.TryComplete();
						textChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
						textStreamStarted = false;
					}

					if (!textStreamStarted)
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						textStreamStarted = true;
						hadNonTextContentSinceLastText = false;
						AddStreamingMessage(sender, textChannel.Reader.ReadAllAsync(cancellationToken), cancellationToken: cancellationToken);
					}
					textChannel.Writer.TryWrite(update.Text);
				}
			}
		}
		finally
		{
			textChannel.Writer.TryComplete();
		}
	}

	/// <summary>
	/// Handles a non-streaming response from the IChatClient.
	/// </summary>
	private void HandleNonStreamingResponse(ISender sender, ChatResponse response)
	{
		var content = response.Text ?? string.Empty;
		AddMessage(sender, new StringMessageContent(content));
	}

	private static async IAsyncEnumerable<T> EnumerateWithCancellation<T>(
		IAsyncEnumerable<T> source,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
	{
		IAsyncEnumerator<T>? enumerator = source.GetAsyncEnumerator(cancellationToken);
		try
		{
			while (true)
			{
				var moveNextTask = enumerator.MoveNextAsync().AsTask();
				bool hasNext;
				try
				{
					hasNext = await moveNextTask.WaitAsync(cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					_ = DisposeAfterMoveNextAsync(enumerator, moveNextTask);
					enumerator = null;
					throw;
				}

				if (!hasNext)
					yield break;

				yield return enumerator.Current;
			}
		}
		finally
		{
			if (enumerator is not null)
				await enumerator.DisposeAsync().ConfigureAwait(false);
		}
	}

	private static async Task DisposeAfterMoveNextAsync<T>(IAsyncEnumerator<T> enumerator, Task<bool> moveNextTask)
	{
		try
		{
			await moveNextTask.ConfigureAwait(false);
		}
		catch
		{
			// The abandoned move-next operation is observed before the enumerator is disposed.
		}

		try
		{
			await enumerator.DisposeAsync().ConfigureAwait(false);
		}
		catch
		{
			// Cleanup runs in the background after cancellation and must not surface an exception.
		}
	}
}
