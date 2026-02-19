using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>
/// Provides data for the event that is raised before a chat request is sent to the <see cref="IChatClient"/>,
/// allowing the developer to define or modify <see cref="Microsoft.Extensions.AI.ChatOptions"/>.
/// </summary>
/// <param name="chatOptions">The current chat options to be used for the request, or <see langword="null"/> if no options are set.</param>
public class ChatOptionsRequestedEventArgs(ChatOptions? chatOptions) : EventArgs
{
	/// <summary>
	/// Gets or sets the <see cref="Microsoft.Extensions.AI.ChatOptions"/> to be passed to the <see cref="IChatClient"/> request.
	/// Set this to customize the options for the current request.
	/// </summary>
	public ChatOptions? ChatOptions { get; set; } = chatOptions;
}
