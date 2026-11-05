using DevExpress.Utils;
using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>Uses semantic skin colors and refreshes vector status icons after palette changes.</summary>
internal sealed class DXStatusIcon : LabelControl
{
	private readonly ChatNoticeSeverity _severity;

	internal DXStatusIcon(ChatNoticeSeverity severity)
	{
		_severity = severity;
		AutoSizeMode = LabelAutoSizeMode.None;
		Size = new Size(18, 18);
		Margin = new Padding(0, 0, 6, 0);
		ImageOptions.SvgImageSize = new Size(16, 16);
		ImageOptions.SvgImageColorizationMode = SvgImageColorizationMode.None;
		LookAndFeel.StyleChanged += SkinStyleChanged;
		UpdateImage();
	}

	private void SkinStyleChanged(object? sender, EventArgs e) => UpdateImage();
	private void UpdateImage() => ImageOptions.SvgImage = FluentIcons.AttachmentNotice(_severity, LookAndFeel);

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			LookAndFeel.StyleChanged -= SkinStyleChanged;
		base.Dispose(disposing);
	}
}
