namespace TinyChat;

/// <summary>
/// Provides data for the <see cref="ChatControl.ConversationReverted"/> event.
/// </summary>
/// <param name="message">The message the conversation was reverted to.</param>
/// <param name="removedMessages">The messages that were removed from the conversation, in their original order.</param>
public class ConversationRevertedEventArgs(IChatMessage message, IReadOnlyList<IChatMessage> removedMessages) : EventArgs
{
	/// <summary>
	/// Gets the message the conversation was reverted to.
	/// When it was sent by the user, it is part of <see cref="RemovedMessages"/> and its text was moved to the input.
	/// </summary>
	public IChatMessage Message { get; } = message;

	/// <summary>
	/// Gets the messages that were removed from the conversation, in their original order.
	/// </summary>
	public IReadOnlyList<IChatMessage> RemovedMessages { get; } = removedMessages;
}
