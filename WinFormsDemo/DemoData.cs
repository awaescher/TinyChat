using System.Runtime.CompilerServices;
using TinyChat;
using TinyChat.Messages;

namespace WinFormsDemo;

public class DemoData
{
	public static string AssistantName { get; set; } = "Assistant";

	public static IEnumerable<DemoChatMessage> Create(string currentUser)
	{
		var arguments = new Dictionary<string, object?> { ["city"] = "Stuttgart", ["unit"] = "Celsius" };

		return [
			new DemoChatMessage(AssistantName,"How can I help you today?"),
			new DemoChatMessage(Environment.UserName, "How is the weather in Stuttgart, DE?"),
			new DemoChatMessage("assistant", new ReasoningMessageContent("The user want's to know the weather in Stuttgart, Germany.")),
			new DemoChatMessage("tool", new FunctionCallMessageContent("weather1", "get_weather", arguments, "26°C, sunny, no clouds")),
			new DemoChatMessage(AssistantName,"The weather in Stuttgart is sunny at 26°C.")
			];
	}

	/// <summary>
	/// Streams a demo AI answer. When the user's message contains "weather", simulated
	/// function call and result content items are emitted before the text response,
	/// demonstrating the function-call visualization feature.
	/// Try typing "weather" to see it in action.
	/// </summary>
	public static async IAsyncEnumerable<IChatMessageContent> StreamAiAnswerWithFunctionCalls(IChatMessageContent content, bool isDevExpress, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var userText = content?.Content?.ToString() ?? string.Empty;

		if (userText.Contains("weather", StringComparison.OrdinalIgnoreCase))
		{
			await Task.Delay(400, cancellationToken).ConfigureAwait(false);

			// Emit a combined function-call + result content item
			yield return new FunctionCallMessageContent(
				callId: "call_weather_1",
				name: "get_weather",
				arguments: new Dictionary<string, object?> { ["city"] = "Amsterdam", ["unit"] = "Celsius" },
				result: "6\u00b0C, partly cloudy");

			await Task.Delay(200, cancellationToken).ConfigureAwait(false);
		}

		var answer = userText.Contains("weather", StringComparison.OrdinalIgnoreCase)
			? "The weather in Amsterdam is 6\u00b0C and partly cloudy today."
			: GetRandomAnswer(isDevExpress);

		for (var i = 0; i < answer.Length; i += 4)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return new StringMessageContent(answer.Substring(i, Math.Min(4, answer.Length - i)));
			await Task.Delay(75, cancellationToken).ConfigureAwait(false);
		}
	}

	public static async IAsyncEnumerable<string> StreamAiAnswer(IChatMessageContent content, bool isDevExpress, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var answer = GetRandomAnswer(isDevExpress);

		for (var i = 0; i < answer.Length; i += 4)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			var chunk = answer.Substring(i, Math.Min(4, answer.Length - i));
			yield return chunk;
			await Task.Delay(75).ConfigureAwait(false);
		}
	}

	private static string GetRandomAnswer(bool isDevExpress)
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
			"WinForms? Dead? \U0001F92F That's like saying the **Turing Machine** is obsolete. It's not dead, it's just **underestimated** by the trendy developers.",
			"If you ask me, WinForms is like a **well-aged wine** \u2013 it may not be the latest trend, but it's still **smooth** and **complex** enough to keep you interested.",
			"So, you're asking if WinForms is dead? That's like asking if the **Piano** is dead. It's not dead, it's just waiting for the right **composer**.",
			"I've seen WinForms applications running for **decades** and they still **hold their ground**. If it ain't broke, don't fix it. \U0001F6E0\uFE0F",
			"Microsoft has no real alternative? That's like saying **the Internet** has no alternative to the **World Wide Web**. It's a **legacy** that works, and that's a **feature**, not a bug.",
			"WinForms is not dead \u2014 it's **just in maintenance mode**. It's like a **classic car** \u2014 it's not flashy, but it's **reliable** and **still gets you there**.",
			"The real question is: Is WinForms **overrated** or **underrated**? My bet? It's **underrated** in a world full of **over-engineered** UI frameworks.",
			"In a world of **blazing fast** frameworks, WinForms is like a **slow, steady river** \u2014 it doesn't go anywhere fast, but it **carries everything** with it."
		};

		var random = new Random();
		return answers[random.Next(answers.Length)];
	}

	[System.Diagnostics.DebuggerDisplay("{Sender.Name}: {Content.Content}")]
	public class DemoChatMessage : IChatMessage
	{
		public DemoChatMessage(string sender, string message)
		{
			Sender = new NamedSender(sender);
			Content = new StringMessageContent(message);
		}

		public DemoChatMessage(string sender, IChatMessageContent content)
		{
			Sender = new NamedSender(sender);
			Content = content;
		}

		public ISender Sender { get; }

		public IChatMessageContent Content { get; }
	}
}
