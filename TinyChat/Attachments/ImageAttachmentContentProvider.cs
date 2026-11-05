using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>Passes image attachments to multimodal chat clients.</summary>
public sealed class ImageAttachmentContentProvider : IAttachmentContentProvider
{
	/// <inheritdoc />
	public IReadOnlyList<string> FileExtensions { get; } = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".tif", ".tiff", ".avif", ".heic", ".svg"];

	/// <inheritdoc />
	public bool CanHandle(string fileName, string mediaType) => mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc />
	public ValueTask<AttachmentConversionResult> ConvertAsync(ChatFileAttachment attachment, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return ValueTask.FromResult(new AttachmentConversionResult([attachment.ToDataContent()]));
	}
}
