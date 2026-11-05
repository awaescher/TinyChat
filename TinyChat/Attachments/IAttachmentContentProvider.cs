using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>Converts supported attachments into content understood by a chat client.</summary>
public interface IAttachmentContentProvider
{
	/// <summary>Gets file extensions (including the dot) advertised to file-selection UIs.</summary>
	IReadOnlyList<string> FileExtensions { get; }

	/// <summary>Checks support using file metadata, without loading or converting the file.</summary>
	bool CanHandle(string fileName, string mediaType);

	/// <summary>Converts an attachment. Implementations must observe cancellation and avoid blocking the UI.</summary>
	ValueTask<AttachmentConversionResult> ConvertAsync(ChatFileAttachment attachment, CancellationToken cancellationToken = default);
}
