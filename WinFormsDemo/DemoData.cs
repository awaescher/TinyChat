using TinyChat;

namespace WinFormsDemo;

public class DemoData
{
	public static IEnumerable<DemoChatMessage> Create(string currentUser)
	{
		return [
			new DemoChatMessage(currentUser, "Hey, someone there?"),
			new DemoChatMessage("Assistant","Yes, I am here. How can I help you today?"),
			new DemoChatMessage(currentUser, "What do you think, is WinForms dead?"),
			new DemoChatMessage("Assistant", "While many people would claim WinForms is an outdated technology, I think it has its raison d'etre.\n" +
			"It's still heavily used in business scenarios and with the help of many control vendors, such as DevExpress or Telerik, it'a still a very valid way of building business applications.\n" +
			"As long as Microsoft has no real alternative, I would not say WinForms is dead."),
			new DemoChatMessage(currentUser, "Wait, you say Microsoft has no alternative for a technology that has been there since 2001?"),
			new DemoChatMessage("Assistant", "Absolutely. Microsoft has no go-to technology for building Windows apps.\n"+
			"They introduced WPF in 2006 but it's been discontinued only a few years later.\n" +
			"Then they introduced UWP in 2015 which was heavily restricted to building Microsoft Store apps and discontinued soon after.\n" +
			"Afterwards they introduced WinUI2, discontinued it and introduced the incompatible WinUI3. This is still the most recommended way but still very restricted in comparison to WinForms and WPF when it comes to application deployment.")
			];
	}

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
