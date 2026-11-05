using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TinyChat;

/// <summary>Creates a filled error circle with a transparent cross on any background.</summary>
internal static class ChatStatusImage
{
	internal static Bitmap CreateError(Size size, Color color)
	{
		var image = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		using var graphics = Graphics.FromImage(image);
		graphics.Clear(Color.Transparent);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.ScaleTransform(size.Width / 16f, size.Height / 16f);
		using var path = new GraphicsPath(FillMode.Alternate);
		path.AddEllipse(1, 1, 14, 14);
		path.AddPolygon([new(5, 4), new(8, 7), new(11, 4), new(12, 5), new(9, 8), new(12, 11),
			new(11, 12), new(8, 9), new(5, 12), new(4, 11), new(7, 8), new(4, 5)]);
		using var brush = new SolidBrush(color);
		graphics.FillPath(brush, path);
		return image;
	}
}
