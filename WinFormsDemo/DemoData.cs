using System.Runtime.CompilerServices;
using TinyChat;

namespace WinFormsDemo;

public class DemoData
{
	public static string AssistantName { get; set; } = "Assistant";

	public static IEnumerable<DemoChatMessage> Create(string currentUser)
	{
		return [
			new DemoChatMessage(currentUser, "Hey, **someone** there?"),
			new DemoChatMessage(AssistantName,"Yes, I am here. How can I _help_ you today?"),
			new DemoChatMessage(currentUser, "What do you think, is **WinForms** dead?"),
			new DemoChatMessage(AssistantName, "While many people would claim WinForms is an ~~outdated~~ **legacy** technology, I think it has its _raison d'être_.\n\n" +
			"It's still **heavily used** in business scenarios and with the help of many control vendors, such as:\n" +
			"- <b>DevExpress</b>\n" +
			"- <i>Telerik</i>\n\n" +
			"It's still a very valid way of building business applications.\n\n" +
			"As long as Microsoft has `no real alternative`, I would ***not*** say WinForms is dead."),
			new DemoChatMessage(currentUser, "Wait, you say Microsoft has <strong>no alternative</strong> for a technology that has been there since `2001`?"),
			new DemoChatMessage(AssistantName, "**Absolutely**. Microsoft has no _go-to_ technology for building Windows apps.\n\n"+
			"They introduced:\n" +
			"1. <b>WPF</b> in `2006` but it's been <s>discontinued</s> only a few years later.\n" +
			"2. **UWP** in `2015` which was heavily restricted to building Microsoft Store apps and ~~discontinued~~ soon after.\n" +
			"3. Afterwards they introduced <i>WinUI2</i>, discontinued it and introduced the ***incompatible*** `WinUI3`.\n\n" +
			"> This is still the most recommended way but still very **restricted** in comparison to _WinForms_ and _WPF_ when it comes to application deployment.\n\n" +
			"See also: [Microsoft Documentation](https://learn.microsoft.com)"),
			new DemoChatMessage(currentUser, "Can you show me a comprehensive example with HTML formatting?"),
			new DemoChatMessage(AssistantName, "Sure! Here's a <b>comprehensive</b> HTML example:\n\n" +
			"<h1>Main Heading</h1>\n" +
			"<h2>Subheading</h2>\n" +
			"<h3>Smaller Heading</h3>\n" +
			"<h4>Even smaller</h4>\n" +
			"<h5>H5 Heading</h5>\n" +
			"<h6>H6 Heading</h6>\n\n" +
			"<p>This is a <strong>strongly emphasized</strong> text with <b>bold</b> formatting.</p>\n" +
			"<p>Here's some <em>emphasized</em> and <i>italic</i> text.</p>\n" +
			"<p>You can also use <u>underlined</u> text and <s>strikethrough</s> or <strike>deleted</strike> text.</p>\n\n" +
			"<p>HTML span colors: <span style='color:red'>Red text</span>, <span style='color:blue'>Blue text</span>, <span style='color:green'>Green text</span></p>\n" +
			"<p>HTML span background colors: <span style='background-color:yellow'>Yellow background</span>, <span style='background-color:#FF6600'>Orange background</span></p>\n" +
			"<p>DevExpress colors: <color=red>Red</color>, <color=#0000FF>HEX Blue</color>, <color=0,255,0>RGB Green</color></p>\n" +
			"<p>DevExpress background colors: <backcolor=yellow>Yellow background</backcolor>, <backcolor=#5555FF>HEX blue background</backcolor>, <backcolor=0,255,0>RGB green background</backcolor></p>\n\n" +
			"<p>Code examples: <code>var x = 42;</code> and <pre>public void Method()\n{\n    Console.WriteLine(\"Hello\");\n}</pre></p>\n\n" +
			"<p>Links: <a href='https://github.com'>GitHub</a> and <a href='https://stackoverflow.com' target='_blank'>Stack Overflow</a></p>\n\n" +
			"<blockquote>This is a blockquote with important information</blockquote>\n\n" +
			"Lists:\n" +
			"<ul>\n" +
			"<li>First <b>unordered</b> item</li>\n" +
			"<li>Second item with <i>italic</i></li>\n" +
			"<li>Third item with <code>code</code></li>\n" +
			"</ul>\n\n" +
			"<ol>\n" +
			"<li>First <strong>ordered</strong> item</li>\n" +
			"<li>Second item</li>\n" +
			"<li>Third item</li>\n" +
			"</ol>\n\n" +
			"<p>Special formatting: <sup>superscript</sup> and <sub>subscript</sub></p>\n" +
			"<p>Line breaks:<br/>New line<br/>Another line</p>"),
			new DemoChatMessage(currentUser, "Great! Now show me all Markdown formatting options."),
			new DemoChatMessage(AssistantName, "Of course! Here's a **complete** Markdown example:\n\n" +
			"# Main Heading\n" +
			"## Subheading\n" +
			"### Smaller Heading\n" +
			"#### Even Smaller\n" +
			"##### H5 Heading\n" +
			"###### H6 Heading\n\n" +
			"This is **bold text** and __also bold__.\n\n" +
			"This is *italic text* and _also italic_.\n\n" +
			"This is ***bold and italic*** combined.\n\n" +
			"This is ~~strikethrough~~ text.\n\n" +
			"Inline `code` example: `var message = \"Hello World\";`\n\n" +
			"```csharp\n" +
			"// Code block example\n" +
			"public class Example\n" +
			"{\n" +
			"    public void Method()\n" +
			"    {\n" +
			"        Console.WriteLine(\"Hello from code block!\");\n" +
			"    }\n" +
			"}\n" +
			"```\n\n" +
			"Links: [GitHub](https://github.com) and [Documentation](https://learn.microsoft.com)\n\n" +
			"> This is a blockquote\n" +
			"> It can span multiple lines\n" +
			"> And contain **formatting**\n\n" +
			"Unordered lists:\n" +
			"- First item\n" +
			"- Second item with **bold**\n" +
			"- Third item with *italic*\n" +
			"  - Nested item\n" +
			"  - Another nested item\n" +
			"- Fourth item with `code`\n\n" +
			"Ordered lists:\n" +
			"1. First item\n" +
			"2. Second item\n" +
			"3. Third item\n" +
			"   1. Nested ordered\n" +
			"   2. Another nested\n" +
			"4. Fourth item\n\n")
			];
	}

