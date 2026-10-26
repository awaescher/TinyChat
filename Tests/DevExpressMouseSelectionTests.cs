using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using DevExpress.Utils.Html;
using DevExpress.Utils.Html.Internal;
using DevExpress.XtraEditors;
using Shouldly;
using TinyChat;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class DevExpressMouseSelectionTests
{
	private const string FORMATTED_TEXT = "The **Transformer** is a neural network architecture introduced in the landmark paper \"Attention Is All You Need\" (Vaswani et al., 2017). Unlike its predecessors (RNNs, LSTMs), it relies entirely on **self-attention mechanisms**, allowing it to process sequences in parallel rather than sequentially.\n\nToday, Transformers power virtually all modern large language models.";

	private static SimplifiedHtmlMessageFormatter CreateDemoFormatter() => new(
		"b", "i", "s", "u", "br", "sub", "sup", "font", "p", "nbsp", "a", "color", "backcolor", "size",
		"h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "table", "thead", "tbody", "tfoot", "tr", "th", "td");

	[Test]
	public void Selection_Follows_The_Mouse_Across_Bold_Text()
	{
		using var form = new Form { ClientSize = new Size(360, 300) };
		using var control = CreateMessageControl(form);
		var editor = control.Controls.OfType<HtmlContentControl>().Single();
		var plainText = editor.Text;
		var selections = new List<string>();

		Send(editor, 0x0201, new Point(4, 6), 1);
		foreach (var x in new[] { 60, 120, 200, 320 })
		{
			Send(editor, 0x0200, new Point(x, 6), 1);
			Pump(20);
			selections.Add(editor.SelectedText);
		}

		Send(editor, 0x0202, new Point(320, 6), 0);

		// Every selection is a contiguous part of the visible text starting at the anchor, including the
		// space after "Transformer", and grows with the mouse instead of skipping text after bold words.
		selections.ShouldAllBe(selection => plainText.Contains(selection, StringComparison.Ordinal), string.Join(" | ", selections));
		selections.ShouldAllBe(selection => selection.StartsWith("he", StringComparison.Ordinal));
		selections[^1].ShouldStartWith("he Transformer is a neural");
		for (var index = 1; index < selections.Count; index++)
			selections[index].Length.ShouldBeGreaterThan(selections[index - 1].Length);
	}

	[Test]
	public void Keeps_Quotes_Emoji_And_Spaces_After_Bold_Text_In_The_Selection_Model()
	{
		using var form = new Form { ClientSize = new Size(360, 300) };
		using var control = CreateMessageControl(form, "The **Transformer** is \"quoted\" with $5 and 🔍 here.");
		var editor = control.Controls.OfType<HtmlContentControl>().Single();
		var texts = Descendants(editor.FindElementById("message-content")!)
			.OfType<DxHtmlTextElementBase>()
			.Select(element => element.Text)
			.ToArray();

		texts.ShouldBe(["The ", "Transformer", " is \"quoted\" with $5 and 🔍 here."]);
	}

	[Test]
	public void Shows_Ampersands_Angle_Brackets_And_Dollar_Signs_As_Typed()
	{
		using var form = new Form { ClientSize = new Size(360, 300) };
		using var control = CreateMessageControl(form, "Use a &lt; b &amp;&amp; c &gt; d, not ${Text}.");
		var editor = control.Controls.OfType<HtmlContentControl>().Single();
		var text = string.Concat(Descendants(editor.FindElementById("message-content")!)
			.OfType<DxHtmlTextElementBase>()
			.Select(element => element.Text));

		text.ShouldBe("Use a < b && c > d, not ${Text}.");
		editor.Text.ShouldBe(text);
	}

	[Explicit("Writes a screenshot for a visual check")]
	[Test]
	public void Renders_The_Demo_Text_With_A_Mouse_Selection()
	{
		using var form = new Form { ClientSize = new Size(360, 200) };
		using var control = CreateMessageControl(form);
		var editor = control.Controls.OfType<HtmlContentControl>().Single();
		Send(editor, 0x0201, new Point(4, 6), 1);
		Send(editor, 0x0200, new Point(180, 22), 1);
		Pump(50);
		Send(editor, 0x0202, new Point(180, 22), 0);
		Pump(50);

		using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
		form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
		bitmap.Save(Path.Combine(Path.GetTempPath(), "opencode", "dx_selection.png"));
		TestContext.Out.WriteLine($"selected='{editor.SelectedText}'");
	}

	private static DXChatMessageControl CreateMessageControl(Form form, string text = FORMATTED_TEXT)
	{
		var control = new DXChatMessageControl
		{
			Dock = DockStyle.Top,
			MessageFormatter = CreateDemoFormatter(),
			Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(text))
		};
		form.Controls.Add(control);
		form.Show();
		Pump(200);

		// DevExpress lays the HTML out asynchronously; the mouse positions are only meaningful once the
		// editor has its final width and the content element exists.
		var editor = control.Controls.OfType<HtmlContentControl>().Single();
		var stopwatch = Stopwatch.StartNew();
		while ((editor.Width < form.ClientSize.Width - 32 || editor.FindElementById("message-content") is null) &&
			stopwatch.ElapsedMilliseconds < 2000)
		{
			Pump(20);
		}

		return control;
	}

	private static IEnumerable<DxHtmlNode> Descendants(DxHtmlNode node)
	{
		foreach (DxHtmlNode child in node.ChildNodes)
		{
			yield return child;
			foreach (var descendant in Descendants(child))
				yield return descendant;
		}
	}

	private static void Send(Control control, int message, Point point, int buttons)
	{
		var lParam = (IntPtr)((point.Y << 16) | (point.X & 0xFFFF));
		SendMessage(control.Handle, message, (IntPtr)buttons, lParam);
	}

	[System.Runtime.InteropServices.DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

	private static void Pump(int milliseconds)
	{
		var stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < milliseconds)
		{
			Application.DoEvents();
			Thread.Sleep(5);
		}
	}
}
