using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>
/// Displays compact, clickable previews for file attachments.
/// </summary>
public class FileAttachmentPreviewPanel : FlowLayoutPanel
{
	private static readonly Size _previewSize = new(64, 64);
	private readonly IAttachmentToolTip _previewToolTip;
	private readonly List<Control> _unavailablePreviews = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="FileAttachmentPreviewPanel"/> class.
	/// </summary>
	/// <param name="content">The attachment content to preview.</param>
	public FileAttachmentPreviewPanel(FileAttachmentMessageContent content)
		: this()
	{
		foreach (var attachment in content.Attachments)
			Controls.Add(CreateAttachmentPreview(attachment));
	}

	private FileAttachmentPreviewPanel()
	{
		_previewToolTip = CreateToolTip();
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		FlowDirection = FlowDirection.LeftToRight;
		Margin = Padding.Empty;
		Padding = new Padding(0, 4, 0, 0);
		WrapContents = true;
		Disposed += (_, _) => _previewToolTip.Dispose();

	}

	/// <summary>Displays binary attachments from model responses, tool results or restored history.</summary>
	public FileAttachmentPreviewPanel(AIMessageContent content)
		: this()
	{
		foreach (var attachment in GetAttachments(content))
			Controls.Add(CreateAttachmentPreview(attachment));
		foreach (var uri in content.Contents.OfType<UriContent>())
		{
			if (uri.Uri.Scheme is "https" or "http")
				Controls.Add(CreateUriPreview(uri));
			else if (uri.Uri.Scheme == "data")
			{
				try
				{
					foreach (var attachment in GetAttachments(new AIMessageContent([new DataContent(uri.Uri.AbsoluteUri)])))
						Controls.Add(CreateAttachmentPreview(attachment));
				}
				catch (ArgumentException)
				{
					Controls.Add(CreateUnavailablePreview());
				}
				catch (FormatException)
				{
					Controls.Add(CreateUnavailablePreview());
				}
			}
		}
	}

	private Control CreateUnavailablePreview()
	{
		var explanation = ChatLocalization.GetString("PreviewUnavailable");
		var icon = CreateUnavailableIcon();
		icon.AccessibleName = explanation;
		_previewToolTip.Set(icon, string.Empty, explanation);
		_unavailablePreviews.Add(icon);
		return icon;
	}

	internal virtual Control CreateUnavailableIcon()
	{
		var image = ChatStatusImage.CreateError(new Size(16, 16), ChatIcons.ErrorColor);
		var icon = new PictureBox { Image = image, Size = new Size(18, 18), SizeMode = PictureBoxSizeMode.CenterImage };
		icon.Disposed += (_, _) => image.Dispose();
		return icon;
	}

	internal void ApplyPresentation(ChatControl owner)
	{
		foreach (var icon in _unavailablePreviews)
		{
			icon.AccessibleName = owner.DisplayTexts.PreviewUnavailable;
			_previewToolTip.Set(icon, string.Empty, icon.AccessibleName!);
		}
	}

	internal virtual IAttachmentToolTip CreateToolTip() => new AttachmentToolTip();

	internal static bool HasAttachments(AIMessageContent content) => content.Contents.Any(item => item is DataContent or UriContent);

	private static Control CreateUriPreview(UriContent content)
	{
		Control control;
		if (content.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
		{
			var picture = new PictureBox { Size = _previewSize, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
			picture.LoadAsync(content.Uri.AbsoluteUri);
			picture.Disposed += (_, _) => picture.Image?.Dispose();
			control = picture;
		}
		else
			control = new Label { AutoSize = true, Text = content.Uri.AbsoluteUri };
		control.AccessibleName = content.Uri.AbsoluteUri;
		control.Cursor = Cursors.Hand;
		control.Margin = new Padding(0, 0, 6, 0);
		control.Click += (_, _) =>
		{
			try
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(content.Uri.AbsoluteUri) { UseShellExecute = true });
			}
			catch (System.ComponentModel.Win32Exception)
			{
			}
		};
		return control;
	}

	internal static IEnumerable<ChatFileAttachment> GetAttachments(AIMessageContent content)
	{
		var index = 0;
		foreach (var data in content.Contents.OfType<DataContent>())
		{
			index++;
			var extension = data.MediaType switch
			{
				"image/png" => ".png",
				"image/jpeg" => ".jpg",
				"image/gif" => ".gif",
				"image/webp" => ".webp",
				"application/pdf" => ".pdf",
				_ => ".bin"
			};
			yield return new ChatFileAttachment(data.Name ?? $"attachment-{index}{extension}", data.MediaType, data.Data);
		}
	}

	/// <summary>
	/// Gets whether at least one attachment preview is available.
	/// </summary>
	public bool HasPreviews => Controls.Count > 0;

	/// <inheritdoc />
	protected override void OnLayout(LayoutEventArgs levent)
	{
		foreach (var card in Controls.OfType<AttachmentCard>())
			card.MaximumSize = new Size(MaximumSize.Width > 0 ? Math.Max(1, MaximumSize.Width - Padding.Horizontal - card.Margin.Horizontal) : 0, 0);
		base.OnLayout(levent);
	}

	/// <summary>Creates a card. Override to use another control suite or a custom preview.</summary>
	protected virtual Control CreateAttachmentPreview(ChatFileAttachment attachment) => new AttachmentCard(attachment);

}
