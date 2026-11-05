namespace TinyChat;

/// <summary>Describes an attachment that could not be added to the draft.</summary>
public sealed class AttachmentRejectedEventArgs(string fileName, Exception exception) : EventArgs
{
	/// <summary>Gets the rejected file's name.</summary>
	public string FileName { get; } = fileName;
	/// <summary>Gets the reason the attachment was rejected.</summary>
	public Exception Exception { get; } = exception;
	/// <summary>Gets or sets whether the host has displayed the rejection.</summary>
	public bool Handled { get; set; }
}
