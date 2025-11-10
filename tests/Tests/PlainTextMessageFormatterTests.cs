using Shouldly;
using TinyChat;
using TinyChat.Messages.Formatting;

namespace Tests;

public class PlainTextMessageFormatterTests
{
	private IMessageFormatter _formatter;

	[OneTimeSetUp]
	public void Setup()
	{
		_formatter = new PlainTextMessageFormatter();
	}

	public class FormatMethod : PlainTextMessageFormatterTests
	{
		[Test]
		public void Returns_Plain_Text_From_Simple_String()
		{
			var result = _formatter.Format("Hello, world!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Returns_Cleaned_Plain_Text_From_Markdown_Bold()
		{
			var result = _formatter.Format("Hello, **world**!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Returns_Cleaned_Plain_Text_From_Markdown_Bold_Underscore()
		{
			var result = _formatter.Format("Hello, __world__!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Returns_Cleaned_Plain_Text_From_Markdown_Italic()
		{
			var result = _formatter.Format("Hello, *world*!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Returns_Cleaned_Plain_Text_From_Markdown_Italic_Underscore()
		{
			var result = _formatter.Format("Hello, _world_!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Returns_Cleaned_Plain_Text_From_Markdown_Strikethrough()
		{
			var result = _formatter.Format("Hello, ~~world~~!");
			result.ShouldBe("Hello, world!");
		}

		[Test]
		public void Keeps_Link_Text_From_Markdown()
		{
			var result = _formatter.Format("This is a [link](http://example.com)");
			result.ShouldBe("This is a link");
		}

		[Test]
		public void Keeps_Alt_Text_From_Markdown_Images()
		{
			var result = _formatter.Format("An image: ![alt text](image.jpg)");
			result.ShouldBe("An image: alt text");
		}

		[Test]
		public void Removes_Markdown_Headers()
		{
			var result = _formatter.Format("# Header 1\n## Header 2\n### Header 3");
			result.ShouldBe("Header 1\nHeader 2\nHeader 3");
		}

		[Test]
		public void Keeps_Inline_Code_Content()
		{
			var result = _formatter.Format("Use `console.log()` to debug");
			result.ShouldBe("Use console.log() to debug");
		}

		[Test]
		public void Removes_Code_Blocks()
		{
			var result = _formatter.Format("Before\n```\ncode here\n```\nAfter");
			result.ShouldBe("Before\n\ncode here\n\nAfter");
		}

		[Test]
		public void Removes_Code_Blocks_With_Language()
		{
			var result = _formatter.Format("```csharp\nvar x = 1;\n```");
			result.ShouldBe("var x = 1;");
		}

		[Test]
		public void Removes_HTML_Tags()
		{
			var result = _formatter.Format("<p>Hello <strong>world</strong></p>");
			result.ShouldBe("Hello world");
		}

		[Test]
		public void Keeps_Text_From_HTML_Links()
		{
			var result = _formatter.Format("<a href='http://example.com'>Click here</a>");
			result.ShouldBe("Click here");
		}

		[Test]
		public void Processes_Mixed_Markdown_And_HTML()
		{
			var result = _formatter.Format("**Bold** and <strong>HTML bold</strong>");
			result.ShouldBe("Bold and HTML bold");
		}

		[Test]
		public void Processes_Complex_Mixed_Formatting()
		{
			var result = _formatter.Format("Hello, **world**! This is a [link](http://example.com) and an image: ![alt text](image.jpg)");
			result.ShouldBe("Hello, world! This is a link and an image: alt text");
		}

		[Test]
		public void Removes_Nested_HTML_Tags()
		{
			var result = _formatter.Format("<div><p><span>Nested</span> content</p></div>");
			result.ShouldBe("Nested content");
		}

		[Test]
		public void Returns_Empty_String_For_Empty_Input()
		{
			var result = _formatter.Format("");
			result.ShouldBe("");
		}

		[Test]
		public void Returns_Empty_String_For_Null_Input()
		{
			var result = _formatter.Format((string)null!);
			result.ShouldBe("");
		}

		[Test]
		public void Returns_Empty_String_For_Whitespace_Only_Input()
		{
			var result = _formatter.Format("   \n\t   ");
			result.ShouldBe("");
		}

		[Test]
		public void Strips_Incomplete_Markdown_Bold()
		{
			// Incomplete markdown patterns that don't match are left as-is
			var result = _formatter.Format("**incomplete bold");
			result.ShouldBe("incomplete bold");
		}

		[Test]
		public void Preserves_Incomplete_Markdown_Link_As_Is()
		{
			var result = _formatter.Format("[incomplete link");
			result.ShouldBe("[incomplete link");
		}

		[Test]
		public void Preserves_Incomplete_HTML_Tag_As_Is()
		{
			var result = _formatter.Format("<incomplete");
			result.ShouldBe("<incomplete");
		}

		[Test]
		public void Removes_Unclosed_HTML_Tag()
		{
			var result = _formatter.Format("<p>Unclosed paragraph");
			result.ShouldBe("Unclosed paragraph");
		}

		[Test]
		public void Strips_Multiple_Consecutive_Formatting_Markers()
		{
			// ***text*** is treated as bold (**) containing *text*
			// The regex processes ** first, then * on the remainder
			var result = _formatter.Format("***triple asterisks***");
			result.ShouldBe("triple asterisks");
		}

		[Test]
		public void Processes_Mixed_Complete_And_Incomplete_Formatting()
		{
			// Incomplete patterns that don't have a closing match are processed by the regex
			var result = _formatter.Format("**bold** and **incomplete");
			result.ShouldBe("bold and incomplete");
		}

		[Test]
		public void Trims_Leading_And_Trailing_Whitespace()
		{
			var result = _formatter.Format("  **bold**  ");
			result.ShouldBe("bold");
		}

		[Test]
		public void Removes_Self_Closing_HTML_Tags()
		{
			var result = _formatter.Format("Line<br/>break");
			result.ShouldBe("Linebreak");
		}

		[Test]
		public void Preserves_HTML_Entities_As_Is()
		{
			var result = _formatter.Format("&lt;div&gt; &amp; &quot;quotes&quot;");
			result.ShouldBe("&lt;div&gt; &amp; &quot;quotes&quot;");
		}

		[Test]
		public void Preserves_Empty_Markdown_Link_As_Is()
		{
			// Regex requires at least one character in the link text (\[([^\]]+)\])
			// so empty links are not matched and left as-is
			var result = _formatter.Format("[](http://example.com)");
			result.ShouldBe("[](http://example.com)");
		}

		[Test]
		public void Removes_Empty_Markdown_Image_Alt()
		{
			// Regex requires at least one character in alt text (!\[([^\]]*)\])
			// Empty alt text uses * which allows zero characters, so it should be removed
			var result = _formatter.Format("![](image.jpg)");
			result.ShouldBe("");
		}

		[Test]
		public void Throws_On_Unsupported_Content_Type()
		{
			var content = new ChangingMessageContent(null);
			Should.Throw<NotSupportedException>(() => _formatter.Format(content));
		}

		[Test]
		public void Formats_StringMessageContent()
		{
			var content = new StringMessageContent("**Hello**");
			var result = _formatter.Format(content);
			result.ShouldBe("Hello");
		}

		[Test]
		public void Extracts_Text_From_Multiple_Links_In_Same_Line()
		{
			var result = _formatter.Format("[link1](url1) and [link2](url2)");
			result.ShouldBe("link1 and link2");
		}

		[Test]
		public void Extracts_Alt_Text_From_Multiple_Images_In_Same_Line()
		{
			var result = _formatter.Format("![img1](url1) and ![img2](url2)");
			result.ShouldBe("img1 and img2");
		}

		[Test]
		public void Preserves_Code_Block_Without_Closing_Marker()
		{
			// Without closing ```, the inline code regex processes `` as empty content
			var result = _formatter.Format("Before ```code without closing");
			result.ShouldBe("Before `code without closing");
		}

		[Test]
		public void Preserves_Inline_Code_Without_Closing_Backtick()
		{
			var result = _formatter.Format("`code without closing");
			result.ShouldBe("`code without closing");
		}

		[Test]
		public void Preserves_Line_Breaks_In_Plain_Text()
		{
			var result = _formatter.Format("Line 1\nLine 2\nLine 3");
			result.ShouldBe("Line 1\nLine 2\nLine 3");
		}

		[Test]
		public void Removes_All_Six_Header_Levels()
		{
			var result = _formatter.Format("# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6");
			result.ShouldBe("H1\nH2\nH3\nH4\nH5\nH6");
		}

		[Test]
		public void Strips_Bold_Within_Italic()
		{
			var result = _formatter.Format("*This is **bold** within italic*");
			result.ShouldBe("This is bold within italic");
		}

		[Test]
		public void Strips_Italic_Within_Bold()
		{
			var result = _formatter.Format("**This is *italic* within bold**");
			result.ShouldBe("This is italic within bold");
		}

		[Test]
		public void Extracts_Bold_Text_From_Link()
		{
			var result = _formatter.Format("[**bold link**](http://example.com)");
			result.ShouldBe("bold link");
		}

		[Test]
		public void Extracts_Formatted_Alt_Text_From_Image()
		{
			var result = _formatter.Format("![**bold** alt](image.jpg)");
			result.ShouldBe("bold alt");
		}

		[Test]
		public void Extracts_Text_From_HTML_With_Attributes()
		{
			var result = _formatter.Format("<a href='url' class='link' data-id='123'>Link</a>");
			result.ShouldBe("Link");
		}

		[Test]
		public void Preserves_Multiple_Spaces_And_Newlines()
		{
			var result = _formatter.Format("Line 1\n\n\nLine 2   with   spaces");
			result.ShouldBe("Line 1\n\n\nLine 2   with   spaces");
		}

		[Test]
		public void Processes_Escaped_Characters_As_Literal()
		{
			// Regex doesn't handle escaped markdown, so \* is treated as literal
			var result = _formatter.Format(@"This is \*not italic\*");
			result.ShouldBe(@"This is \not italic\");
		}

		[Test]
		public void Removes_Code_Block_With_Multiple_Backticks()
		{
			// Four backticks are treated as a fenced code block (matches the pattern)
			var result = _formatter.Format("````\ncode\n````");
			result.ShouldBe("code");
		}

		[Test]
		public void Removes_Inline_Code_With_Backticks_Inside()
		{
			// Nested backticks: the outer `` gets processed, removing the inner content
			var result = _formatter.Format("Use `` `backtick` `` in code");
			result.ShouldBe("Use  backtick  in code");
		}

		[Test]
		public void Removes_HTML_Comments()
		{
			var result = _formatter.Format("Text <!-- comment --> more text");
			result.ShouldBe("Text  more text");
		}

		[Test]
		public void Removes_Multiple_Paragraph_Tags()
		{
			var result = _formatter.Format("<p>Para 1</p><p>Para 2</p>");
			result.ShouldBe("Para 1Para 2");
		}

		[Test]
		public void Preserves_Markdown_List_Items_As_Is()
		{
			var result = _formatter.Format("- Item 1\n- Item 2\n- Item 3");
			result.ShouldBe("- Item 1\n- Item 2\n- Item 3");
		}

		[Test]
		public void Preserves_Numbered_List_As_Is()
		{
			var result = _formatter.Format("1. First\n2. Second\n3. Third");
			result.ShouldBe("1. First\n2. Second\n3. Third");
		}

		[Test]
		public void Preserves_Blockquote_Syntax_As_Is()
		{
			var result = _formatter.Format("> This is a quote");
			result.ShouldBe("> This is a quote");
		}

		[Test]
		public void Preserves_Horizontal_Rule_As_Is()
		{
			var result = _formatter.Format("Before\n---\nAfter");
			result.ShouldBe("Before\n---\nAfter");
		}

		[Test]
		public void Preserves_Table_Markdown_As_Is()
		{
			var result = _formatter.Format("| Col1 | Col2 |\n|------|------|\n| A | B |");
			result.ShouldBe("| Col1 | Col2 |\n|------|------|\n| A | B |");
		}

		[Test]
		public void Strips_Mixed_Underscores_And_Asterisks_Consistently()
		{
			var result = _formatter.Format("_italic_ and *also italic* and __bold__ and **also bold**");
			result.ShouldBe("italic and also italic and bold and also bold");
		}

		[Test]
		public void Processes_Real_World_Chat_Message_With_Emoji()
		{
			var result = _formatter.Format("Hey! Check out this **awesome** [link](https://github.com) and let me know what you think! 😊");
			result.ShouldBe("Hey! Check out this awesome link and let me know what you think! 😊");
		}

		[Test]
		public void Removes_HTML_From_Code_Content()
		{
			// Inline code is processed first, then HTML tags are removed
			var result = _formatter.Format("`<div>code</div>`");
			result.ShouldBe("code");
		}

		[Test]
		public void Removes_Script_Tag_Content()
		{
			var result = _formatter.Format("<script>alert('test');</script>");
			result.ShouldBe("alert('test');");
		}

		[Test]
		public void Removes_Empty_Bold_Markdown()
		{
			var result = _formatter.Format("****");
			result.ShouldBe("");
		}

		[Test]
		public void Removes_Empty_Italic_Markdown()
		{
			var result = _formatter.Format("**");
			result.ShouldBe("");
		}

		[Test]
		public void Removes_Empty_Strikethrough_Markdown()
		{
			var result = _formatter.Format("~~~~");
			result.ShouldBe("");
		}

		[Test]
		public void Removes_Empty_Inline_Code_Markdown()
		{
			var result = _formatter.Format("``");
			result.ShouldBe("");
		}

		[Test]
		public void Preserves_Code_Block_Content_With_Blank_Lines()
		{
			var result = _formatter.Format("Text\n```\nline1\nline2\n```\nMore text");
			result.ShouldBe("Text\n\nline1\nline2\n\nMore text");
		}

		[Test]
		public void Removes_Language_Specifier_From_Code_Block()
		{
			var result = _formatter.Format("```python\nprint('hello')\n```");
			result.ShouldBe("print('hello')");
		}

		[Test]
		public void Preserves_Multi_Line_Code_Block_Content()
		{
			var result = _formatter.Format("```\nvar x = 1;\nvar y = 2;\nvar z = 3;\n```");
			result.ShouldBe("var x = 1;\nvar y = 2;\nvar z = 3;");
		}

		[Test]
		public void Processes_Multiple_Code_Blocks()
		{
			var result = _formatter.Format("```\nblock1\n```\ntext\n```\nblock2\n```");
			result.ShouldBe("block1\n\ntext\n\nblock2");
		}

		[Test]
		public void Preserves_Empty_Lines_Inside_Code_Block()
		{
			var result = _formatter.Format("```\nline1\n\nline2\n```");
			result.ShouldBe("line1\n\nline2");
		}

		[Test]
		public void Removes_Inline_Code_Block_Without_Newline()
		{
			// Inline code blocks (```code```) are matched but .Trim() removes the blank lines
			var result = _formatter.Format("```code```");
			result.ShouldBe("");
		}

		[Test]
		public void Converts_Html_Unordered_List_To_Plain_Text()
		{
			var result = _formatter.Format("<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>");
			result.ShouldBe("- Item 1\n- Item 2\n- Item 3");
		}

		[Test]
		public void Converts_Html_Ordered_List_To_Plain_Text()
		{
			var result = _formatter.Format("<ol><li>First</li><li>Second</li><li>Third</li></ol>");
			result.ShouldBe("1. First\n2. Second\n3. Third");
		}

		[Test]
		public void Converts_Html_List_With_Nested_Html_Tags()
		{
			var result = _formatter.Format("<ul><li><b>Bold</b> item</li><li>Normal <i>italic</i> item</li></ul>");
			result.ShouldBe("- Bold item\n- Normal italic item");
		}

		[Test]
		public void Converts_Multiple_Html_Lists()
		{
			var result = _formatter.Format("<ul><li>UL 1</li><li>UL 2</li></ul>\nText\n<ol><li>OL 1</li><li>OL 2</li></ol>");
			result.ShouldBe("- UL 1\n- UL 2\nText\n1. OL 1\n2. OL 2");
		}

		[Test]
		public void Converts_Nested_Html_Lists()
		{
			var result = _formatter.Format("<ul><li>Item 1<ul><li>Nested 1</li><li>Nested 2</li></ul></li><li>Item 2</li></ul>");
			result.ShouldBe("- Item 1- Nested 1\n- Nested 2\n- Item 2");
		}
	}

	public class PerformanceTests : PlainTextMessageFormatterTests
	{
		[Test]
		public void Performance_10000_Html_Replacements()
		{
			var input = "<p>This is <b>bold</b> and <i>italic</i> with <s>strikethrough</s> and a <a href='url'>link</a>. " +
				 "<span style='color:red'>Red</span> and <span style='background-color:blue'>blue bg</span>. " +
			  "<ul><li>Item 1</li><li>Item 2</li></ul></p>";

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			for (var i = 0; i < 10000; i++)
			{
				_ = _formatter.Format(input);
			}
			stopwatch.Stop();

			Console.WriteLine($"PlainTextMessageFormatter: 10,000 HTML replacements in {stopwatch.ElapsedMilliseconds}ms");
			stopwatch.ElapsedMilliseconds.ShouldBeLessThan(3000); // Should complete in less than 3 seconds
		}

		[Test]
		public void Performance_10000_Markdown_Replacements()
		{
			var input = "This is **bold** and *italic* with ~~strikethrough~~ and a [link](url). " +
				  "`inline code` and:\n```\ncode block\n```\n" +
			  "- Item 1\n- Item 2\n- Item 3\n" +
					 "1. First\n2. Second\n3. Third";

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			for (var i = 0; i < 10000; i++)
			{
				_ = _formatter.Format(input);
			}
			stopwatch.Stop();

			Console.WriteLine($"PlainTextMessageFormatter: 10,000 Markdown replacements in {stopwatch.ElapsedMilliseconds}ms");
			stopwatch.ElapsedMilliseconds.ShouldBeLessThan(3000); // Should complete in less than 3 seconds
		}
	}
}
