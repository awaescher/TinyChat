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
			"See also: [Microsoft Documentation](https://learn.microsoft.com)")
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
			"Which can be `invaluable` for <b>business-critical</b> applications."
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
