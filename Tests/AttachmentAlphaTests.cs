using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class AttachmentAlphaTests
{
	private static IEnumerable<Control> Descendants(Control parent)
	{
		foreach (Control child in parent.Controls)
		{
			yield return child;
			foreach (var descendant in Descendants(child))
				yield return descendant;
		}
	}

	public class CompositedBadges
	{
		[TestCase(false, 32)]
		[TestCase(true, 32)]
		[TestCase(false, 230)]
		[TestCase(true, 230)]
		public async Task Preserves_Alpha_Outside_The_Image_And_Warning_Shape(bool devExpress, int background)
		{
			var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
			using (var source = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
			{
				using var graphics = Graphics.FromImage(source);
				graphics.Clear(Color.Transparent);
				graphics.FillRectangle(Brushes.Blue, 2, 2, 8, 8);
				source.Save(path, ImageFormat.Png);
			}
			try
			{
				using var chat = devExpress ? new DXChatControl() : new ChatControl();
				chat.Size = new Size(600, 400);
				chat.BackColor = Color.FromArgb(background, background, background);
				chat.AllowFileAttachments = true;
				chat.AttachmentContentProviders.Insert(0, new BinaryAttachmentContentProvider());
				chat.CreateControl();
				(await chat.AddFilesToInputAsync([path])).ShouldBe(1);
				var card = Descendants(chat.InputControl!).OfType<AttachmentCard>().Single();
				card.NoticeSeverity.ShouldBe(ChatNoticeSeverity.Warning);
				var preview = Descendants(card).Single(control => control is PictureBox or DevExpress.XtraEditors.PictureEdit);
				var image = (Bitmap)(preview is PictureBox picture ? picture.Image! : ((DevExpress.XtraEditors.PictureEdit)preview).Image);
				image.GetPixel(0, 0).A.ShouldBe((byte)0);
				image.GetPixel(image.Width - 1, image.Height - 1).A.ShouldBe((byte)0);
				image.GetPixel(image.Width - 16, image.Height - 16).A.ShouldBe((byte)0);
				var warningPixels = 0;
				for (var y = image.Height - 16; y < image.Height; y++)
					for (var x = image.Width - 16; x < image.Width; x++)
					{
						var pixel = image.GetPixel(x, y);
						if (pixel.A > 0 && pixel.R > 180 && pixel.G > 80 && pixel.B < 100)
							warningPixels++;
					}
				warningPixels.ShouldBeGreaterThan(0);
			}
			finally
			{
				File.Delete(path);
			}
		}
	}

	public class RejectedFiles
	{
		[TestCase(false)]
		[TestCase(true)]
		public async Task Retains_The_File_Type_Icon_Without_Creating_A_Sendable_Attachment(bool devExpress)
		{
			using var chat = devExpress ? new DXChatControl() : new ChatControl();
			chat.Size = new Size(600, 400);
			chat.AllowFileAttachments = true;
			chat.CreateControl();
			var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
			(await chat.AddFilesToInputAsync([path])).ShouldBe(0);
			var card = Descendants(chat.InputControl!).OfType<AttachmentCard>().Single();
			card.InputNotice.ShouldNotBeNull().FileName.ShouldBe(Path.GetFileName(path));
			card.NoticeSeverity.ShouldBe(ChatNoticeSeverity.Error);
			card.AccessibleName.ShouldBe(Path.GetFileName(path));
			var preview = Descendants(card).Single(control => control is PictureBox or DevExpress.XtraEditors.PictureEdit);
			(preview is PictureBox picture ? picture.Image : ((DevExpress.XtraEditors.PictureEdit)preview).Image).ShouldNotBeNull();
			((IChatAttachmentInputControl)chat.InputControl!).PendingAttachments.ShouldBeEmpty();
			chat.Messages.ShouldBeEmpty();
			((IChatAttachmentNoticeInput)chat.InputControl!).ClearAttachmentNotices(Path.GetFileName(path));
			card.IsDisposed.ShouldBeTrue();
		}
	}
}
