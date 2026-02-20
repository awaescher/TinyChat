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
		_statusLabel.Text = $"Connecting to Ollama and loading model '{TestIChatClientDemo.ModelName}'…";
		_chatControl.AssistantSenderName = TestIChatClientDemo.ModelName;
		_chatControl.ChatOptions = TestIChatClientDemo.CreateChatOptions();
		_ = InitializeOllamaAsync();
	}

	private void StreamingCheckEdit_CheckedChanged(object? sender, EventArgs e)
	{
		_chatControl.UseStreaming = _streamingCheckEdit.Checked;
	}

	private void NewChatButton_Click(object? sender, EventArgs e)
	{
		_chatControl.Messages = [];
	}

	private async Task InitializeOllamaAsync()
	{
		var progress = new Progress<string>(msg =>
		{
			if (_statusLabel.IsHandleCreated)
				_statusLabel.Invoke(() => _statusLabel.Text = msg);
		});

		try
		{
			var serviceProvider = await TestIChatClientDemo.CreateServiceProviderWithOllamaChatClientAsync(progress);

			_chatControl.Invoke(() =>
			{
				_chatControl.ServiceProvider = serviceProvider;
				_chatControl.Enabled = true;
				_statusLabel.Text = $"Model '{TestIChatClientDemo.ModelName}' ready. Ask about the time or weather!";
			});
		}
		catch (Exception ex)
		{
			_statusLabel.Invoke(() =>
				_statusLabel.Text = $"Error: {ex.Message} — make sure Ollama is running on http://localhost:11434");
		}
	}
}
