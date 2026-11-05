namespace TinyChat;

/// <summary>Internal presentation support for rejected files, which have no attachment preview.</summary>
internal interface IChatAttachmentNoticeInput
{
	bool HasAttachmentNotices { get; }
	void AddAttachmentNotice(Control notice);
	void ClearAttachmentNotices(string? fileName);
}
