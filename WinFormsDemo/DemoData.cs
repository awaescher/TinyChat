using System.Runtime.CompilerServices;
using TinyChat;

namespace WinFormsDemo;

public class DemoData
{
	public static string AssistantName { get; set; } = "Assistant";

	public static IEnumerable<DemoChatMessage> Create(string currentUser)
	{
		return [
			new DemoChatMessage(currentUser, "Hey, someone there?"),
			new DemoChatMessage(AssistantName,"Yes, I am here. How can I help you today?"),
			new DemoChatMessage(currentUser, "What do you think, is WinForms dead?"),
			new DemoChatMessage(AssistantName, "While many people would claim WinForms is an outdated technology, I think it has its raison d'etre.\n" +
			"It's still heavily used in business scenarios and with the help of many control vendors, such as DevExpress or Telerik, it'a still a very valid way of building business applications.\n" +
			"As long as Microsoft has no real alternative, I would not say WinForms is dead."),
			new DemoChatMessage(currentUser, "Wait, you say Microsoft has no alternative for a technology that has been there since 2001?"),
			new DemoChatMessage(AssistantName, "Absolutely. Microsoft has no go-to technology for building Windows apps.\n"+
			"They introduced WPF in 2006 but it's been discontinued only a few years later.\n" +
			"Then they introduced UWP in 2015 which was heavily restricted to building Microsoft Store apps and discontinued soon after.\n" +
			"Afterwards they introduced WinUI2, discontinued it and introduced the incompatible WinUI3. This is still the most recommended way but still very restricted in comparison to WinForms and WPF when it comes to application deployment.")
			];
	}

	public static async IAsyncEnumerable<string> StreamAiAnswer([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var answers = new[]
		{
			"Well that is true, nothing to argue about.",
			"I think the key factor here is understanding the context and requirements of your specific project. Different technologies serve different purposes, and what works best depends on your team's expertise, timeline, and long-term maintenance considerations.",
			"Absolutely, this is a common concern in software development. The technology landscape evolves rapidly, but established frameworks often have strong ecosystems and community support that keep them viable for many years. It's important to weigh the benefits of stability against the advantages of newer approaches.",
			"Not necessarily, as it depends on several factors including your target audience and deployment requirements.",
			"From my perspective, the choice between different UI frameworks should be based on practical considerations rather than just following trends. Legacy technologies often provide robust solutions with extensive documentation and proven track records in production environments, which can be invaluable for business-critical applications."
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
