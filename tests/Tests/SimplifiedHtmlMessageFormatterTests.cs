using Shouldly;
using TinyChat;
using TinyChat.Messages.Formatting;

namespace Tests;

public class SimplifiedHtmlMessageFormatterTests
{
	public class FormatMethod : SimplifiedHtmlMessageFormatterTests
	{
		[Test]
		public void Returns_Plain_Text_When_No_Tags_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter();
			var result = formatter.Format("Hello, **world**!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Returns_Html_Text_From_Simple_String()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Hello, world!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Converts_Markdown_Bold_To_Html_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Hello, **world**!");
			result.ShouldBe("Hello, <b>world</b>!");
		}

		[Test]
		public void Converts_Markdown_Bold_Underscore_To_Html_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Hello, __world__!");
			result.ShouldBe("Hello, <b>world</b>!");
		}

		[Test]
		public void Strips_Markdown_Bold_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("i");
			var result = formatter.Format("Hello, **world**!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Converts_Markdown_Italic_To_Html_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("i");
			var result = formatter.Format("Hello, *world*!");
			result.ShouldBe("Hello, <i>world</i>!");
		}

		[Test]
		public void Converts_Markdown_Italic_Underscore_To_Html_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("i");
			var result = formatter.Format("Hello, _world_!");
			result.ShouldBe("Hello, <i>world</i>!");
		}

		[Test]
		public void Strips_Markdown_Italic_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Hello, *world*!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Converts_Markdown_Strikethrough_To_Html_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("s");
			var result = formatter.Format("Hello, ~~world~~!");
			result.ShouldBe("Hello, <s>world</s>!");
		}

		[Test]
		public void Strips_Markdown_Strikethrough_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Hello, ~~world~~!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Converts_Markdown_Link_To_Html_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("a");
			var result = formatter.Format("This is a [link](http://example.com)");
			result.ShouldBe("This is a <a href=\"http://example.com\">link</a>");
		}

		[Test]
		public void Strips_Markdown_Link_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("This is a [link](http://example.com)");
			result.ShouldBe("This is a link");
		}

		[Test]
		public void Converts_Markdown_Image_To_Html_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("img");
			var result = formatter.Format("An image: ![alt text](image.jpg)");
			result.ShouldBe("An image: <img src=\"image.jpg\" alt=\"alt text\">");
		}

		[Test]
		public void Strips_Markdown_Image_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("An image: ![alt text](image.jpg)");
			result.ShouldBe("An image: alt text");
		}

		[Test]
		public void Converts_Markdown_H1_To_Html_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("h1");
			var result = formatter.Format("# Header 1");
			result.ShouldBe("<h1>Header 1</h1>");
		}

		[Test]
		public void Converts_Multiple_Headers_When_All_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("h1", "h2", "h3");
			var result = formatter.Format("# Header 1\n## Header 2\n### Header 3");
			result.ShouldBe("<h1>Header 1</h1>\n<h2>Header 2</h2>\n<h3>Header 3</h3>");
		}

		[Test]
		public void Strips_Headers_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("# Header 1\n## Header 2");
			result.ShouldBe("<b>Header 1</b>\n<b>Header 2</b>");
		}

		[Test]
		public void Converts_Some_Headers_And_Strips_Others()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("h1", "h3");
			var result = formatter.Format("# Header 1\n## Header 2\n### Header 3");
			result.ShouldBe("<h1>Header 1</h1>\nHeader 2\n<h3>Header 3</h3>");
		}

		[Test]
		public void Keeps_Supported_Html_Tags()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "i");
			var result = formatter.Format("<b>Bold</b> and <i>italic</i>");
			result.ShouldBe("<b>Bold</b> and <i>italic</i>");
		}

		[Test]
		public void Strips_Unsupported_Html_Tags()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<b>Bold</b> and <i>italic</i>");
			result.ShouldBe("<b>Bold</b> and italic");
		}

		[Test]
		public void Normalizes_Strong_To_B_When_B_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<strong>Bold</strong>");
			result.ShouldBe("<b>Bold</b>");
		}

		[Test]
		public void Normalizes_Em_To_I_When_I_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("i");
			var result = formatter.Format("<em>Italic</em>");
			result.ShouldBe("<i>Italic</i>");
		}

		[Test]
		public void Normalizes_Del_To_S_When_S_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("s");
			var result = formatter.Format("<del>Deleted</del>");
			result.ShouldBe("<s>Deleted</s>");
		}

		[Test]
		public void Normalizes_Strike_To_S_When_S_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("s");
			var result = formatter.Format("<strike>Struck</strike>");
			result.ShouldBe("<s>Struck</s>");
		}

		[Test]
		public void Converts_Mixed_Markdown_And_Html()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("**Markdown** and <b>HTML</b> bold");
			result.ShouldBe("<b>Markdown</b> and <b>HTML</b> bold");
		}

		[Test]
		public void Keeps_Nested_Html_Tags_When_All_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "i");
			var result = formatter.Format("<b>Bold with <i>nested italic</i></b>");
			result.ShouldBe("<b>Bold with <i>nested italic</i></b>");
		}

		[Test]
		public void Strips_Nested_Unsupported_Html_Tags()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<b>Bold with <i>nested italic</i></b>");
			result.ShouldBe("<b>Bold with nested italic</b>");
		}

		[Test]
		public void Strips_All_Tags_When_None_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter();
			var result = formatter.Format("<div><p><span>Nested</span> content</p></div>");
			result.ShouldBe("Nested content");
		}

		[Test]
		public void Returns_Empty_String_For_Empty_Input()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("");
			result.ShouldBe("");
		}

		[Test]
		public void Returns_Empty_String_For_Null_Input()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format((string)null!);
			result.ShouldBe("");
		}

		[Test]
		public void Returns_Empty_String_For_Whitespace_Only_Input()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("   \n\t   ");
			result.ShouldBe("");
		}

		[Test]
		public void Converts_Bold_Within_Italic_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "i");
			var result = formatter.Format("*This is **bold** within italic*");
			result.ShouldBe("<i>This is <b>bold</b> within italic</i>");
		}

		[Test]
		public void Strips_Bold_Within_Italic_When_Only_Italic_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("i");
			var result = formatter.Format("*This is **bold** within italic*");
			result.ShouldBe("<i>This is bold within italic</i>");
		}

		[Test]
		public void Converts_Italic_Within_Bold_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "i");
			var result = formatter.Format("**This is *italic* within bold**");
			result.ShouldBe("<b>This is <i>italic</i> within bold</b>");
		}

		[Test]
		public void Strips_Italic_Within_Bold_When_Only_Bold_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("**This is *italic* within bold**");
			result.ShouldBe("<b>This is italic within bold</b>");
		}

		[Test]
		public void Converts_Link_With_Bold_Text_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("a", "b");
			var result = formatter.Format("[**bold link**](http://example.com)");
			result.ShouldBe("<a href=\"http://example.com\"><b>bold link</b></a>");
		}

		[Test]
		public void Strips_Bold_In_Link_When_Only_Link_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("a");
			var result = formatter.Format("[**bold link**](http://example.com)");
			result.ShouldBe("<a href=\"http://example.com\">bold link</a>");
		}

		[Test]
		public void Keeps_Bold_But_Strips_Link_When_Only_Bold_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("[**bold link**](http://example.com)");
			result.ShouldBe("<b>bold link</b>");
		}

		[Test]
		public void Converts_Complex_Mixed_Formatting()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "a", "img");
			var result = formatter.Format("Hello, **world**! This is a [link](http://example.com) and an image: ![alt text](image.jpg)");
			result.ShouldBe("Hello, <b>world</b>! This is a <a href=\"http://example.com\">link</a> and an image: <img src=\"image.jpg\" alt=\"alt text\">");
		}

		[Test]
		public void Converts_Real_World_Chat_Message_With_Emoji()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "a");
			var result = formatter.Format("Hey! Check out this **awesome** [link](https://github.com) and let me know what you think! 😊");
			result.ShouldBe("Hey! Check out this <b>awesome</b> <a href=\"https://github.com\">link</a> and let me know what you think! 😊");
		}

		[Test]
		public void Converts_Unordered_List_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("ul", "li");
			var result = formatter.Format("- Item 1\n- Item 2\n- Item 3");
			result.ShouldBe("<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>");
		}

		[Test]
		public void Strips_Unordered_List_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("- Item 1\n- Item 2");
			result.ShouldBe("- Item 1\n- Item 2");
		}

		[Test]
		public void Requires_Both_Ul_And_Li_For_Lists()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("ul");
			var result = formatter.Format("- Item 1\n- Item 2");
			result.ShouldBe("- Item 1\n- Item 2");
		}

		[Test]
		public void Converts_Ordered_List_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("ol", "li");
			var result = formatter.Format("1. First\n2. Second\n3. Third");
			result.ShouldBe("<ol><li>First</li><li>Second</li><li>Third</li></ol>");
		}

		[Test]
		public void Strips_Ordered_List_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("1. First\n2. Second");
			result.ShouldBe("1. First\n2. Second");
		}

		[Test]
		public void Converts_List_Items_With_Inline_Formatting()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("ul", "li", "b");
			var result = formatter.Format("- **Bold** item\n- Normal item");
			result.ShouldBe("<ul><li><b>Bold</b> item</li><li>Normal item</li></ul>");
		}

		[Test]
		public void Converts_Blockquote_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("blockquote");
			var result = formatter.Format("> This is a quote");
			result.ShouldBe("<blockquote>This is a quote</blockquote>");
		}

		[Test]
		public void Strips_Blockquote_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("> This is a quote");
			result.ShouldBe("> This is a quote");
		}

		[Test]
		public void Converts_Blockquote_With_Inline_Formatting()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("blockquote", "b");
			var result = formatter.Format("> This is **bold** quote");
			result.ShouldBe("<blockquote>This is <b>bold</b> quote</blockquote>");
		}

		[Test]
		public void Converts_All_Six_Header_Levels()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("h1", "h2", "h3", "h4", "h5", "h6");
			var result = formatter.Format("# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6");
			result.ShouldBe("<h1>H1</h1>\n<h2>H2</h2>\n<h3>H3</h3>\n<h4>H4</h4>\n<h5>H5</h5>\n<h6>H6</h6>");
		}

		[Test]
		public void Converts_Headers_With_Inline_Formatting()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("h1", "b");
			var result = formatter.Format("# Header with **bold** text");
			result.ShouldBe("<h1>Header with <b>bold</b> text</h1>");
		}

		[Test]
		public void Converts_Multiple_Links_In_Same_Line()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("a");
			var result = formatter.Format("[link1](url1) and [link2](url2)");
			result.ShouldBe("<a href=\"url1\">link1</a> and <a href=\"url2\">link2</a>");
		}

		[Test]
		public void Converts_Multiple_Images_In_Same_Line()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("img");
			var result = formatter.Format("![img1](url1) and ![img2](url2)");
			result.ShouldBe("<img src=\"url1\" alt=\"img1\"> and <img src=\"url2\" alt=\"img2\">");
		}

		[Test]
		public void Preserves_Line_Breaks_In_Plain_Text()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Line 1\nLine 2\nLine 3");
			result.ShouldBe("Line 1\nLine 2\nLine 3");
		}

		[Test]
		public void Encodes_Html_Entities_In_Link_Urls()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("a");
			var result = formatter.Format("[link](http://example.com?a=1&b=2)");
			result.ShouldBe("<a href=\"http://example.com?a=1&amp;b=2\">link</a>");
		}

		[Test]
		public void Encodes_Html_Entities_In_Image_Attributes()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("img");
			var result = formatter.Format("![alt & text](image.jpg?a=1&b=2)");
			result.ShouldBe("<img src=\"image.jpg?a=1&amp;b=2\" alt=\"alt &amp; text\">");
		}

		[Test]
		public void Converts_Mixed_Underscores_And_Asterisks_Consistently()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "i");
			var result = formatter.Format("_italic_ and *also italic* and __bold__ and **also bold**");
			result.ShouldBe("<i>italic</i> and <i>also italic</i> and <b>bold</b> and <b>also bold</b>");
		}

		[Test]
		public void Throws_On_Unsupported_Content_Type()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var content = new ChangingMessageContent(null);
			Should.Throw<NotSupportedException>(() => formatter.Format(content));
		}

		[Test]
		public void Formats_StringMessageContent()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var content = new StringMessageContent("**Hello**");
			var result = formatter.Format(content);
			result.ShouldBe("<b>Hello</b>");
		}

		[Test]
		public void Formats_FunctionCallMessageContent()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var content = new FunctionCallMessageContent("call1", "get_weather", new Dictionary<string, object?> { ["location"] = "Paris" });
			var result = formatter.Format(content);
			result.ShouldBe("[Calling: get_weather(location: Paris)]");
		}

		[Test]
		public void Formats_FunctionCallMessageContent_Without_Arguments()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var content = new FunctionCallMessageContent("call1", "get_time", null);
			var result = formatter.Format(content);
			result.ShouldBe("[Calling: get_time()]");
		}

		[Test]
		public void Formats_FunctionCallMessageContent_With_Result()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var content = new FunctionCallMessageContent("call1", "get_weather", new Dictionary<string, object?> { ["city"] = "Amsterdam" }, result: "6°C");
			var result = formatter.Format(content);
			result.ShouldBe("{Tool: get_weather(city: Amsterdam) = 6°C}");
		}

		[Test]
		public void Formats_FunctionResultMessageContent()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var content = new FunctionResultMessageContent("call1", "15°C, cloudy");
			var result = formatter.Format(content);
			result.ShouldBe("[Result: 15°C, cloudy]");
		}

		[Test]
		public void Formats_FunctionResultMessageContent_With_Null_Result()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var content = new FunctionResultMessageContent("call1", null);
			var result = formatter.Format(content);
			result.ShouldBe("[Result: ]");
		}

		[Test]
		public void Preserves_Empty_Markdown_Link_As_Is()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("a");
			var result = formatter.Format("[](http://example.com)");
			result.ShouldBe("[](http://example.com)");
		}

		[Test]
		public void Converts_Markdown_Image_With_Empty_Alt_Text()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("img");
			var result = formatter.Format("![](image.jpg)");
			result.ShouldBe("<img src=\"image.jpg\" alt=\"\">");
		}

		[Test]
		public void Strips_Additional_Link_Attributes_For_Security()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("a");
			var result = formatter.Format("<a href='url' class='link' data-id='123'>Link</a>");
			result.ShouldBe("<a href=\"url\">Link</a>");
		}

		[Test]
		public void Keeps_Self_Closing_Html_Tags_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("br");
			var result = formatter.Format("Line<br/>break");
			result.ShouldBe("Line<br/>break");
		}

		[Test]
		public void Strips_Self_Closing_Html_Tags_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Line<br/>break");
			result.ShouldBe("Linebreak");
		}

		[Test]
		public void Formats_Inline_Code_With_Font_When_Font_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("Use `console.log()` to debug");
			result.ShouldBe("Use <font=\"Consolas\">console.log()</font> to debug");
		}

		[Test]
		public void Formats_Inline_Code_Plain_When_Font_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Use `console.log()` to debug");
			result.ShouldBe("Use console.log() to debug");
		}

		[Test]
		public void Formats_Code_Block_With_Font_And_Line_Breaks_When_Font_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("Before\n```\ncode here\n```\nAfter");
			result.ShouldBe("Before\n\n<font=\"Consolas\">code here</font>\n\nAfter");
		}

		[Test]
		public void Formats_Code_Block_Plain_When_Font_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("Before\n```\ncode here\n```\nAfter");
			result.ShouldBe("Before\n\ncode here\n\nAfter");
		}

		[Test]
		public void Formats_Code_Block_With_Language_Using_Font()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("```csharp\nvar x = 1;\n```");
			result.ShouldBe("<font=\"Consolas\">var x = 1;</font>");
		}

		[Test]
		public void Encodes_Html_Entities_In_Inline_Code()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("`<div>` tag");
			result.ShouldBe("<font=\"Consolas\">&lt;div&gt;</font> tag");
		}

		[Test]
		public void Formats_Code_Block_With_Multiple_Lines_Using_Font()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("```\nvar x = 1;\nvar y = 2;\nvar z = 3;\n```");
			result.ShouldBe("<font=\"Consolas\">var x = 1;\nvar y = 2;\nvar z = 3;</font>");
		}

		[Test]
		public void Formats_Multiple_Code_Blocks_With_Font()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("```\nblock1\n```\ntext\n```\nblock2\n```");
			result.ShouldBe("<font=\"Consolas\">block1</font>\n\ntext\n\n<font=\"Consolas\">block2</font>");
		}

		[Test]
		public void Escapes_Potential_Xss_In_Inline_Code()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("`<script>alert('xss')</script>`");
			result.ShouldBe("<font=\"Consolas\">&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;</font>");
		}

		[Test]
		public void Formats_Inline_Code_With_Custom_Font()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font") { DefaultCodeFontName = "Courier New" };
			var result = formatter.Format("`code`");
			result.ShouldBe("<font=\"Courier New\">code</font>");
		}

		[Test]
		public void Encodes_Font_Name_In_Face_Attribute()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font") { DefaultCodeFontName = "Font\"Name" };
			var result = formatter.Format("`code`");
			result.ShouldBe("<font=\"Font&quot;Name\">code</font>");
		}

		[Test]
		public void Converts_Html_Color_Span_To_DevExpress_Format_When_Color_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("color");
			var result = formatter.Format("<span style='color:red'>Red text</span>");
			result.ShouldBe("<color=red>Red text</color>");
		}

		[Test]
		public void Converts_Html_BackColor_Span_To_DevExpress_Format_When_BackColor_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("backcolor");
			var result = formatter.Format("<span style='background-color:blue'>Blue bg</span>");
			result.ShouldBe("<backcolor=blue>Blue bg</backcolor>");
		}

		[Test]
		public void Converts_Html_Color_Span_With_Hex_To_DevExpress_Format()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("color");
			var result = formatter.Format("<span style='color:#FF0000'>Red text</span>");
			result.ShouldBe("<color=#FF0000>Red text</color>");
		}

		[Test]
		public void Converts_Html_Color_Span_With_Rgb_To_DevExpress_Format()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("color");
			var result = formatter.Format("<span style='color:rgb(255,0,0)'>Red text</span>");
			result.ShouldBe("<color=255,0,0>Red text</color>");
		}

		[Test]
		public void Keeps_DevExpress_Color_Tags_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("color");
			var result = formatter.Format("<color=red>Red text</color>");
			result.ShouldBe("<color=red>Red text</color>");
		}

		[Test]
		public void Keeps_DevExpress_Color_Tags_With_Hex_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("color");
			var result = formatter.Format("<color=#0000FF>Blue text</color>");
			result.ShouldBe("<color=#0000FF>Blue text</color>");
		}

		[Test]
		public void Keeps_DevExpress_Color_Tags_With_Rgb_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("color");
			var result = formatter.Format("<color=255,0,0>Red text</color>");
			result.ShouldBe("<color=255,0,0>Red text</color>");
		}

		[Test]
		public void Keeps_DevExpress_BackColor_Tags_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("backcolor");
			var result = formatter.Format("<backcolor=yellow>Yellow bg</backcolor>");
			result.ShouldBe("<backcolor=yellow>Yellow bg</backcolor>");
		}

		[Test]
		public void Strips_Color_Tags_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<color=red>Red text</color>");
			result.ShouldBe("Red text");
		}

		[Test]
		public void Converts_Markdown_H1_To_Size_And_Bold_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("# Header 1");
			result.ShouldBe("<size=+5><b>Header 1</b></size>");
		}

		[Test]
		public void Converts_Markdown_H2_To_Size_And_Bold_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("## Header 2");
			result.ShouldBe("<size=+4><b>Header 2</b></size>");
		}

		[Test]
		public void Converts_Markdown_H3_To_Size_And_Bold_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("### Header 3");
			result.ShouldBe("<size=+3><b>Header 3</b></size>");
		}

		[Test]
		public void Converts_Markdown_H4_To_Size_And_Bold_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("#### Header 4");
			result.ShouldBe("<size=+2><b>Header 4</b></size>");
		}

		[Test]
		public void Converts_Markdown_H5_To_Size_And_Bold_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("##### Header 5");
			result.ShouldBe("<size=+1><b>Header 5</b></size>");
		}

		[Test]
		public void Converts_Markdown_H6_To_Bold_Only_When_Both_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("###### Header 6");
			result.ShouldBe("<b>Header 6</b>");
		}

		[Test]
		public void Converts_Markdown_H1_To_Bold_Only_When_Size_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("# Header 1");
			result.ShouldBe("<b>Header 1</b>");
		}

		[Test]
		public void Converts_Markdown_H1_To_Size_Only_When_Bold_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("size");
			var result = formatter.Format("# Header 1");
			result.ShouldBe("<size=+5>Header 1</size>");
		}

		[Test]
		public void Strips_Markdown_Headers_When_Neither_Bold_Nor_Size_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("i");
			var result = formatter.Format("# Header 1\n## Header 2");
			result.ShouldBe("Header 1\nHeader 2");
		}

		[Test]
		public void Keeps_Size_Tags_With_Value_When_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("size");
			var result = formatter.Format("<size=+3>Big text</size>");
			result.ShouldBe("<size=+3>Big text</size>");
		}

		[Test]
		public void Strips_Size_Tags_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<size=+3>Big text</size>");
			result.ShouldBe("Big text");
		}

		[Test]
		public void Converts_Html_H1_To_Size_And_Bold_When_H1_Not_Supported_But_Size_And_Bold_Are()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("<h1>Header 1</h1>");
			result.ShouldBe("<size=+5><b>Header 1</b></size>");
		}

		[Test]
		public void Converts_Html_H2_To_Size_And_Bold_When_H2_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("<h2>Header 2</h2>");
			result.ShouldBe("<size=+4><b>Header 2</b></size>");
		}

		[Test]
		public void Converts_Html_H3_To_Size_And_Bold_When_H3_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("<h3>Header 3</h3>");
			result.ShouldBe("<size=+3><b>Header 3</b></size>");
		}

		[Test]
		public void Keeps_Html_H1_When_H1_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("h1");
			var result = formatter.Format("<h1>Header 1</h1>");
			result.ShouldBe("<h1>Header 1</h1>");
		}

		[Test]
		public void Converts_Html_Code_To_Font_When_Code_Not_Supported_But_Font_Is()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("<code>var x = 1;</code>");
			result.ShouldBe("<font=\"Consolas\">var x = 1;</font>");
		}

		[Test]
		public void Converts_Html_Pre_To_Font_When_Pre_Not_Supported_But_Font_Is()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("<pre>code block</pre>");
			result.ShouldBe("<font=\"Consolas\">code block</font>");
		}

		[Test]
		public void Keeps_Html_Code_When_Code_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("code");
			var result = formatter.Format("<code>var x = 1;</code>");
			result.ShouldBe("<code>var x = 1;</code>");
		}

		[Test]
		public void Strips_Html_Code_When_Neither_Code_Nor_Font_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<code>var x = 1;</code>");
			result.ShouldBe("var x = 1;");
		}

		[Test]
		public void Converts_Html_H6_To_Bold_Only_When_H6_Not_Supported_But_Bold_Is()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("<h6>Header 6</h6>");
			result.ShouldBe("<b>Header 6</b>");
		}

		[Test]
		public void Strips_Html_H1_When_Neither_H1_Nor_Bold_Nor_Size_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("i");
			var result = formatter.Format("<h1>Header 1</h1>");
			result.ShouldBe("Header 1");
		}

		[Test]
		public void Converts_Nested_Html_H2_Inside_Div()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "size");
			var result = formatter.Format("<div><h2>Nested Header</h2></div>");
			result.ShouldBe("<size=+4><b>Nested Header</b></size>");
		}

		[Test]
		public void Converts_Nested_Html_Code_Inside_Paragraph()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("font");
			var result = formatter.Format("<p>Text with <code>inline code</code> inside</p>");
			result.ShouldBe("Text with <font=\"Consolas\">inline code</font> inside");
		}

		[Test]
		public void Converts_Html_Unordered_List_To_Plain_Text_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>");
			result.ShouldBe("- Item 1\n- Item 2\n- Item 3");
		}

		[Test]
		public void Converts_Html_Ordered_List_To_Plain_Text_When_Not_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<ol><li>First</li><li>Second</li><li>Third</li></ol>");
			result.ShouldBe("1. First\n2. Second\n3. Third");
		}

		[Test]
		public void Keeps_Html_Unordered_List_When_Ul_And_Li_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("ul", "li");
			var result = formatter.Format("<ul><li>Item 1</li><li>Item 2</li></ul>");
			result.ShouldBe("<ul><li>Item 1</li><li>Item 2</li></ul>");
		}

		[Test]
		public void Keeps_Html_Ordered_List_When_Ol_And_Li_Supported()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("ol", "li");
			var result = formatter.Format("<ol><li>First</li><li>Second</li></ol>");
			result.ShouldBe("<ol><li>First</li><li>Second</li></ol>");
		}

		[Test]
		public void Converts_Html_List_With_Nested_Html_Tags()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b");
			var result = formatter.Format("<ul><li><b>Bold</b> item</li><li>Normal item</li></ul>");
			result.ShouldBe("- Bold item\n- Normal item");
		}
	}

	public class PerformanceTests : SimplifiedHtmlMessageFormatterTests
	{
		[Test]
		public void Performance_10000_Html_Replacements()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "i", "s", "a", "ul", "ol", "li", "color", "backcolor", "font", "size");
			var input = "<p>This is <b>bold</b> and <i>italic</i> with <s>strikethrough</s> and a <a href='url'>link</a>. " +
				"<span style='color:red'>Red</span> and <span style='background-color:blue'>blue bg</span>. " +
				"<ul><li>Item 1</li><li>Item 2</li></ul></p>";

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			for (var i = 0; i < 10000; i++)
			{
				_ = formatter.Format(input);
			}
			stopwatch.Stop();

			Console.WriteLine($"SimplifiedHtmlMessageformatter: 10,000 HTML replacements in {stopwatch.ElapsedMilliseconds}ms");
			stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000); // Should complete in less than 5 seconds
		}

		[Test]
		public void Performance_10000_Markdown_Replacements()
		{
			var formatter = new SimplifiedHtmlMessageFormatter("b", "i", "s", "a", "ul", "ol", "li", "font");
			var input = "This is **bold** and *italic* with ~~strikethrough~~ and a [link](url). " +
				"`inline code` and:\n```\ncode block\n```\n" +
				"- Item 1\n- Item 2\n- Item 3";

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			for (var i = 0; i < 10000; i++)
			{
				_ = formatter.Format(input);
			}
			stopwatch.Stop();

			Console.WriteLine($"SimplifiedHtmlMessageformatter: 10,000 Markdown replacements in {stopwatch.ElapsedMilliseconds}ms");
			stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000); // Should complete in less than 5 seconds
		}
	}
}
