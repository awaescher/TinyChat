using Microsoft.Extensions.AI;

namespace TinyChat.Samples.DocumentAttachments;

/// <summary>Runs local document extraction off the UI thread and bounds the resulting model input.</summary>
public abstract class DevExpressDocumentContentProvider(int maximumTextLength = 200_000) : IAttachmentContentProvider
{
	/// <summary>Gets the maximum extracted text length, excluding the file wrapper and truncation notice.</summary>
	public int MaximumTextLength { get; } = maximumTextLength > 0 ? maximumTextLength : throw new ArgumentOutOfRangeException(nameof(maximumTextLength));
	/// <inheritdoc />
	public abstract IReadOnlyList<string> FileExtensions { get; }
	/// <inheritdoc />
	public virtual bool CanHandle(string fileName, string mediaType) => FileExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

	/// <inheritdoc />
	public async ValueTask<AttachmentConversionResult> ConvertAsync(ChatFileAttachment attachment, CancellationToken cancellationToken = default)
	{
		var extraction = await Task.Run(() => ExtractText(attachment, cancellationToken), cancellationToken).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
		if (string.IsNullOrWhiteSpace(extraction.Text))
			throw new AttachmentContentException(new ChatNotice(ChatNoticeCode.NoExtractableText, attachment.Name));
		var text = extraction.Text + (extraction.IsTruncated ? "\n[Document truncated: extracted text limit reached.]" : string.Empty);
		return new AttachmentConversionResult([new TextContent($"--- BEGIN ATTACHED FILE: {attachment.Name} ---\n{text}\n--- END ATTACHED FILE: {attachment.Name} ---")],
			extraction.IsTruncated ? [new ChatNotice(ChatNoticeCode.DocumentTruncated, attachment.Name, MaximumTextLength)] : []);
	}

	/// <summary>Extracts the document text, observing cancellation and MaximumTextLength.</summary>
	protected abstract DocumentTextExtraction ExtractText(ChatFileAttachment attachment, CancellationToken cancellationToken);

	/// <summary>Bounds text and adds an explicit truncation notice.</summary>
	protected DocumentTextExtraction LimitText(string text) => new(text.Length <= MaximumTextLength ? text : text[..MaximumTextLength], text.Length > MaximumTextLength);

	/// <summary>Contains extracted text and an explicit truncation flag.</summary>
	protected sealed record DocumentTextExtraction(string Text, bool IsTruncated);
}
