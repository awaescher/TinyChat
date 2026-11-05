namespace TinyChat;

public partial class ChatControl
{
	private readonly List<ChatNotice> _pendingInputAttachmentNotices = [];

	private void ShowInputAttachmentError(Exception exception, string? fileName = null)
	{
		var notice = exception is AttachmentContentException error ? error.Notice : new ChatNotice(fileName is null ? ChatNoticeCode.GeneralError : ChatNoticeCode.AttachmentRejected, fileName, Exception: exception);
		if (exception is AttachmentContentException { Attachment: { } attachment } &&
			((InputControl is IChatAttachmentInputControl input && input.PendingAttachments.Contains(attachment)) ||
			_messages.Any(message => message.Content is FileAttachmentMessageContent files && files.Attachments.Contains(attachment))))
		{
			ShowAttachmentNotice(attachment, notice);
			return;
		}
		var args = new ChatNoticeEventArgs(notice);
		NoticeOccurred?.Invoke(this, args);
		if (args.Handled)
			return;
		ClearInputAttachmentNotices(notice.FileName);
		if (InputControl is IChatAttachmentNoticeInput noticeInput)
			noticeInput.AddAttachmentNotice(CreateInputAttachmentNotice(notice));
		else
			_pendingInputAttachmentNotices.Add(notice);
	}

	internal virtual AttachmentCard CreateInputAttachmentNotice(ChatNotice notice) =>
		new(notice, value => NoticeTextProvider(value), () => ClearInputAttachmentNotices(notice.FileName));

	internal void ClearInputAttachmentNotices(string? fileName = null)
	{
		_pendingInputAttachmentNotices.RemoveAll(notice => fileName is null || string.Equals(notice.FileName, fileName, StringComparison.OrdinalIgnoreCase));
		(InputControl as IChatAttachmentNoticeInput)?.ClearAttachmentNotices(fileName);
	}

	private void FlushInputAttachmentNotices()
	{
		if (InputControl is not IChatAttachmentNoticeInput noticeInput)
			return;
		foreach (var notice in _pendingInputAttachmentNotices)
			noticeInput.AddAttachmentNotice(CreateInputAttachmentNotice(notice));
		_pendingInputAttachmentNotices.Clear();
	}
}
