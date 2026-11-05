using DevExpress.XtraRichEdit;

namespace TinyChat.Samples.DocumentAttachments;

/// <summary>Extracts text from Word and rich-text documents.</summary>
public sealed class WordAttachmentContentProvider(int maximumTextLength = 200_000) : DevExpressDocumentContentProvider(maximumTextLength)
{
	/// <inheritdoc />
	public override IReadOnlyList<string> FileExtensions { get; } = [".doc", ".docx", ".docm", ".rtf", ".odt"];
	/// <inheritdoc />
	public override bool CanHandle(string fileName, string mediaType) => base.CanHandle(fileName, mediaType) || mediaType.Equals("application/msword", StringComparison.OrdinalIgnoreCase) || mediaType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase) || mediaType.Equals("application/rtf", StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc />
	protected override DocumentTextExtraction ExtractText(ChatFileAttachment attachment, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		using var stream = new MemoryStream(attachment.Data.ToArray(), writable: false);
		using var document = new RichEditDocumentServer();
		if (!document.LoadDocument(stream))
			throw new InvalidDataException($"The document '{attachment.Name}' could not be loaded.");
		cancellationToken.ThrowIfCancellationRequested();
		return LimitText(document.Document.Text);
	}
}
