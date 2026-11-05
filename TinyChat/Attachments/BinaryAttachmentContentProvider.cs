using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>Fallback for arbitrary binary attachments, with a separate UI warning. Register last.</summary>
public sealed class BinaryAttachmentContentProvider : IAttachmentContentProvider
{
	/// <inheritdoc />
	public IReadOnlyList<string> FileExtensions { get; } = [];

	/// <inheritdoc />
	public bool CanHandle(string fileName, string mediaType) => true;

	/// <inheritdoc />
	public ValueTask<AttachmentConversionResult> ConvertAsync(ChatFileAttachment attachment, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return ValueTask.FromResult(new AttachmentConversionResult([attachment.ToDataContent()], [new ChatNotice(ChatNoticeCode.BinaryAttachment, attachment.Name)]));
	}
}
