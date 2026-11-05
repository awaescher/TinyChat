using DevExpress.Spreadsheet;
using DevExpress.XtraRichEdit;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;
using TinyChat.Samples.DocumentAttachments;

namespace Tests;

// Enabled explicitly with -p:TestDocumentAttachmentSamples=true; requires an Office File API license.
[Apartment(ApartmentState.STA)]
public class DocumentAttachmentProviderSampleTests
{
	[Test]
	public async Task Extracts_All_Worksheet_Regions_And_Formula_Expressions()
	{
		using var book = new Workbook();
		book.Worksheets[0].Name = "First";
		book.Worksheets[0].Cells["A1"].Value = "header";
		book.Worksheets[0].Cells["D5"].Value = "separate region";
		book.Worksheets.Add("Second").Cells["A1"].Formula = "=1+2";
		using var stream = new MemoryStream();
		book.SaveDocument(stream, DevExpress.Spreadsheet.DocumentFormat.Xlsx);
		var result = await new SpreadsheetAttachmentContentProvider(includeFormulas: true).ConvertAsync(new ChatFileAttachment("book.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", stream.ToArray()));
		var text = result.Contents.OfType<TextContent>().Single().Text;
		text.ShouldContain("First");
		text.ShouldContain("Second");
		text.ShouldContain("separate region");
		text.ShouldContain("formula: =1+2");
	}

	[Test]
	public async Task Extracts_Word_And_Pdf_Text_And_Marks_Truncation()
	{
		using var document = new RichEditDocumentServer();
		document.Document.AppendText("Attachment provider test text");
		using var word = new MemoryStream();
		document.SaveDocument(word, DevExpress.XtraRichEdit.DocumentFormat.OpenXml);
		var result = await new WordAttachmentContentProvider().ConvertAsync(new ChatFileAttachment("letter.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", word.ToArray()));
		result.Contents.OfType<TextContent>().Single().Text.ShouldContain("Attachment provider test text");
		var limited = await new WordAttachmentContentProvider(10).ConvertAsync(new ChatFileAttachment("letter.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", word.ToArray()));
		limited.Contents.OfType<TextContent>().Single().Text.ShouldContain("Document truncated");
		using var pdf = new MemoryStream();
		document.ExportToPdf(pdf);
		var pdfResult = await new PdfAttachmentContentProvider().ConvertAsync(new ChatFileAttachment("letter.pdf", "application/pdf", pdf.ToArray()));
		pdfResult.Contents.OfType<TextContent>().Single().Text.ShouldContain("Attachment provider test text");
		pdfResult.Contents.OfType<TextContent>().Single().Text.ShouldContain("Page 1:");
	}

	[Test]
	public async Task Rejects_Cancelled_Conversions()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var attachment = new ChatFileAttachment("document.pdf", "application/pdf", ReadOnlyMemory<byte>.Empty);
		await Should.ThrowAsync<OperationCanceledException>(async () => await new PdfAttachmentContentProvider().ConvertAsync(attachment, cancellation.Token));
	}

	[Test]
	public async Task Reports_Textless_Pdf_And_Invalid_Documents()
	{
		using var pdf = new DevExpress.Pdf.PdfDocumentProcessor();
		pdf.CreateEmptyDocument();
		pdf.AddNewPage(new DevExpress.Pdf.PdfRectangle(0, 0, 200, 200));
		using var stream = new MemoryStream();
		pdf.SaveDocument(stream);
		var error = await Should.ThrowAsync<AttachmentContentException>(async () => await new PdfAttachmentContentProvider().ConvertAsync(new ChatFileAttachment("scan.pdf", "application/pdf", stream.ToArray())));
		error.Notice.Code.ShouldBe(ChatNoticeCode.NoExtractableText);
		await Should.ThrowAsync<Exception>(async () => await new SpreadsheetAttachmentContentProvider().ConvertAsync(new ChatFileAttachment("bad.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "not a workbook"u8.ToArray())));
	}

	[Test]
	public async Task Extracts_Excel_Before_The_Binary_Fallback_And_Reports_Truncation_Separately()
	{
		using var workbook = new Workbook();
		workbook.Worksheets[0].Cells["A1"].Value = new string('x', 200);
		using var stream = new MemoryStream();
		workbook.SaveDocument(stream, DevExpress.Spreadsheet.DocumentFormat.Xlsx);
		var attachment = new ChatFileAttachment("book.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", stream.ToArray());
		var converted = await new SpreadsheetAttachmentContentProvider(50).ConvertAsync(attachment);
		converted.Notices.Single().Code.ShouldBe(ChatNoticeCode.DocumentTruncated);
		converted.Notices.Single().Limit.ShouldBe(50);
		using var chat = new ConversionDxChatControl();
		chat.AddForConversion(new FileAttachmentMessageContent([attachment]));
		var messages = await chat.ConvertAsync();
		messages.Single().Contents.OfType<DataContent>().ShouldBeEmpty();
		messages.Single().Text.ShouldContain(new string('x', 200));
		chat.Messages.ShouldNotContain(message => message.Content is ChatNoticeMessageContent);
	}

	private sealed class ConversionDxChatControl : DXChatControl
	{
		public ConversionDxChatControl()
		{
			AttachmentContentProviders.Insert(0, new SpreadsheetAttachmentContentProvider());
		}

		public void AddForConversion(IChatMessageContent content) => AddChatMessage(Sender, content);
		public Task<List<Microsoft.Extensions.AI.ChatMessage>> ConvertAsync() => ConvertToChatMessagesAsync();
	}

}
