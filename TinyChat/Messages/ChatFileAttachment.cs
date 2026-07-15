using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>
/// Represents a file that is attached to a chat message.
/// </summary>
public sealed class ChatFileAttachment
{
	private const string DEFAULT_MEDIA_TYPE = "application/octet-stream";
	private readonly object _openPathLock = new();
	private readonly string? _sourcePath;
	private string? _temporaryFilePath;

	private static readonly IReadOnlyDictionary<string, string> _mediaTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		[".avif"] = "image/avif",
		[".bmp"] = "image/bmp",
		[".csv"] = "text/csv",
		[".doc"] = "application/msword",
		[".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
		[".gif"] = "image/gif",
		[".heic"] = "image/heic",
		[".htm"] = "text/html",
		[".html"] = "text/html",
		[".jpeg"] = "image/jpeg",
		[".jpg"] = "image/jpeg",
		[".json"] = "application/json",
		[".md"] = "text/markdown",
		[".mp3"] = "audio/mpeg",
		[".mp4"] = "video/mp4",
		[".pdf"] = "application/pdf",
		[".png"] = "image/png",
		[".ppt"] = "application/vnd.ms-powerpoint",
		[".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
		[".rtf"] = "application/rtf",
		[".svg"] = "image/svg+xml",
		[".tif"] = "image/tiff",
		[".tiff"] = "image/tiff",
		[".tsv"] = "text/tab-separated-values",
		[".txt"] = "text/plain",
		[".wav"] = "audio/wav",
		[".webp"] = "image/webp",
		[".xls"] = "application/vnd.ms-excel",
		[".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
		[".xml"] = "application/xml",
		[".zip"] = "application/zip"
	};
	private static readonly HashSet<string> _textMediaTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"application/json",
		"application/ld+json",
		"application/sql",
		"application/xml",
		"application/yaml",
		"application/x-httpd-php",
		"application/x-sh",
		"application/x-yaml"
	};

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatFileAttachment"/> class.
	/// </summary>
	/// <param name="name">The file name shown to users and sent to AI services.</param>
	/// <param name="mediaType">The MIME type of the file.</param>
	/// <param name="data">The file contents.</param>
	public ChatFileAttachment(string name, string mediaType, ReadOnlyMemory<byte> data)
		: this(name, mediaType, data, null)
	{
	}

	private ChatFileAttachment(string name, string mediaType, ReadOnlyMemory<byte> data, string? sourcePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

		var fileName = Path.GetFileName(name);
		if (string.IsNullOrWhiteSpace(fileName))
			throw new ArgumentException("A valid file name is required.", nameof(name));

		Name = fileName;
		MediaType = mediaType;
		Data = data;
		_sourcePath = sourcePath;
	}

	/// <summary>
	/// Gets the file name without any local directory information.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the MIME type of the file.
	/// </summary>
	public string MediaType { get; }

	/// <summary>
	/// Gets the file contents.
	/// </summary>
	public ReadOnlyMemory<byte> Data { get; }

	/// <summary>
	/// Gets whether this attachment is an image.
	/// </summary>
	public bool IsImage => MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Gets whether this attachment contains text that can be passed directly to a language model.
	/// </summary>
	public bool IsText => MediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || _textMediaTypes.Contains(MediaType);

	/// <summary>
	/// Loads an attachment from disk. The local path is retained privately so that the file can be opened later.
	/// </summary>
	/// <param name="filePath">The path of the file to load.</param>
	/// <param name="maximumFileSize">The maximum allowed file size in bytes, or <see langword="null"/> for no limit.</param>
	/// <param name="cancellationToken">The token used to cancel file loading.</param>
	/// <returns>The loaded attachment.</returns>
	public static async Task<ChatFileAttachment> LoadAsync(
		string filePath,
		long? maximumFileSize = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
		if (maximumFileSize < 0)
			throw new ArgumentOutOfRangeException(nameof(maximumFileSize));

		var fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists)
			throw new FileNotFoundException("The attachment file was not found.", filePath);

		if (maximumFileSize is { } maximum && fileInfo.Length > maximum)
			throw new IOException($"The file '{fileInfo.Name}' exceeds the maximum attachment size of {maximum:N0} bytes.");

		var data = await File.ReadAllBytesAsync(fileInfo.FullName, cancellationToken).ConfigureAwait(false);
		if (maximumFileSize is { } actualMaximum && data.LongLength > actualMaximum)
			throw new IOException($"The file '{fileInfo.Name}' exceeds the maximum attachment size of {actualMaximum:N0} bytes.");

		var mediaType = _mediaTypes.GetValueOrDefault(fileInfo.Extension, DEFAULT_MEDIA_TYPE);
		return new ChatFileAttachment(fileInfo.Name, mediaType, data, fileInfo.FullName);
	}

	/// <summary>
	/// Opens the attachment with the operating system's default application.
	/// </summary>
	/// <remarks>
	/// Attachments created from in-memory data are written to a temporary file before they are opened.
	/// </remarks>
	public void OpenWithDefaultApplication()
	{
		var filePath = _sourcePath is not null && File.Exists(_sourcePath)
			? _sourcePath
			: GetOrCreateTemporaryFile();

		Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
	}

	/// <summary>
	/// Converts the attachment to the standard Microsoft.Extensions.AI binary content representation.
	/// </summary>
	/// <returns>The AI content for the attachment.</returns>
	public DataContent ToDataContent() => new(Data, MediaType) { Name = Name };

	/// <summary>
	/// Decodes a text attachment, including UTF byte-order-mark detection.
	/// </summary>
	/// <returns>The decoded file contents.</returns>
	/// <exception cref="InvalidOperationException">The attachment is not a known text format.</exception>
	public string GetText()
	{
		if (!IsText)
			throw new InvalidOperationException($"The attachment '{Name}' is not a text file.");

		using var stream = new MemoryStream(Data.ToArray(), writable: false);
		using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		return reader.ReadToEnd();
	}

	private string GetOrCreateTemporaryFile()
	{
		lock (_openPathLock)
		{
			if (_temporaryFilePath is not null && File.Exists(_temporaryFilePath))
				return _temporaryFilePath;

			var directoryPath = Path.Combine(Path.GetTempPath(), "TinyChat", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directoryPath);
			_temporaryFilePath = Path.Combine(directoryPath, Name);
			File.WriteAllBytes(_temporaryFilePath, Data.ToArray());
			return _temporaryFilePath;
		}
	}
}
