using TinyChat;

namespace DevExpressDemo;

/// <summary>
/// The TinyChat chat control implementation using DevExpress controls.
/// </summary>
public class DXChatControl : ChatControl
{
	/// <inheritdoc />
	protected override IChatMessageHistoryControl CreateMessageHistoryControl() => new StackPanelMessageHistoryControl();

	/// <inheritdoc />
	protected override IChatMessageControl CreateMessageControl(IChatMessage message) => new DXChatMessageControl { Message = message };

	/// <inheritdoc />
	protected override IChatInputControl CreateChatInputControl() => new DXChatInputControl();

	/// <inheritdoc />
	protected override ISplitContainerControl CreateSplitContainerControl() => new DXChatSplitContainerControl();
}
