using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class AttachmentPresentationTests
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

	private static void PumpUntilCompleted(Task task)
	{
		var timer = System.Diagnostics.Stopwatch.StartNew();
		while (!task.IsCompleted && timer.Elapsed < TimeSpan.FromSeconds(10))
		{
			Application.DoEvents();
			Thread.Sleep(1);
		}
		task.IsCompleted.ShouldBeTrue("The live preview must finish loading without blocking the UI thread.");
		task.GetAwaiter().GetResult();
	}

	private static void DrawPreview(ChatControl chat, Bitmap bitmap)
	{
		chat.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
		// DrawToBitmap prints overlapping child controls in reverse order.
		// Paint the floating badges/actions last to match the on-screen z order.
		foreach (var overlay in Descendants(chat).OfType<AttachmentCard>().SelectMany(card => Descendants(card)).Where(control => control.Visible && (control is Label || control is DevExpress.XtraEditors.LabelControl || control is Button || control is DevExpress.XtraEditors.SimpleButton)))
		{
			using var layer = new Bitmap(overlay.Width, overlay.Height);
			overlay.DrawToBitmap(layer, new Rectangle(Point.Empty, layer.Size));
			using var graphics = Graphics.FromImage(bitmap);
			graphics.DrawImageUnscaled(layer, chat.PointToClient(overlay.PointToScreen(Point.Empty)));
		}
	}

	public class DesignerSettings
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Updates_Draft_And_History_Cards_With_The_Same_Settings(bool devExpress)
		{
			using var chat = devExpress ? new DXChatControl() : new ChatControl();
			chat.Size = new Size(640, 600);
			chat.AllowFileAttachments = true;
			chat.CreateControl();
			var file = new ChatFileAttachment("invoice.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new byte[2048]);
			chat.AddMessage(chat.Sender, new FileAttachmentMessageContent([file]));
			((IChatAttachmentInputControl)chat.InputControl!).AddAttachments([file]);
			chat.DisplayTexts.RemoveAttachment = "Entfernen (Host)";
			chat.AttachmentAppearance.CardWidth = 310;
			chat.AttachmentAppearance.BackColor = Color.MidnightBlue;
			chat.AttachmentAppearance.ForeColor = Color.White;

			var cards = Descendants(chat).Where(control => control.GetType().Name.EndsWith("AttachmentCard")).ToArray();
			cards.Length.ShouldBe(2);
			cards.ShouldAllBe(card => card.Width == (int)Math.Round(310 * card.DeviceDpi / 96f));
			cards.ShouldAllBe(card => card.Controls[0].BackColor == Color.MidnightBlue);
			Descendants(cards[0]).ShouldNotContain(control => control.Text == "invoice.xlsx" || control.Text.Contains("2 KB"));
			var remove = Descendants(chat.InputControl!).Single(control => control.AccessibleName == "Entfernen (Host)");
			remove.Visible.ShouldBeFalse();
			typeof(Control).GetMethod("OnMouseEnter", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(remove.Parent, [EventArgs.Empty]);
			remove.Visible.ShouldBeTrue();
			// Leaving a child for the remove button must not hide the action immediately.
			typeof(Control).GetMethod("OnMouseLeave", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(remove.Parent, [EventArgs.Empty]);
			remove.Visible.ShouldBeTrue();
			typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(remove, [EventArgs.Empty]);
			((IChatAttachmentInputControl)chat.InputControl!).PendingAttachments.ShouldBeEmpty();
			Descendants(chat).Count(control => control.GetType().Name.EndsWith("AttachmentCard")).ShouldBe(1);
		}

		[Test]
		public void Keeps_Resource_Defaults_And_Serializes_Only_Explicit_Localized_Overrides()
		{
			var texts = new ChatDisplayTexts();
			var property = TypeDescriptor.GetProperties(texts)[nameof(texts.RemoveAttachment)]!;
			property.IsLocalizable.ShouldBeTrue();
			property.ShouldSerializeValue(texts).ShouldBeFalse();
			using var chat = new ChatControl();
			chat.DisplayTexts.BinaryAttachment = "Datei {0}: Binärformat";
			chat.NoticeTextProvider(new ChatNotice(ChatNoticeCode.BinaryAttachment, "test.bin")).ShouldBe("Datei test.bin: Binärformat");
			texts.RemoveAttachment = "Host override";
			property.ShouldSerializeValue(texts).ShouldBeTrue();
			property.ResetValue(texts);
			property.ShouldSerializeValue(texts).ShouldBeFalse();
			var original = CultureInfo.CurrentUICulture;
			try
			{
				CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
				texts.RemoveAttachment.ShouldBe("Anhang entfernen");
				CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
				texts.RemoveAttachment.ShouldBe("Remove attachment");
			}
			finally
			{
				CultureInfo.CurrentUICulture = original;
			}
		}
	}

	public class AttachmentWarnings
	{
		[TestCase(false)]
		[TestCase(true)]
		public async Task Places_The_Warning_On_The_File_And_Preserves_It_In_History(bool devExpress)
		{
			var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.unknown");
			File.WriteAllBytes(path, [1, 2, 3]);
			try
			{
				using var chat = devExpress ? new DXChatControl() : new ChatControl();
				chat.Size = new Size(640, 400);
				chat.AllowFileAttachments = true;
				chat.NoticeTextProvider = notice => $"host:{notice.Code}";
				chat.CreateControl();
				(await chat.AddFilesToInputAsync([path])).ShouldBe(1);
				chat.Messages.ShouldBeEmpty();
				var draft = (IChatAttachmentInputControl)chat.InputControl!;
				var file = draft.PendingAttachments.Single();
				var card = Descendants(chat.InputControl!).OfType<AttachmentCard>().Single();
				card.Width.ShouldBe((int)Math.Round(48 * card.DeviceDpi / 96f));
				card.Height.ShouldBe((int)Math.Round(48 * card.DeviceDpi / 96f));
				card.NoticeSeverity.ShouldBe(ChatNoticeSeverity.Warning);
				card.AccessibleDescription.ShouldNotBeNull().ShouldContain("host:BinaryAttachment");
				draft.ClearAttachments();
				var history = (Control)chat.AddMessage(chat.Sender, new FileAttachmentMessageContent([file]));
				Descendants(history).OfType<AttachmentCard>().Single().NoticeSeverity.ShouldBe(ChatNoticeSeverity.Warning);
				// A different file with the same name must not inherit this file's warning.
				draft.AddAttachments([new ChatFileAttachment(file.Name, "text/plain", "text"u8.ToArray())]);
				Descendants(chat.InputControl!).OfType<AttachmentCard>().Single().NoticeSeverity.ShouldBeNull();
				chat.Messages.ShouldNotContain(message => message.Content is ChatNoticeMessageContent);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void Uses_DevExpress_Scrolling_For_Overflowing_Draft_Attachments()
		{
			using var input = new DXChatInputControl { Size = new Size(400, 150) };
			input.CreateControl();
			input.AddAttachments(Enumerable.Range(1, 8).Select(index => new ChatFileAttachment($"file-{index}.txt", "text/plain", "text"u8.ToArray())));
			input.PerformLayout();
			var strip = Descendants(input).OfType<DevExpress.Utils.Layout.StackPanel>().Single();
			strip.AutoScroll.ShouldBeTrue();
			strip.HorizontalScroll.Visible.ShouldBeTrue();
			strip.VerticalScroll.Visible.ShouldBeFalse();
			strip.Controls.Cast<Control>().Max(control => control.Right).ShouldBeGreaterThan(strip.ClientSize.Width);
			Descendants(input).OfType<ScrollableControl>().Where(control => control.AutoScroll && control is not DevExpress.XtraEditors.XtraScrollableControl).ShouldBeEmpty();
			strip.ScrollControlIntoView(strip.Controls[strip.Controls.Count - 1]);
			strip.HorizontalScroll.Value.ShouldBeGreaterThan(0);
		}
	}

	public class RemovalLifetime
	{
		[TestCase(false)]
		[TestCase(true)]
		public async Task Removing_With_Pending_Hover_And_Icon_Work_Does_Not_Revive_The_Control(bool devExpress)
		{
			using var input = devExpress ? (Control)new DXChatInputControl() : new ChatInputControl();
			input.Size = new Size(400, 160);
			input.CreateControl();
			var draft = (IChatAttachmentInputControl)input;
			draft.AddAttachments([new ChatFileAttachment("report.xlsx", "application/octet-stream", new byte[1])]);
			var card = Descendants(input).OfType<AttachmentCard>().Single();
			card.CreateControl();
			var completion = card.PreviewLoadCompletion;
			var timer = (System.Windows.Forms.Timer)typeof(AttachmentCard).GetField("_hoverTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card)!;
			var leave = typeof(Control).GetMethod("OnMouseLeave", BindingFlags.Instance | BindingFlags.NonPublic)!;
			leave.Invoke(card, [EventArgs.Empty]);
			timer.Enabled.ShouldBeTrue();
			card.Disposed += (_, _) => leave.Invoke(card, [EventArgs.Empty]);
			var remove = Descendants(card).Single(control => control.AccessibleName == ChatLocalization.GetString("RemoveAttachment"));
			typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(remove, [EventArgs.Empty]);
			card.IsDisposed.ShouldBeTrue();
			draft.PendingAttachments.ShouldBeEmpty();
			// Simulate an already queued tick and leave/focus notifications after removal.
			typeof(System.Windows.Forms.Timer).GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(timer, [EventArgs.Empty]);
			typeof(AttachmentCard).GetMethod("HoverTimerTick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(card, [timer, EventArgs.Empty]);
			leave.Invoke(card, [EventArgs.Empty]);
			typeof(Control).GetMethod("OnLostFocus", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(card, [EventArgs.Empty]);
			await completion;
			timer.Enabled.ShouldBeFalse();
			card.IsHandleCreated.ShouldBeFalse();
		}
	}

	public class FileIconsAndTooltips
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Loads_The_Windows_Icon_Into_A_Live_Preview(bool devExpress)
		{
			using var input = devExpress ? (Control)new DXChatInputControl() : new ChatInputControl();
			input.Size = new Size(400, 160);
			((IChatAttachmentInputControl)input).AddAttachments([new ChatFileAttachment("live.txt", "text/plain", "text"u8.ToArray())]);
			var card = Descendants(input).OfType<AttachmentCard>().Single();
			var preview = Descendants(card).Single(control => control is PictureBox or DevExpress.XtraEditors.PictureEdit);
			Image? GetImage() => preview is PictureBox picture ? picture.Image : ((DevExpress.XtraEditors.PictureEdit)preview).Image;
			var placeholder = GetImage();
			input.CreateControl();
			card.CreateControl();
			PumpUntilCompleted(card.PreviewLoadCompletion);
			GetImage().ShouldNotBeNull().ShouldNotBeSameAs(placeholder);
		}


		[Test]
		public async Task Supplies_Independent_Windows_Icon_Images_For_The_Same_File_Type()
		{
			using var first = await WindowsFileIconFactory.CreateAsync("first.XLSX");
			using var second = await WindowsFileIconFactory.CreateAsync("second.xlsx");
			first.ShouldNotBeSameAs(second);
			first.Dispose();
			second.Width.ShouldBeGreaterThan(0);
			second.Height.ShouldBeGreaterThan(0);
			using var unknown = await WindowsFileIconFactory.CreateAsync("test.unregistered-tinychat-extension");
			unknown.Width.ShouldBeGreaterThan(0);
		}

		[Test]
		public void Uses_DevExpress_SuperTips_For_File_Details_And_Hover_Actions()
		{
			using var input = new DXChatInputControl();
			input.AddAttachments([new ChatFileAttachment("sales & costs.txt", "text/plain", "text"u8.ToArray())]);
			var preview = Descendants(input).OfType<DevExpress.XtraEditors.PictureEdit>().Single();
			preview.ToolTipController.ShouldNotBeNull();
			preview.SuperTip.Items.OfType<DevExpress.Utils.ToolTipTitleItem>().Single().Text.ShouldBe("sales & costs.txt");
			preview.SuperTip.Items.OfType<DevExpress.Utils.ToolTipItem>().Where(item => item is not DevExpress.Utils.ToolTipTitleItem).Single().Text.ShouldContain("4 B");
			preview.SuperTip.Items.OfType<DevExpress.Utils.ToolTipItem>().Where(item => item is not DevExpress.Utils.ToolTipTitleItem).Single().Text.ShouldContain(ChatLocalization.GetString("OpenAttachment"));
			Descendants(input).ShouldNotContain(control => control.Text.Contains("sales & costs.txt") || control.Text.Contains("4 B"));
			var remove = Descendants(input).OfType<DevExpress.XtraEditors.SimpleButton>().Single(button => button.AccessibleName == ChatLocalization.GetString("RemoveAttachment"));
			remove.ToolTipController.ShouldBeSameAs(preview.ToolTipController);
		}
	}

	public class VisualCheck
	{
		[TestCase(false)]
		[TestCase(true)]
		[Explicit("Writes offscreen-rendered attachment screenshots for visual inspection")]
		public async Task Renders_Attachment_Cards(bool devExpress)
		{
			var directory = Path.Combine(Path.GetTempPath(), $"tinychat-preview-{Guid.NewGuid():N}");
			Directory.CreateDirectory(directory);
			var skin = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSkinName;
			try
			{
				if (devExpress)
					DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(DevExpress.LookAndFeel.SkinStyle.Office2010Blue);
				using var form = new Form { ClientSize = new Size(580, 420), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
				using var chat = devExpress ? new DXChatControl { Dock = DockStyle.Fill } : new ChatControl { Dock = DockStyle.Fill };
				if (devExpress)
					chat.BackColor = DevExpress.LookAndFeel.LookAndFeelHelper.GetSystemColor(DevExpress.LookAndFeel.UserLookAndFeel.Default, SystemColors.Control);
				form.Controls.Add(chat);
				form.Show();
				chat.AllowFileAttachments = true;
				chat.SplitterPosition = 100;
				File.WriteAllBytes(Path.Combine(directory, "PIA_ProductModel_Matching_Kurzfassung.xlsx"), new byte[125440]);
				File.WriteAllBytes(Path.Combine(directory, "DevExpress Invoice 2.pdf"), new byte[4096]);
				File.WriteAllBytes(Path.Combine(directory, "Archive.zip"), new byte[2048]);
				using var image = new Bitmap(80, 60);
				using (var graphics = Graphics.FromImage(image))
				{
					graphics.Clear(Color.LightSkyBlue);
					graphics.FillEllipse(Brushes.CornflowerBlue, 10, 8, 46, 46);
				}
				image.Save(Path.Combine(directory, "Screenshot.png"), System.Drawing.Imaging.ImageFormat.Png);
				await chat.AddFilesToInputAsync([.. Directory.GetFiles(directory).Order(), Path.Combine(directory, "cannot-read.xlsx")]);
				var files = ((IChatAttachmentInputControl)chat.InputControl!).PendingAttachments.ToArray();
				chat.AddMessage(chat.Sender, new FileAttachmentMessageContent(files, "Dokumente zur Prüfung"));
				PumpUntilCompleted(Task.WhenAll(Descendants(chat).OfType<AttachmentCard>().Select(card => card.PreviewLoadCompletion)));
				for (var pass = 0; pass < 5; pass++)
					Application.DoEvents();
				using var bitmap = new Bitmap(chat.ClientSize.Width, chat.ClientSize.Height);
				DrawPreview(chat, bitmap);
				bitmap.Save(Path.Combine(Path.GetTempPath(), $"tinychat-attachments-{(devExpress ? "dx" : "native")}.png"));
				var remove = Descendants(chat.InputControl!).First(control => control.AccessibleName == ChatLocalization.GetString("RemoveAttachment"));
				typeof(Control).GetMethod("OnMouseEnter", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(remove.Parent, [EventArgs.Empty]);
				DrawPreview(chat, bitmap);
				bitmap.Save(Path.Combine(Path.GetTempPath(), $"tinychat-attachments-{(devExpress ? "dx" : "native")}-hover.png"));
			}
			finally
			{
				DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(skin);
				foreach (var path in Directory.GetFiles(directory))
					File.Delete(path);
				Directory.Delete(directory);
			}
		}
	}
}
