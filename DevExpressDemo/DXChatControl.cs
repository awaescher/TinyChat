using TinyChat;
using TinyChat.Messages.Rendering;

namespace DevExpressDemo;

/// <summary>
/// The TinyChat chat control implementation using DevExpress controls.
/// </summary>
public class DXChatControl : ChatControl
{
	/// <inheritdoc />
	protected override IChatMessageHistoryControl CreateMessageHistoryControl() => new StackPanelMessageHistoryControl();

	/// <inheritdoc />
	protected override IChatMessageControl CreateMessageControl(IChatMessage message) => new DXChatMessageControl { Message = message, MessageRenderer = MessageRenderer };

	/// <inheritdoc />
	protected override IChatInputControl CreateChatInputControl() => new DXChatInputControl();

	/// <inheritdoc />
	protected override ISplitContainerControl CreateSplitContainerControl() => new DXChatSplitContainerControl();

	/// <summary>
	/// DevExpress offers basic HTML rendering capabilities.
	/// With the SimplifiedHtmlMessageRenderer and the limited tags that are supported by DevExpress,
	/// we might be able to render most of the common formatting properly.
	/// See: https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting
	/// </summary>
	protected override IMessageRenderer CreateDefaultMessageRenderer() => new SimplifiedHtmlMessageRenderer("b", "i", "s", "u", "br", "sub", "sup", "font", "p", "nbsp", "a", "href", "color", "backcolor", "size");
}
