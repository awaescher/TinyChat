namespace TinyChat;

internal static class FileAttachmentImageFactory
{
	public static Image Create(ChatFileAttachment attachment)
	{
		try
		{
			using var stream = new MemoryStream(attachment.Data.ToArray());
			using var sourceImage = Image.FromStream(stream);
			return new Bitmap(sourceImage);
		}
		catch (ArgumentException)
		{
			return SystemIcons.Application.ToBitmap();
		}
		catch (OutOfMemoryException)
		{
			return SystemIcons.Application.ToBitmap();
		}
	}
}
