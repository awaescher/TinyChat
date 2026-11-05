using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ChatControlAttachmentAndCancellationTests
{
	private static IEnumerable<Control> DescendantsOf(Control parent)
	{
		foreach (Control child in parent.Controls)
		{
			yield return child;
			foreach (var descendant in DescendantsOf(child))
				yield return descendant;
		}
	}

	public class FileAttachments
	{
		[Test]
		public async Task Loads_Image_With_Detected_Media_Type()
		{
			var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
			var expectedData = new byte[] { 1, 2, 3, 4 };
			try
			{
				await File.WriteAllBytesAsync(filePath, expectedData);

				var attachment = await ChatFileAttachment.LoadAsync(filePath);

				attachment.Name.ShouldBe(Path.GetFileName(filePath));
				attachment.MediaType.ShouldBe("image/png");
				attachment.IsImage.ShouldBeTrue();
				attachment.Data.ToArray().ShouldBe(expectedData);
			}
			finally
			{
				File.Delete(filePath);
			}
		}

		[Test]
		public async Task Rejects_File_Over_Configured_Size_Limit()
		{
			var filePath = Path.GetTempFileName();
			try
			{
				await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2 });

				await Should.ThrowAsync<IOException>(() => ChatFileAttachment.LoadAsync(filePath, maximumFileSize: 1));
			}
			finally
			{
				File.Delete(filePath);
			}
		}

		[Test]
		public void Converts_Attachments_To_Multimodal_AI_Content()
		{
			using var control = new TestChatControl();
			var attachment = new ChatFileAttachment("photo.png", "image/png", new byte[] { 7, 8, 9 });
			control.AddForConversion(control.Sender, new FileAttachmentMessageContent([attachment], "Describe this image"));

			var message = control.ConvertMessages().Single();

			message.Role.ShouldBe(ChatRole.User);
			message.Contents.OfType<TextContent>().Single().Text.ShouldBe("Describe this image");
			var dataContent = message.Contents.OfType<DataContent>().Single();
			dataContent.Name.ShouldBe("photo.png");
			dataContent.MediaType.ShouldBe("image/png");
			dataContent.Data.ToArray().ShouldBe(new byte[] { 7, 8, 9 });
		}

		[Test]
		public void Converts_Markdown_Attachment_To_Named_Text_Content()
		{
			using var control = new TestChatControl();
			var attachment = new ChatFileAttachment(
				"notes.md",
				"text/markdown",
				System.Text.Encoding.UTF8.GetBytes("# Heading\n\nThe document body."));
			control.AddForConversion(control.Sender, new FileAttachmentMessageContent([attachment], "Summarize the file"));

			var message = control.ConvertMessages().Single();
			var textContents = message.Contents.OfType<TextContent>().ToArray();

			textContents.Length.ShouldBe(2);
			textContents[0].Text.ShouldBe("Summarize the file");
			textContents[1].Text.ShouldContain("BEGIN ATTACHED FILE: notes.md (text/markdown)");
			textContents[1].Text.ShouldContain("# Heading\n\nThe document body.");
			textContents[1].Text.ShouldContain("END ATTACHED FILE: notes.md");
			message.Contents.OfType<DataContent>().ShouldBeEmpty();
		}

		[Test]
		public void Keeps_Binary_Documents_As_Data_With_A_Ui_Only_Warning()
		{
			using var control = new TestChatControl();
			control.AttachmentContentProviders.Add(new BinaryAttachmentContentProvider());
			var attachment = new ChatFileAttachment("report.pdf", "application/pdf", new byte[] { 1, 2, 3 });
			control.AddForConversion(control.Sender, new FileAttachmentMessageContent([attachment], "Summarize the file"));

			var message = control.ConvertMessages().Single();

			message.Contents.OfType<TextContent>().Single().Text.ShouldBe("Summarize the file");
			control.Messages.ShouldNotContain(message => message.Content is ChatNoticeMessageContent);
			message.Contents.OfType<DataContent>().Single().Name.ShouldBe("report.pdf");
		}

		[Test]
		public void Hides_Image_File_Names_But_Keeps_Document_File_Names_In_Display_Text()
		{
			var image = new ChatFileAttachment("photo.png", "image/png", new byte[] { 1 });
			var document = new ChatFileAttachment("notes.txt", "text/plain", new byte[] { 2 });
			var content = new FileAttachmentMessageContent([image, document], "Caption");

			content.ToString().ShouldNotContain("photo.png");
			content.ToString().ShouldContain("Caption");
			content.ToString().ShouldContain("notes.txt");
			content.ToString().ShouldNotContain("\U0001F4CE");
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Shows_Small_Image_Preview_In_Both_Message_Controls(bool useDevExpress)
		{
			var imageData = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
			var attachment = new ChatFileAttachment("photo.png", "image/png", imageData);
			var message = new TinyChat.ChatMessage(new NamedSender("User"), new FileAttachmentMessageContent([attachment], "Caption"));
			using var control = useDevExpress
				? (Control)new DXChatMessageControl { MessageFormatter = new PlainTextMessageFormatter(), Message = message }
				: new ChatMessageControl { MessageFormatter = new PlainTextMessageFormatter(), Message = message };

			var previewPanel = control.Controls.OfType<FileAttachmentPreviewPanel>().Single();
			var preview = DescendantsOf(previewPanel).Single(child => child is PictureBox or PictureEdit);
			control.Size = new Size(300, 200);
			control.PerformLayout();

			(preview is PictureBox picture ? picture.Image : ((PictureEdit)preview).Image).ShouldNotBeNull();
			preview.Width.ShouldBeGreaterThanOrEqualTo(28);
			control.Controls.Cast<Control>().ShouldContain(child => child.Text == "Caption");
			control.Controls.Cast<Control>()
				.Where(child => child != previewPanel)
				.ShouldAllBe(child => !previewPanel.Bounds.IntersectsWith(child.Bounds));
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Shows_Documents_As_File_Tiles_Without_A_Unicode_Paperclip(bool useDevExpress)
		{
			var attachment = new ChatFileAttachment("notes.md", "text/markdown", "# Notes"u8.ToArray());
			var message = new TinyChat.ChatMessage(
				new NamedSender("User"),
				new FileAttachmentMessageContent([attachment], "Summarize this"));
			using var control = useDevExpress
				? (Control)new DXChatMessageControl { MessageFormatter = new PlainTextMessageFormatter(), Message = message }
				: new ChatMessageControl { MessageFormatter = new PlainTextMessageFormatter(), Message = message };

			var previewPanel = control.Controls.OfType<FileAttachmentPreviewPanel>().Single();
			var tile = previewPanel.Controls.Cast<Control>().Single();
			control.Size = new Size(400, 200);
			control.PerformLayout();
			tile.Height.ShouldBeGreaterThanOrEqualTo(48);
			previewPanel.Height.ShouldBeGreaterThanOrEqualTo(tile.Height);

			tile.AccessibleName.ShouldBe("notes.md");
			tile.Cursor.ShouldBe(Cursors.Hand);
			DescendantsOf(tile).ShouldNotContain(child => child.Text == "notes.md");
			control.Controls.Cast<Control>()
				.Concat(tile.Controls.Cast<Control>())
				.ShouldNotContain(child => child.Text.Contains("\U0001F4CE"));
			control.Controls.Cast<Control>().ShouldContain(child => child.Text == "Summarize this");
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Supports_Message_Before_Formatter_Initialization(bool useDevExpress)
		{
			var imageData = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
			var attachment = new ChatFileAttachment("photo.png", "image/png", imageData);
			var message = new TinyChat.ChatMessage(new NamedSender("User"), new FileAttachmentMessageContent([attachment], "Caption"));
			using var control = useDevExpress
				? (Control)new DXChatMessageControl { Message = message, MessageFormatter = new PlainTextMessageFormatter() }
				: new ChatMessageControl { Message = message, MessageFormatter = new PlainTextMessageFormatter() };

			control.ToString().ShouldBe("Caption");
		}

		[Test]
		public void Native_History_Uses_The_Available_Width_For_Messages()
		{
			var imageData = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
			var caption = "A caption that must use the available chat width instead of wrapping into a narrow column like the previous native layout did.";
			var attachments = new[]
			{
				new ChatFileAttachment("first.png", "image/png", imageData),
				new ChatFileAttachment("second.png", "image/png", imageData)
			};
			var message = new TinyChat.ChatMessage(
				new NamedSender("User"),
				new FileAttachmentMessageContent(attachments, caption));
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(400, 300) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = message
			};

			history.AppendMessageControl(messageControl);
			history.PerformLayout();
			messageControl.PerformLayout();

			messageControl.MinimumSize.Width.ShouldBe(messageControl.MaximumSize.Width);
			messageControl.Width.ShouldBeGreaterThan(300);
			var messageTextControl = messageControl.Controls.OfType<Label>().Single(label => label.Text.StartsWith("A caption"));
			messageTextControl.Width.ShouldBeGreaterThan(300);
			var previewPanel = messageControl.Controls.OfType<FileAttachmentPreviewPanel>().Single();
			previewPanel.Width.ShouldBeGreaterThan(128);
			DescendantsOf(previewPanel).OfType<PictureBox>().Count().ShouldBe(2);
		}

	}

	public class AttachmentDrafts
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Shows_And_Removes_Pending_Attachments(bool useDevExpress)
		{
			using var inputControl = CreateInputControl(useDevExpress);
			inputControl.Size = new Size(400, 180);
			var attachmentInput = (IChatAttachmentInputControl)inputControl;
			var image = new ChatFileAttachment("image.png", "image/png", CreatePngData());
			var document = new ChatFileAttachment("notes.txt", "text/plain", "Hello"u8.ToArray());

			attachmentInput.AddAttachments([image, document]);

			attachmentInput.PendingAttachments.Count.ShouldBe(2);
			var attachmentPanel = Descendants(inputControl).Single(control => useDevExpress ? control is DevExpress.Utils.Layout.StackPanel : control is FlowLayoutPanel);
			attachmentPanel.Visible.ShouldBeTrue();
			var contentLayout = Descendants(inputControl).OfType<TableLayoutPanel>().Single();
			contentLayout.RowStyles[0].Height.ShouldBe(attachmentInput.AttachmentDisplayHeight);
			Descendants(inputControl).ShouldNotContain(control => control.Text == "image.png");
			Descendants(inputControl).ShouldNotContain(control => control.Text.Contains("\U0001F4CE"));
			var removeButtons = Descendants(inputControl).Where(control => control.AccessibleName == ChatLocalization.GetString("RemoveAttachment")).ToArray();
			removeButtons.Length.ShouldBe(2);
			var documentTile = Descendants(inputControl).OfType<AttachmentCard>().Single(control => control.AccessibleName == "notes.txt").Controls[0];
			documentTile.PerformLayout();
			var documentRemoveButton = documentTile.Controls.Cast<Control>().Single(control => control.AccessibleName == ChatLocalization.GetString("RemoveAttachment"));
			Descendants(documentTile).ShouldNotContain(control => control.Text == "notes.txt");
			removeButtons.ShouldAllBe(button => !button.Visible);
			typeof(Control).GetMethod("OnMouseEnter", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(documentTile, [EventArgs.Empty]);
			documentRemoveButton.Visible.ShouldBeTrue();
			typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(documentRemoveButton, [EventArgs.Empty]);
			attachmentInput.PendingAttachments.Count.ShouldBe(1);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Sends_Text_And_Attachments_Together_Then_Clears_Draft(bool useDevExpress)
		{
			using var inputControl = CreateInputControl(useDevExpress);
			var attachmentInput = (IChatAttachmentInputControl)inputControl;
			var chatInput = (IChatInputControl)inputControl;
			var attachment = new ChatFileAttachment("notes.txt", "text/plain", "Hello"u8.ToArray());
			attachmentInput.AddAttachments([attachment]);
			var textBox = useDevExpress
				? (Control)Descendants(inputControl).OfType<MemoEdit>().Single()
				: Descendants(inputControl).OfType<TextBox>().Single();
			inputControl.Size = new Size(400, 120);
			inputControl.PerformLayout();
			var attachmentPanel = Descendants(inputControl).Single(control => useDevExpress ? control is DevExpress.Utils.Layout.StackPanel : control is FlowLayoutPanel);
			var contentLayout = Descendants(inputControl).OfType<TableLayoutPanel>().Single();
			contentLayout.PerformLayout();
			attachmentPanel.Bounds.IntersectsWith(textBox.Bounds).ShouldBeFalse();
			textBox.Height.ShouldBeGreaterThan(0);
			textBox.Text = "Please summarize this";
			MessageSendingEventArgs? sentArgs = null;
			chatInput.MessageSending += (_, e) => sentArgs = e;

			inputControl.GetType().GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(inputControl, null);

			sentArgs.ShouldNotBeNull();
			var content = sentArgs.Content.ShouldBeOfType<FileAttachmentMessageContent>();
			content.Text.ShouldBe("Please summarize this");
			content.Attachments.ShouldBe([attachment]);
			attachmentInput.PendingAttachments.ShouldBeEmpty();
			textBox.Text.ShouldBeEmpty();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Keeps_Draft_When_Sending_Is_Cancelled(bool useDevExpress)
		{
			using var inputControl = CreateInputControl(useDevExpress);
			var attachmentInput = (IChatAttachmentInputControl)inputControl;
			var chatInput = (IChatInputControl)inputControl;
			var attachment = new ChatFileAttachment("notes.txt", "text/plain", "Hello"u8.ToArray());
			attachmentInput.AddAttachments([attachment]);
			var textBox = useDevExpress
				? (Control)Descendants(inputControl).OfType<MemoEdit>().Single()
				: Descendants(inputControl).OfType<TextBox>().Single();
			textBox.Text = "Keep this draft";
			chatInput.MessageSending += (_, e) => e.Cancel = true;

			inputControl.GetType().GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(inputControl, null);

			attachmentInput.PendingAttachments.ShouldBe([attachment]);
			textBox.Text.ShouldBe("Keep this draft");
		}

		[TestCase(false, false)]
		[TestCase(false, true)]
		[TestCase(true, false)]
		[TestCase(true, true)]
		public async Task Adds_Clipboard_Images_And_Files_To_Draft_Without_Sending(
			bool useDevExpress,
			bool useImage)
		{
			using var chatControl = useDevExpress ? (ChatControl)new DXChatControl() : new ChatControl();
			using var inputControl = CreateInputControl(useDevExpress);
			typeof(ChatControl).GetProperty(nameof(ChatControl.InputControl))!.SetValue(chatControl, inputControl);
			var attachmentInput = (IChatAttachmentInputControl)inputControl;
			chatControl.AttachmentContentProviders.Add(new BinaryAttachmentContentProvider());
			var data = new DataObject();
			var filePath = Path.GetTempFileName();
			using var bitmap = new Bitmap(2, 2);

			try
			{
				if (useImage)
					data.SetData(DataFormats.Bitmap, bitmap);
				else
					data.SetData(DataFormats.FileDrop, new[] { filePath });

				var added = await chatControl.AddClipboardContentToInputAsync(data);

				added.ShouldBe(1);
				attachmentInput.PendingAttachments.Count.ShouldBe(1);
				chatControl.Messages.ShouldAllBe(message => message.Content is ChatNoticeMessageContent);
				attachmentInput.PendingAttachments[0].MediaType.ShouldBe(useImage ? "image/png" : "application/octet-stream");
			}
			finally
			{
				File.Delete(filePath);
			}
		}

		private static Control CreateInputControl(bool useDevExpress)
		{
			return useDevExpress ? new DXChatInputControl() : new ChatInputControl();
		}

		private static byte[] CreatePngData()
		{
			return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
		}

		private static IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var descendant in Descendants(child))
					yield return descendant;
			}
		}

		private static void PerformClick(Control control)
		{
			if (control is Button button)
				button.PerformClick();
			else
				((SimpleButton)control).PerformClick();
		}
	}

	public class Cancellation
	{
		[Test]
		public async Task Cancels_Standalone_Stream_Without_External_Token()
		{
			using var control = new TestChatControl();
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			control.AddStreamingMessage(
				new NamedSender("Assistant"),
				CreateNonCooperativeStream(entered, release, disposed),
				new SynchronizationContext(),
				_ => completed.TrySetResult());

			await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
			control.CancelCurrentOperation();

			await completed.Task.WaitAsync(TimeSpan.FromSeconds(2));
			release.TrySetResult();
			await disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
		}

		[Test]
		public async Task Cancels_Chat_Client_Root_Token_After_Text_Stream_Starts()
		{
			var chatClient = new BlockingChatClient();
			using var control = new TestChatControl
			{
				ServiceProvider = new SingleServiceProvider(chatClient)
			};

			control.SendMessage("Hello").ShouldBeTrue();
			await chatClient.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(2));

			control.CancelCurrentOperation();

			await chatClient.Completed.Task.WaitAsync(TimeSpan.FromSeconds(2));
			chatClient.RequestToken.IsCancellationRequested.ShouldBeTrue();

			// Wait for the send operation to fully unwind so no background continuation is left to
			// resume on the test synchronization context after it is torn down.
			await control.ChatClientSendCompletion.WaitAsync(TimeSpan.FromSeconds(2));
		}
	}

	private static async IAsyncEnumerable<string> CreateNonCooperativeStream(
		TaskCompletionSource entered,
		TaskCompletionSource release,
		TaskCompletionSource disposed)
	{
		try
		{
			yield return "Started";
			entered.TrySetResult();
			await release.Task.ConfigureAwait(false);
		}
		finally
		{
			disposed.TrySetResult();
		}
	}

	private sealed class TestChatControl : ChatControl
	{
		public void AddForConversion(ISender sender, IChatMessageContent content) => AddChatMessage(sender, content);

		public List<Microsoft.Extensions.AI.ChatMessage> ConvertMessages() => ConvertToChatMessagesAsync().GetAwaiter().GetResult();

		public override IChatMessageControl AddStreamingMessage(
			ISender sender,
			IAsyncEnumerable<string> stream,
			SynchronizationContext? synchronizationContext = default,
			Action<string>? completionCallback = default,
			Action<Exception>? exceptionCallback = default,
			CancellationToken cancellationToken = default)
		{
			return base.AddStreamingMessage(
				sender,
				stream,
				synchronizationContext ?? new SynchronizationContext(),
				completionCallback,
				exceptionCallback,
				cancellationToken);
		}
	}

	private sealed class SingleServiceProvider(IChatClient chatClient) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType == typeof(IChatClient) ? chatClient : null;
	}

	private sealed class BlockingChatClient : IChatClient
	{
		public TaskCompletionSource Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public CancellationToken RequestToken { get; private set; }

		public void Dispose() { }

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "Done")));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			RequestToken = cancellationToken;
			try
			{
				yield return new ChatResponseUpdate
				{
					Role = ChatRole.Assistant,
					Contents = [new TextContent("Started")]
				};

				Waiting.TrySetResult();
				await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
			}
			finally
			{
				Completed.TrySetResult();
			}
		}

		public object? GetService(Type serviceType, object? serviceKey = null) => serviceType == typeof(IChatClient) ? this : null;
	}
}
