namespace TinyChat;

/// <summary>
/// Provides clipboard data that contains files or an image.
/// </summary>
/// <param name="data">The clipboard data to add to the attachment draft.</param>
public sealed class AttachmentPasteRequestedEventArgs(IDataObject data) : EventArgs
{
	/// <summary>
	/// Gets the clipboard data being pasted.
	/// </summary>
	public IDataObject Data { get; } = data;
}
