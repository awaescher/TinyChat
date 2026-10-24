namespace TinyChat;

/// <summary>
/// Represents a control that displays reasoning content and can optionally allow expanding its details.
/// </summary>
public interface IReasoningMessageControl : IChatMessageControl
{
	/// <summary>
	/// Gets or sets whether the user can expand the reasoning details.
	/// Setting this to <see langword="false"/> also collapses an expanded control.
	/// </summary>
	bool AllowExpand { get; set; }
}
