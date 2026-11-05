using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class AttachmentInputErrorTests
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

	private static IEnumerable<AttachmentCard> Rejections(Control parent) => Descendants(parent).OfType<AttachmentCard>().Where(card => card.InputNotice is not null);

	public class AddingFiles
	{
		[TestCase(false)]
		[TestCase(true)]
		public async Task Keeps_Rejection_Feedback_In_The_Input_With_Valid_Files(bool devExpress)
		{
			var small = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
			var large = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
			File.WriteAllText(small, "text");
			File.WriteAllBytes(large, new byte[20]);
			try
			{
				using var chat = devExpress ? new DXChatControl() : new ChatControl();
				chat.Size = new Size(600, 400);
				chat.AllowFileAttachments = true;
				chat.MaximumAttachmentFileSize = 10;
				chat.CreateControl();
				var errors = 0;
				chat.ErrorOccurred += (_, _) => errors++;
				(await chat.AddFilesToInputAsync([large, small])).ShouldBe(1);
				chat.Messages.ShouldBeEmpty();
				((IChatAttachmentInputControl)chat.InputControl!).PendingAttachments.Single().Name.ShouldBe(Path.GetFileName(small));
				var notice = Rejections(chat.InputControl!).Single();
				var content = notice.InputNotice!;
				content.Code.ShouldBe(ChatNoticeCode.AttachmentTooLarge);
				content.FileName.ShouldBe(Path.GetFileName(large));
				notice.Visible.ShouldBeTrue();
				errors.ShouldBe(1);
				Descendants(chat.MessageHistoryControl!).OfType<ChatNoticeControl>().ShouldBeEmpty();
				((IChatInputDraftControl)chat.InputControl!).ClearDraft();
				Rejections(chat.InputControl!).ShouldBeEmpty();
				((IChatAttachmentNoticeInput)chat.InputControl!).HasAttachmentNotices.ShouldBeFalse();
			}
			finally
			{
				File.Delete(small);
				File.Delete(large);
			}
		}

		[TestCase(false)]
		[TestCase(true)]
		public async Task Replaces_The_Previous_Error_When_The_Same_File_Is_Retried(bool devExpress)
		{
			var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
			try
			{
				using var chat = devExpress ? new DXChatControl() : new ChatControl();
				chat.Size = new Size(600, 400);
				chat.AllowFileAttachments = true;
				chat.CreateControl();
				(await chat.AddFilesToInputAsync([path])).ShouldBe(0);
				(await chat.AddFilesToInputAsync([path])).ShouldBe(0);
				Rejections(chat.InputControl!).Count().ShouldBe(1);
				File.WriteAllText(path, "available");
				(await chat.AddFilesToInputAsync([path])).ShouldBe(1);
				Rejections(chat.InputControl!).ShouldBeEmpty();
				chat.Messages.ShouldBeEmpty();
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void Preserves_Pre_Handle_Rejections_For_The_Input_Without_Creating_History()
		{
			using var chat = new ChatControl { AllowFileAttachments = true, MaximumAttachmentFileSize = 1, Size = new Size(600, 400) };
			chat.SendMessage(chat.Sender, new FileAttachmentMessageContent([new ChatFileAttachment("too-large.txt", "text/plain", "text"u8.ToArray())])).ShouldBeFalse();
			chat.Messages.ShouldBeEmpty();
			chat.CreateControl();
			Rejections(chat.InputControl!).Count().ShouldBe(1);
			chat.StartNewChat();
			Rejections(chat.InputControl!).ShouldBeEmpty();
		}
	}

	public class ConversionFailures
	{
		[Test]
		public async Task Places_A_Conversion_Error_On_The_Sent_Attachment_Without_A_Separate_Message()
		{
			var client = new RecordingClient();
			using var chat = new ChatControl { AllowFileAttachments = true, UseStreaming = false, ServiceProvider = new ClientServices(client), Size = new Size(600, 400) };
			chat.AttachmentContentProviders.Insert(0, new FailingProvider());
			chat.CreateControl();
			var file = new ChatFileAttachment("unreadable.xlsx", "application/octet-stream", new byte[1]);
			chat.SendMessage(chat.Sender, new FileAttachmentMessageContent([file], "read it")).ShouldBeTrue();
			await chat.ChatClientSendCompletion;
			chat.Messages.Count().ShouldBe(1);
			chat.Messages.Single().Content.ShouldBeOfType<FileAttachmentMessageContent>();
			Descendants(chat.MessageHistoryControl!).OfType<ChatNoticeControl>().ShouldBeEmpty();
			var card = Descendants(chat.MessageHistoryControl!).OfType<AttachmentCard>().Single();
			card.NoticeSeverity.ShouldBe(ChatNoticeSeverity.Error);
			card.AccessibleDescription.ShouldNotBeNull().ShouldContain(chat.NoticeTextProvider(new ChatNotice(ChatNoticeCode.NoExtractableText, file.Name)));
			client.Requests.ShouldBe(0);
		}
	}

	private sealed class FailingProvider : IAttachmentContentProvider
	{
		public IReadOnlyList<string> FileExtensions => [".xlsx"];
		public bool CanHandle(string fileName, string mediaType) => true;
		public ValueTask<AttachmentConversionResult> ConvertAsync(ChatFileAttachment attachment, CancellationToken cancellationToken = default) => throw new AttachmentContentException(new ChatNotice(ChatNoticeCode.NoExtractableText, attachment.Name));
	}

	private sealed class ClientServices(IChatClient client) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType == typeof(IChatClient) ? client : null;
	}

	private sealed class RecordingClient : IChatClient
	{
		public int Requests { get; private set; }
		public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
		{
			Requests++;
			return Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "unexpected")));
		}
		public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public object? GetService(Type serviceType, object? serviceKey = null) => null;
		public void Dispose() { }
	}
}
