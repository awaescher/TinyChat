using TinyChat;
using TinyChat.Messages.Formatting;

namespace DevExpressDemo;

/// <summary>
/// The TinyChat chat control implementation using DevExpress controls.
/// </summary>
public class DXChatControl : ChatControl
{
	/// <inheritdoc />
	protected override IChatMessageHistoryControl CreateMessageHistoryControl() => new StackPanelMessageHistoryControl();

	/// <inheritdoc />
	protected override IChatMessageControl CreateMessageControl(IChatMessage message) => new DXChatMessageControl { MessageFormatter = MessageFormatter, Message = message };

	/// <inheritdoc />
	protected override IChatInputControl CreateChatInputControl() => new DXChatInputControl();

	/// <inheritdoc />
	protected override ISplitContainerControl CreateSplitContainerControl() => new DXChatSplitContainerControl();

	/// <summary>
	/// DevExpress offers basic HTML formatting capabilities.
	/// With the SimplifiedHtmlMessageFormatter and the limited tags that are supported by DevExpress,
	/// we might be able to format most of the common formatting properly.
	/// See: https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting
	/// </summary>
	protected override IMessageFormatter CreateDefaultMessageFormatter() => new SimplifiedHtmlMessageFormatter("b", "i", "s", "u", "br", "sub", "sup", "font", "p", "nbsp", "a", "href", "color", "backcolor", "size");
}
