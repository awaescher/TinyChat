using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace WinFormsDemo;

/// <summary>
/// Demonstrates IChatClient integration using OllamaSharp with Microsoft.Extensions.AI,
/// including tool support for getting the current time and weather.
/// </summary>
public static class TestIChatClientDemo
{
	public const string ModelName = "qwen3:0.6b";

	/// <summary>
	/// Creates a service provider backed by a real Ollama IChatClient with function invocation enabled.
	/// The model is pulled automatically if it is not yet available locally.
	/// </summary>
	public static async Task<IServiceProvider> CreateServiceProviderWithOllamaChatClientAsync(
		IProgress<string>? progress = null,
		CancellationToken cancellationToken = default)
	{
		var ollamaClient = new OllamaApiClient(new Uri("http://localhost:11434"), ModelName);

		await EnsureModelAvailableAsync(ollamaClient, progress, cancellationToken);

		var services = new ServiceCollection();

		services.AddChatClient((IChatClient)ollamaClient)
			.UseFunctionInvocation();

		return services.BuildServiceProvider();
	}

	/// <summary>
	/// Returns ChatOptions pre-configured with the available tools.
	/// </summary>
	public static ChatOptions CreateChatOptions() => new()
	{
		Tools =
		[
			AIFunctionFactory.Create(GetCurrentTime),
			AIFunctionFactory.Create(GetCurrentWeather)
		]
	};

	private static async Task EnsureModelAvailableAsync(
		OllamaApiClient client,
		IProgress<string>? progress,
		CancellationToken cancellationToken)
	{
		var models = await client.ListLocalModelsAsync(cancellationToken);
		var isAvailable = models.Any(m => m.Name.StartsWith(ModelName, StringComparison.OrdinalIgnoreCase));

		if (!isAvailable)
		{
			progress?.Report($"Model '{ModelName}' not found locally. Downloading...");

			await foreach (var status in client.PullModelAsync(ModelName, cancellationToken))
			{
				if (!string.IsNullOrWhiteSpace(status?.Status))
					progress?.Report(status.Status);
			}

			progress?.Report($"Model '{ModelName}' is ready.");
		}
		else
		{
			progress?.Report($"Model '{ModelName}' is available.");
		}
	}

	[Description("Gets the current local date and time.")]
	private static string GetCurrentTime() =>
		DateTime.Now.ToString("ddd, MMM d yyyy HH:mm:ss");

	[Description("Gets the current weather for a given city with randomised data.")]
	private static async Task<string> GetCurrentWeather(
		[Description("The city name to get the weather for")] string city)
	{
		await Task.Delay(2000);

		var rng = new Random();
		var conditions = new[] { "sunny", "partly cloudy", "overcast", "light rain", "heavy rain", "snow", "foggy", "windy" };
		var temp = rng.Next(-10, 38);
		var humidity = rng.Next(30, 95);
		var condition = conditions[rng.Next(conditions.Length)];
		return $"Weather in {city}: {temp}\u00b0C, {condition}, humidity {humidity}%.";
	}
}
