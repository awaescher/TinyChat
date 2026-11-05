using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace TinyChat;

public partial class ChatControl
{
	private readonly ConditionalWeakTable<ChatFileAttachment, PreparedAttachment> _preparedAttachments = new();
	private readonly ConditionalWeakTable<ChatFileAttachment, AttachmentNotices> _attachmentNotices = new();

	/// <summary>Gets the ordered converters. The first matching provider wins. Replace a provider to invalidate its cached conversions.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IList<IAttachmentContentProvider> AttachmentContentProviders { get; } = [new TextAttachmentContentProvider(), new ImageAttachmentContentProvider(), new BinaryAttachmentContentProvider()];

	/// <summary>Occurs when a file cannot be added. Valid files in the same selection are retained.</summary>
	public event EventHandler<AttachmentRejectedEventArgs>? AttachmentRejected;

	/// <summary>Gets the extensions advertised by the current providers.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IReadOnlyList<string> SupportedAttachmentFileExtensions => AttachmentContentProviders.SelectMany(provider => provider.FileExtensions).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();

	/// <summary>Checks whether the configured providers support a file.</summary>
	public bool CanAttachFile(string fileName, string? mediaType = null) => FindAttachmentProvider(fileName, mediaType ?? ChatFileAttachment.GetMediaType(fileName)) is not null;

	private IAttachmentContentProvider? FindAttachmentProvider(string fileName, string mediaType) => AttachmentContentProviders.FirstOrDefault(provider => provider.CanHandle(fileName, mediaType));

	private void ValidateAttachment(ChatFileAttachment attachment)
	{
		if (!AllowFileAttachments)
			throw new AttachmentContentException(new ChatNotice(ChatNoticeCode.AttachmentsDisabled, attachment.Name)) { Attachment = attachment };
		if (!CanAttachFile(attachment.Name, attachment.MediaType))
			throw new AttachmentContentException(new ChatNotice(ChatNoticeCode.AttachmentRejected, attachment.Name)) { Attachment = attachment };
		if (MaximumAttachmentFileSize > 0 && attachment.Data.Length > MaximumAttachmentFileSize)
			throw new AttachmentContentException(new ChatNotice(ChatNoticeCode.AttachmentTooLarge, attachment.Name, MaximumAttachmentFileSize)) { Attachment = attachment };
		if (FindAttachmentProvider(attachment.Name, attachment.MediaType) is BinaryAttachmentContentProvider)
			ShowAttachmentNotice(attachment, new ChatNotice(ChatNoticeCode.BinaryAttachment, attachment.Name));
	}

	private void ShowAttachmentNotice(ChatFileAttachment attachment, ChatNotice notice)
	{
		var notices = _attachmentNotices.GetValue(attachment, static _ => new AttachmentNotices());
		if (!notices.Reported.Add(notice.Code))
			return;
		var args = new ChatNoticeEventArgs(notice);
		NoticeOccurred?.Invoke(this, args);
		if (!args.Handled)
		{
			notices.Visible.Add(notice);
			RefreshAttachmentPresentation();
		}
	}

	internal IReadOnlyList<ChatNotice> GetAttachmentNotices(ChatFileAttachment attachment) =>
		_attachmentNotices.TryGetValue(attachment, out var notices) ? notices.Visible : [];

	private sealed class AttachmentNotices
	{
		internal HashSet<ChatNoticeCode> Reported { get; } = [];
		internal List<ChatNotice> Visible { get; } = [];
	}

	private void ReportRejectedAttachment(string fileName, Exception exception)
	{
		var args = new AttachmentRejectedEventArgs(fileName, exception);
		AttachmentRejected?.Invoke(this, args);
		if (!args.Handled)
			ReportAttachmentError(exception, fileName);
	}

	/// <summary>Converts an attachment using the first matching provider and caches successful results for this control.</summary>
	protected virtual async ValueTask<IReadOnlyList<AIContent>> ConvertFileAttachmentAsync(ChatFileAttachment attachment, CancellationToken cancellationToken = default)
	{
		ValidateAttachment(attachment);
		cancellationToken.ThrowIfCancellationRequested();
		var provider = FindAttachmentProvider(attachment.Name, attachment.MediaType)!;
		if (_preparedAttachments.TryGetValue(attachment, out var prepared) && ReferenceEquals(prepared.Provider, provider))
			return prepared.Contents;

		AttachmentConversionResult result;
		try
		{
			result = await provider.ConvertAsync(attachment, cancellationToken).ConfigureAwait(true);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			if (ex is AttachmentContentException error)
			{
				error.Attachment = attachment;
				throw;
			}
			throw new AttachmentContentException(new ChatNotice(ChatNoticeCode.AttachmentRejected, attachment.Name, Exception: ex)) { Attachment = attachment };
		}
		var contents = result.Contents;
		cancellationToken.ThrowIfCancellationRequested();
		if (contents.Count == 0)
			throw new AttachmentContentException(new ChatNotice(ChatNoticeCode.NoExtractableText, attachment.Name)) { Attachment = attachment };
		_preparedAttachments.Remove(attachment);
		var snapshot = contents.ToArray();
		_preparedAttachments.Add(attachment, new PreparedAttachment(provider, snapshot));
		foreach (var notice in result.Notices)
			ShowAttachmentNotice(attachment, notice);
		return snapshot;
	}

	private sealed record PreparedAttachment(IAttachmentContentProvider Provider, IReadOnlyList<AIContent> Contents);
}
