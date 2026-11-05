using System.ComponentModel;

namespace TinyChat;

/// <summary>Identifies a localizable UI diagnostic, independent of its wording.</summary>
public enum ChatNoticeCode
{
	/// <summary>Identifies the GeneralError diagnostic.</summary>
	GeneralError,
	/// <summary>Identifies the BinaryAttachment diagnostic.</summary>
	BinaryAttachment,
	/// <summary>Identifies the AttachmentRejected diagnostic.</summary>
	AttachmentRejected,
	/// <summary>Identifies the AttachmentsDisabled diagnostic.</summary>
	AttachmentsDisabled,
	/// <summary>Identifies the AttachmentTooLarge diagnostic.</summary>
	AttachmentTooLarge,
	/// <summary>Identifies the NoExtractableText diagnostic.</summary>
	NoExtractableText,
	/// <summary>Identifies the DocumentTruncated diagnostic.</summary>
	DocumentTruncated
}

/// <summary>Distinguishes warnings from errors in the chat's icon presentation.</summary>
public enum ChatNoticeSeverity
{
	/// <summary>The operation can proceed with a limitation.</summary>
	Warning,
	/// <summary>The operation failed.</summary>
	Error
}

/// <summary>Describes a UI-only diagnostic. Technical exception details remain available to the host.</summary>
public sealed record ChatNotice(ChatNoticeCode Code, string? FileName = null, long? Limit = null, Exception? Exception = null)
{
	/// <summary>Gets the severity used to select an icon.</summary>
	public ChatNoticeSeverity Severity => Code is ChatNoticeCode.BinaryAttachment or ChatNoticeCode.DocumentTruncated ? ChatNoticeSeverity.Warning : ChatNoticeSeverity.Error;
}

/// <summary>Allows a host to present a diagnostic itself instead of the default icon.</summary>
public sealed class ChatNoticeEventArgs(ChatNotice notice) : EventArgs
{
	/// <summary>Gets the diagnostic and its structured details.</summary>
	public ChatNotice Notice { get; } = notice;
	/// <summary>Gets or sets whether the host has presented the diagnostic.</summary>
	public bool Handled { get; set; }
}

/// <summary>Preserves a diagnostic's structured details without relying on exception-message parsing.</summary>
public sealed class AttachmentContentException(ChatNotice notice) : IOException(ChatLocalization.Format(notice), notice.Exception)
{
	/// <summary>Gets the diagnostic associated with the failed conversion.</summary>
	public ChatNotice Notice { get; } = notice;
	internal ChatFileAttachment? Attachment { get; set; }
}

/// <summary>A presentation-only history item, always excluded from model requests.</summary>
public sealed class ChatNoticeMessageContent(ChatNotice notice) : IChatMessageContent
{
	/// <summary>Gets the diagnostic.</summary>
	public ChatNotice Notice { get; } = notice;
	/// <inheritdoc />
	public object Content => Notice.FileName ?? string.Empty;
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
	/// <inheritdoc />
	public override string ToString() => Content.ToString() ?? string.Empty;
}
