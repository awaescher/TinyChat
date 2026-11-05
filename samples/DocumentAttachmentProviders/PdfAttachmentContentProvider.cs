using System.Text;
using DevExpress.Pdf;

namespace TinyChat.Samples.DocumentAttachments;

/// <summary>Extracts PDF text by page. Image-only scans require a separately registered OCR provider.</summary>
public sealed class PdfAttachmentContentProvider(int maximumTextLength = 200_000) : DevExpressDocumentContentProvider(maximumTextLength)
{
	/// <inheritdoc />
	public override IReadOnlyList<string> FileExtensions { get; } = [".pdf"];
	/// <inheritdoc />
	public override bool CanHandle(string fileName, string mediaType) => base.CanHandle(fileName, mediaType) || mediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc />
	protected override DocumentTextExtraction ExtractText(ChatFileAttachment attachment, CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(attachment.Data.ToArray(), writable: false);
		using var document = new PdfDocumentProcessor();
		document.LoadDocument(stream);
		var builder = new StringBuilder();
		var hasText = false;
		for (var page = 1; page <= document.Document.Pages.Count; page++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var text = document.GetPageText(page);
			hasText |= !string.IsNullOrWhiteSpace(text);
			builder.AppendLine($"\nPage {page}:");
			builder.AppendLine(string.IsNullOrWhiteSpace(text) ? "[No extractable text on this page.]" : text);
			if (builder.Length > MaximumTextLength)
				return hasText ? LimitText(builder.ToString()) : LimitText(string.Empty);
		}
		return hasText ? LimitText(builder.ToString()) : LimitText(string.Empty);
	}
}
