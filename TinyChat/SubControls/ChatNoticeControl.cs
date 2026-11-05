using System.ComponentModel;

namespace TinyChat;

/// <summary>Displays request errors inline and keeps attachment diagnostics compact.</summary>
internal sealed class ChatNoticeControl : FlowLayoutPanel, IChatMessageControl, IChatMessageLayoutControl
{
	private readonly Control _icon;
	private readonly Control _text;
	private readonly IAttachmentToolTip _toolTip;
	private readonly Func<ChatNotice, string> _localize;
	private IChatMessage? _message;

	internal ChatNoticeControl(IChatMessage message, Control icon, Func<ChatNotice, string> localize, IAttachmentToolTip toolTip, Control? text = null)
	{
		_icon = icon;
		_text = text ?? new Label { AutoSize = true, UseMnemonic = false, Margin = Padding.Empty };
		_localize = localize;
		_toolTip = toolTip;
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		WrapContents = false;
		Controls.Add(_icon);
		Controls.Add(_text);
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
				var explanation = ChatLocalization.IncludeException(_localize(content.Notice), content.Notice);
				_text.Text = content.Notice.Code == ChatNoticeCode.GeneralError ? explanation : content.Notice.FileName ?? string.Empty;
				AccessibleName = explanation;
				_icon.AccessibleName = explanation;
				_toolTip.Set(_icon, string.Empty, explanation);
				_toolTip.Set(_text, string.Empty, explanation);
			}
			PerformLayout();
			AfterLayoutChange?.Invoke(this, EventArgs.Empty);
		}
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		if (_text is not null && _icon is not null)
		{
			var width = MaximumSize.Width > 0 ? MaximumSize.Width : ClientSize.Width;
			var textWidth = Math.Max(1, width - Padding.Horizontal - _icon.Width - _icon.Margin.Horizontal - _text.Margin.Horizontal);
			_text.MaximumSize = new Size(textWidth, 0);
			if (_text is not Label)
				_text.Width = textWidth;
		}
		base.OnLayout(e);
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
