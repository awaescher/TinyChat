using DevExpress.XtraEditors;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// The TinyChat chat control implementation using DevExpress controls.
/// </summary>
public class DXChatControl : ChatControl
{
	/// <inheritdoc />
	protected override IChatMessageHistoryControl CreateMessageHistoryControl() => new StackPanelMessageHistoryControl();

	/// <inheritdoc />
	protected override Control CreateWelcomeControl() => new DXWelcomeControl();

	/// <inheritdoc />
	protected override void ApplyWelcomeControlSettings(Control control)
	{
		base.ApplyWelcomeControlSettings(control);
		if (control is DXWelcomeControl welcomeControl)
			welcomeControl.TextAlign = WelcomeTextAlign;

		ApplyDevExpressRootFont(control, WelcomeFont);
	}

	/// <inheritdoc />
	protected override void ApplyMessageFont(Control control)
	{
		base.ApplyMessageFont(control);
		ApplyDevExpressRootFont(control, MessageFont);
	}

	/// <inheritdoc />
	protected override IChatMessageControl CreateMessageControl(IChatMessage message) => new DXChatMessageControl { MessageFormatter = MessageFormatter, Message = message };

	/// <inheritdoc />
	protected override Control CreateMessageCopyButton() => new SimpleButton
	{
		Cursor = Cursors.Hand,
		Size = new Size(26, 26),
		TabStop = false,
		Text = "⧉"
	};

	/// <inheritdoc />
	protected override IFunctionCallMessageControl CreateFunctionCallMessageControl(IChatMessage message) => new DXFunctionCallMessageControl { Message = message, AllowExpand = AllowExpandFunctionCalls };

	/// <inheritdoc />
	protected override IChatMessageControl CreateReasoningMessageControl(IChatMessage message) => new DXReasoningMessageControl { MessageFormatter = MessageFormatter, Message = message };

	/// <inheritdoc />
	protected override IChatInputControl CreateChatInputControl() => new DXChatInputControl();

	/// <inheritdoc />
	protected override ISplitContainerControl CreateSplitContainerControl() => new DXChatSplitContainerControl();

	/// <summary>
	/// Creates the safe HTML subset rendered by the selectable DevExpress HTML content controls.
	/// See: https://docs.devexpress.com/WindowsForms/403398/common-features/html-css-based-desktop-ui/html-tags
	/// </summary>
	protected override IMessageFormatter CreateDefaultMessageFormatter() => new SimplifiedHtmlMessageFormatter(
		"b", "i", "s", "u", "br", "sub", "sup", "font", "p", "nbsp", "a", "color", "backcolor", "size",
		"h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "table", "thead", "tbody", "tfoot", "tr", "th", "td");

	private static void ApplyDevExpressRootFont(Control control, Font font)
	{
		if (control is not PanelControl panel)
			return;

		panel.Appearance.Font = font;
		panel.Appearance.Options.UseFont = true;
	}
}
