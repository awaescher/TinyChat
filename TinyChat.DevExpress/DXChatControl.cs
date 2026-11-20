using System.Text;
using DevExpress.LookAndFeel;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// The TinyChat chat control implementation using DevExpress controls.
/// </summary>
public class DXChatControl : ChatControl
{
	internal override AttachmentCard CreateInputAttachmentNotice(ChatNotice notice) =>
		new DXAttachmentCard(notice, value => NoticeTextProvider(value), () => ClearInputAttachmentNotices(notice.FileName));

	internal override IAttachmentToolTip CreateToolTip() => new DXAttachmentToolTip();

	/// <inheritdoc />
	protected override Control CreateNoticeIcon(ChatNoticeSeverity severity) => new DXStatusIcon(severity);

	/// <inheritdoc />
	protected override IChatMessageControl CreateNoticeMessageControl(IChatMessage message)
	{
		var notice = ((ChatNoticeMessageContent)message.Content).Notice;
		var text = new LabelControl { AutoSizeMode = LabelAutoSizeMode.Vertical, AllowHtmlString = false, UseMnemonic = false, Margin = Padding.Empty };
		text.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
		return new ChatNoticeControl(message, CreateNoticeIcon(notice.Severity), value => NoticeTextProvider(value), CreateToolTip(), text);
	}

	/// <inheritdoc />
	protected override IChatMessageHistoryControl CreateMessageHistoryControl() => new StackPanelMessageHistoryControl();

	/// <inheritdoc />
	protected override Control CreateWelcomeControl() => new DXWelcomeControl();

