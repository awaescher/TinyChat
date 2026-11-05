using DevExpress.Utils;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace TinyChat;

internal sealed class DXAttachmentCard : AttachmentCard
{
	internal DXAttachmentCard(ChatFileAttachment attachment, Action? remove = null)
		: base(attachment, new PanelControl { BorderStyle = BorderStyles.NoBorder }, CreatePreview(attachment), CreateRemoveButton(remove), new DXAttachmentToolTip(), remove) { }

	internal DXAttachmentCard(ChatNotice notice, Func<ChatNotice, string> localize, Action? remove)
		: base(null, new PanelControl { BorderStyle = BorderStyles.NoBorder }, CreatePreview(null), CreateRemoveButton(remove), new DXAttachmentToolTip(), remove, notice, localize) { }

	private static Control? CreateRemoveButton(Action? remove)
	{
		if (remove is null)
			return null;
		var button = new SimpleButton { AllowFocus = false, Text = string.Empty, PaintStyle = PaintStyles.Light, Cursor = Cursors.Hand };
		button.ImageOptions.SvgImage = FluentIcons.Rejected;
		button.ImageOptions.SvgImageSize = new Size(16, 16);
		return button;
	}

	private static Control CreatePreview(ChatFileAttachment? attachment)
	{
		var preview = new PictureEdit { EditValue = attachment is { IsImage: true } ? FileAttachmentImageFactory.Create(attachment) : SystemIcons.Application.ToBitmap() };
		preview.Properties.BorderStyle = BorderStyles.NoBorder;
		preview.Properties.SizeMode = PictureSizeMode.Zoom;
		preview.Properties.ShowMenu = false;
		preview.Properties.ReadOnly = true;
		preview.Properties.AllowFocused = false;
		preview.Disposed += (_, _) => preview.Image?.Dispose();
		return preview;
	}

	protected override Image? GetPreviewImage() => ((PictureEdit)PreviewControl).Image;

	protected override Image CreateNoticeBadge(Size size, ChatNoticeSeverity severity)
	{
		var svg = new SvgBitmap(FluentIcons.AttachmentNotice(severity));
		return svg.Render(size, null, DefaultBoolean.Default, DefaultBoolean.False);
	}

	protected override void ReplacePreviewImage(Image image)
	{
		var preview = (PictureEdit)PreviewControl;
		var oldImage = preview.Image;
		preview.Image = image;
		oldImage?.Dispose();
	}
}
