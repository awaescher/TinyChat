using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>Decodes text attachments and includes their file names in the model input.</summary>
public sealed class TextAttachmentContentProvider : IAttachmentContentProvider
{
	/// <inheritdoc />
	public IReadOnlyList<string> FileExtensions { get; } = [".txt", ".md", ".csv", ".tsv", ".json", ".xml", ".html", ".htm", ".yaml", ".yml", ".sql", ".cs", ".vb", ".js", ".ts", ".py", ".log"];

	/// <inheritdoc />
	public bool CanHandle(string fileName, string mediaType) => ChatFileAttachment.IsTextMediaType(mediaType) || FileExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

	/// <inheritdoc />
	public ValueTask<AttachmentConversionResult> ConvertAsync(ChatFileAttachment attachment, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		using var stream = new MemoryStream(attachment.Data.ToArray(), writable: false);
		using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		var text = reader.ReadToEnd();
		return ValueTask.FromResult(new AttachmentConversionResult([new TextContent($"--- BEGIN ATTACHED FILE: {attachment.Name} ({attachment.MediaType}) ---\n{text}\n--- END ATTACHED FILE: {attachment.Name} ---")]));
	}
}
