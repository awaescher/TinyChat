using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoApp;

/// <summary>
/// A demonstration form showing IChatClient integration backed by a real Ollama model
/// via OllamaSharp and Microsoft.Extensions.AI, with tools for time and weather.
/// </summary>
public partial class NativeOllamaDemoForm : Form
{
	private string _selectedModel = OllamaDemo.MODELNAME;

	public NativeOllamaDemoForm()
	{
		InitializeComponent();
		Text = $"TinyChat - Ollama WinForms Demo";
		statusLabel.Text = $"Connecting to Ollama and loading model '{_selectedModel}'...";
		chatControl.AssistantSenderName = _selectedModel;
		chatControl.ChatOptions = OllamaDemo.CreateChatOptions();
		_ = InitializeOllamaAsync();
	}

	private void StreamingCheckBox_CheckedChanged(object? sender, EventArgs e)
	{
		chatControl.UseStreaming = streamingCheckBox.Checked;
	}

	private void NewChatButton_Click(object? sender, EventArgs e)
	{
		chatControl.StartNewChat();
	}

	private void ModelComboBox_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (modelComboBox.SelectedItem is string model && !string.Equals(model, _selectedModel, StringComparison.Ordinal))
		{
			_selectedModel = model;
			chatControl.StartNewChat();
			chatControl.AssistantSenderName = _selectedModel;
			_ = InitializeOllamaAsync();
		}
	}

	private async Task InitializeOllamaAsync()
	{
		chatControl.Enabled = false;

		var progress = new Progress<string>(msg =>
		{
			if (statusLabel.IsHandleCreated)
				statusLabel.Invoke(() => statusLabel.Text = msg);
		});

		try
		{
			var serviceProvider = await OllamaDemo.CreateServiceProviderWithOllamaChatClientAsync(_selectedModel, progress);

			chatControl.Invoke(() =>
			{
				chatControl.ServiceProvider = serviceProvider;
				chatControl.Enabled = true;
				statusLabel.Text = $"Model '{_selectedModel}' ready. Ask about the time or weather!";
				streamingCheckBox.Visible = true;
				newChatButton.Visible = true;
			});

			await LoadModelListAsync();
		}
		catch (Exception ex)
		{
			statusLabel.Invoke(() => statusLabel.Text = $"Error: {ex.Message} — make sure Ollama is running on http://localhost:11434");
		}
	}

	private async Task LoadModelListAsync()
	{
		if (modelComboBox.Items.Count > 0)
			return;

		try
		{
			var models = await OllamaDemo.ListLocalModelNamesAsync();

			modelComboBox.Invoke(() =>
			{
				modelComboBox.Items.AddRange(models);
				modelComboBox.SelectedItem = _selectedModel;
				modelComboBox.Visible = true;
			});
		}
		catch
		{
			// Model list is optional — ignore errors silently.
		}
	}
}
