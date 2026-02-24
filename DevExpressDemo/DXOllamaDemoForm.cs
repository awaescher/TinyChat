using System;
using System.Threading.Tasks;
using DevExpress.XtraEditors;
using WinFormsDemo;

namespace DevExpressDemo;

/// <summary>
/// A DevExpress demonstration form showing IChatClient integration backed by a real Ollama model
/// via OllamaSharp and Microsoft.Extensions.AI, with tools for time and weather.
/// </summary>
public partial class DXOllamaDemoForm : XtraForm
{
	public DXOllamaDemoForm()
	{
		InitializeComponent();
		Text = $"TinyChat – Ollama Demo [{TestIChatClientDemo.ModelName}] (DevExpress)";
		statusLabel.Text = $"Connecting to Ollama and loading model '{TestIChatClientDemo.ModelName}'…";
		chatControl.AssistantSenderName = TestIChatClientDemo.ModelName;
		chatControl.ChatOptions = TestIChatClientDemo.CreateChatOptions();
		_ = InitializeOllamaAsync();
	}

	private void StreamingCheckEdit_CheckedChanged(object? sender, EventArgs e)
	{
		chatControl.UseStreaming = streamingCheckEdit.Checked;
	}

	private void NewChatButton_Click(object? sender, EventArgs e)
	{
		chatControl.Messages = [];
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
}
