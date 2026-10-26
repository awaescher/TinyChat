namespace TinyChat;

/// <summary>
/// Provides data for the <see cref="ChatControl.ToolApprovalRequested"/> event.
/// </summary>
/// <param name="content">The function call waiting for approval.</param>
public class ToolApprovalRequestedEventArgs(FunctionCallMessageContent content) : EventArgs
{
	/// <summary>
	/// Gets the function call waiting for approval.
	/// Call <see cref="FunctionCallMessageContent.Approve"/> or <see cref="FunctionCallMessageContent.Reject"/>
	/// to decide without user interaction.
	/// </summary>
	public FunctionCallMessageContent Content { get; } = content;
}
