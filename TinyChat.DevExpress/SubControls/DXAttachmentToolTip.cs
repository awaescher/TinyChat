using DevExpress.Utils;
using DevExpress.XtraEditors;

namespace TinyChat;

internal sealed class DXAttachmentToolTip : IAttachmentToolTip
{
	private readonly ToolTipController _controller = new() { ToolTipType = ToolTipType.SuperTip };

	public void Set(Control control, string title, string text)
	{
		var tip = new SuperToolTip { AllowHtmlText = DefaultBoolean.False };
		if (!string.IsNullOrEmpty(title))
			tip.Items.Add(new ToolTipTitleItem { Text = title });
		tip.Items.Add(new ToolTipItem { Text = text });
		if (control is BaseControl editor)
		{
			editor.ToolTipController = _controller;
			editor.SuperTip = tip;
		}
		else
			_controller.SetSuperTip(control, tip);
	}

	public void Dispose()
	{
		_controller.HideHint();
		_controller.Dispose();
	}
}
