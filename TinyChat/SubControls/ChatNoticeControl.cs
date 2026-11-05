using System.ComponentModel;

namespace TinyChat;

/// <summary>Displays a status icon and optional file name; the explanation is a localized tooltip.</summary>
internal sealed class ChatNoticeControl : FlowLayoutPanel, IChatMessageControl, IChatMessageLayoutControl
{
	private readonly Control _icon;
	private readonly Label _fileName = new() { AutoSize = true, UseMnemonic = false, Margin = Padding.Empty };
	private readonly IAttachmentToolTip _toolTip;
	private readonly Func<ChatNotice, string> _localize;
	private IChatMessage? _message;

	internal ChatNoticeControl(IChatMessage message, Control icon, Func<ChatNotice, string> localize, IAttachmentToolTip toolTip)
	{
		_icon = icon;
		_localize = localize;
		_toolTip = toolTip;
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		WrapContents = false;
		Controls.Add(_icon);
		Controls.Add(_fileName);
		Message = message;
	}

	public event EventHandler? BeforeLayoutChange;
	public event EventHandler? AfterLayoutChange;
	public event EventHandler? SizeUpdatedWhileStreaming { add { } remove { } }
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Padding ContentPadding { get => Padding; set => Padding = value; }
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
			_message = value;
			if (value?.Content is ChatNoticeMessageContent content)
			{
				var explanation = _localize(content.Notice);
				_fileName.Text = content.Notice.FileName ?? string.Empty;
				AccessibleName = explanation;
				_icon.AccessibleName = explanation;
				_toolTip.Set(_icon, string.Empty, explanation);
				_toolTip.Set(_fileName, string.Empty, explanation);
			}
			AfterLayoutChange?.Invoke(this, EventArgs.Empty);
		}
	}

	public void SetIsReceivingStream(bool isReceiving) { }
	public void ShowSenderHeader(bool show) { }
	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_toolTip.Dispose();
		base.Dispose(disposing);
	}
}
