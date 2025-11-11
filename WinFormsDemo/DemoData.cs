using System.Runtime.CompilerServices;
using TinyChat;

namespace WinFormsDemo;

public class DemoData
{
	public static string AssistantName { get; set; } = "Assistant";

	public static IEnumerable<DemoChatMessage> Create(string currentUser)
	{
		return [
			new DemoChatMessage(AssistantName,"How can I help you today?")
			];
	}

	public static async IAsyncEnumerable<string> StreamAiAnswer(IChatMessageContent content, bool isDevExpress, [EnumeratorCancellation] CancellationToken cancellationToken)
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

		if (content?.Content?.ToString()?.Contains("format", StringComparison.OrdinalIgnoreCase) ?? false)
		{
			if (isDevExpress)
			{
				answers =
				[
					"DevExpress controls have built-it support for basic HTML formatting.\n\n" +
					"TinyChat will convert HTML and Markdown input to supported HTML tags on the fly. Unsupported formatting will be removed to keep the text readable.\n\n" +
					"Here's a Markdown example:\n\n" +
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
					"Links: [GitHub](https://github.com) and [Documentation](https://learn.microsoft.com)\n\n"
				];
			}
			else
			{
				answers =
				[
					"Standard Windows Forms controls don't allow partial formatting. Therefore, the following Markdown text will be reduced to plain text for better readability." +
					"" +
					"Here's a Markdown example:\n\n" +
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
					"Links: [GitHub](https://github.com) and [Documentation](https://learn.microsoft.com)\n\n"
				];
			}
		}

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
