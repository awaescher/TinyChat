using Shouldly;
using TinyChat.Messages.Rendering;
using TinyChat;

namespace Tests;

public class SimplifiedHtmlMessageRendererTests
{
	public class RenderMethod : SimplifiedHtmlMessageRendererTests
	{
		[Test]
		public void Returns_Plain_Text_When_No_Tags_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer();
			var result = renderer.Render("Hello, **world**!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Returns_Html_Text_From_Simple_String()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Hello, world!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Converts_Markdown_Bold_To_Html_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Hello, **world**!");
			result.ShouldBe("Hello, <b>world</b>!");
		}

		[Test]
		public void Converts_Markdown_Bold_Underscore_To_Html_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Hello, __world__!");
			result.ShouldBe("Hello, <b>world</b>!");
		}

		[Test]
		public void Strips_Markdown_Bold_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("i");
			var result = renderer.Render("Hello, **world**!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Converts_Markdown_Italic_To_Html_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("i");
			var result = renderer.Render("Hello, *world*!");
			result.ShouldBe("Hello, <i>world</i>!");
		}

		[Test]
		public void Converts_Markdown_Italic_Underscore_To_Html_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("i");
			var result = renderer.Render("Hello, _world_!");
			result.ShouldBe("Hello, <i>world</i>!");
		}

		[Test]
		public void Strips_Markdown_Italic_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Hello, *world*!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Converts_Markdown_Strikethrough_To_Html_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("s");
			var result = renderer.Render("Hello, ~~world~~!");
			result.ShouldBe("Hello, <s>world</s>!");
		}

		[Test]
		public void Strips_Markdown_Strikethrough_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Hello, ~~world~~!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Converts_Markdown_Link_To_Html_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("a");
			var result = renderer.Render("This is a [link](http://example.com)");
			result.ShouldBe("This is a <a href=\"http://example.com\">link</a>");
		}

		[Test]
		public void Strips_Markdown_Link_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("This is a [link](http://example.com)");
			result.ShouldBe("This is a link");
		}

		[Test]
		public void Converts_Markdown_Image_To_Html_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("img");
			var result = renderer.Render("An image: ![alt text](image.jpg)");
			result.ShouldBe("An image: <img src=\"image.jpg\" alt=\"alt text\">");
		}

		[Test]
		public void Strips_Markdown_Image_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("An image: ![alt text](image.jpg)");
			result.ShouldBe("An image: alt text");
		}

		[Test]
		public void Converts_Markdown_H1_To_Html_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("h1");
			var result = renderer.Render("# Header 1");
			result.ShouldBe("<h1>Header 1</h1>");
		}

		[Test]
		public void Converts_Multiple_Headers_When_All_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("h1", "h2", "h3");
			var result = renderer.Render("# Header 1\n## Header 2\n### Header 3");
			result.ShouldBe("<h1>Header 1</h1>\n<h2>Header 2</h2>\n<h3>Header 3</h3>");
		}

		[Test]
		public void Strips_Headers_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("# Header 1\n## Header 2");
			result.ShouldBe("<b>Header 1</b>\n<b>Header 2</b>");
		}

		[Test]
		public void Converts_Some_Headers_And_Strips_Others()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("h1", "h3");
			var result = renderer.Render("# Header 1\n## Header 2\n### Header 3");
			result.ShouldBe("<h1>Header 1</h1>\nHeader 2\n<h3>Header 3</h3>");
		}

		[Test]
		public void Keeps_Supported_Html_Tags()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "i");
			var result = renderer.Render("<b>Bold</b> and <i>italic</i>");
			result.ShouldBe("<b>Bold</b> and <i>italic</i>");
		}

		[Test]
		public void Strips_Unsupported_Html_Tags()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<b>Bold</b> and <i>italic</i>");
			result.ShouldBe("<b>Bold</b> and italic");
		}

		[Test]
		public void Normalizes_Strong_To_B_When_B_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<strong>Bold</strong>");
			result.ShouldBe("<b>Bold</b>");
		}

		[Test]
		public void Normalizes_Em_To_I_When_I_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("i");
			var result = renderer.Render("<em>Italic</em>");
			result.ShouldBe("<i>Italic</i>");
		}

		[Test]
		public void Normalizes_Del_To_S_When_S_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("s");
			var result = renderer.Render("<del>Deleted</del>");
			result.ShouldBe("<s>Deleted</s>");
		}

		[Test]
		public void Normalizes_Strike_To_S_When_S_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("s");
			var result = renderer.Render("<strike>Struck</strike>");
			result.ShouldBe("<s>Struck</s>");
		}

		[Test]
		public void Converts_Mixed_Markdown_And_Html()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("**Markdown** and <b>HTML</b> bold");
			result.ShouldBe("<b>Markdown</b> and <b>HTML</b> bold");
		}

		[Test]
		public void Keeps_Nested_Html_Tags_When_All_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "i");
			var result = renderer.Render("<b>Bold with <i>nested italic</i></b>");
			result.ShouldBe("<b>Bold with <i>nested italic</i></b>");
		}

		[Test]
		public void Strips_Nested_Unsupported_Html_Tags()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<b>Bold with <i>nested italic</i></b>");
			result.ShouldBe("<b>Bold with nested italic</b>");
		}

		[Test]
		public void Strips_All_Tags_When_None_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer();
			var result = renderer.Render("<div><p><span>Nested</span> content</p></div>");
			result.ShouldBe("Nested content");
		}

		[Test]
		public void Returns_Empty_String_For_Empty_Input()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("");
			result.ShouldBe("");
		}

		[Test]
		public void Returns_Empty_String_For_Null_Input()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render((string)null!);
			result.ShouldBe("");
		}

		[Test]
		public void Returns_Empty_String_For_Whitespace_Only_Input()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("   \n\t   ");
			result.ShouldBe("");
		}

		[Test]
		public void Converts_Bold_Within_Italic_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "i");
			var result = renderer.Render("*This is **bold** within italic*");
			result.ShouldBe("<i>This is <b>bold</b> within italic</i>");
		}

		[Test]
		public void Strips_Bold_Within_Italic_When_Only_Italic_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("i");
			var result = renderer.Render("*This is **bold** within italic*");
			result.ShouldBe("<i>This is bold within italic</i>");
		}

		[Test]
		public void Converts_Italic_Within_Bold_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "i");
			var result = renderer.Render("**This is *italic* within bold**");
			result.ShouldBe("<b>This is <i>italic</i> within bold</b>");
		}

		[Test]
		public void Strips_Italic_Within_Bold_When_Only_Bold_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("**This is *italic* within bold**");
			result.ShouldBe("<b>This is italic within bold</b>");
		}

		[Test]
		public void Converts_Link_With_Bold_Text_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("a", "b");
			var result = renderer.Render("[**bold link**](http://example.com)");
			result.ShouldBe("<a href=\"http://example.com\"><b>bold link</b></a>");
		}

		[Test]
		public void Strips_Bold_In_Link_When_Only_Link_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("a");
			var result = renderer.Render("[**bold link**](http://example.com)");
			result.ShouldBe("<a href=\"http://example.com\">bold link</a>");
		}

		[Test]
		public void Keeps_Bold_But_Strips_Link_When_Only_Bold_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("[**bold link**](http://example.com)");
			result.ShouldBe("<b>bold link</b>");
		}

		[Test]
		public void Converts_Complex_Mixed_Formatting()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "a", "img");
			var result = renderer.Render("Hello, **world**! This is a [link](http://example.com) and an image: ![alt text](image.jpg)");
			result.ShouldBe("Hello, <b>world</b>! This is a <a href=\"http://example.com\">link</a> and an image: <img src=\"image.jpg\" alt=\"alt text\">");
		}

		[Test]
		public void Converts_Real_World_Chat_Message_With_Emoji()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "a");
			var result = renderer.Render("Hey! Check out this **awesome** [link](https://github.com) and let me know what you think! 😊");
			result.ShouldBe("Hey! Check out this <b>awesome</b> <a href=\"https://github.com\">link</a> and let me know what you think! 😊");
		}

		[Test]
		public void Converts_Unordered_List_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("ul", "li");
			var result = renderer.Render("- Item 1\n- Item 2\n- Item 3");
			result.ShouldBe("<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>");
		}

		[Test]
		public void Strips_Unordered_List_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("- Item 1\n- Item 2");
			result.ShouldBe("- Item 1\n- Item 2");
		}

		[Test]
		public void Requires_Both_Ul_And_Li_For_Lists()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("ul");
			var result = renderer.Render("- Item 1\n- Item 2");
			result.ShouldBe("- Item 1\n- Item 2");
		}

		[Test]
		public void Converts_Ordered_List_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("ol", "li");
			var result = renderer.Render("1. First\n2. Second\n3. Third");
			result.ShouldBe("<ol><li>First</li><li>Second</li><li>Third</li></ol>");
		}

		[Test]
		public void Strips_Ordered_List_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("1. First\n2. Second");
			result.ShouldBe("1. First\n2. Second");
		}

		[Test]
		public void Converts_List_Items_With_Inline_Formatting()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("ul", "li", "b");
			var result = renderer.Render("- **Bold** item\n- Normal item");
			result.ShouldBe("<ul><li><b>Bold</b> item</li><li>Normal item</li></ul>");
		}

		[Test]
		public void Converts_Blockquote_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("blockquote");
			var result = renderer.Render("> This is a quote");
			result.ShouldBe("<blockquote>This is a quote</blockquote>");
		}

		[Test]
		public void Strips_Blockquote_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("> This is a quote");
			result.ShouldBe("> This is a quote");
		}

		[Test]
		public void Converts_Blockquote_With_Inline_Formatting()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("blockquote", "b");
			var result = renderer.Render("> This is **bold** quote");
			result.ShouldBe("<blockquote>This is <b>bold</b> quote</blockquote>");
		}

		[Test]
		public void Converts_All_Six_Header_Levels()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("h1", "h2", "h3", "h4", "h5", "h6");
			var result = renderer.Render("# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6");
			result.ShouldBe("<h1>H1</h1>\n<h2>H2</h2>\n<h3>H3</h3>\n<h4>H4</h4>\n<h5>H5</h5>\n<h6>H6</h6>");
		}

		[Test]
		public void Converts_Headers_With_Inline_Formatting()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("h1", "b");
			var result = renderer.Render("# Header with **bold** text");
			result.ShouldBe("<h1>Header with <b>bold</b> text</h1>");
		}

		[Test]
		public void Converts_Multiple_Links_In_Same_Line()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("a");
			var result = renderer.Render("[link1](url1) and [link2](url2)");
			result.ShouldBe("<a href=\"url1\">link1</a> and <a href=\"url2\">link2</a>");
		}

		[Test]
		public void Converts_Multiple_Images_In_Same_Line()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("img");
			var result = renderer.Render("![img1](url1) and ![img2](url2)");
			result.ShouldBe("<img src=\"url1\" alt=\"img1\"> and <img src=\"url2\" alt=\"img2\">");
		}

		[Test]
		public void Preserves_Line_Breaks_In_Plain_Text()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Line 1\nLine 2\nLine 3");
			result.ShouldBe("Line 1\nLine 2\nLine 3");
		}

		[Test]
		public void Encodes_Html_Entities_In_Link_Urls()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("a");
			var result = renderer.Render("[link](http://example.com?a=1&b=2)");
			result.ShouldBe("<a href=\"http://example.com?a=1&amp;b=2\">link</a>");
		}

		[Test]
		public void Encodes_Html_Entities_In_Image_Attributes()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("img");
			var result = renderer.Render("![alt & text](image.jpg?a=1&b=2)");
			result.ShouldBe("<img src=\"image.jpg?a=1&amp;b=2\" alt=\"alt &amp; text\">");
		}

		[Test]
		public void Converts_Mixed_Underscores_And_Asterisks_Consistently()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "i");
			var result = renderer.Render("_italic_ and *also italic* and __bold__ and **also bold**");
			result.ShouldBe("<i>italic</i> and <i>also italic</i> and <b>bold</b> and <b>also bold</b>");
		}

		[Test]
		public void Throws_On_Unsupported_Content_Type()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var content = new ChangingMessageContent(null);
			Should.Throw<NotSupportedException>(() => renderer.Render(content));
		}

		[Test]
		public void Renders_StringMessageContent()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var content = new StringMessageContent("**Hello**");
			var result = renderer.Render(content);
			result.ShouldBe("<b>Hello</b>");
		}

		[Test]
		public void Preserves_Empty_Markdown_Link_As_Is()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("a");
			var result = renderer.Render("[](http://example.com)");
			result.ShouldBe("[](http://example.com)");
		}

		[Test]
		public void Converts_Markdown_Image_With_Empty_Alt_Text()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("img");
			var result = renderer.Render("![](image.jpg)");
			result.ShouldBe("<img src=\"image.jpg\" alt=\"\">");
		}

		[Test]
		public void Strips_Additional_Link_Attributes_For_Security()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("a");
			var result = renderer.Render("<a href='url' class='link' data-id='123'>Link</a>");
			result.ShouldBe("<a href=\"url\">Link</a>");
		}

		[Test]
		public void Keeps_Self_Closing_Html_Tags_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("br");
			var result = renderer.Render("Line<br/>break");
			result.ShouldBe("Line<br/>break");
		}

		[Test]
		public void Strips_Self_Closing_Html_Tags_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Line<br/>break");
			result.ShouldBe("Linebreak");
		}

		[Test]
		public void Renders_Inline_Code_With_Font_When_Font_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("Use `console.log()` to debug");
			result.ShouldBe("Use <font=\"Consolas\">console.log()</font> to debug");
		}

		[Test]
		public void Renders_Inline_Code_Plain_When_Font_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Use `console.log()` to debug");
			result.ShouldBe("Use console.log() to debug");
		}

		[Test]
		public void Renders_Code_Block_With_Font_And_Line_Breaks_When_Font_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("Before\n```\ncode here\n```\nAfter");
			result.ShouldBe("Before\n\n<font=\"Consolas\">code here</font>\n\nAfter");
		}

		[Test]
		public void Renders_Code_Block_Plain_When_Font_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("Before\n```\ncode here\n```\nAfter");
			result.ShouldBe("Before\n\ncode here\n\nAfter");
		}

		[Test]
		public void Renders_Code_Block_With_Language_Using_Font()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("```csharp\nvar x = 1;\n```");
			result.ShouldBe("<font=\"Consolas\">var x = 1;</font>");
		}

		[Test]
		public void Encodes_Html_Entities_In_Inline_Code()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("`<div>` tag");
			result.ShouldBe("<font=\"Consolas\">&lt;div&gt;</font> tag");
		}

		[Test]
		public void Renders_Code_Block_With_Multiple_Lines_Using_Font()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("```\nvar x = 1;\nvar y = 2;\nvar z = 3;\n```");
			result.ShouldBe("<font=\"Consolas\">var x = 1;\nvar y = 2;\nvar z = 3;</font>");
		}

		[Test]
		public void Renders_Multiple_Code_Blocks_With_Font()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("```\nblock1\n```\ntext\n```\nblock2\n```");
			result.ShouldBe("<font=\"Consolas\">block1</font>\n\ntext\n\n<font=\"Consolas\">block2</font>");
		}

		[Test]
		public void Escapes_Potential_Xss_In_Inline_Code()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("`<script>alert('xss')</script>`");
			result.ShouldBe("<font=\"Consolas\">&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;</font>");
		}

		[Test]
		public void Renders_Inline_Code_With_Custom_Font()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font") { DefaultCodeFontName = "Courier New" };
			var result = renderer.Render("`code`");
			result.ShouldBe("<font=\"Courier New\">code</font>");
		}

		[Test]
		public void Encodes_Font_Name_In_Face_Attribute()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font") { DefaultCodeFontName = "Font\"Name" };
			var result = renderer.Render("`code`");
			result.ShouldBe("<font=\"Font&quot;Name\">code</font>");
		}

		[Test]
		public void Converts_Html_Color_Span_To_DevExpress_Format_When_Color_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("color");
			var result = renderer.Render("<span style='color:red'>Red text</span>");
			result.ShouldBe("<color=red>Red text</color>");
		}

		[Test]
		public void Converts_Html_BackColor_Span_To_DevExpress_Format_When_BackColor_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("backcolor");
			var result = renderer.Render("<span style='background-color:blue'>Blue bg</span>");
			result.ShouldBe("<backcolor=blue>Blue bg</backcolor>");
		}

		[Test]
		public void Converts_Html_Color_Span_With_Hex_To_DevExpress_Format()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("color");
			var result = renderer.Render("<span style='color:#FF0000'>Red text</span>");
			result.ShouldBe("<color=#FF0000>Red text</color>");
		}

		[Test]
		public void Converts_Html_Color_Span_With_Rgb_To_DevExpress_Format()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("color");
			var result = renderer.Render("<span style='color:rgb(255,0,0)'>Red text</span>");
			result.ShouldBe("<color=255,0,0>Red text</color>");
		}

		[Test]
		public void Keeps_DevExpress_Color_Tags_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("color");
			var result = renderer.Render("<color=red>Red text</color>");
			result.ShouldBe("<color=red>Red text</color>");
		}

		[Test]
		public void Keeps_DevExpress_Color_Tags_With_Hex_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("color");
			var result = renderer.Render("<color=#0000FF>Blue text</color>");
			result.ShouldBe("<color=#0000FF>Blue text</color>");
		}

		[Test]
		public void Keeps_DevExpress_Color_Tags_With_Rgb_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("color");
			var result = renderer.Render("<color=255,0,0>Red text</color>");
			result.ShouldBe("<color=255,0,0>Red text</color>");
		}

		[Test]
		public void Keeps_DevExpress_BackColor_Tags_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("backcolor");
			var result = renderer.Render("<backcolor=yellow>Yellow bg</backcolor>");
			result.ShouldBe("<backcolor=yellow>Yellow bg</backcolor>");
		}

		[Test]
		public void Strips_Color_Tags_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<color=red>Red text</color>");
			result.ShouldBe("Red text");
		}

		[Test]
		public void Converts_Markdown_H1_To_Size_And_Bold_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("# Header 1");
			result.ShouldBe("<size=+5><b>Header 1</b></size>");
		}

		[Test]
		public void Converts_Markdown_H2_To_Size_And_Bold_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("## Header 2");
			result.ShouldBe("<size=+4><b>Header 2</b></size>");
		}

		[Test]
		public void Converts_Markdown_H3_To_Size_And_Bold_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("### Header 3");
			result.ShouldBe("<size=+3><b>Header 3</b></size>");
		}

		[Test]
		public void Converts_Markdown_H4_To_Size_And_Bold_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("#### Header 4");
			result.ShouldBe("<size=+2><b>Header 4</b></size>");
		}

		[Test]
		public void Converts_Markdown_H5_To_Size_And_Bold_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("##### Header 5");
			result.ShouldBe("<size=+1><b>Header 5</b></size>");
		}

		[Test]
		public void Converts_Markdown_H6_To_Bold_Only_When_Both_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("###### Header 6");
			result.ShouldBe("<b>Header 6</b>");
		}

		[Test]
		public void Converts_Markdown_H1_To_Bold_Only_When_Size_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("# Header 1");
			result.ShouldBe("<b>Header 1</b>");
		}

		[Test]
		public void Converts_Markdown_H1_To_Size_Only_When_Bold_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("size");
			var result = renderer.Render("# Header 1");
			result.ShouldBe("<size=+5>Header 1</size>");
		}

		[Test]
		public void Strips_Markdown_Headers_When_Neither_Bold_Nor_Size_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("i");
			var result = renderer.Render("# Header 1\n## Header 2");
			result.ShouldBe("Header 1\nHeader 2");
		}

		[Test]
		public void Keeps_Size_Tags_With_Value_When_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("size");
			var result = renderer.Render("<size=+3>Big text</size>");
			result.ShouldBe("<size=+3>Big text</size>");
		}

		[Test]
		public void Strips_Size_Tags_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<size=+3>Big text</size>");
			result.ShouldBe("Big text");
		}

		[Test]
		public void Converts_Html_H1_To_Size_And_Bold_When_H1_Not_Supported_But_Size_And_Bold_Are()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("<h1>Header 1</h1>");
			result.ShouldBe("<size=+5><b>Header 1</b></size>");
		}

		[Test]
		public void Converts_Html_H2_To_Size_And_Bold_When_H2_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("<h2>Header 2</h2>");
			result.ShouldBe("<size=+4><b>Header 2</b></size>");
		}

		[Test]
		public void Converts_Html_H3_To_Size_And_Bold_When_H3_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("<h3>Header 3</h3>");
			result.ShouldBe("<size=+3><b>Header 3</b></size>");
		}

		[Test]
		public void Keeps_Html_H1_When_H1_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("h1");
			var result = renderer.Render("<h1>Header 1</h1>");
			result.ShouldBe("<h1>Header 1</h1>");
		}

		[Test]
		public void Converts_Html_Code_To_Font_When_Code_Not_Supported_But_Font_Is()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("<code>var x = 1;</code>");
			result.ShouldBe("<font=\"Consolas\">var x = 1;</font>");
		}

		[Test]
		public void Converts_Html_Pre_To_Font_When_Pre_Not_Supported_But_Font_Is()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("<pre>code block</pre>");
			result.ShouldBe("<font=\"Consolas\">code block</font>");
		}

		[Test]
		public void Keeps_Html_Code_When_Code_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("code");
			var result = renderer.Render("<code>var x = 1;</code>");
			result.ShouldBe("<code>var x = 1;</code>");
		}

		[Test]
		public void Strips_Html_Code_When_Neither_Code_Nor_Font_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<code>var x = 1;</code>");
			result.ShouldBe("var x = 1;");
		}

		[Test]
		public void Converts_Html_H6_To_Bold_Only_When_H6_Not_Supported_But_Bold_Is()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("<h6>Header 6</h6>");
			result.ShouldBe("<b>Header 6</b>");
		}

		[Test]
		public void Strips_Html_H1_When_Neither_H1_Nor_Bold_Nor_Size_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("i");
			var result = renderer.Render("<h1>Header 1</h1>");
			result.ShouldBe("Header 1");
		}

		[Test]
		public void Converts_Nested_Html_H2_Inside_Div()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "size");
			var result = renderer.Render("<div><h2>Nested Header</h2></div>");
			result.ShouldBe("<size=+4><b>Nested Header</b></size>");
		}

		[Test]
		public void Converts_Nested_Html_Code_Inside_Paragraph()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("font");
			var result = renderer.Render("<p>Text with <code>inline code</code> inside</p>");
			result.ShouldBe("Text with <font=\"Consolas\">inline code</font> inside");
		}

		[Test]
		public void Converts_Html_Unordered_List_To_Plain_Text_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>");
			result.ShouldBe("- Item 1\n- Item 2\n- Item 3");
		}

		[Test]
		public void Converts_Html_Ordered_List_To_Plain_Text_When_Not_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<ol><li>First</li><li>Second</li><li>Third</li></ol>");
			result.ShouldBe("1. First\n2. Second\n3. Third");
		}

		[Test]
		public void Keeps_Html_Unordered_List_When_Ul_And_Li_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("ul", "li");
			var result = renderer.Render("<ul><li>Item 1</li><li>Item 2</li></ul>");
			result.ShouldBe("<ul><li>Item 1</li><li>Item 2</li></ul>");
		}

		[Test]
		public void Keeps_Html_Ordered_List_When_Ol_And_Li_Supported()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("ol", "li");
			var result = renderer.Render("<ol><li>First</li><li>Second</li></ol>");
			result.ShouldBe("<ol><li>First</li><li>Second</li></ol>");
		}

		[Test]
		public void Converts_Html_List_With_Nested_Html_Tags()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b");
			var result = renderer.Render("<ul><li><b>Bold</b> item</li><li>Normal item</li></ul>");
			result.ShouldBe("- Bold item\n- Normal item");
		}
	}

	public class PerformanceTests : SimplifiedHtmlMessageRendererTests
	{
		[Test]
		public void Performance_10000_Html_Replacements()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "i", "s", "a", "ul", "ol", "li", "color", "backcolor", "font", "size");
			var input = "<p>This is <b>bold</b> and <i>italic</i> with <s>strikethrough</s> and a <a href='url'>link</a>. " +
				"<span style='color:red'>Red</span> and <span style='background-color:blue'>blue bg</span>. " +
				"<ul><li>Item 1</li><li>Item 2</li></ul></p>";

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < 10000; i++)
			{
				_ = renderer.Render(input);
			}
			stopwatch.Stop();

			Console.WriteLine($"SimplifiedHtmlMessageRenderer: 10,000 HTML replacements in {stopwatch.ElapsedMilliseconds}ms");
			stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000); // Should complete in less than 5 seconds
		}

		[Test]
		public void Performance_10000_Markdown_Replacements()
		{
			var renderer = new SimplifiedHtmlMessageRenderer("b", "i", "s", "a", "ul", "ol", "li", "font");
			var input = "This is **bold** and *italic* with ~~strikethrough~~ and a [link](url). " +
				"`inline code` and:\n```\ncode block\n```\n" +
				"- Item 1\n- Item 2\n- Item 3";

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			for (int i = 0; i < 10000; i++)
			{
				_ = renderer.Render(input);
			}
			stopwatch.Stop();

			Console.WriteLine($"SimplifiedHtmlMessageRenderer: 10,000 Markdown replacements in {stopwatch.ElapsedMilliseconds}ms");
			stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000); // Should complete in less than 5 seconds
		}
	}
}
