using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class AttachmentContentProviderTests
{
	[Test]
	public void Uses_The_Same_Default_Providers_In_Both_Suites_And_Can_Reject_Binary_Files()
	{
		using var plain = new ChatControl();
		using var dx = new DXChatControl();
		dx.AttachmentContentProviders.Select(provider => provider.GetType()).ShouldBe(plain.AttachmentContentProviders.Select(provider => provider.GetType()));
		plain.CanAttachFile("notes.md").ShouldBeTrue();
		plain.CanAttachFile("book.xlsx").ShouldBeTrue();
		dx.CanAttachFile("BOOK.XLSX").ShouldBeTrue();
		dx.CanAttachFile("letter.docx").ShouldBeTrue();
		dx.CanAttachFile("report.pdf").ShouldBeTrue();
		dx.CanAttachFile("program.exe").ShouldBeTrue();
		dx.AttachmentContentProviders.Remove(dx.AttachmentContentProviders.OfType<BinaryAttachmentContentProvider>().Single());
		dx.CanAttachFile("program.exe").ShouldBeFalse();
		dx.CanAttachFile("report.pdf").ShouldBeFalse();
		dx.CanAttachFile("book.xlsx").ShouldBeFalse();
		dx.SupportedAttachmentFileExtensions.ShouldNotContain(".pdf");
		dx.AttachmentContentProviders.Add(new BinaryAttachmentContentProvider());
		dx.CanAttachFile("program.exe").ShouldBeTrue();
	}

	[Test]
	public async Task Keeps_Valid_Files_When_Another_File_Is_Rejected()
	{
		using var chat = new ChatControl();
		chat.AttachmentContentProviders.Remove(chat.AttachmentContentProviders.OfType<BinaryAttachmentContentProvider>().Single());
		using var input = new ChatInputControl();
		typeof(ChatControl).GetProperty(nameof(ChatControl.InputControl))!.SetValue(chat, input);
		var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var valid = Path.Combine(directory, "notes.txt");
			var invalid = Path.Combine(directory, "unknown.xyz");
			await File.WriteAllTextAsync(valid, "hello");
			await File.WriteAllTextAsync(invalid, "unsupported");
			var rejections = new List<string>();
			chat.AttachmentRejected += (_, e) => { rejections.Add(e.FileName); e.Handled = true; };
			(await chat.AddFilesToInputAsync([invalid, valid])).ShouldBe(1);
			rejections.ShouldBe(["unknown.xyz"]);
			((IChatAttachmentInputControl)chat.InputControl!).PendingAttachments.Single().Name.ShouldBe("notes.txt");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public async Task Uses_First_Provider_Caches_Success_And_Reconverts_After_Replacement()
	{
		using var chat = new ConversionChatControl();
		var attachment = new ChatFileAttachment("note.txt", "text/plain", "original"u8.ToArray());
		var provider = new CountingProvider("custom");
		chat.AttachmentContentProviders.Insert(0, provider);
		chat.AddForConversion(new FileAttachmentMessageContent([attachment]));
		(await chat.ConvertAsync()).Single().Text.ShouldBe("custom");
		(await chat.ConvertAsync()).Single().Text.ShouldBe("custom");
		provider.Calls.ShouldBe(1);
		chat.AttachmentContentProviders[0] = new CountingProvider("replacement");
		(await chat.ConvertAsync()).Single().Text.ShouldBe("replacement");
	}

	[Test]
	public void Restores_Structured_Tool_Results_From_Json_Without_Exposing_Base64_As_Text()
	{
		var image = CreateImage();
		AIContent[] contents = [new TextContent("screenshot"), image];
		var json = JsonSerializer.SerializeToElement(contents, new JsonSerializerOptions(JsonSerializerDefaults.Web));
		var restored = AIMessageContent.FromResult(json).ShouldNotBeNull();
		restored.Text.ShouldBe("screenshot");
		restored.Contents.OfType<DataContent>().Single().Data.ToArray().ShouldBe(image.Data.ToArray());
		AIMessageContent.FromResult(json.GetRawText()).ShouldNotBeNull().Contents.OfType<DataContent>().Count().ShouldBe(1);
		AIMessageContent.FromResult("{\"ordinary\":\"object\"}").ShouldBeNull();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Preserves_Image_History_And_Renders_The_Preview(bool devExpress)
	{
		using var chat = new ConversionChatControl();
		var content = new AIMessageContent([new TextContent("image caption"), CreateImage()]);
		chat.AddForConversion(content);
		var message = (await chat.ConvertAsync()).Single();
		message.Contents.OfType<DataContent>().Count().ShouldBe(1);
		using var control = devExpress
			? (System.Windows.Forms.Control)new DXChatMessageControl { MessageFormatter = new PlainTextMessageFormatter() }
			: new ChatMessageControl { MessageFormatter = new PlainTextMessageFormatter() };
		((IChatMessageControl)control).Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), content);
		control.Controls.OfType<FileAttachmentPreviewPanel>().Single().Controls.Count.ShouldBe(1);
		control.Controls.OfType<FileAttachmentPreviewPanel>().Single().Controls[0].AccessibleName.ShouldBe("screenshot.png");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Renders_Images_Inside_Tool_Results(bool devExpress)
	{
		using var chat = devExpress ? (ChatControl)new DXChatControl() : new ChatControl();
		var result = new FunctionCallMessageContent("call", "screenshot", null, new AIContent[] { CreateImage() });
		var control = (System.Windows.Forms.Control)chat.AddMessage(new NamedSender("Assistant"), result);
		var preview = FindPreviews(control).Single();
		preview.Visible.ShouldBeFalse();
		control.GetType().GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, [null, EventArgs.Empty]);
		preview.Visible.ShouldBeTrue();
		control.GetType().GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, [null, EventArgs.Empty]);
		preview.Visible.ShouldBeFalse();
	}

	[Test]
	public async Task Loads_Images_And_Tool_Results_From_Chat_History_Without_Resending_Reasoning()
	{
		using var chat = new ConversionChatControl();
		var image = CreateImage();
		chat.LoadChatHistory([
			new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, [new TextContent("inspect"), image]),
			new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, [new TextReasoningContent("thinking"), new FunctionCallContent("call", "capture")]),
			new Microsoft.Extensions.AI.ChatMessage(ChatRole.Tool, [new FunctionResultContent("call", new AIContent[] { image })])
		]);
		var converted = await chat.ConvertAsync();
		converted[0].Contents.OfType<DataContent>().Single().Data.ToArray().ShouldBe(image.Data.ToArray());
		converted.SelectMany(message => message.Contents).OfType<TextReasoningContent>().ShouldBeEmpty();
		converted.SelectMany(message => message.Contents).OfType<FunctionCallContent>().Single().Name.ShouldBe("capture");
		var toolResult = converted.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Single();
		AIMessageContent.FromResult(toolResult.Result).ShouldNotBeNull().Contents.OfType<DataContent>().Count().ShouldBe(1);
	}

	[Test]
	public async Task Displays_Images_From_Streaming_Responses()
	{
		using var chat = new ChatControl();
		static async IAsyncEnumerable<ChatResponseUpdate> Updates()
		{
			yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [CreateImage()] };
			await Task.CompletedTask;
		}
		var method = typeof(ChatControl).GetMethod("HandleStreamingResponseAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
		await (Task)method.Invoke(chat, [new NamedSender("Assistant"), Updates(), new List<FunctionCallMessageContent>(), CancellationToken.None])!;
		chat.Messages.Single().Content.ShouldBeOfType<AIMessageContent>().Contents.OfType<DataContent>().Count().ShouldBe(1);
	}

	[Test]
	public void Displays_Images_From_NonStreaming_Responses()
	{
		using var chat = new ChatControl();
		var response = new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, [CreateImage()]));
		typeof(ChatControl).GetMethod("HandleNonStreamingResponse", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(chat, [new NamedSender("Assistant"), response, new List<FunctionCallMessageContent>()]);
		chat.Messages.Single().Content.ShouldBeOfType<AIMessageContent>().Contents.OfType<DataContent>().Count().ShouldBe(1);
	}

	private static IEnumerable<FileAttachmentPreviewPanel> FindPreviews(System.Windows.Forms.Control control)
	{
		foreach (System.Windows.Forms.Control child in control.Controls)
		{
			if (child is FileAttachmentPreviewPanel preview)
				yield return preview;
			foreach (var nested in FindPreviews(child))
				yield return nested;
		}
	}

	private static DataContent CreateImage()
	{
		using var bitmap = new Bitmap(2, 2);
		using var stream = new MemoryStream();
		bitmap.Save(stream, ImageFormat.Png);
		return new DataContent(stream.ToArray(), "image/png") { Name = "screenshot.png" };
	}

	private sealed class ConversionChatControl : ChatControl
	{
		public void AddForConversion(IChatMessageContent content) => AddChatMessage(Sender, content);
		public Task<List<Microsoft.Extensions.AI.ChatMessage>> ConvertAsync() => ConvertToChatMessagesAsync();
	}

	private sealed class CountingProvider(string text) : IAttachmentContentProvider
	{
		public int Calls { get; private set; }
		public IReadOnlyList<string> FileExtensions { get; } = [".txt"];
		public bool CanHandle(string fileName, string mediaType) => mediaType == "text/plain";
		public ValueTask<AttachmentConversionResult> ConvertAsync(ChatFileAttachment attachment, CancellationToken cancellationToken = default)
		{
			Calls++;
			return ValueTask.FromResult(new AttachmentConversionResult([new TextContent(text)]));
		}
	}
}
