using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace TinyChat;

/// <summary>
/// Represents a chat message containing one or more file attachments and optional text.
/// </summary>
public sealed class FileAttachmentMessageContent : IChatMessageContent
{
	private readonly string _displayText;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileAttachmentMessageContent"/> class.
	/// </summary>
	/// <param name="attachments">The files attached to the message.</param>
	/// <param name="text">Optional text accompanying the files.</param>
	public FileAttachmentMessageContent(IEnumerable<ChatFileAttachment> attachments, string? text = null)
	{
		ArgumentNullException.ThrowIfNull(attachments);

		Attachments = attachments.ToArray();
		if (Attachments.Count == 0)
			throw new ArgumentException("At least one attachment is required.", nameof(attachments));

		Text = text ?? string.Empty;
		_displayText = CreateDisplayText();
	}

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged
	{
		add
		{
		}
		remove
		{
		}
	}

	/// <summary>
	/// Gets the optional text accompanying the attachments.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// Gets the attached files.
	/// </summary>
	public IReadOnlyList<ChatFileAttachment> Attachments { get; }

	/// <inheritdoc />
	public object Content => _displayText;

	/// <inheritdoc />
	public override string ToString() => _displayText;

	private string CreateDisplayText()
	{
		var builder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(Text))
		{
			builder.AppendLine(Text.Trim());
			builder.AppendLine();
		}

		foreach (var attachment in Attachments.Where(attachment => !attachment.IsImage))
		{
			builder.Append(attachment.Name);
			builder.Append(" (");
			builder.Append(FormatFileSize(attachment.Data.Length));
			builder.AppendLine(")");
		}

		return builder.ToString().TrimEnd();
	}

	private static string FormatFileSize(long byteCount)
	{
		string[] units = ["B", "KB", "MB", "GB"];
		var size = (double)byteCount;
		var unitIndex = 0;

		while (size >= 1024 && unitIndex < units.Length - 1)
		{
			size /= 1024;
			unitIndex++;
		}

		var format = unitIndex == 0 ? "0" : "0.#";
		return $"{size.ToString(format, CultureInfo.CurrentCulture)} {units[unitIndex]}";
	}
}
