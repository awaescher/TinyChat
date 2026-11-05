using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>Preserves structured model content for display and subsequent requests, including restored history.</summary>
public sealed class AIMessageContent(IEnumerable<AIContent> contents) : IChatMessageContent
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

	/// <summary>Gets the original content items in order.</summary>
	public IReadOnlyList<AIContent> Contents { get; } = contents.ToArray();

	/// <summary>Gets the text portion without serializing binary content.</summary>
	public string Text => string.Join(Environment.NewLine, Contents.OfType<TextContent>().Select(content => content.Text));

	/// <inheritdoc />
	public object Content => Text;

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

	/// <inheritdoc />
	public override string ToString() => Text;

	/// <summary>Recognizes structured tool results, including their JSON representation after transport or persistence.</summary>
	public static AIMessageContent? FromResult(object? result)
	{
		if (result is AIMessageContent content)
			return content;
		if (result is Microsoft.Extensions.AI.ChatMessage message)
			return new AIMessageContent(message.Contents);
		if (result is AIContent item)
			return new AIMessageContent([item]);
		if (result is IEnumerable<AIContent> items)
			return new AIMessageContent(items);
		if (result is string text && (text.TrimStart().StartsWith('{') || text.TrimStart().StartsWith('[')))
		{
			try
			{
				using var document = JsonDocument.Parse(text);
				return FromResult(document.RootElement);
			}
			catch (JsonException)
			{
				return null;
			}
		}
		if (result is JsonElement element)
		{
			try
			{
				if (element.ValueKind == JsonValueKind.Object)
				{
					if (element.TryGetProperty("contents", out var nested) || element.TryGetProperty("Contents", out nested))
						return FromResult(nested);
					if (element.TryGetProperty("$type", out _))
						return new AIMessageContent([element.Deserialize<AIContent>(_jsonOptions)!]);
					if (element.TryGetProperty("uri", out var uriValue) && uriValue.ValueKind == JsonValueKind.String && Uri.TryCreate(uriValue.GetString(), UriKind.Absolute, out var uri))
					{
						var mediaType = element.TryGetProperty("mediaType", out var media) && media.ValueKind == JsonValueKind.String ? media.GetString() ?? "application/octet-stream" : "application/octet-stream";
						AIContent mediaContent = uri.Scheme == "data" ? new DataContent(uri.AbsoluteUri) : new UriContent(uri, mediaType);
						if (mediaContent is DataContent data && element.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
							data.Name = name.GetString();
						return new AIMessageContent([mediaContent]);
					}
				}
				if (element.ValueKind == JsonValueKind.Array)
				{
					var parts = new List<AIContent>();
					foreach (var part in element.EnumerateArray())
					{
						if (FromResult(part) is not { } structured)
							return null;
						parts.AddRange(structured.Contents);
					}
					return new AIMessageContent(parts);
				}
			}
			catch (JsonException)
			{
				// Unknown content versions remain available through the ordinary JSON/text result view.
			}
			catch (NotSupportedException)
			{
			}
			catch (ArgumentException)
			{
			}
			catch (FormatException)
			{
			}
		}
		return null;
	}
}
