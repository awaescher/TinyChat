namespace TinyChat;

/// <summary>
/// Displays compact, clickable previews for file attachments.
/// </summary>
public sealed class FileAttachmentPreviewPanel : FlowLayoutPanel
{
	private static readonly Size _previewSize = new(64, 64);

	/// <summary>
	/// Initializes a new instance of the <see cref="FileAttachmentPreviewPanel"/> class.
	/// </summary>
	/// <param name="content">The attachment content to preview.</param>
	public FileAttachmentPreviewPanel(FileAttachmentMessageContent content)
	{
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		FlowDirection = FlowDirection.LeftToRight;
		Margin = Padding.Empty;
		Padding = new Padding(0, 4, 0, 0);
		WrapContents = true;

		foreach (var attachment in content.Attachments)
		{
			Controls.Add(attachment.IsImage
				? CreateImagePreview(attachment)
				: CreateFilePreview(attachment));
		}
	}

	/// <summary>
	/// Gets whether at least one attachment preview is available.
	/// </summary>
	public bool HasPreviews => Controls.Count > 0;

	private static PictureBox CreateImagePreview(ChatFileAttachment attachment)
	{
		var image = FileAttachmentImageFactory.Create(attachment);
		var preview = new PictureBox
		{
			AccessibleName = attachment.Name,
			BorderStyle = BorderStyle.FixedSingle,
			Cursor = Cursors.Hand,
			Image = image,
			Margin = new Padding(0, 0, 6, 0),
			Size = _previewSize,
			SizeMode = PictureBoxSizeMode.Zoom,
			TabStop = false
		};
		WireOpenWithDefaultApplication(preview, attachment);
		preview.Disposed += (_, _) => image.Dispose();
		return preview;
	}

	private static Control CreateFilePreview(ChatFileAttachment attachment)
	{
		var tile = new Panel
		{
			AccessibleName = attachment.Name,
			BorderStyle = BorderStyle.FixedSingle,
			Cursor = Cursors.Hand,
			Margin = new Padding(0, 0, 6, 0),
			Size = new Size(180, 40)
		};
		var nameLabel = new Label
		{
			AutoEllipsis = true,
			Cursor = Cursors.Hand,
			Dock = DockStyle.Fill,
			Padding = new Padding(4, 0, 4, 0),
			Text = attachment.Name,
			TextAlign = ContentAlignment.MiddleLeft,
			UseMnemonic = false
		};
		var icon = new FileAttachmentIconControl
		{
			Cursor = Cursors.Hand,
			Dock = DockStyle.Left,
			Width = 30
		};
		tile.Controls.Add(nameLabel);
		tile.Controls.Add(icon);
		WireOpenWithDefaultApplication(tile, attachment);
		WireOpenWithDefaultApplication(nameLabel, attachment);
		WireOpenWithDefaultApplication(icon, attachment);
		return tile;
	}

	private static void WireOpenWithDefaultApplication(Control control, ChatFileAttachment attachment)
	{
		control.Click += (_, _) =>
		{
			try
			{
				attachment.OpenWithDefaultApplication();
			}
			catch
			{
				// Opening files is delegated to the operating system and may fail without an associated application.
			}
		};
	}

}
