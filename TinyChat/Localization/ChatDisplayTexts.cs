using System.ComponentModel;
using System.Globalization;

namespace TinyChat;

/// <summary>Localizable designer overrides for the chat's explanations and attachment actions.</summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class ChatDisplayTexts
{
	private readonly Dictionary<string, string> _overrides = [];
	internal event EventHandler? Changed;
	private string Get(string key) => _overrides.TryGetValue(key, out var text) ? text : ChatLocalization.GetString(key);
	private void Set(string key, string? value)
	{
		if (value is null)
			_overrides.Remove(key);
		else
			_overrides[key] = value;
		Changed?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>Gets or sets the localized GeneralError text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string GeneralError { get => Get(nameof(GeneralError)); set => Set(nameof(GeneralError), value); }
	private bool ShouldSerializeGeneralError() => _overrides.ContainsKey(nameof(GeneralError));
	private void ResetGeneralError() => Set(nameof(GeneralError), null);

	/// <summary>Gets or sets the localized BinaryAttachment text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string BinaryAttachment { get => Get(nameof(BinaryAttachment)); set => Set(nameof(BinaryAttachment), value); }
	private bool ShouldSerializeBinaryAttachment() => _overrides.ContainsKey(nameof(BinaryAttachment));
	private void ResetBinaryAttachment() => Set(nameof(BinaryAttachment), null);

	/// <summary>Gets or sets the localized AttachmentRejected text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string AttachmentRejected { get => Get(nameof(AttachmentRejected)); set => Set(nameof(AttachmentRejected), value); }
	private bool ShouldSerializeAttachmentRejected() => _overrides.ContainsKey(nameof(AttachmentRejected));
	private void ResetAttachmentRejected() => Set(nameof(AttachmentRejected), null);

	/// <summary>Gets or sets the localized AttachmentsDisabled text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string AttachmentsDisabled { get => Get(nameof(AttachmentsDisabled)); set => Set(nameof(AttachmentsDisabled), value); }
	private bool ShouldSerializeAttachmentsDisabled() => _overrides.ContainsKey(nameof(AttachmentsDisabled));
	private void ResetAttachmentsDisabled() => Set(nameof(AttachmentsDisabled), null);

	/// <summary>Gets or sets the localized AttachmentTooLarge text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string AttachmentTooLarge { get => Get(nameof(AttachmentTooLarge)); set => Set(nameof(AttachmentTooLarge), value); }
	private bool ShouldSerializeAttachmentTooLarge() => _overrides.ContainsKey(nameof(AttachmentTooLarge));
	private void ResetAttachmentTooLarge() => Set(nameof(AttachmentTooLarge), null);

	/// <summary>Gets or sets the localized NoExtractableText text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string NoExtractableText { get => Get(nameof(NoExtractableText)); set => Set(nameof(NoExtractableText), value); }
	private bool ShouldSerializeNoExtractableText() => _overrides.ContainsKey(nameof(NoExtractableText));
	private void ResetNoExtractableText() => Set(nameof(NoExtractableText), null);

	/// <summary>Gets or sets the localized DocumentTruncated text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string DocumentTruncated { get => Get(nameof(DocumentTruncated)); set => Set(nameof(DocumentTruncated), value); }
	private bool ShouldSerializeDocumentTruncated() => _overrides.ContainsKey(nameof(DocumentTruncated));
	private void ResetDocumentTruncated() => Set(nameof(DocumentTruncated), null);

	/// <summary>Gets or sets the localized PreviewUnavailable text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string PreviewUnavailable { get => Get(nameof(PreviewUnavailable)); set => Set(nameof(PreviewUnavailable), value); }
	private bool ShouldSerializePreviewUnavailable() => _overrides.ContainsKey(nameof(PreviewUnavailable));
	private void ResetPreviewUnavailable() => Set(nameof(PreviewUnavailable), null);

	/// <summary>Gets or sets the localized RemoveAttachment text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string RemoveAttachment { get => Get(nameof(RemoveAttachment)); set => Set(nameof(RemoveAttachment), value); }
	private bool ShouldSerializeRemoveAttachment() => _overrides.ContainsKey(nameof(RemoveAttachment));
	private void ResetRemoveAttachment() => Set(nameof(RemoveAttachment), null);

	/// <summary>Gets or sets the localized OpenAttachment text. Notice templates accept {0} (file name) and {1} (limit).</summary>
	[Localizable(true)]
	public string OpenAttachment { get => Get(nameof(OpenAttachment)); set => Set(nameof(OpenAttachment), value); }
	private bool ShouldSerializeOpenAttachment() => _overrides.ContainsKey(nameof(OpenAttachment));
	private void ResetOpenAttachment() => Set(nameof(OpenAttachment), null);

	internal string Format(ChatNotice notice) => string.Format(CultureInfo.CurrentCulture, Get(notice.Code.ToString()), notice.FileName ?? string.Empty, notice.Limit ?? 0);
	/// <inheritdoc />
	public override string ToString() => string.Empty;
}
