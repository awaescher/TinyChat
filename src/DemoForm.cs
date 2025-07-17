
using WinFormsChat.Messages;

namespace WinFormsChat;

public partial class DemoForm : Form
{
	public DemoForm()
	{
		InitializeComponent();
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);

		chatControl1.DataContext = new List<IChatMessage>([
			new DemoChatMessage("Andreas", "Hey, bist du da?"),
			new DemoChatMessage("Qwen3:32b","Ja, klar. Wie kann ich dir heute helfen?"),
			new DemoChatMessage("Andreas", "Kannst du mir sagen, wer die CHG ist?"),
			new DemoChatMessage("Qwen3:32b", "Klar. Die CHG-MERIDIAN AG ist eine Firma mit Sitz in Weingarten.\n" +
			"Sie hat Standorte in über 25 verschiedenen Ländern.\n" +
			"Soll ich fortfahren?"),
			new DemoChatMessage("Andreas", "Nein danke, das genügt."),
			new DemoChatMessage("Qwen3:32b", "Alles klar. Melde dich einfach, wenn ich noch was tun kann.")
			]);
	}
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
