namespace TinyChat;

/// <summary>
/// Opts a chat input control into receiving a draft, for example when the conversation is reverted to a user message.
/// </summary>
public interface IChatInputDraftEditorControl
{
	/// <summary>
	/// Replaces the current draft with the given text and attachments.
	/// </summary>
	/// <param name="text">The text to place in the editor.</param>
	/// <param name="attachments">The attachments to place in the attachment draft.</param>
	void SetDraft(string text, IEnumerable<ChatFileAttachment> attachments);
}
