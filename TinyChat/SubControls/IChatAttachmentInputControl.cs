namespace TinyChat;

/// <summary>
/// Extends a chat input control with attachment draft support.
/// </summary>
public interface IChatAttachmentInputControl
{
	/// <summary>
	/// Occurs when attachment data from the clipboard should be added to the draft.
	/// </summary>
	event EventHandler<AttachmentPasteRequestedEventArgs>? AttachmentPasteRequested;

	/// <summary>
	/// Occurs when the pending attachment collection changes.
	/// </summary>
	event EventHandler? AttachmentsChanged;

	/// <summary>
	/// Gets the attachments currently waiting to be sent.
	/// </summary>
	IReadOnlyList<ChatFileAttachment> PendingAttachments { get; }

	/// <summary>
	/// Gets the additional height needed to display the attachment draft.
	/// </summary>
	int AttachmentDisplayHeight { get; }

	/// <summary>
	/// Adds attachments to the current draft.
	/// </summary>
	/// <param name="attachments">The attachments to add.</param>
	void AddAttachments(IEnumerable<ChatFileAttachment> attachments);

	/// <summary>
	/// Removes all attachments from the current draft.
	/// </summary>
	void ClearAttachments();
}
