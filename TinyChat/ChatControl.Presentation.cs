using System.ComponentModel;

namespace TinyChat;

public partial class ChatControl
{
	/// <summary>Gets localizable designer overrides. Unset entries follow the current UI culture.</summary>
	[Category("Chat Appearance")]
	[Localizable(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public ChatDisplayTexts DisplayTexts { get; } = new();

	/// <summary>Gets the appearance shared by attachment cards in the input and conversation.</summary>
	[Category("Chat Appearance")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public AttachmentAppearance AttachmentAppearance { get; } = new();

	private void RefreshAttachmentPresentation(Control? parent = null)
	{
		foreach (var card in EnumerateDescendantControls(parent ?? this).OfType<AttachmentCard>())
			card.ApplyPresentation(this);
		foreach (var preview in EnumerateDescendantControls(parent ?? this).OfType<FileAttachmentPreviewPanel>())
			preview.ApplyPresentation(this);
		foreach (var notice in EnumerateDescendantControls(parent ?? this).OfType<ChatNoticeControl>())
			notice.Message = notice.Message;
	}
}
