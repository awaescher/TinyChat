namespace TinyChat;

internal interface IAttachmentToolTip : IDisposable
{
	void Set(Control control, string title, string text);
}

internal sealed class AttachmentToolTip : IAttachmentToolTip
{
	private readonly ToolTip _toolTip = new();

	public void Set(Control control, string title, string text)
	{
		_toolTip.SetToolTip(control, string.IsNullOrEmpty(title) ? text : title + Environment.NewLine + text);
	}

	public void Dispose() => _toolTip.Dispose();
}
