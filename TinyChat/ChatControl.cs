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
	private const string ROBOT_WELCOME = "●\n┌─┴─┐\n●‿●\n└───┘\n\nGreetings human.\nHow can I help you today?";

	private const long DEFAULT_MAXIMUM_ATTACHMENT_FILE_SIZE = 20 * 1024 * 1024;
	private static readonly Font _defaultWelcomeFont = new("Tahoma", 14f);
	private static readonly Padding _defaultInputPadding = new(8);
	private static readonly Padding _defaultMessageMargin = new(3);
	private static readonly Padding _defaultMessagePadding = new(8);

	private List<IChatMessage> _messages = [];
	private readonly List<IChatMessageControl> _messageControls = [];
	private readonly HashSet<IChatMessage> _functionCallVisualizationMessages = new(ReferenceEqualityComparer.Instance);
	private readonly HashSet<IChatMessage> _reasoningVisualizationMessages = new(ReferenceEqualityComparer.Instance);
	private readonly object _cancellationLock = new();
	private readonly HashSet<Control> _fileDropTargets = [];
	private readonly MutableSender _assistantSender = new("Assistant");
	private Padding _historyPadding = Padding.Empty;
	private Padding _inputPadding = _defaultInputPadding;
	private Padding _messageMargin = _defaultMessageMargin;
	private Padding _messagePadding = _defaultMessagePadding;
	private IMessageFormatter _messageFormatter = new PlainTextMessageFormatter();
	private Font? _inputFont;
	private Font? _messageFont;
	private Font? _welcomeFont;
	private Color _inputEditorBackColor = Color.Empty;
	private Color _inputEditorForeColor = Color.Empty;
	private BorderStyle _inputEditorBorderStyle = BorderStyle.Fixed3D;
	private string _inputPlaceholderText = string.Empty;
	private ContentAlignment _welcomeTextAlign = ContentAlignment.MiddleCenter;
	private string _welcomeMessage = ROBOT_WELCOME;
	private int _splitterPosition = 60;
	private bool _isSplitterFixed;
	private bool _allowExpandFunctionCalls = true;
	private bool _allowExpandReasoning = true;
	private bool _includeFunctionCalls;
	private bool _includeSkillCalls = true;

	private bool _includeReasoning;
	private bool _allowFileAttachments = true;
	private bool _messageFormatterWasExplicitlySet;
	private bool _defaultMessageFormatterWasApplied;
	private CancellationTokenSource? _currentCancellationTokenSource;
	private Task _chatClientSendCompletion = Task.CompletedTask;
	private bool _attachmentDraftVisible;
	private readonly PlainTextMessageFormatter _plainTextFormatter = new();
	private Control? _messageActionBar;
	private IChatMessageControl? _messageActionsTarget;
	private System.Windows.Forms.Timer? _messageActionsHoverTimer;
	private System.Windows.Forms.Timer? _messageActionsHideTimer;
	private bool _showMessageActions = true;
	private int _streamingMessageCount;
	private bool _isWaitingForApproval;

	/// <summary>
	/// Occurs when a message is sent from the text box and allows the cancellation of sending.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs>? MessageSending;

	/// <summary>
	/// Occurs when a message has been sent from the user interface.
	/// </summary>
	public event EventHandler<MessageSentEventArgs>? MessageSent;

	/// <summary>
	/// Occurs after the current conversation and input draft have been cleared by <see cref="StartNewChat"/>.
	/// </summary>
	public event EventHandler? NewChatStarted;

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
	/// Occurs when the <see cref="IChatClient"/> asks for approval before executing a function.
	/// The request is shown in the chat; handlers can also decide by calling
	/// <see cref="FunctionCallMessageContent.Approve"/> or <see cref="FunctionCallMessageContent.Reject"/>.
	/// </summary>
	public event EventHandler<ToolApprovalRequestedEventArgs>? ToolApprovalRequested;

	/// <summary>
	/// Occurs after the conversation was reverted to a message by <see cref="RevertToMessage"/>.
	/// </summary>
	public event EventHandler<ConversationRevertedEventArgs>? ConversationReverted;

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
			var messages = value is null ? [] : new List<IChatMessage>(value);
			CancelCurrentOperation();
			(InputControl as IChatInputControl)?.SetIsReceivingStream(false, allowCancellation: false);

			_functionCallVisualizationMessages.RemoveWhere(
				visualizationMessage => !messages.Any(message => ReferenceEquals(message, visualizationMessage)));
			_reasoningVisualizationMessages.RemoveWhere(
				visualizationMessage => !messages.Any(message => ReferenceEquals(message, visualizationMessage)));
			_messages = messages;
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
	public string WelcomeMessage
	{
		get => _welcomeMessage;
		set
		{
			_welcomeMessage = value ?? string.Empty;
			ApplyWelcomeControlSettings();
		}
	}

	/// <summary>
	/// Gets or sets the font used to display chat messages.
	/// When not explicitly set, the inherited <see cref="Control.Font"/> is used.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the font used to display chat messages. Reset the value to inherit the control font.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Font MessageFont
	{
		get => _messageFont ?? Font;
		set
		{
			_messageFont = value ?? throw new ArgumentNullException(nameof(value));
			ApplyMessageFont();
		}
	}

	/// <summary>
	/// Gets or sets the font used by the chat input.
	/// When not explicitly set, the inherited <see cref="Control.Font"/> is used.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the font used by the chat input. Reset the value to inherit the control font.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Font InputFont
	{
		get => _inputFont ?? Font;
		set
		{
			_inputFont = value ?? throw new ArgumentNullException(nameof(value));
			ApplyInputFont();
		}
	}

	/// <summary>
	/// Gets or sets the background colour of the editable chat input surface.
	/// <see cref="Color.Empty"/> uses the native Windows or active DevExpress skin default.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the input editor background colour. Color.Empty uses the platform or active skin default.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Color InputEditorBackColor
	{
		get => _inputEditorBackColor;
		set
		{
			_inputEditorBackColor = value;
			ApplyInputAppearance();
		}
	}

	/// <summary>
	/// Gets or sets the foreground colour of the editable chat input surface.
	/// <see cref="Color.Empty"/> uses the native Windows or active DevExpress skin default.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the input editor foreground colour. Color.Empty uses the platform or active skin default.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Color InputEditorForeColor
	{
		get => _inputEditorForeColor;
		set
		{
			_inputEditorForeColor = value;
			ApplyInputAppearance();
		}
	}

	/// <summary>
	/// Gets or sets the border style of the editable chat input surface.
	/// DevExpress maps the value to the corresponding skin-aware border style.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the input editor border style. DevExpress maps this to its corresponding skin-aware style.")]
	[DefaultValue(BorderStyle.Fixed3D)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public BorderStyle InputEditorBorderStyle
	{
		get => _inputEditorBorderStyle;
		set
		{
			if (!Enum.IsDefined(value))
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(BorderStyle));

			_inputEditorBorderStyle = value;
			ApplyInputAppearance();
		}
	}

	/// <summary>
	/// Gets or sets the placeholder displayed while the chat input editor is empty.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the placeholder displayed while the chat input editor is empty.")]
	[DefaultValue("")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public string InputPlaceholderText
	{
		get => _inputPlaceholderText;
		set
		{
			_inputPlaceholderText = value ?? string.Empty;
			ApplyInputAppearance();
		}
	}

	/// <summary>
	/// Gets or sets the font used by the welcome message.
	/// When not explicitly set, a 14-point Tahoma font is used.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the font used by the welcome message. Reset the value to restore the default.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Font WelcomeFont
	{
		get => _welcomeFont ?? _defaultWelcomeFont;
		set
		{
			_welcomeFont = value ?? throw new ArgumentNullException(nameof(value));
			ApplyWelcomeControlSettings();
		}
	}

	/// <summary>
	/// Gets or sets the padding inside the history area, including the welcome surface.
	/// </summary>
	[Category("Chat Layout")]
	[Description("Gets or sets the padding inside the history area, including the welcome surface.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Padding HistoryPadding
	{
		get => _historyPadding;
		set
		{
			_historyPadding = ValidatePadding(value, nameof(value));
			ApplyHistoryLayout();
		}
	}

	/// <summary>
	/// Gets or sets the padding around the visible content of each chat message.
	/// </summary>
	[Category("Chat Layout")]
	[Description("Gets or sets the padding around regular, function-call, and reasoning message content.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Padding MessagePadding
	{
		get => _messagePadding;
		set
		{
			_messagePadding = ValidatePadding(value, nameof(value));
			ApplyMessageLayout();
		}
	}

	/// <summary>
	/// Gets or sets the outer margin around each chat message.
	/// </summary>
	[Category("Chat Layout")]
	[Description("Gets or sets the outer margin around each chat message.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Padding MessageMargin
	{
		get => _messageMargin;
		set
		{
			_messageMargin = ValidatePadding(value, nameof(value));
			ApplyMessageLayout();
		}
	}

	/// <summary>
	/// Gets or sets the padding between the input surface and its editor content.
	/// </summary>
	[Category("Chat Layout")]
	[Description("Gets or sets the padding between the input surface and its editor content.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Padding InputPadding
	{
		get => _inputPadding;
		set
		{
			_inputPadding = ValidatePadding(value, nameof(value));
			ApplyInputLayout();
		}
	}

	/// <summary>
	/// Gets or sets the alignment of the welcome text.
	/// </summary>
	[Category("Chat Layout")]
	[Description("Gets or sets the horizontal and vertical alignment of the welcome text.")]
	[DefaultValue(ContentAlignment.MiddleCenter)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public ContentAlignment WelcomeTextAlign
	{
		get => _welcomeTextAlign;
		set
		{
			if (!Enum.IsDefined(value))
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ContentAlignment));

			_welcomeTextAlign = value;
			ApplyWelcomeControlSettings();
		}
	}

	/// <summary>
	/// Gets or sets whether the splitter between the history and input areas can be moved by the user.
	/// </summary>
	[Category("Chat Layout")]
	[Description("Gets or sets whether the splitter between the history and input areas can be moved by the user.")]
	[DefaultValue(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool IsSplitterFixed
	{
		get => _isSplitterFixed;
		set
		{
			_isSplitterFixed = value;
			ApplySplitContainerLayout();
		}
	}

	/// <summary>
	/// Gets or sets the splitter position dividing the chat message history from the chat input box below.
	/// </summary>
	[Category("Chat Layout")]
	[DefaultValue(60)]
	[Description("Gets or sets the splitter position dividing the chat message history from the chat input box below.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public int SplitterPosition
	{
		get => (SplitContainerControl as ISplitContainerControl)?.SplitterPosition ?? _splitterPosition;
		set
		{
			if (SplitContainerControl is ISplitContainerControl splitContainer)
			{
				splitContainer.SplitterPosition = value;
				_splitterPosition = splitContainer.SplitterPosition;
			}
			else
			{
				_splitterPosition = value;
			}
		}
	}

	private void ResetMessageFont()
	{
		_messageFont = null;
		ApplyMessageFont();
	}

	private bool ShouldSerializeMessageFont() => _messageFont is not null;

	private void ResetInputFont()
	{
		_inputFont = null;
		ApplyInputFont();
	}

	private bool ShouldSerializeInputFont() => _inputFont is not null;

	private void ResetInputEditorBackColor() => InputEditorBackColor = Color.Empty;

	private bool ShouldSerializeInputEditorBackColor() => !InputEditorBackColor.IsEmpty;

	private void ResetInputEditorForeColor() => InputEditorForeColor = Color.Empty;

	private bool ShouldSerializeInputEditorForeColor() => !InputEditorForeColor.IsEmpty;

	private void ResetInputEditorBorderStyle() => InputEditorBorderStyle = BorderStyle.Fixed3D;

	private bool ShouldSerializeInputEditorBorderStyle() => InputEditorBorderStyle != BorderStyle.Fixed3D;

	private void ResetInputPlaceholderText() => InputPlaceholderText = string.Empty;

	private bool ShouldSerializeInputPlaceholderText() => !string.IsNullOrEmpty(InputPlaceholderText);

	private void ResetWelcomeFont()
	{
		_welcomeFont = null;
		ApplyWelcomeControlSettings();
	}

	private bool ShouldSerializeWelcomeFont() => _welcomeFont is not null;

	private void ResetHistoryPadding()
	{
		HistoryPadding = Padding.Empty;
	}

	private bool ShouldSerializeHistoryPadding() => HistoryPadding != Padding.Empty;

	private void ResetMessagePadding()
	{
		MessagePadding = _defaultMessagePadding;
	}

	private bool ShouldSerializeMessagePadding() => MessagePadding != _defaultMessagePadding;

	private void ResetMessageMargin()
	{
		MessageMargin = _defaultMessageMargin;
	}

	private bool ShouldSerializeMessageMargin() => MessageMargin != _defaultMessageMargin;

	private void ResetInputPadding()
	{
		InputPadding = _defaultInputPadding;
	}

	private bool ShouldSerializeInputPadding() => InputPadding != _defaultInputPadding;

	/// <summary>
	/// Gets or sets whether the user is allowed to expand function call messages by clicking on them.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Category("Chat")]
	[Description("Gets or sets whether function call messages can be expanded by clicking on them.")]
	[DefaultValue(true)]
	public bool AllowExpandFunctionCalls
	{
		get => _allowExpandFunctionCalls;
		set
		{
			_allowExpandFunctionCalls = value;

			foreach (var functionCallMessageControl in _messageControls.OfType<IFunctionCallMessageControl>())
				functionCallMessageControl.AllowExpand = value;
		}
	}

	/// <summary>
	/// Gets or sets whether the user is allowed to expand reasoning messages by clicking on them.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Category("Chat")]
	[Description("Gets or sets whether reasoning messages can be expanded by clicking on them.")]
	[DefaultValue(true)]
	public bool AllowExpandReasoning
	{
		get => _allowExpandReasoning;
		set
		{
			_allowExpandReasoning = value;

			foreach (var reasoningMessageControl in _messageControls.OfType<IReasoningMessageControl>())
				reasoningMessageControl.AllowExpand = value;
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
	public IMessageFormatter MessageFormatter
	{
		get => _messageFormatter;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			_messageFormatterWasExplicitlySet = true;
			SetMessageFormatter(value);
		}
	}

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
	public bool IncludeFunctionCalls
	{
		get => _includeFunctionCalls;
		set
		{
			if (_includeFunctionCalls == value)
				return;

			_includeFunctionCalls = value;
			ApplyMessageVisibilityAndHeaders();
		}
	}

	/// <summary>
	/// Gets or sets whether skill accesses recognized by <see cref="SkillCallRecognizer"/> should be displayed.
	/// Skill accesses are shown independently from <see cref="IncludeFunctionCalls"/>.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether recognized skill accesses should be displayed.")]
	[DefaultValue(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool IncludeSkillCalls
	{
		get => _includeSkillCalls;
		set
		{
			if (_includeSkillCalls == value)
				return;

			_includeSkillCalls = value;
			ApplyMessageVisibilityAndHeaders();
		}
	}

	/// <summary>
	/// Gets or sets whether function calls are checked by <see cref="SkillCallRecognizer"/> and displayed as skill accesses.
	/// When disabled, skill tools are shown as regular function calls.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether skill tools such as load_skill are recognized and displayed as skill accesses.")]
	[DefaultValue(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool RecognizeSkillCalls { get; set; } = true;

	/// <summary>
	/// Gets or sets the recognizer that decides whether a function call is a skill access.
	/// Set to <see langword="null"/> to display all calls as regular function calls.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ISkillCallRecognizer? SkillCallRecognizer { get; set; } = new DefaultSkillCallRecognizer();

	/// <summary>
	/// Gets or sets whether reasoning content should be included in the streaming visualization.
	/// When true, reasoning text will be displayed alongside text content during streaming.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether reasoning content should be included in the streaming visualization.")]
	[DefaultValue(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool IncludeReasoning
	{
		get => _includeReasoning;
		set
		{
			if (_includeReasoning == value)
				return;

			_includeReasoning = value;
			ApplyMessageVisibilityAndHeaders();
		}
	}

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
	public string AssistantSenderName
	{
		get => _assistantSender.Name;
		set
		{
			var senderName = value ?? string.Empty;
			if (string.Equals(_assistantSender.Name, senderName, StringComparison.Ordinal))
				return;

			_assistantSender.Name = senderName;
			RefreshAssistantMessageControls();
		}
	}

	/// <summary>
	/// Gets or sets whether files can be dropped onto the chat and sent as attachments.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether files can be dropped onto the chat and sent as attachments.")]
	[DefaultValue(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool AllowFileAttachments
	{
		get => _allowFileAttachments;
		set
		{
			if (_allowFileAttachments == value)
				return;

			_allowFileAttachments = value;
			foreach (var fileDropTarget in _fileDropTargets)
			{
				if (!fileDropTarget.IsDisposed)
					fileDropTarget.AllowDrop = value;
			}
		}
	}

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
	/// Gets or sets whether an action bar (copy, go back) is shown when the pointer rests over a message.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether an action bar with copy and go-back buttons is shown when the pointer rests over a message.")]
	[DefaultValue(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool ShowMessageActions
	{
		get => _showMessageActions;
		set
		{
			_showMessageActions = value;
			if (!value)
				HideMessageActionBar(clearTarget: true);
		}
	}

	/// <summary>
	/// Gets or sets the delay in milliseconds before the message action bar is shown.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets the hover delay in milliseconds before the message action bar is shown.")]
	[DefaultValue(800)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public int MessageActionsHoverDelay { get; set; } = 800;

	/// <summary>
	/// Updates the visibility of the welcome control based on the current message history.
	/// </summary>
	protected virtual void UpdateWelcomeControlVisibility()
	{
		if (WelcomeControl is not null)
			WelcomeControl.Visible = ShouldShowWelcomeControl();
	}

	private void ApplyWelcomeControlSettings()
	{
		if (WelcomeControl is not null)
			ApplyWelcomeControlSettings(WelcomeControl);
	}

	/// <summary>
	/// Applies the current welcome text and font to a welcome control.
	/// </summary>
	/// <param name="control">The welcome control to update.</param>
	protected virtual void ApplyWelcomeControlSettings(Control control)
	{
		control.Font = WelcomeFont;
		control.ForeColor = ForeColor;
		control.BackColor = BackColor;

		var contentControl = control.Controls.Count == 1
			? control.Controls[0]
			: control;
		contentControl.Font = WelcomeFont;
		contentControl.ForeColor = ForeColor;
		contentControl.BackColor = BackColor;
		contentControl.Text = WelcomeMessage;
		ApplyWelcomeTextAlignment(contentControl);
	}

	/// <summary>
	/// Applies the configured welcome text alignment to the control that renders the text.
	/// </summary>
	/// <param name="control">The welcome content control to update.</param>
	protected virtual void ApplyWelcomeTextAlignment(Control control)
	{
		if (control is Label label)
			label.TextAlign = WelcomeTextAlign;
	}

	private void ApplyMessageFont()
	{
		if (MessageHistoryControl is null)
			return;

		foreach (var control in EnumerateDescendantControls(MessageHistoryControl))
		{
			if (control is IChatMessageControl)
				ApplyMessageFont(control);
		}
	}

	private void ApplyInputFont()
	{
		if (InputControl is not null)
			ApplyInputFont(InputControl);
	}

	/// <summary>
	/// Applies the configured message font to a message control.
	/// </summary>
	/// <param name="control">The message control to update.</param>
	protected virtual void ApplyMessageFont(Control control) => control.Font = MessageFont;

	/// <summary>
	/// Applies the configured input font to the input control.
	/// </summary>
	/// <param name="control">The input control to update.</param>
	protected virtual void ApplyInputFont(Control control) => control.Font = InputFont;

	private void ApplyHistoryColors()
	{
		if (SplitContainerControl is ISplitContainerControl { HistoryPanel: { } historyPanel })
		{
			historyPanel.ForeColor = ForeColor;
			historyPanel.BackColor = BackColor;
		}

		if (MessageHistoryControl is not null)
			ApplyHistoryColors(MessageHistoryControl);
	}

	/// <summary>
	/// Applies the global chat colors to the history control.
	/// </summary>
	/// <param name="control">The history control to update.</param>
	protected virtual void ApplyHistoryColors(Control control)
	{
		control.ForeColor = ForeColor;
		control.BackColor = BackColor;
	}

	private void ApplyMessageColors()
	{
		if (MessageHistoryControl is null)
			return;

		foreach (var control in EnumerateDescendantControls(MessageHistoryControl))
		{
			if (control is IChatMessageControl)
				ApplyMessageColors(control);
		}
	}

	/// <summary>
	/// Applies the global chat colors to a message control.
	/// </summary>
	/// <param name="control">The message control to update.</param>
	protected virtual void ApplyMessageColors(Control control)
	{
		control.ForeColor = ForeColor;
		control.BackColor = BackColor;
	}

	private void ApplyInputColors()
	{
		if (SplitContainerControl is ISplitContainerControl { ChatInputPanel: { } inputPanel })
		{
			inputPanel.ForeColor = ForeColor;
			inputPanel.BackColor = BackColor;
		}

		if (InputControl is not null)
			ApplyInputColors(InputControl);
	}

	/// <summary>
	/// Applies the global chat colors to the input control.
	/// </summary>
	/// <param name="control">The input control to update.</param>
	protected virtual void ApplyInputColors(Control control)
	{
		control.ForeColor = ForeColor;
		control.BackColor = BackColor;
	}

	private void ApplyInputAppearance()
	{
		if (InputControl is not null)
			ApplyInputAppearance(InputControl);
	}

	/// <summary>
	/// Applies the configured editor-specific appearance to an input control that opts in.
	/// </summary>
	/// <param name="control">The input control to update.</param>
	protected virtual void ApplyInputAppearance(Control control)
	{
		if (control is not IChatInputAppearanceControl appearanceControl)
			return;

		appearanceControl.EditorBackColor = InputEditorBackColor;
		appearanceControl.EditorForeColor = InputEditorForeColor;
		appearanceControl.EditorBorderStyle = InputEditorBorderStyle;
		appearanceControl.PlaceholderText = InputPlaceholderText;
	}

	private void ApplyHistoryLayout()
	{
		if (SplitContainerControl is ISplitContainerControl { HistoryPanel: { } historyPanel })
		{
			historyPanel.Padding = HistoryPadding;
			return;
		}

		if (MessageHistoryControl is not null)
			MessageHistoryControl.Padding = HistoryPadding;
	}

	private void ApplyMessageLayout()
	{
		if (MessageHistoryControl is null)
			return;

		foreach (var control in EnumerateDescendantControls(MessageHistoryControl))
		{
			if (control is IChatMessageControl)
				ApplyMessageLayout(control);
		}
	}

	/// <summary>
	/// Applies the configured content padding and outer margin to a message control.
	/// </summary>
	/// <param name="control">The message control to update.</param>
	protected virtual void ApplyMessageLayout(Control control)
	{
		if (control is IChatMessageLayoutControl layoutControl)
		{
			control.Margin = MessageMargin;
			layoutControl.ContentPadding = MessagePadding;
		}
	}

	private void ApplyInputLayout()
	{
		if (InputControl is not null)
			ApplyInputLayout(InputControl);
	}

	/// <summary>
	/// Applies the configured content padding to the input control.
	/// </summary>
	/// <param name="control">The input control to update.</param>
	protected virtual void ApplyInputLayout(Control control)
	{
		if (control is IChatInputLayoutControl layoutControl)
			layoutControl.ContentPadding = InputPadding;
	}

	private void ApplySplitContainerLayout()
	{
		if (SplitContainerControl is IChatSplitContainerLayoutControl layoutControl)
			layoutControl.IsSplitterFixed = IsSplitterFixed;
	}

	private static IEnumerable<Control> EnumerateDescendantControls(Control parent)
	{
		foreach (Control control in parent.Controls)
		{
			yield return control;

			foreach (var descendant in EnumerateDescendantControls(control))
				yield return descendant;
		}
	}

	private static Padding ValidatePadding(Padding value, string parameterName)
	{
		if (value.Left < 0 || value.Top < 0 || value.Right < 0 || value.Bottom < 0)
			throw new ArgumentOutOfRangeException(parameterName, value, "Padding values cannot be negative.");

		return value;
	}

	/// <summary>
	/// Determines whether the welcome control should be displayed based on the current message history.
	/// </summary>

	protected virtual bool ShouldShowWelcomeControl() => !_messages.Any(ShouldDisplayMessage);

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);

		if (_messageFont is null)
			ApplyMessageFont();
		if (_inputFont is null)
			ApplyInputFont();
		if (_welcomeFont is null)
			ApplyWelcomeControlSettings();
	}

	/// <inheritdoc />
	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		ApplyHistoryColors();
		ApplyMessageColors();
		ApplyInputColors();
		ApplyWelcomeControlSettings();
	}

	/// <inheritdoc />
	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		ApplyHistoryColors();
		ApplyMessageColors();
		ApplyInputColors();
		ApplyWelcomeControlSettings();
	}


	/// <inheritdoc/>
	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);

		if (!_defaultMessageFormatterWasApplied)
		{
			if (!_messageFormatterWasExplicitlySet && CreateDefaultMessageFormatter() is { } defaultFormatter)
				SetMessageFormatter(defaultFormatter);
			_defaultMessageFormatterWasApplied = true;
		}
	}

	/// <inheritdoc />
	protected override void OnHandleDestroyed(EventArgs e)
	{
		if (Disposing)
		{
			CancelCurrentOperation();
			DisposeMessageActionBar();
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

	private IChatMessageControl AddStreamingVisualizationMessage(
		ISender sender,
		IChatMessageContent content,
		HashSet<IChatMessage> visualizationMessages)
	{
		var message = AddChatMessage(sender, content);
		visualizationMessages.Add(message);
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

		UpdateWelcomeControlVisibility()
;
		var messageControl = AppendMessageControl(message);

		var inputControl = InputControl as IChatInputControl;
		Interlocked.Increment(ref _streamingMessageCount);

		// loop through the stream in a background thread and append the chunks to the string builder
		context.Post(async (_) =>
		{
			try
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
				finally
				{
					messageControl.SetIsReceivingStream(false);
				}
			}
			catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
			{
				// Cancellation is expected and must not be reported as an error.
			}
			catch (Exception ex)
			{
				try
				{
					var errorArgs = new Helper.ChatErrorEventArgs(ex);
					ErrorOccurred?.Invoke(this, errorArgs);
					if (!errorArgs.Handled && !cancellationSource.Token.IsCancellationRequested)
						throw;
				}
				finally
				{
					exceptionCallback?.Invoke(ex);
				}
			}
			finally
			{
				try
				{
					if (ownsCurrentOperation && ClearCancellationSource(cancellationSource))
						inputControl?.SetIsReceivingStream(false, allowCancellation: false);
				}
				finally
				{
					cancellationSource.Dispose();
					Interlocked.Decrement(ref _streamingMessageCount);
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
		var messageIndex = _messages.FindIndex(candidate => ReferenceEquals(candidate, message));
		if (messageIndex < 0)
			messageIndex = _messages.IndexOf(message);
		if (messageIndex < 0)
			return;

		var removedMessage = _messages[messageIndex];
		_messages.RemoveAt(messageIndex);
		_functionCallVisualizationMessages.Remove(removedMessage);
		_reasoningVisualizationMessages.Remove(removedMessage);

		var removedControl = _messageControls.FirstOrDefault(
			messageControl => ReferenceEquals(messageControl.Message, removedMessage));
		if (removedControl is not null)
			_messageControls.Remove(removedControl);
		if (removedControl is not null && ReferenceEquals(removedControl, _messageActionsTarget))
			HideMessageActionBar(clearTarget: true);

		if (MessageHistoryControl is IChatMessageHistoryControl casted)
		{
			casted.RemoveMessageControl(removedMessage);
		}
		else if (removedControl is Control control)
		{
			removedControl.Message = null;
			control.Dispose();
		}

		ApplyMessageVisibilityAndHeaders();
	}

	/// <summary>
	/// Starts a new conversation by cancelling the current operation and clearing the
	/// message history and input draft.
	/// </summary>
	public virtual void StartNewChat()
	{
		CancelCurrentOperation();
		(InputControl as IChatInputControl)?.SetIsReceivingStream(false, allowCancellation: false);
		HideMessageActionBar(clearTarget: true);

		_messages.Clear();
		_functionCallVisualizationMessages.Clear();
		_reasoningVisualizationMessages.Clear();
		PopulateMessages();

		if (InputControl is IChatInputDraftControl draftControl)
			draftControl.ClearDraft();

		_attachmentDraftVisible = false;
		NewChatStarted?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Permanently reverts the conversation to the given message. Every later message is removed from the
	/// history, so it is no longer sent to the <see cref="IChatClient"/>. A message sent by the user is removed
	/// as well and its text and attachments are placed in the input, ready to be edited and sent again.
	/// Otherwise, the message is kept and the input draft is cleared.
	/// Nothing happens while a response is still being received (see <see cref="IsBusy"/>). A tool call that
	/// still waits for approval is rejected first.
	/// </summary>
	/// <param name="message">The message to revert the conversation to.</param>
	/// <returns><see langword="true"/> if the conversation was reverted; otherwise, <see langword="false"/>.</returns>
	public virtual bool RevertToMessage(IChatMessage message)
	{
		ArgumentNullException.ThrowIfNull(message);

		if (IsBusy)
			return false;

		var messageIndex = _messages.FindIndex(candidate => ReferenceEquals(candidate, message));
		if (messageIndex < 0)
			return false;

		AbandonPendingApprovals();
		HideMessageActionBar(clearTarget: true);

		var isUserMessage = IsUserMessage(message);
		var firstRemovedIndex = isUserMessage ? messageIndex : messageIndex + 1;
		var removedMessages = _messages.Skip(firstRemovedIndex).ToArray();
		for (var index = removedMessages.Length - 1; index >= 0; index--)
			RemoveMessage(removedMessages[index]);

		if (isUserMessage)
			SetInputDraft(message.Content);
		else if (InputControl is IChatInputDraftControl draftControl)
			draftControl.ClearDraft();

		ConversationReverted?.Invoke(this, new ConversationRevertedEventArgs(message, removedMessages));
		return true;
	}

	/// <summary>
	/// Determines whether a message was sent by the user of this chat control.
	/// </summary>
	/// <param name="message">The message to evaluate.</param>
	/// <returns><see langword="true"/> if the message was sent by the user; otherwise, <see langword="false"/>.</returns>
	protected virtual bool IsUserMessage(IChatMessage message)
	{
		if (ReferenceEquals(message.Sender, Sender))
			return true;

		return message.Content is not (FunctionCallMessageContent or ReasoningMessageContent) &&
			DetermineChatRole(message.Sender?.Name ?? "User") == ChatRole.User;
	}

	private void SetInputDraft(IChatMessageContent content)
	{
		if (InputControl is not IChatInputDraftEditorControl draftEditor)
			return;

		if (content is FileAttachmentMessageContent attachmentContent)
			draftEditor.SetDraft(attachmentContent.Text, attachmentContent.Attachments);
		else
			draftEditor.SetDraft(content.ToString() ?? string.Empty, []);

		InputControl.Focus();
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
		ApplySplitContainerLayout();

		MessageHistoryControl = (Control)CreateMessageHistoryControl();
		splitContainer?.HistoryPanel?.Controls.Add(MessageHistoryControl);
		LayoutMessageHistoryControl(MessageHistoryControl);
		ApplyHistoryColors();
		ApplyHistoryLayout();

		WelcomeControl = CreateWelcomeControl();
		ApplyWelcomeControlSettings(WelcomeControl);
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
		ApplyInputFont(InputControl);
		ApplyInputColors();
		ApplyInputAppearance(InputControl);
		ApplyInputLayout(InputControl);

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
		HideMessageActionBar(clearTarget: true);
		var previousControls = _messageControls.ToArray();
		_messageControls.Clear();

		var messageHistory = MessageHistoryControl as IChatMessageHistoryControl;
		messageHistory?.ClearMessageControls();

		foreach (var messageControl in previousControls)
		{
			var control = (Control)messageControl;
			if (control.IsDisposed)
				continue;

			messageControl.Message = null;
			control.Dispose();
		}

		if (messageHistory is null)
		{
			UpdateWelcomeControlVisibility();
			return;
		}

		foreach (var message in _messages)
			AppendMessageControl(message);

		UpdateWelcomeControlVisibility();
	}

	private void SetMessageFormatter(IMessageFormatter messageFormatter)
	{
		ArgumentNullException.ThrowIfNull(messageFormatter);
		if (ReferenceEquals(_messageFormatter, messageFormatter))
			return;

		_messageFormatter = messageFormatter;
		foreach (var formattingControl in _messageControls.OfType<IChatMessageFormattingControl>())
			formattingControl.MessageFormatter = messageFormatter;
	}

	private void RefreshAssistantMessageControls()
	{
		foreach (var messageControl in _messageControls)
		{
			if (!ReferenceEquals(messageControl.Message?.Sender, _assistantSender))
				continue;

			if (messageControl is IChatMessageRefreshControl refreshControl)
			{
				refreshControl.RefreshMessageDisplay();
			}
			else
			{
				messageControl.Message = messageControl.Message;
			}
		}

		ApplyMessageVisibilityAndHeaders();
	}

	private void ApplyMessageVisibilityAndHeaders()
	{
		MessageHistoryControl?.SuspendLayout();
		try
		{
			IChatMessage? previousVisibleMessage = null;
			foreach (var messageControl in _messageControls)
			{
				if (messageControl.Message is not { } message)
					continue;

				var control = (Control)messageControl;
				var visible = ShouldDisplayMessage(message);
				if (control.Visible != visible)
					control.Visible = visible;

				var showSenderHeader = visible &&
					(previousVisibleMessage is null ||
						!string.Equals(previousVisibleMessage.Sender?.Name, message.Sender?.Name, StringComparison.Ordinal));
				messageControl.ShowSenderHeader(showSenderHeader);

				if (visible)
					previousVisibleMessage = message;
			}
		}
		finally
		{
			MessageHistoryControl?.ResumeLayout(true);
			MessageHistoryControl?.PerformLayout();
		}

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
			messageControl = CreateFunctionCallMessageControl(message);
		}
		else if (message.Content is ReasoningMessageContent)
		{
			messageControl = CreateReasoningMessageControl(message);
		}
		else
		{
			messageControl = CreateMessageControl(message);
		}

		if (!ReferenceEquals(messageControl.Message, message))
			messageControl.Message = message;
		if (messageControl is IFunctionCallMessageControl functionCallMessageControl)
			functionCallMessageControl.AllowExpand = AllowExpandFunctionCalls;
		if (messageControl is IReasoningMessageControl reasoningMessageControl)
			reasoningMessageControl.AllowExpand = AllowExpandReasoning;

		var control = (Control)messageControl;
		control.Visible = ShouldDisplayMessage(message);
		messageControl.ShowSenderHeader(control.Visible && !IsContinuationMessage(message));
		ApplyMessageFont(control);
		ApplyMessageColors(control);
		ApplyMessageLayout(control);
		_messageControls.Add(messageControl);

		if (MessageHistoryControl is IChatMessageHistoryControl casted)
		{
			LayoutMessageControl(MessageHistoryControl, control);
			casted.AppendMessageControl(messageControl);
		}

		if (control.Parent is not null)
			AttachMessageActionBar(messageControl);

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
		var index = _messages.FindIndex(candidate => ReferenceEquals(candidate, message));
		if (index < 0)
			index = _messages.IndexOf(message);
		if (index <= 0 || !ShouldDisplayMessage(message))
			return false;

		for (var previousIndex = index - 1; previousIndex >= 0; previousIndex--)
		{
			var previousMessage = _messages[previousIndex];
			if (!ShouldDisplayMessage(previousMessage))
				continue;

			return string.Equals(previousMessage.Sender?.Name, message.Sender?.Name, StringComparison.Ordinal);
		}

		return false;
	}

	/// <summary>
	/// Determines whether a message should currently be visible in the history.
	/// </summary>
	/// <param name="message">The message to evaluate.</param>
	/// <returns><see langword="true"/> when the message should be displayed; otherwise, <see langword="false"/>.</returns>
	protected virtual bool ShouldDisplayMessage(IChatMessage message)
	{
		if (_functionCallVisualizationMessages.Contains(message))
		{
			if (message.Content is FunctionCallMessageContent { ApprovalState: not ToolApprovalState.None })
				return true;

			return message.Content is FunctionCallMessageContent { Skill: not null }
				? IncludeSkillCalls
				: IncludeFunctionCalls;
		}

		if (_reasoningVisualizationMessages.Contains(message))
			return IncludeReasoning;

		return true;
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
		var label = new Label { Text = WelcomeMessage, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = WelcomeFont, UseMnemonic = false };
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
	/// Creates a control for displaying a tool call with its result
	/// </summary>
	/// <param name="message">The chat message to create a control for.</param>
	/// <returns>An <see cref="IChatMessageControl"/> instance for the message.</returns>
	protected virtual IFunctionCallMessageControl CreateFunctionCallMessageControl(

IChatMessage message) => new FunctionCallMessageControl { Message = message, AllowExpand = AllowExpandFunctionCalls };

	/// <summary>
	/// Creates a control for displaying reasoning message
	/// </summary>
	/// <param name="message">The chat message to create a control for.</param>
	/// <returns>An <see cref="IChatMessageControl"/> instance for the message.</returns>
	protected virtual IChatMessageControl CreateReasoningMessageControl(IChatMessage message) => new ReasoningMessageControl { MessageFormatter = MessageFormatter, Message = message, AllowExpand = AllowExpandReasoning };

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
		((ISplitContainerControl)splitter).SplitterPosition = _splitterPosition;
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

		if (!AllowFileAttachments)
			return 0;

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
			AbandonPendingApprovals();
			AddMessage(sender, e.Content);
			_chatClientSendCompletion = SendMessageByChatClient();
			MessageSent?.Invoke(this, new MessageSentEventArgs(sender, e.Content));
		}
	}

	/// <summary>
	/// Gets a task that completes when the most recent chat-client send operation has fully unwound,
	/// including background cleanup after a cancellation. Intended for tests and hosts that need to
	/// await the operation deterministically before disposing their synchronization context.
	/// </summary>
	internal Task ChatClientSendCompletion => _chatClientSendCompletion;

	/// <summary>
	/// Cancels the current chat client or streaming operation, if one is running.
	/// </summary>
	public virtual void CancelCurrentOperation()
	{
		CancellationTokenSource? cancellationSource;
		lock (_cancellationLock)
			cancellationSource = _currentCancellationTokenSource;

		TryCancel(cancellationSource);
		RejectPendingApprovals();
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

	private bool ClearCancellationSource(CancellationTokenSource cancellationSource)
	{
		lock (_cancellationLock)
		{
			if (!ReferenceEquals(_currentCancellationTokenSource, cancellationSource))
				return false;

			_currentCancellationTokenSource = null;
			return true;
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

		control.AllowDrop = AllowFileAttachments;
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
			if (!AllowFileAttachments)
				return;

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

	/// <summary>
	/// Creates the action bar shown in the top-right corner of a message while the pointer rests over it.
	/// The bar receives one button per action from <see cref="CreateMessageActionButton"/>.
	/// </summary>
	/// <returns>The action bar control.</returns>
	protected virtual Control CreateMessageActionBar()
	{
		return new MessageActionBar
		{
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			FlowDirection = FlowDirection.LeftToRight,
			Margin = Padding.Empty,
			Padding = new Padding(2),
			WrapContents = false
		};
	}

	/// <summary>
	/// Creates the icon-only button for an action of the action bar.
	/// </summary>
	/// <param name="action">The action the button runs.</param>
	/// <returns>The button control. Its click is wired by the caller.</returns>
	protected virtual Control CreateMessageActionButton(ChatMessageAction action)
	{
		ArgumentNullException.ThrowIfNull(action);

		var button = new MessageActionButton
		{
			FlatStyle = FlatStyle.Flat,
			Margin = new Padding(1, 0, 1, 0),
			Size = new Size(26, 24),
			TabStop = false,
			UseMnemonic = false
		};
		button.FlatAppearance.BorderSize = 0;

		if (action.Image is not null)
		{
			button.Image = action.Image;
		}
		else
		{
			button.Glyph = action.Kind switch
			{
				ChatMessageActionKind.Revert => ChatIcons.Revert,
				_ => ChatIcons.Copy
			};
			if (ChatIcons.IconFontName is not null)
				button.Font = ChatIcons.CreateFont(10f);
		}

		return button;
	}

	/// <summary>
	/// Gets the actions offered in the action bar of a message.
	/// Going back is not offered while a response is still being received.
	/// </summary>
	/// <param name="messageControl">The control displaying the message.</param>
	/// <returns>The actions in display order.</returns>
	protected virtual IEnumerable<ChatMessageAction> GetMessageActions(IChatMessageControl messageControl)
	{
		if (messageControl.Message is not { } message)
			yield break;

		yield return new ChatMessageAction(ChatMessageActionKind.Copy, () => CopyMessage(messageControl));

		if (!IsBusy)
			yield return new ChatMessageAction(ChatMessageActionKind.Revert, () => RevertToMessage(message));
	}

	/// <summary>
	/// Gets whether a response is currently being requested or streamed.
	/// While the response only waits for the user to approve or reject a tool call, the chat is not busy:
	/// sending a new message or going back rejects the open approvals instead.
	/// </summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool IsBusy
	{
		get
		{
			if (_streamingMessageCount > 0)
				return true;
			if (_isWaitingForApproval)
				return false;

			lock (_cancellationLock)
			{
				if (_currentCancellationTokenSource is not null)
					return true;
			}

			return !_chatClientSendCompletion.IsCompleted;
		}
	}

	/// <summary>
	/// Gets whether the chat waits for the user to approve or reject a tool call.
	/// </summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool IsWaitingForApproval => _isWaitingForApproval;

	/// <summary>
	/// Ends the turn that waits for approvals by rejecting them, so a new message or a revert starts from a
	/// consistent conversation. The rejection stays in the history and is sent with the next request.
	/// </summary>
	private void AbandonPendingApprovals()
	{
		if (!_isWaitingForApproval)
			return;

		CancelCurrentOperation();
	}

	/// <summary>
	/// Copies the plain text of a message to the clipboard.
	/// </summary>
	/// <param name="messageControl">The control displaying the message.</param>
	protected void CopyMessage(IChatMessageControl messageControl)
	{
		var text = GetMessagePlainText(messageControl);
		if (!string.IsNullOrEmpty(text))
			Clipboard.SetText(text);
	}

	/// <summary>
	/// Gets the readable text of a message without any formatting.
	/// </summary>
	/// <param name="messageControl">The control displaying the message.</param>
	/// <returns>The plain text of the message.</returns>
	protected virtual string GetMessagePlainText(IChatMessageControl messageControl)
	{
		var content = messageControl.Message?.Content;
		var text = content switch
		{
			null => messageControl.ToString(),
			FileAttachmentMessageContent attachmentContent => attachmentContent.Text,
			FunctionCallMessageContent or ReasoningMessageContent => messageControl.ToString(),
			_ => content.ToString()
		};

		return _plainTextFormatter.Format(text ?? string.Empty);
	}

	/// <summary>
	/// Runs a message action and reports a failure through <see cref="ErrorOccurred"/> instead of throwing,
	/// as the clipboard can temporarily be unavailable.
	/// </summary>
	/// <param name="action">The action to run.</param>
	protected void RunMessageAction(ChatMessageAction action)
	{
		ArgumentNullException.ThrowIfNull(action);

		try
		{
			action.Execute();
		}
		catch (Exception ex)
		{
			try
			{
				ErrorOccurred?.Invoke(this, new Helper.ChatErrorEventArgs(ex));
			}
			catch
			{
				// A failing error handler must not escape the button click.
			}
		}
	}

	private void AttachMessageActionBar(IChatMessageControl messageControl)
	{
		EnsureMessageActionBar();

		void WireHoverEvents(Control control)
		{
			control.MouseEnter += (_, _) => StartMessageActionsHover(messageControl);
			control.MouseMove += (_, _) => StartMessageActionsHover(messageControl);
			control.MouseLeave += (_, _) => StartMessageActionsHideDelay();
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

	private void EnsureMessageActionBar()
	{
		if (_messageActionBar is not null)
			return;

		_messageActionsHoverTimer = new System.Windows.Forms.Timer { Interval = Math.Max(1, MessageActionsHoverDelay) };
		_messageActionsHideTimer = new System.Windows.Forms.Timer { Interval = 150 };
		_messageActionBar = CreateMessageActionBar();
		_messageActionBar.Visible = false;
		_messageActionBar.MouseEnter += (_, _) => _messageActionsHideTimer.Stop();
		_messageActionBar.MouseLeave += (_, _) => StartMessageActionsHideDelay();
		_messageActionsHoverTimer.Tick += (_, _) =>
		{
			_messageActionsHoverTimer.Stop();
			DisplayMessageActionBar();
		};
		_messageActionsHideTimer.Tick += (_, _) =>
		{
			_messageActionsHideTimer.Stop();
			if (!IsPointerInsideMessageActionsArea())
				HideMessageActionBar(clearTarget: true);
		};

		Controls.Add(_messageActionBar);
		_messageActionBar.BringToFront();

		if (MessageHistoryControl is IChatMessageHistoryViewport viewport)
			viewport.ViewportChanged += MessageHistoryViewport_Changed;

		if (MessageHistoryControl is not null)
		{
			MessageHistoryControl.SizeChanged += MessageHistoryViewport_Changed;
			MessageHistoryControl.VisibleChanged += MessageHistoryViewport_Changed;
		}
	}

	private void StartMessageActionsHover(IChatMessageControl messageControl)
	{
		if (_messageActionBar is null || _messageActionsHoverTimer is null || _messageActionsHideTimer is null)
			return;
		if (!ShowMessageActions)
			return;

		_messageActionsHideTimer.Stop();
		if (!ReferenceEquals(_messageActionsTarget, messageControl))
		{
			HideMessageActionBar(clearTarget: false);
			_messageActionsTarget = messageControl;
		}

		if (_messageActionBar.Visible || _messageActionsHoverTimer.Enabled)
			return;

		_messageActionsHoverTimer.Interval = Math.Max(1, MessageActionsHoverDelay);
		_messageActionsHoverTimer.Start();
	}

	private void StartMessageActionsHideDelay()
	{
		if (_messageActionsHideTimer is null)
			return;

		_messageActionsHideTimer.Stop();
		_messageActionsHideTimer.Start();
	}

	private void DisplayMessageActionBar()
	{
		if (_messageActionBar is null ||
			_messageActionsTarget is not Control messageControl ||
			_messageActionsTarget.Message is null ||
			MessageHistoryControl is null ||
			messageControl.IsDisposed ||
			!messageControl.Visible ||
			!IsHandleCreated ||
			!messageControl.IsHandleCreated ||
			!MessageHistoryControl.IsHandleCreated ||
			!IsPointerInsideMessageActionsArea())
		{
			return;
		}

		var actions = GetMessageActions(_messageActionsTarget).ToArray();
		if (actions.Length == 0)
			return;

		PopulateMessageActionBar(actions);

		var messageBounds = RectangleToClient(messageControl.RectangleToScreen(messageControl.ClientRectangle));
		var historyBounds = RectangleToClient(MessageHistoryControl.RectangleToScreen(MessageHistoryControl.ClientRectangle));
		historyBounds.Intersect(ClientRectangle);
		var visibleMessageBounds = Rectangle.Intersect(messageBounds, historyBounds);
		var barSize = _messageActionBar.GetPreferredSize(Size.Empty);
		if (visibleMessageBounds.Width < barSize.Width || visibleMessageBounds.Height < barSize.Height)
			return;

		// Straddle the top edge of the message, so the bar covers the spacing and sender header but not the text.
		var preferredX = messageBounds.Right - messageControl.Padding.Right - barSize.Width;
		var maximumX = visibleMessageBounds.Right - barSize.Width;
		var preferredY = messageBounds.Top - (barSize.Height / 2);
		var minimumY = historyBounds.Top;
		var maximumY = visibleMessageBounds.Bottom - barSize.Height;

		_messageActionBar.Size = barSize;
		_messageActionBar.Location = new Point(
			Math.Clamp(preferredX, visibleMessageBounds.Left, maximumX),
			Math.Clamp(preferredY, minimumY, Math.Max(minimumY, maximumY)));
		_messageActionBar.BringToFront();
		_messageActionBar.Visible = true;
	}

	private void PopulateMessageActionBar(IReadOnlyList<ChatMessageAction> actions)
	{
		if (_messageActionBar is null)
			return;

		_messageActionBar.SuspendLayout();
		try
		{
			while (_messageActionBar.Controls.Count > 0)
				_messageActionBar.Controls[0].Dispose();

			foreach (var action in actions)
			{
				var button = CreateMessageActionButton(action);
				button.Cursor = Cursors.Default;
				button.Click += (_, _) =>
				{
					HideMessageActionBar(clearTarget: true);
					RunMessageAction(action);
				};
				button.MouseEnter += (_, _) => _messageActionsHideTimer?.Stop();
				button.MouseLeave += (_, _) => StartMessageActionsHideDelay();
				_messageActionBar.Controls.Add(button);
			}

			ApplyMessageActionBarColors(_messageActionBar);
		}
		finally
		{
			_messageActionBar.ResumeLayout(performLayout: true);
		}
	}

	/// <summary>
	/// Applies the chat colors to the action bar and its buttons.
	/// </summary>
	/// <param name="actionBar">The action bar to update.</param>
	protected virtual void ApplyMessageActionBarColors(Control actionBar)
	{
		ArgumentNullException.ThrowIfNull(actionBar);

		var backColor = SystemColors.Window;
		actionBar.BackColor = backColor;
		actionBar.ForeColor = SystemColors.WindowText;
		foreach (Control button in actionBar.Controls)
		{
			button.BackColor = backColor;
			button.ForeColor = SystemColors.WindowText;
		}
	}

	private bool IsPointerInsideMessageActionsArea()
	{
		var pointerPosition = Cursor.Position;
		var insideMessage = _messageActionsTarget is Control messageControl &&
			messageControl.IsHandleCreated &&
			messageControl.RectangleToScreen(messageControl.ClientRectangle).Contains(pointerPosition);
		var insideBar = _messageActionBar is { Visible: true, IsHandleCreated: true } &&
			_messageActionBar.RectangleToScreen(_messageActionBar.ClientRectangle).Contains(pointerPosition);
		return insideMessage || insideBar;
	}

	private void HideMessageActionBar(bool clearTarget)
	{
		_messageActionsHoverTimer?.Stop();
		_messageActionsHideTimer?.Stop();
		if (_messageActionBar is { IsDisposed: false })
			_messageActionBar.Visible = false;

		if (clearTarget)
			_messageActionsTarget = null;
	}

	private void MessageHistoryViewport_Changed(object? sender, EventArgs e)
	{
		HideMessageActionBar(clearTarget: true);
	}

	private void DisposeMessageActionBar()
	{
		if (MessageHistoryControl is IChatMessageHistoryViewport viewport)
			viewport.ViewportChanged -= MessageHistoryViewport_Changed;

		if (MessageHistoryControl is not null)
		{
			MessageHistoryControl.SizeChanged -= MessageHistoryViewport_Changed;
			MessageHistoryControl.VisibleChanged -= MessageHistoryViewport_Changed;
		}

		_messageActionsHoverTimer?.Dispose();
		_messageActionsHideTimer?.Dispose();
		_messageActionBar?.Dispose();
		_messageActionsHoverTimer = null;
		_messageActionsHideTimer = null;
		_messageActionBar = null;
		_messageActionsTarget = null;
	}

	/// <summary>
	/// A flow panel with a thin border, so the action bar stands out from the message below it.
	/// </summary>
	private sealed class MessageActionBar : FlowLayoutPanel
	{
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var bounds = ClientRectangle;
			bounds.Width--;
			bounds.Height--;
			using var pen = new Pen(Color.FromArgb(96, ForeColor));
			e.Graphics.DrawRectangle(pen, bounds);
		}
	}

	private sealed class MessageActionButton : Button
	{
		public MessageActionButton()
		{
			SetStyle(ControlStyles.Selectable, false);
		}

		/// <summary>
		/// Gets or sets the icon font glyph. It is painted with grayscale anti-aliasing, because ClearType
		/// adds coloured fringes to the thin icon strokes.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string Glyph { get; set; } = string.Empty;

		protected override bool ShowFocusCues => false;

		protected override void OnPaint(PaintEventArgs pevent)
		{
			base.OnPaint(pevent);
			if (string.IsNullOrEmpty(Glyph))
				return;

			pevent.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			using var brush = new SolidBrush(ForeColor);
			using var format = new StringFormat(StringFormat.GenericTypographic)
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			pevent.Graphics.DrawString(Glyph, Font, brush, ClientRectangle, format);
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
	private async Task SendMessageByChatClient()
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

				// Approval requests end a turn; the decisions are sent back in a follow-up request.
				while (true)
				{
					var chatMessages = ConvertToChatMessages();

					var chatOptionsArgs = new ChatOptionsRequestedEventArgs(ChatOptions);
					ChatOptionsRequested?.Invoke(this, chatOptionsArgs);
					var chatOptions = chatOptionsArgs.ChatOptions;
					var approvalRequests = new List<FunctionCallMessageContent>();

					if (UseStreaming)
					{
						// Use streaming response
						var streamingResponse = chatClient.GetStreamingResponseAsync(chatMessages, chatOptions, cancellationToken: cancellationSource.Token);
						await HandleStreamingResponseAsync(_assistantSender, streamingResponse, approvalRequests, cancellationSource.Token).ConfigureAwait(true);
					}
					else
					{
						// Use non-streaming response
						var response = await chatClient
							.GetResponseAsync(chatMessages, chatOptions, cancellationToken: cancellationSource.Token)
							.WaitAsync(cancellationSource.Token)
							.ConfigureAwait(true);
						HandleNonStreamingResponse(_assistantSender, response, approvalRequests);
					}

					if (approvalRequests.Count == 0 || cancellationSource.IsCancellationRequested)
						break;

					// While the user decides, the chat is idle: the input offers sending again and messages can
					// be reverted. Both reject the open approvals first (see AbandonPendingApprovals).
					_isWaitingForApproval = true;
					inputControl?.SetIsReceivingStream(false, allowCancellation: false);
					try
					{
						await WaitForApprovalsAsync(approvalRequests, cancellationSource.Token).ConfigureAwait(true);
					}
					finally
					{
						_isWaitingForApproval = false;
					}

					if (cancellationSource.IsCancellationRequested)
						break;

					inputControl?.SetIsReceivingStream(true, allowCancellation: true);
				}
			}
			catch (OperationCanceledException)
			{
				// Operation was cancelled, this is expected behavior
			}
			catch (Exception ex)
			{
				if (!cancellationSource.IsCancellationRequested)
				{
					var errorArgs = new Helper.ChatErrorEventArgs(ex);
					ErrorOccurred?.Invoke(this, errorArgs);
					if (!errorArgs.Handled)
						AddMessage(new NamedSender("System"), new StringMessageContent($"Error: {ex.Message}"));
				}
			}
			finally
			{
				try
				{
					if (ClearCancellationSource(cancellationSource))
						inputControl?.SetIsReceivingStream(false, allowCancellation: false);
				}
				finally
				{
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

			// Reasoning is presentation-only and must not be sent back as assistant output.
			if (message.Content is ReasoningMessageContent)
				continue;

			// Handle function call messages separately to preserve structured tool call data
			if (message.Content is FunctionCallMessageContent funcCallContent)
			{
				// Convert IReadOnlyDictionary to IDictionary for FunctionCallContent
				IDictionary<string, object?>? arguments = funcCallContent.Arguments is not null
					? new Dictionary<string, object?>(funcCallContent.Arguments)
					: null;
				var functionCall = new FunctionCallContent(funcCallContent.CallId, funcCallContent.Name, arguments);

				// Decided approvals without a result yet are sent back so the client can execute or reject the call.
				if (funcCallContent.Result is null && funcCallContent.ApprovalRequestId is { } requestId)
				{
					if (funcCallContent.ApprovalState is ToolApprovalState.Approved or ToolApprovalState.Rejected)
					{
						var approvalRequest = new ToolApprovalRequestContent(requestId, functionCall);
						result.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, [approvalRequest]));
						result.Add(new Microsoft.Extensions.AI.ChatMessage(
							ChatRole.User,
							[approvalRequest.CreateResponse(funcCallContent.ApprovalState == ToolApprovalState.Approved)]));
					}

					continue;
				}

				// Add the function call from the assistant
				var functionCallMessage = new Microsoft.Extensions.AI.ChatMessage(
					ChatRole.Assistant,
					[functionCall]
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
	private async Task HandleStreamingResponseAsync(ISender sender, IAsyncEnumerable<ChatResponseUpdate> stream, List<FunctionCallMessageContent> approvalRequests, CancellationToken cancellationToken)
	{
		var pendingCalls = new Dictionary<string, FunctionCallMessageContent>();
		var textChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
		var textStreamStarted = false;
		var hadNonTextContentSinceLastText = false;
		var textStreamCompletionTasks = new List<Task>();
		ReasoningMessageContent? reasoningMessageContent = null;
		IChatMessageControl? reasoningMessageControl = null;

		void CompleteReasoningMessage()
		{
			reasoningMessageContent?.SetDone();
			reasoningMessageControl?.SetIsReceivingStream(false);
			reasoningMessageContent = null;
			reasoningMessageControl = null;
		}

		try
		{
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

					if (item is FunctionCallContent or FunctionResultContent or ToolApprovalRequestContent)
					{
						if (ProcessToolContent(sender, item, pendingCalls, approvalRequests))
							hadNonTextContentSinceLastText = true;

						// reset the reasoning to be able to start a new control
						CompleteReasoningMessage();
					}
					else if (item is TextReasoningContent reasoningContent)
					{
						if (!string.IsNullOrEmpty(reasoningContent.Text))
						{
							if (reasoningMessageContent == null)
							{
								reasoningMessageContent = new ReasoningMessageContent(reasoningContent.Text);
								reasoningMessageControl = AddStreamingVisualizationMessage(
									sender,
									reasoningMessageContent,
									_reasoningVisualizationMessages);
								reasoningMessageControl.SetIsReceivingStream(true);
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
						CompleteReasoningMessage();
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
						var textStreamCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
						textStreamCompletionTasks.Add(textStreamCompletion.Task);
						try
						{
							AddStreamingMessage(
								sender,
								textChannel.Reader.ReadAllAsync(cancellationToken),
								completionCallback: _ => textStreamCompletion.TrySetResult(),
								exceptionCallback: _ => textStreamCompletion.TrySetResult(),
								cancellationToken: cancellationToken);
						}
						catch
						{
							textStreamCompletion.TrySetResult();
							throw;
						}
					}
					textChannel.Writer.TryWrite(update.Text);
				}
			}
		}
		finally
		{
			try
			{
				CompleteReasoningMessage();
			}
			finally
			{
				textChannel.Writer.TryComplete();
				if (textStreamCompletionTasks.Count > 0)
					await Task.WhenAll(textStreamCompletionTasks).ConfigureAwait(true);
			}
		}
	}

	/// <summary>
	/// Handles a non-streaming response from the IChatClient.
	/// </summary>
	private void HandleNonStreamingResponse(ISender sender, ChatResponse response, List<FunctionCallMessageContent> approvalRequests)
	{
		var pendingCalls = new Dictionary<string, FunctionCallMessageContent>();
		ReasoningMessageContent? reasoningMessageContent = null;
		var addedContent = false;

		foreach (var responseMessage in response.Messages)
		{
			foreach (var item in responseMessage.Contents)
			{
				if (item is FunctionCallContent or FunctionResultContent or ToolApprovalRequestContent)
				{
					reasoningMessageContent?.SetDone();
					reasoningMessageContent = null;

					ProcessToolContent(sender, item, pendingCalls, approvalRequests);
					addedContent = true;
				}
				else if (item is TextReasoningContent reasoningContent &&
					!string.IsNullOrEmpty(reasoningContent.Text))
				{
					if (reasoningMessageContent is null)
					{
						reasoningMessageContent = new ReasoningMessageContent(reasoningContent.Text);
						AddStreamingVisualizationMessage(sender, reasoningMessageContent, _reasoningVisualizationMessages);
					}
					else
					{
						reasoningMessageContent.AppendText(reasoningContent.Text);
					}

					addedContent = true;
				}
				else if (item is TextContent textContent && !string.IsNullOrEmpty(textContent.Text))
				{
					reasoningMessageContent?.SetDone();
					reasoningMessageContent = null;
					AddMessage(sender, new StringMessageContent(textContent.Text));
					addedContent = true;
				}
			}
		}

		reasoningMessageContent?.SetDone();

		if (!addedContent)
			AddMessage(sender, new StringMessageContent(response.Text ?? string.Empty));
	}

	/// <summary>
	/// Displays a function call, its result or an approval request. Calls that are re-sent by the
	/// <see cref="IChatClient"/> after an approval update the existing message instead of adding a new one.
	/// </summary>
	/// <returns><see langword="true"/> when a message was added or updated.</returns>
	private bool ProcessToolContent(
		ISender sender,
		AIContent item,
		Dictionary<string, FunctionCallMessageContent> pendingCalls,
		List<FunctionCallMessageContent> approvalRequests)
	{
		switch (item)
		{
			case ToolApprovalRequestContent { ToolCall: FunctionCallContent approvalCall } approvalRequest:
				{
					var content = FindFunctionCallContent(approvalCall.CallId);
					var isNew = content is null;
					content ??= CreateFunctionCallMessageContent(approvalCall);
					content.RequireApproval(approvalRequest.RequestId);
					if (isNew)
						AddStreamingVisualizationMessage(sender, content, _functionCallVisualizationMessages);
					else
						ApplyMessageVisibilityAndHeaders();

					approvalRequests.Add(content);
					ScrollMessageIntoView(content);
					ToolApprovalRequested?.Invoke(this, new ToolApprovalRequestedEventArgs(content));
					return true;
				}

			case FunctionCallContent functionCall:
				{
					if (FindFunctionCallContent(functionCall.CallId) is { } existing)
					{
						pendingCalls[functionCall.CallId] = existing;
						return false;
					}

					var content = CreateFunctionCallMessageContent(functionCall);
					content.IsFunctionExecuting = true;
					AddStreamingVisualizationMessage(sender, content, _functionCallVisualizationMessages);
					pendingCalls[functionCall.CallId] = content;
					return true;
				}

			case FunctionResultContent functionResult:
				{
					if (!pendingCalls.Remove(functionResult.CallId, out var content))
						content = FindFunctionCallContent(functionResult.CallId);

					if (content is null)
						return false;

					content.SetResult(functionResult.Result, IsErrorResult(content, functionResult));
					return true;
				}
		}

		return false;
	}

	private FunctionCallMessageContent CreateFunctionCallMessageContent(FunctionCallContent functionCall)
	{
		var content = new FunctionCallMessageContent(functionCall.CallId, functionCall.Name ?? string.Empty, functionCall.Arguments);
		content.Skill = RecognizeSkillCalls ? SkillCallRecognizer?.Recognize(content.Name, content.Arguments) : null;
		return content;
	}

	/// <summary>
	/// Scrolls a message into view once its layout has settled, so the approval buttons are not cut off.
	/// </summary>
	private void ScrollMessageIntoView(IChatMessageContent content)
	{
		if (MessageHistoryControl is not ScrollableControl history || !history.IsHandleCreated)
			return;

		if (_messageControls.FirstOrDefault(messageControl => ReferenceEquals(messageControl.Message?.Content, content)) is not Control control)
			return;

		void ScrollToBottom()
		{
			if (history.IsDisposed || control.IsDisposed || !control.Visible)
				return;

			(control as IChatMessageRefreshControl)?.RefreshMessageDisplay();
			history.PerformLayout();
			history.ScrollControlIntoView(control);
			if (history.VerticalScroll.Visible)
				history.AutoScrollPosition = new Point(0, history.DisplayRectangle.Height);
		}

		// The detail rows measure their height asynchronously, so keep following the control until it is decided.
		void OnSizeChanged(object? sender, EventArgs e)
		{
			if (content is FunctionCallMessageContent { ApprovalState: not ToolApprovalState.Pending })
			{
				control.SizeChanged -= OnSizeChanged;
				return;
			}

			history.BeginInvoke(ScrollToBottom);
		}

		control.SizeChanged += OnSizeChanged;
		history.BeginInvoke(ScrollToBottom);
	}

	private FunctionCallMessageContent? FindFunctionCallContent(string callId)
	{
		for (var index = _messages.Count - 1; index >= 0; index--)
		{
			if (_messages[index].Content is FunctionCallMessageContent content &&
				string.Equals(content.CallId, callId, StringComparison.Ordinal))
			{
				return content;
			}
		}

		return null;
	}

	private static bool IsErrorResult(FunctionCallMessageContent content, FunctionResultContent result)
	{
		if (result.Exception is not null)
			return true;

		// Skill providers report failures as text results instead of exceptions.
		return content.Skill is not null &&
			result.Result?.ToString() is { } text &&
			text.StartsWith("Error:", StringComparison.Ordinal);
	}

	/// <summary>
	/// Waits until the user decided on every approval request of the current turn.
	/// Requests still open when the operation is cancelled are rejected.
	/// </summary>
	private static async Task WaitForApprovalsAsync(
		IReadOnlyList<FunctionCallMessageContent> approvalRequests,
		CancellationToken cancellationToken)
	{
		var decided = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		void CheckDecided(object? sender, PropertyChangedEventArgs e)
		{
			if (approvalRequests.All(request => request.ApprovalState != ToolApprovalState.Pending))
				decided.TrySetResult();
		}

		foreach (var request in approvalRequests)
			request.PropertyChanged += CheckDecided;

		try
		{
			CheckDecided(null, new PropertyChangedEventArgs(nameof(FunctionCallMessageContent.ApprovalState)));
			await decided.Task.WaitAsync(cancellationToken).ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			foreach (var request in approvalRequests)
				request.Reject();
			throw;
		}
		finally
		{
			foreach (var request in approvalRequests)
				request.PropertyChanged -= CheckDecided;
		}
	}

	private void RejectPendingApprovals()
	{
		foreach (var message in _messages)
		{
			if (message.Content is FunctionCallMessageContent { ApprovalState: ToolApprovalState.Pending } content)
				content.Reject();
		}
	}

	private static IAsyncEnumerable<T> EnumerateWithCancellation<T>(
		IAsyncEnumerable<T> source,
		CancellationToken cancellationToken)
	{
		// The source is pumped by a detached background task into a channel, and the caller reads the
		// channel. This decouples the caller's continuations from the source iterator entirely:
		//
		//  * The caller awaits the ChannelReader with ConfigureAwait(true), so its own per-item work
		//    (AddMessage and friends) still resumes on the UI thread.
		//  * The pump task runs with any ambient SynchronizationContext suppressed, so every await it
		//    owns - including the source's finally/DisposeAsync cleanup for a cancelled, non-cooperative
		//    stream that only unblocks in the background - stays on the thread pool.
		//
		// Without this decoupling the source iterator would capture the caller's context and try to
		// marshal its late background cleanup back to it. When that context has since been torn down
		// (for example a unit-test SingleThreadedTestSynchronizationContext) the post would throw
		// "This SynchronizationContext has been shut down" and crash the process.
		var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = true
		});

		_ = PumpSourceIntoChannelAsync(source, channel.Writer, cancellationToken);

		return channel.Reader.ReadAllAsync(CancellationToken.None);
	}

	private static async Task PumpSourceIntoChannelAsync<T>(
		IAsyncEnumerable<T> source,
		ChannelWriter<T> writer,
		CancellationToken cancellationToken)
	{
		// The whole body is guarded so this fire-and-forget task can never fault. An unobserved fault
		// would otherwise be re-thrown by the runtime onto a captured SynchronizationContext, which may
		// already be gone (for example a torn-down unit-test context), crashing the process.
		//
		// The source itself is still advanced on the caller's context: chat clients may invoke tools
		// from within the stream, and those tools commonly access UI controls and must therefore run
		// on the UI thread, just like they did when the stream was enumerated directly.
		var previousContext = SynchronizationContext.Current;
		var callerContext = previousContext;
		var callerThreadId = Environment.CurrentManagedThreadId;
		SynchronizationContext.SetSynchronizationContext(null);
		try
		{
			Exception? failure = null;
			IAsyncEnumerator<T>? enumerator = null;
			try
			{
				enumerator = source.GetAsyncEnumerator(cancellationToken);
				while (true)
				{
					var moveNextTask = MoveNextOnContextAsync(enumerator, callerContext, callerThreadId);
					bool hasNext;
					try
					{
						// WaitAsync abandons a non-cooperative MoveNextAsync as soon as cancellation is
						// requested instead of blocking until the source happens to yield or complete.
						hasNext = await moveNextTask.WaitAsync(cancellationToken).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
					{
						// The abandoned enumerator is disposed in the background once its pending
						// move-next completes, keeping cleanup off any captured context.
						_ = DisposeAbandonedEnumeratorAsync(enumerator, moveNextTask);
						enumerator = null;
						break;
					}

					if (!hasNext)
						break;

					await writer.WriteAsync(enumerator.Current, CancellationToken.None).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				// Cancellation is expected and simply completes the channel so the reader stops.
			}
			catch (Exception ex)
			{
				failure = ex;
			}
			finally
			{
				if (enumerator is not null)
				{
					try
					{
						await enumerator.DisposeAsync().ConfigureAwait(false);
					}
					catch
					{
						// Disposal failures during cleanup must not fault this background task.
					}
				}
			}

			writer.TryComplete(failure);
		}
		catch
		{
			// Last-resort guard: never let this fire-and-forget task fault.
			writer.TryComplete();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previousContext);
		}
	}

	private static Task<bool> MoveNextOnContextAsync<T>(IAsyncEnumerator<T> enumerator, SynchronizationContext? context, int contextThreadId)
	{
		if (context is null)
			return enumerator.MoveNextAsync().AsTask();

		if (Environment.CurrentManagedThreadId == contextThreadId)
			return MoveNextWithContext(enumerator, context);

		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		try
		{
			context.Post(_ =>
			{
				try
				{
					MoveNextWithContext(enumerator, context).ContinueWith(
						task =>
						{
							if (task.IsCanceled)
								completion.TrySetCanceled();
							else if (task.IsFaulted)
								completion.TrySetException(task.Exception!.InnerExceptions);
							else
								completion.TrySetResult(task.Result);
						},
						CancellationToken.None,
						TaskContinuationOptions.ExecuteSynchronously,
						TaskScheduler.Default);
				}
				catch (Exception ex)
				{
					completion.TrySetException(ex);
				}
			}, null);
		}
		catch (Exception ex)
		{
			completion.TrySetException(ex);
		}

		return completion.Task;
	}

	private static Task<bool> MoveNextWithContext<T>(IAsyncEnumerator<T> enumerator, SynchronizationContext context)
	{
		var previousContext = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(context);
		try
		{
			return enumerator.MoveNextAsync().AsTask();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previousContext);
		}
	}

	private static async Task DisposeAbandonedEnumeratorAsync<T>(IAsyncEnumerator<T> enumerator, Task<bool> moveNextTask)
	{
		var previousContext = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(null);
		try
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
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previousContext);
		}
	}

	private sealed class MutableSender(string name) : ISender
	{
		public string Name { get; set; } = name;

		public override string ToString() => Name;
	}
}
