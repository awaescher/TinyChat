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
					// an empty array is an ordinary result (e.g. an empty list) and carries no content to restore
					if (element.GetArrayLength() == 0)
						return null;

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

	/// <summary>
	/// Recognizes tool results for display, including Model Context Protocol content blocks
	/// (for example <c>{"type":"image","data":"…","mimeType":"image/png"}</c>), call tool results
	/// with a <c>content</c> array and plain data URIs.
	/// </summary>
	internal static AIMessageContent? FromDisplayResult(object? result)
	{
		if (FromResult(result) is { } content)
			return content;

		if (result is string text)
		{
			var trimmed = text.Trim();
			if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && !trimmed.Any(char.IsWhiteSpace))
			{
				try
				{
					return new AIMessageContent([new DataContent(trimmed)]);
				}
				catch (ArgumentException)
				{
					return null;
				}
				catch (FormatException)
				{
					return null;
				}
			}

			if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
				return null;

			try
			{
				using var document = JsonDocument.Parse(trimmed);
				return FromContentBlocks(document.RootElement);
			}
			catch (JsonException)
			{
				return null;
			}
		}

		return result is JsonElement element ? FromContentBlocks(element) : null;
	}

	private static AIMessageContent? FromContentBlocks(JsonElement element)
	{
		var blocks = element.ValueKind switch
		{
			JsonValueKind.Object when element.TryGetProperty("content", out var nested) && nested.ValueKind == JsonValueKind.Array => nested,
			JsonValueKind.Object when element.TryGetProperty("type", out _) => element,
			JsonValueKind.Array => element,
			_ => default
		};
		if (blocks.ValueKind == JsonValueKind.Undefined)
			return null;

		var parts = new List<AIContent>();
		foreach (var block in blocks.ValueKind == JsonValueKind.Array ? blocks.EnumerateArray() : Enumerable.Repeat(blocks, 1))
		{
			if (block.ValueKind != JsonValueKind.Object || !block.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
				return null;

			parts.Add(FromContentBlock(block, type.GetString()) ?? new TextContent(block.GetRawText()));
		}

		return parts.Count > 0 && (parts.Any(part => part is not TextContent) ||
			(element.ValueKind == JsonValueKind.Object && element.TryGetProperty("content", out _)))
			? new AIMessageContent(parts)
			: null;
	}

	private static AIContent? FromContentBlock(JsonElement block, string? type)
	{
		try
		{
			switch (type)
			{
				case "text":
					return GetString(block, "text") is { } text ? new TextContent(text) : null;

				case "image":
				case "audio":
					return GetString(block, "data") is { } data && GetString(block, "mimeType") is { } mediaType
						? new DataContent(Convert.FromBase64String(data), mediaType)
						: null;

				case "resource_link":
					return GetString(block, "uri") is { } link && Uri.TryCreate(link, UriKind.Absolute, out var linkUri)
						? new UriContent(linkUri, GetString(block, "mimeType") ?? "application/octet-stream")
						: null;

				case "resource":
					if (!block.TryGetProperty("resource", out var resource) || resource.ValueKind != JsonValueKind.Object)
						return null;
					if (GetString(resource, "text") is { } resourceText)
						return new TextContent(resourceText);
					if (GetString(resource, "blob") is not { } blob)
						return null;

					var name = GetString(resource, "uri") is { } resourceUri && Uri.TryCreate(resourceUri, UriKind.Absolute, out var parsedUri)
						? Path.GetFileName(parsedUri.LocalPath)
						: null;
					return new DataContent(Convert.FromBase64String(blob), GetString(resource, "mimeType") ?? "application/octet-stream")
					{
						Name = string.IsNullOrWhiteSpace(name) ? null : name
					};

				default:
					return null;
			}
		}
		catch (FormatException)
		{
			return null;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private static string? GetString(JsonElement element, string propertyName) =>
		element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
