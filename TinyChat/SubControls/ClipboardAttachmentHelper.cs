namespace TinyChat;

internal static class ClipboardAttachmentHelper
{
	public static bool ContainsAttachments(IDataObject data)
	{
		return data.GetDataPresent(DataFormats.FileDrop) || data.GetDataPresent(DataFormats.Bitmap);
	}

	public static IDataObject? GetClipboardAttachmentData()
	{
		try
		{
			var data = Clipboard.GetDataObject();
			return data is not null && ContainsAttachments(data) ? data : null;
		}
		catch
		{
			return null;
		}
	}
}
