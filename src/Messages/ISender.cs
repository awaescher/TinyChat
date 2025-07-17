namespace TinyChat.Messages;

/// <summary>
/// Represents a sender of chat messages.
/// </summary>
public interface ISender
{
	/// <summary>
	/// Gets the name of the sender.
	/// </summary>
	public string Name { get; }
}
