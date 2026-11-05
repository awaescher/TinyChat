using System.Drawing;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.Utils.Colors;
using DevExpress.XtraEditors;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ChatErrorPresentationTests
{
	public class RequestErrors
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Shows_Localized_Text_And_The_Actual_Error_Without_Hovering(bool devExpress)
		{
			using var chat = devExpress ? new DXChatControl() : new ChatControl();
			chat.Size = new Size(600, 400);
			chat.CreateControl();
			chat.DisplayTexts.GeneralError = "Request failed";
			var content = new ChatNoticeMessageContent(new ChatNotice(ChatNoticeCode.GeneralError, Exception: new InvalidOperationException("Server unavailable <details>")));
			var row = (Control)chat.AddMessage(new NamedSender(string.Empty), content);
			var text = row.Controls.Cast<Control>().Single(control => control.Text.Contains("Request failed"));
			text.Text.ShouldContain("Server unavailable <details>");
			row.AccessibleName.ShouldNotBeNull().ShouldContain("Server unavailable <details>");
			if (devExpress)
			{
				text.ShouldBeOfType<LabelControl>().AllowHtmlString.ShouldBeFalse();
				row.Controls.OfType<Label>().ShouldBeEmpty();
			}

			row.MinimumSize = Size.Empty;
			row.MaximumSize = new Size(160, 0);
			row.Width = 160;
			row.PerformLayout();
			text.Width.ShouldBeLessThanOrEqualTo(136);
			text.Height.ShouldBeGreaterThan(text.Font.Height);

			chat.DisplayTexts.GeneralError = "Changed by host";
			text.Text.ShouldStartWith("Changed by host");
			text.Text.ShouldContain("Server unavailable <details>");
		}

		[Test]
		public void Draws_A_Filled_Error_Circle_With_A_Transparent_Cross()
		{
			using var image = ChatStatusImage.CreateError(new Size(32, 32), Color.Red);
			image.GetPixel(16, 4).A.ShouldBe((byte)255);
			image.GetPixel(16, 4).R.ShouldBe((byte)255);
			image.GetPixel(16, 16).A.ShouldBe((byte)0);
			image.GetPixel(0, 0).A.ShouldBe((byte)0);
		}
	}

	public class SkinColors
	{
		[Test]
		public void Refreshes_Error_Badges_When_The_DevExpress_Skin_Changes()
		{
			var lookAndFeel = UserLookAndFeel.Default;
			var previousSkin = lookAndFeel.ActiveSkinName;
			var previousPalette = lookAndFeel.ActiveSvgPaletteName;
			try
			{
				using var chat = new DXChatControl { Size = new Size(600, 400) };
				chat.CreateControl();
				var notice = new ChatNotice(ChatNoticeCode.AttachmentRejected, "missing.txt");
				var card = (Control)typeof(DXChatControl).GetMethod("CreateInputAttachmentNotice", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(chat, [notice])!;
				using (card)
				{
					var preview = card.Controls.Cast<Control>().SelectMany(control => control.Controls.Cast<Control>()).OfType<PictureEdit>().Single();
					foreach (var skin in new[] { SkinStyle.Office2010Blue, SkinStyle.Office2019Black })
					{
						lookAndFeel.SetSkinStyle(skin);
						card.PerformLayout();
						var image = (Bitmap)preview.Image;
						var color = DXSkinColorHelper.GetDXSkinColor(DXSkinColors.FillColors.Danger, preview.LookAndFeel.ActiveSkinName, preview.LookAndFeel.ActiveSvgPaletteName);
						var badge = new Rectangle(image.Width - 16, image.Height - 16, 16, 16);
						using var status = (Bitmap)card.GetType().GetMethod("CreateNoticeBadge", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(card, [new Size(16, 16), ChatNoticeSeverity.Error])!;
						status.GetPixel(8, 8).A.ShouldBe((byte)0);
						var fill = image.GetPixel(badge.Left + 8, badge.Top + 2);
						fill.ToArgb().ShouldBe(color.ToArgb());
					}
				}
			}
			finally
			{
				lookAndFeel.SetSkinStyle(previousSkin, previousPalette);
			}
		}
	}

	public class VisualCheck
	{
		[TestCase(false)]
		[TestCase(true)]
		[Explicit("Renders inline errors and their status icons for visual inspection")]
		public void Renders_Request_Errors(bool devExpress)
		{
			var lookAndFeel = UserLookAndFeel.Default;
			var previousSkin = lookAndFeel.ActiveSkinName;
			var previousPalette = lookAndFeel.ActiveSvgPaletteName;
			try
			{
				using var form = new Form { ClientSize = new Size(440, 330), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
				using var chat = devExpress ? new DXChatControl { Dock = DockStyle.Fill } : new ChatControl { Dock = DockStyle.Fill };
				form.Controls.Add(chat);
				form.Show();
				chat.DisplayTexts.GeneralError = "Die Anfrage konnte nicht abgeschlossen werden.";
				chat.AddMessage(chat.Sender, new StringMessageContent("Und das?"));
				chat.AddMessage(new NamedSender(string.Empty), new ChatNoticeMessageContent(new ChatNotice(ChatNoticeCode.GeneralError,
					Exception: new InvalidOperationException("Server unavailable: the request timed out while processing this document. Please try again later."))));
				foreach (var skin in devExpress ? new[] { SkinStyle.Office2010Blue, SkinStyle.Office2019Black } : new[] { SkinStyle.Office2010Blue })
				{
					if (devExpress)
					{
						lookAndFeel.SetSkinStyle(skin);
						chat.BackColor = LookAndFeelHelper.GetSystemColor(lookAndFeel, SystemColors.Control);
						chat.ForeColor = LookAndFeelHelper.GetSystemColor(lookAndFeel, SystemColors.ControlText);
					}
					for (var pass = 0; pass < 5; pass++)
						Application.DoEvents();
					using var image = new Bitmap(chat.Width, chat.Height);
					chat.DrawToBitmap(image, chat.ClientRectangle);
					image.Save(Path.Combine(Path.GetTempPath(), $"tinychat-error-{(devExpress ? lookAndFeel.ActiveSkinName.Replace(' ', '-') : "native")}.png"));
				}
			}
			finally
			{
				lookAndFeel.SetSkinStyle(previousSkin, previousPalette);
			}
		}
	}
}
