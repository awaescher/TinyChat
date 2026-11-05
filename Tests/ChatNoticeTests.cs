using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ChatNoticeTests
{
	public class Localization : ChatNoticeTests
	{
		[TestCase("de-DE", "Dateianhänge sind deaktiviert.")]
		[TestCase("en-US", "File attachments are disabled.")]
		public void Uses_The_Current_Ui_Culture_For_Notice_Tooltips(string cultureName, string expected)
		{
			var previous = CultureInfo.CurrentUICulture;
			try
			{
				CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
				using var chat = new ChatControl();
				var notice = new ChatNotice(ChatNoticeCode.AttachmentsDisabled);
				var message = new TinyChat.ChatMessage(new NamedSender(string.Empty), new ChatNoticeMessageContent(notice));
				var control = (Control)chat.AddMessage(message.Sender, message.Content);
				control.AccessibleName.ShouldBe(expected);
				control.Controls.OfType<Label>().ShouldAllBe(label => label.Text != expected);
			}
			finally
			{
				CultureInfo.CurrentUICulture = previous;
			}
		}

		[Test]
		public void Lets_The_Host_Use_Its_Existing_Text_Catalog()
		{
			using var chat = new ChatControl { NoticeTextProvider = notice => $"host:{notice.Code}" };
			var control = (Control)chat.AddMessage(new NamedSender(string.Empty), new ChatNoticeMessageContent(new ChatNotice(ChatNoticeCode.GeneralError)));
			control.AccessibleName.ShouldBe("host:GeneralError");
		}
	}

	public class BinaryFallback : ChatNoticeTests
	{
		[Test]
		public async Task Warns_Once_And_Excludes_Ui_Notices_From_Subsequent_Model_Requests()
		{
			using var chat = new ConversionChatControl();
			var notices = new List<ChatNotice>();
			chat.NoticeOccurred += (_, args) => notices.Add(args.Notice);
			var attachment = new ChatFileAttachment("unknown.custom", "application/octet-stream", new byte[] { 1, 2, 3 });
			chat.AddForConversion(new FileAttachmentMessageContent([attachment], "Read this"));
			await chat.ConvertAsync();
			var converted = await chat.ConvertAsync();
			notices.Single().Code.ShouldBe(ChatNoticeCode.BinaryAttachment);
			converted.Single().Text.ShouldBe("Read this");
			converted.Single().Contents.OfType<DataContent>().Single().Data.ToArray().ShouldBe(attachment.Data.ToArray());
			chat.Messages.ShouldNotContain(message => message.Content is ChatNoticeMessageContent);
		}

		[Test]
		public async Task Lets_The_Host_Handle_A_Warning_Without_Adding_A_Row()
		{
			using var chat = new ConversionChatControl();
			chat.NoticeOccurred += (_, args) => args.Handled = true;
			chat.AddForConversion(new FileAttachmentMessageContent([new ChatFileAttachment("unknown.bin", "application/octet-stream", new byte[] { 1 })]));
			(await chat.ConvertAsync()).Single().Contents.OfType<DataContent>().Count().ShouldBe(1);
			chat.Messages.ShouldNotContain(message => message.Content is ChatNoticeMessageContent);
		}
	}

	private sealed class ConversionChatControl : ChatControl
	{
		public void AddForConversion(IChatMessageContent content) => AddChatMessage(Sender, content);
		public Task<List<Microsoft.Extensions.AI.ChatMessage>> ConvertAsync() => ConvertToChatMessagesAsync();
	}

}
