namespace TinyChat;

/// <summary>
/// Shows the complete text of reasoning or a tool call so that it can be read, selected and copied.
/// </summary>
internal sealed class FullTextDialog : Form
{
	private readonly TextBox _textBox;

	internal FullTextDialog(string title, string text, Font? font)
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

		_textBox = new TextBox
		{
			AccessibleName = title,
			BackColor = SystemColors.Window,
			Dock = DockStyle.Fill,
			Multiline = true,
			ReadOnly = true,
			ScrollBars = ScrollBars.Vertical,
			Text = text.ReplaceLineEndings("\r\n"),
			WordWrap = true
		};
		if (font is not null)
			_textBox.Font = font;

		Controls.Add(_textBox);
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		_textBox.Select(0, 0);
		_textBox.Focus();
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
			_textBox.SelectAll();
		}
	}
}
