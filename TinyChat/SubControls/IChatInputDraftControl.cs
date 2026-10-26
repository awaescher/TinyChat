namespace TinyChat;

/// <summary>
/// Defines optional draft-reset support for a chat input control.
/// </summary>
/// <remarks>
/// Custom input controls can implement this interface to participate in
/// <see cref="ChatControl.StartNewChat"/> without changing the base
/// <see cref="IChatInputControl"/> contract.
/// </remarks>
public interface IChatInputDraftControl
{
	/// <summary>
	/// Clears all text and attachments currently being composed.
	/// </summary>
	void ClearDraft();
}
