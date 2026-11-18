using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Provides the entries of the input menu before it opens, so they can be added, removed, or replaced.
/// </summary>
/// <param name="items">The entries the menu will show.</param>
public sealed class ChatInputMenuOpeningEventArgs(IList<ChatInputMenuItem> items) : CancelEventArgs
{
	/// <summary>
	/// Gets the entries the menu will show. The menu is not opened when the list is empty.
	/// </summary>
	public IList<ChatInputMenuItem> Items { get; } = items;
}
