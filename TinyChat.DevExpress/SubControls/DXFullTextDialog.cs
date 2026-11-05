using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>
/// Shows the complete text of reasoning or a tool call with the active DevExpress skin so that it can be read,
/// selected and copied.
/// </summary>
internal sealed class DXFullTextDialog : XtraForm
{
	private readonly MemoEdit _memo;

	internal DXFullTextDialog(string title, string text, Font? font)
	{
		AutoScaleMode = AutoScaleMode.Dpi;
		AutoScaleDimensions = new SizeF(96f, 96f);
		ClientSize = new Size(720, 480);
		KeyPreview = true;
		MinimumSize = new Size(320, 200);
		MinimizeBox = false;
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = title;

		_memo = new MemoEdit { AccessibleName = title, Dock = DockStyle.Fill };
		_memo.Properties.ReadOnly = true;
		_memo.Properties.UseReadOnlyAppearance = false;
		_memo.Properties.WordWrap = true;
		_memo.Properties.ScrollBars = ScrollBars.Vertical;
		_memo.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
		if (font is not null)
		{
			_memo.Properties.Appearance.Font = font;
			_memo.Properties.Appearance.Options.UseFont = true;
		}
		_memo.Text = text.ReplaceLineEndings("\r\n");

		Controls.Add(_memo);
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		_memo.Focus();
		_memo.Select(0, 0);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Escape)
		{
			e.Handled = true;
			Close();
		}
		else if (e.KeyData == (Keys.Control | Keys.A))
		{
			e.Handled = true;
			e.SuppressKeyPress = true;
			_memo.SelectAll();
		}
	}
}
