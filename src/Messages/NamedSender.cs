using TinyChat.Messages;

namespace TinyChat;

/// <summary>
/// Represents a sender identified by their name.
/// </summary>
/// <param name="Name">The name of the sender.</param>
public record NamedSender(string Name) : ISender;
