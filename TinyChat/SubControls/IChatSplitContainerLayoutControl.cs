namespace TinyChat;

/// <summary>
/// Provides optional layout customization for a chat split container.
/// </summary>
public interface IChatSplitContainerLayoutControl
{
	/// <summary>
	/// Gets or sets whether the splitter can be moved by the user.
	/// </summary>
	bool IsSplitterFixed { get; set; }
}
