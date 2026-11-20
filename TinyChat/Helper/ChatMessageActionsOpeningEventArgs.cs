using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Provides the message and the actions available before its hover action bar opens.
/// </summary>
/// <param name="messageControl">The control displaying the message.</param>
/// <param name="actions">The initial actions in display order.</param>
public sealed class ChatMessageActionsOpeningEventArgs(IChatMessageControl messageControl, IList<ChatMessageAction> actions) : CancelEventArgs
{
	/// <summary>Gets the control displaying the message.</summary>
	public IChatMessageControl MessageControl { get; } = messageControl;

	/// <summary>Gets the message targeted by the action bar.</summary>
	public IChatMessage? Message => MessageControl.Message;

	/// <summary>Gets the editable list of actions. An empty list suppresses the action bar.</summary>
	public IList<ChatMessageAction> Actions { get; } = actions;
}