	/// <inheritdoc />
	protected override void ApplyWelcomeControlSettings(Control control)
	{
		base.ApplyWelcomeControlSettings(control);
		if (control is DXWelcomeControl welcomeControl)
		{
			welcomeControl.SetWelcomeText(WelcomeMessage);
			welcomeControl.TextAlign = WelcomeTextAlign;
		}

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
	protected override IFunctionCallMessageControl CreateFunctionCallMessageControl(IChatMessage message) => new DXFunctionCallMessageControl { Message = message, AllowExpand = AllowExpandFunctionCalls };

	/// <inheritdoc />
	protected override IChatMessageControl CreateReasoningMessageControl(IChatMessage message) => new DXReasoningMessageControl { MessageFormatter = MessageFormatter, Message = message, AllowExpand = AllowExpandReasoning };

	/// <inheritdoc />
	protected override IChatInputControl CreateChatInputControl() => new DXChatInputControl();

	/// <inheritdoc />
	protected override IReadOnlyList<string>? ShowAttachFilesDialog(string title, string filter)
	{
		using var dialog = new XtraOpenFileDialog
		{
			CheckFileExists = true,
			Filter = filter,
			Multiselect = true,
			RestoreDirectory = true,
			Title = title
		};

		return dialog.ShowDialog(FindForm()) == DialogResult.OK ? dialog.FileNames : null;
	}

	/// <inheritdoc />
	protected override Form CreateFullTextDialog(string title, string text) => new DXFullTextDialog(title, text, MessageFont);

	/// <inheritdoc />
	protected override ISplitContainerControl CreateSplitContainerControl() => new DXChatSplitContainerControl();

	/// <inheritdoc />
	protected override Control CreateMessageActionButton(ChatMessageAction action)
	{
		ArgumentNullException.ThrowIfNull(action);

		var button = new SimpleButton
		{
			AllowFocus = false,
			Margin = new Padding(1, 0, 1, 0),
			PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light,
			Size = new Size(26, 24),
			TabStop = false,
			ToolTip = action.ToolTipText ?? string.Empty,
			AccessibleName = action.ToolTipText
		};
		button.ImageOptions.Location = ImageLocation.MiddleCenter;
		// The button is created before it is parented, so use the DPI of the chat control.
		var iconPixels = Math.Max(16, (int)Math.Round(16 * DeviceDpi / 96f));
		if (!string.IsNullOrWhiteSpace(action.SvgIcon))
		{
			using var stream = new MemoryStream(Encoding.UTF8.GetBytes(action.SvgIcon));
			button.ImageOptions.SvgImage = SvgImage.FromStream(stream);
		}
		else if (action.Image is not null)
		{
			button.ImageOptions.Image = action.Image;
		}
		else if (!string.IsNullOrWhiteSpace(action.SegoeFluentIcon) && ChatIcons.IconFontName is not null)
		{
			var color = LookAndFeelHelper.GetSystemColor(UserLookAndFeel.Default, SystemColors.WindowText);
			var glyphImage = ChatIcons.CreateImage(action.SegoeFluentIcon, iconPixels, color);
			button.ImageOptions.Image = glyphImage;
			button.Disposed += (_, _) => glyphImage.Dispose();
		}
		else
		{
			button.ImageOptions.SvgImage = action.Kind switch
			{
				ChatMessageActionKind.CopyFormatted => FluentIcons.CopyFormatted(iconPixels),
				ChatMessageActionKind.Revert => FluentIcons.Revert(iconPixels),
				_ => FluentIcons.Copy(iconPixels)
			};
		}

		if (button.ImageOptions.SvgImage is not null)
			button.ImageOptions.SvgImageSize = new Size(iconPixels, iconPixels);

		return button;
	}

	/// <inheritdoc />
	protected override void ApplyMessageActionBarColors(Control actionBar)
	{
		var backColor = LookAndFeelHelper.GetSystemColor(UserLookAndFeel.Default, SystemColors.Window);
		var foreColor = LookAndFeelHelper.GetSystemColor(UserLookAndFeel.Default, SystemColors.WindowText);
		actionBar.BackColor = backColor;
		actionBar.ForeColor = foreColor;
		foreach (var button in actionBar.Controls.OfType<SimpleButton>())
		{
			button.Appearance.BackColor = backColor;
			button.Appearance.Options.UseBackColor = true;
		}
	}

	/// <inheritdoc />
	protected override IEnumerable<ChatMessageAction> GetMessageActions(IChatMessageControl messageControl)
	{
		var actions = base.GetMessageActions(messageControl).ToList();
		var copyIndex = actions.FindIndex(action => action.Kind == ChatMessageActionKind.Copy);
		if (messageControl is DXChatMessageControl formattedMessage && copyIndex >= 0)
		{
			actions.Insert(copyIndex + 1, new ChatMessageAction(
				ChatMessageActionKind.CopyFormatted,
				() => CopyMessageWithFormatting(formattedMessage))
			{
				ToolTipText = DisplayTexts.CopyFormattedMessage
			});
		}

		return actions;
	}

	/// <summary>
	/// Copies a message to the clipboard as HTML, keeping its formatting, together with its plain text.
	/// </summary>
	/// <param name="messageControl">The control displaying the message.</param>
	protected void CopyMessageWithFormatting(DXChatMessageControl messageControl)
	{
		var html = DXFormattedText.ToHtml(messageControl.FormattedText);
		var plainText = GetMessagePlainText(messageControl);
		if (string.IsNullOrEmpty(html) && string.IsNullOrEmpty(plainText))
			return;

		Clipboard.SetDataObject(HtmlClipboard.CreateDataObject(html, plainText, MessageFont), copy: true);
	}

	/// <summary>
	/// Creates the safe HTML subset supported by DevExpress label controls.
	/// See: https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting
	/// </summary>
	protected override IMessageFormatter CreateDefaultMessageFormatter() => new SimplifiedHtmlMessageFormatter(
		"b", "i", "s", "u", "br", "sub", "sup", "font", "p", "nbsp", "a", "href", "color", "backcolor", "size");

	private static void ApplyDevExpressRootFont(Control control, Font font)
	{
		if (control is not PanelControl panel)
			return;

		panel.Appearance.Font = font;
		panel.Appearance.Options.UseFont = true;
	}
}