	public static async IAsyncEnumerable<string> StreamAiAnswer([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var answers = new[]
		{
			"Well that is **true**, _nothing_ to argue about.",
			"I think the ***key factor*** here is understanding the <b>context</b> and <i>requirements</i> of your specific project. Different technologies serve different purposes, and what works best depends on your team's `expertise`, `timeline`, and ~~long-term~~ maintenance considerations.",
			"**Absolutely**, this is a _common concern_ in software development. The technology landscape evolves ***rapidly***, but established frameworks often have:\n- Strong ecosystems\n- <b>Community support</b>\n\nThat keep them viable for many years. It's important to weigh the benefits of <i>stability</i> against the advantages of `newer approaches`.",
			"Not necessarily, as it depends on several factors including your **target audience** and _deployment requirements_. See: <a href='#'>more info</a>",
			"From my perspective, the choice between different UI frameworks should be based on ***practical considerations*** rather than just following <s>trends</s>. Legacy technologies often provide:\n\n" +
			"> Robust solutions with **extensive documentation** and _proven track records_ in production environments\n\n" +
			"Which can be `invaluable` for <b>business-critical</b> applications.",
			"In my opinion, **WinForms is not dead**, it's just **sleeping** in a corner of the codebase, dreaming of a better UI framework. But it's still **very much alive** in enterprise environments.",
			"WinForms? Dead? 🤯 That’s like saying the **Turing Machine** is obsolete. It’s not dead, it’s just **underestimated** by the trendy developers.",
			"If you ask me, WinForms is like a **well-aged wine** – it may not be the latest trend, but it’s still **smooth** and **complex** enough to keep you interested.",
			"So, you're asking if WinForms is dead? That's like asking if the **Piano** is dead. It’s not dead, it’s just waiting for the right **composer**.",
			"I’ve seen WinForms applications running for **decades** and they still **hold their ground**. If it ain’t broke, don’t fix it. 🛠️",
			"Microsoft has no real alternative? That’s like saying **the Internet** has no alternative to the **World Wide Web**. It’s a **legacy** that works, and that’s a **feature**, not a bug.",
			"WinForms is not dead — it’s **just in maintenance mode**. It's like a **classic car** — it’s not flashy, but it’s **reliable** and **still gets you there**.",
			"The real question is: Is WinForms **overrated** or **underrated**? My bet? It’s **underrated** in a world full of **over-engineered** UI frameworks.",
			"In a world of **blazing fast** frameworks, WinForms is like a **slow, steady river** — it doesn’t go anywhere fast, but it **carries everything** with it."
		};

		var random = new Random();
		var selectedAnswer = answers[random.Next(answers.Length)];

		for (var i = 0; i < selectedAnswer.Length; i += 4)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			var chunk = selectedAnswer.Substring(i, Math.Min(4, selectedAnswer.Length - i));
			yield return chunk;
			await Task.Delay(75).ConfigureAwait(false);
		}
	}

	[System.Diagnostics.DebuggerDisplay("{Sender.Name}: {Content.Content}")]
	public class DemoChatMessage : IChatMessage
	{
		public DemoChatMessage(string sender, string message)
		{
			Sender = new NamedSender(sender);
			Content = new StringMessageContent(message);
		}

		public ISender Sender { get; }

		public IChatMessageContent Content { get; }
	}
}
