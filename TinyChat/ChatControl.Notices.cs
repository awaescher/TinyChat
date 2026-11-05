using System.ComponentModel;

namespace TinyChat;

public partial class ChatControl
{
	/// <summary>Occurs for UI-only errors and warnings. Set Handled to use the host's presentation.</summary>
	public event EventHandler<ChatNoticeEventArgs>? NoticeOccurred;

	/// <summary>Gets or sets a host-supplied localizer, for example an application's existing text catalog.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<ChatNotice, string> NoticeTextProvider
	{
		get => _noticeTextProvider ?? DisplayTexts.Format;
		set
		{
			_noticeTextProvider = value;
			RefreshAttachmentPresentation();
		}
	}
	private Func<ChatNotice, string>? _noticeTextProvider;

	private void ShowNotice(ChatNotice notice)
	{
		var args = new ChatNoticeEventArgs(notice);
		NoticeOccurred?.Invoke(this, args);
		if (!args.Handled)
			AddMessage(new NamedSender(string.Empty), new ChatNoticeMessageContent(notice));
	}

	private void ShowErrorNotice(Exception exception, string? fileName = null)
	{
		if (exception is AttachmentContentException || fileName is not null)
		{
			ShowInputAttachmentError(exception, fileName);
			return;
		}
		ShowNotice(new ChatNotice(ChatNoticeCode.GeneralError, Exception: exception));
	}

	/// <summary>Creates the shared icon-only diagnostic row.</summary>
	protected virtual IChatMessageControl CreateNoticeMessageControl(IChatMessage message)
	{
		var notice = ((ChatNoticeMessageContent)message.Content).Notice;
		return new ChatNoticeControl(message, CreateNoticeIcon(notice.Severity), noticeValue => NoticeTextProvider(noticeValue), CreateToolTip());
	}

	internal virtual IAttachmentToolTip CreateToolTip() => new AttachmentToolTip();

	/// <summary>Creates an error or warning icon, replaceable by alternate control suites.</summary>
	protected virtual Control CreateNoticeIcon(ChatNoticeSeverity severity)
	{
		var font = ChatIcons.CreateFont(Font.SizeInPoints);
		var icon = new Label
		{
			AutoSize = true,
			UseMnemonic = false,
			Font = font,
			Text = ChatIcons.IconFontName is null ? "\u26a0" : ChatIcons.StatusFailed,
			ForeColor = severity == ChatNoticeSeverity.Error ? ChatIcons.ErrorColor : ChatIcons.ToolColor,
			Margin = new Padding(0, 0, 6, 0)
		};
		icon.Disposed += (_, _) => font.Dispose();
		return icon;
	}
}
