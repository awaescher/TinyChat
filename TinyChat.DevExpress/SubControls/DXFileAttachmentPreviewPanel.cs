namespace TinyChat;

/// <summary>Displays attachment cards using DevExpress labels, panels, buttons and image editors.</summary>
public class DXFileAttachmentPreviewPanel : FileAttachmentPreviewPanel
{
	/// <inheritdoc />
	public DXFileAttachmentPreviewPanel(FileAttachmentMessageContent content) : base(content) { }
	/// <inheritdoc />
	public DXFileAttachmentPreviewPanel(AIMessageContent content) : base(content) { }
	internal override Control CreateUnavailableIcon() => new DXStatusIcon(ChatNoticeSeverity.Error);

	internal override IAttachmentToolTip CreateToolTip() => new DXAttachmentToolTip();

	/// <inheritdoc />
	protected override Control CreateAttachmentPreview(ChatFileAttachment attachment) => new DXAttachmentCard(attachment);
}
