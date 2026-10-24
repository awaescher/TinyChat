using DevExpress.Utils.Html;
using DevExpress.Utils.Html.Internal;
using DevExpress.XtraEditors;
using Shouldly;
using TinyChat;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class DevExpressQuoteRenderingTests
{
	[Test]
	public void Displays_Literal_And_Encoded_Quotation_Marks_In_The_Html_Dom()
	{
		const string source = "The paper &quot;Attention&quot;, \"literal\", &#34;numeric&#34;, “typographic”, „deutsch“, and &#39;apostrophe&#39; is quoted &amp; escaped.";
		const string expected = "The paper \"Attention\", \"literal\", \"numeric\", “typographic”, „deutsch“, and 'apostrophe' is quoted & escaped.";
		using var control = new DXChatMessageControl
		{
			MessageFormatter = new SimplifiedHtmlMessageFormatter("b"),
			Message = new ChatMessage(
				new NamedSender("Assistant"),
				new StringMessageContent(source))
		};
		control.CreateControl();
		var editor = control.Controls.OfType<HtmlContentControl>().Single();
		_ = editor.GetContentSize();
		var contentElement = editor.FindElementById("message-content");
		contentElement.ShouldNotBeNull();

		var renderedText = string.Concat(
			DescendantHtmlNodes(contentElement)
				.OfType<DxHtmlTextElementBase>()
				.Select(element => element.Text));

		editor.Text.ShouldBe(expected);
		renderedText.ShouldBe(expected);
	}

	[Test]
	public void Displays_Quotes_Inside_Formatting_Without_Interpreting_Sensitive_Text_As_Html()
	{
		const string source = "**The &quot;Transformer&quot;** costs $5 &amp; shows &lt;script&gt;alert(&quot;x&quot;)&lt;/script&gt; literally.";
		const string expected = "The \"Transformer\" costs $5 & shows <script>alert(\"x\")</script> literally.";
		using var control = new DXChatMessageControl
		{
			MessageFormatter = new SimplifiedHtmlMessageFormatter("b"),
			Message = new ChatMessage(
				new NamedSender("Assistant"),
				new StringMessageContent(source))
		};
		control.CreateControl();
		var editor = control.Controls.OfType<HtmlContentControl>().Single();
		_ = editor.GetContentSize();
		var contentElement = editor.FindElementById("message-content");
		contentElement.ShouldNotBeNull();

		var renderedText = string.Concat(
			DescendantHtmlNodes(contentElement)
				.OfType<DxHtmlTextElementBase>()
				.Select(element => element.Text));
		var renderedBoldText = string.Concat(
			editor.FindElementsByTag("b")
				.SelectMany(DescendantHtmlNodes)
				.OfType<DxHtmlTextElementBase>()
				.Select(element => element.Text));

		editor.Text.ShouldBe(expected);
		renderedText.ShouldBe(expected);
		renderedBoldText.ShouldBe("The \"Transformer\"");
		editor.FindElementsByTag("script").ShouldBeEmpty();
		editor.HtmlTemplate.Template.ShouldNotContain("<script>");
		editor.HtmlTemplate.Template.ShouldNotContain("$5");
	}

	private static IEnumerable<DxHtmlNode> DescendantHtmlNodes(DxHtmlNode parent)
	{
		foreach (DxHtmlNode child in parent.ChildNodes)
		{
			yield return child;

			foreach (var descendant in DescendantHtmlNodes(child))
				yield return descendant;
		}
	}
}
