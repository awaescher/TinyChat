using TinyChat;

namespace WinFormsDemo;

/// <summary>
/// A demonstration form showing IChatClient integration backed by a real Ollama model
/// via OllamaSharp and Microsoft.Extensions.AI, with tools for time and weather.
/// </summary>
public partial class OllamaDemoForm : Form
{
	private ChatControl chatControl = null!;
	private CheckBox streamingCheckBox = null!;
	private Label statusLabel = null!;
	private Button newChatButton = null!;

	public OllamaDemoForm()
	{
		InitializeComponent();
		SetupForm();
		_ = InitializeOllamaAsync();
	}

	private void SetupForm()
	{
		this.Text = $"TinyChat - Ollama Demo ({TestIChatClientDemo.ModelName})";
		this.Size = new Size(800, 600);
		this.StartPosition = FormStartPosition.CenterScreen;

		var topPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 60,
			BackColor = SystemColors.Control
		};

		statusLabel = new Label
		{
			Text = $"Connecting to Ollama and loading model '{TestIChatClientDemo.ModelName}'...",
			Location = new Point(10, 10),
			Size = new Size(760, 18),
			AutoSize = false
		};
		topPanel.Controls.Add(statusLabel);

		streamingCheckBox = new CheckBox
		{
			Text = "Use Streaming",
			Location = new Point(10, 33),
			Checked = true,
			Width = 130
		};
		streamingCheckBox.CheckedChanged += (s, e) =>
		{
			if (chatControl != null)
				chatControl.UseStreaming = streamingCheckBox.Checked;
		};
		topPanel.Controls.Add(streamingCheckBox);

		newChatButton = new Button
		{
			Text = "New Chat",
			Location = new Point(150, 30),
			Width = 90,
			Height = 26
		};
		newChatButton.Click += (s, e) => chatControl.Messages = [];
		topPanel.Controls.Add(newChatButton);

		this.Controls.Add(topPanel);

		chatControl = new ChatControl
		{
			Dock = DockStyle.Fill,
			UseStreaming = true,
			AssistantSenderName = TestIChatClientDemo.ModelName,
			IncludeFunctionCalls = true,
			ChatOptions = TestIChatClientDemo.CreateChatOptions(),
			Enabled = false
		};

		this.Controls.Add(chatControl);
	}

	private async Task InitializeOllamaAsync()
	{
		var progress = new Progress<string>(msg =>
		{
			if (statusLabel.IsHandleCreated)
				statusLabel.Invoke(() => statusLabel.Text = msg);
		});

		try
		{
			var serviceProvider = await TestIChatClientDemo.CreateServiceProviderWithOllamaChatClientAsync(progress);

			chatControl.Invoke(() =>
			{
				chatControl.ServiceProvider = serviceProvider;
				chatControl.Enabled = true;
				statusLabel.Text = $"Model '{TestIChatClientDemo.ModelName}' ready. Ask about the time or weather!";
			});
		}
		catch (Exception ex)
		{
			statusLabel.Invoke(() =>
				statusLabel.Text = $"Error: {ex.Message} — make sure Ollama is running on http://localhost:11434");
		}
	}

	private void InitializeComponent()
	{
		this.SuspendLayout();
		this.ResumeLayout(false);
	}
}
