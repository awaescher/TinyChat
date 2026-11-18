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

	/// <summary>Gets or sets the tooltip and accessible name of the button that shows shortened reasoning or tool results in full.</summary>
	[Localizable(true)]
	public string ShowFullText { get => Get(nameof(ShowFullText)); set => Set(nameof(ShowFullText), value); }
	private bool ShouldSerializeShowFullText() => _overrides.ContainsKey(nameof(ShowFullText));
	private void ResetShowFullText() => Set(nameof(ShowFullText), null);

	/// <summary>Gets or sets the title of the full text dialog for reasoning.</summary>
	[Localizable(true)]
	public string Reasoning { get => Get(nameof(Reasoning)); set => Set(nameof(Reasoning), value); }
	private bool ShouldSerializeReasoning() => _overrides.ContainsKey(nameof(Reasoning));
	private void ResetReasoning() => Set(nameof(Reasoning), null);

	/// <summary>Gets or sets the heading of the function arguments in the full text dialog.</summary>
	[Localizable(true)]
	public string FunctionArguments { get => Get(nameof(FunctionArguments)); set => Set(nameof(FunctionArguments), value); }
	private bool ShouldSerializeFunctionArguments() => _overrides.ContainsKey(nameof(FunctionArguments));
	private void ResetFunctionArguments() => Set(nameof(FunctionArguments), null);

	/// <summary>Gets or sets the heading of the function result in the full text dialog.</summary>
	[Localizable(true)]
	public string FunctionResult { get => Get(nameof(FunctionResult)); set => Set(nameof(FunctionResult), value); }
	private bool ShouldSerializeFunctionResult() => _overrides.ContainsKey(nameof(FunctionResult));
	private void ResetFunctionResult() => Set(nameof(FunctionResult), null);

	/// <summary>Gets or sets the tooltip and accessible name of the menu button in the lower left corner of the input.</summary>
	[Localizable(true)]
	public string InputMenu { get => Get(nameof(InputMenu)); set => Set(nameof(InputMenu), value); }
	private bool ShouldSerializeInputMenu() => _overrides.ContainsKey(nameof(InputMenu));
	private void ResetInputMenu() => Set(nameof(InputMenu), null);

	/// <summary>Gets or sets the text of the input menu entry that adds files to the draft.</summary>
	[Localizable(true)]
	public string AttachFiles { get => Get(nameof(AttachFiles)); set => Set(nameof(AttachFiles), value); }
	private bool ShouldSerializeAttachFiles() => _overrides.ContainsKey(nameof(AttachFiles));
	private void ResetAttachFiles() => Set(nameof(AttachFiles), null);

	/// <summary>Gets or sets the title of the file dialog that adds files to the draft.</summary>
	[Localizable(true)]
	public string AttachFilesDialogTitle { get => Get(nameof(AttachFilesDialogTitle)); set => Set(nameof(AttachFilesDialogTitle), value); }
	private bool ShouldSerializeAttachFilesDialogTitle() => _overrides.ContainsKey(nameof(AttachFilesDialogTitle));
	private void ResetAttachFilesDialogTitle() => Set(nameof(AttachFilesDialogTitle), null);

	/// <summary>Gets or sets the file dialog filter name of the files the attachment providers support.</summary>
	[Localizable(true)]
	public string SupportedAttachmentFiles { get => Get(nameof(SupportedAttachmentFiles)); set => Set(nameof(SupportedAttachmentFiles), value); }
	private bool ShouldSerializeSupportedAttachmentFiles() => _overrides.ContainsKey(nameof(SupportedAttachmentFiles));
	private void ResetSupportedAttachmentFiles() => Set(nameof(SupportedAttachmentFiles), null);

	/// <summary>Gets or sets the file dialog filter name for all files.</summary>
	[Localizable(true)]
	public string AllFiles { get => Get(nameof(AllFiles)); set => Set(nameof(AllFiles), value); }
	private bool ShouldSerializeAllFiles() => _overrides.ContainsKey(nameof(AllFiles));
	private void ResetAllFiles() => Set(nameof(AllFiles), null);

	internal string Format(ChatNotice notice) => string.Format(CultureInfo.CurrentCulture, Get(notice.Code.ToString()), notice.FileName ?? string.Empty, notice.Limit ?? 0);
	/// <inheritdoc />
	public override string ToString() => string.Empty;
}
