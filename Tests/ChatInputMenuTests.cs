using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ChatInputMenuTests
{
	private static ChatControl CreateChat(bool devExpress)
	{
		var chat = devExpress ? new DialogStubDXChatControl() : (ChatControl)new DialogStubChatControl();
		chat.Size = new Size(600, 400);
		return chat;
	}

	private static IChatInputMenuControl MenuInput(ChatControl chat) => (IChatInputMenuControl)chat.InputControl!;

	public class MenuButton : ChatInputMenuTests
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Is_Shown_With_A_Localized_Tooltip_When_Attachments_Are_Allowed(bool devExpress)
		{
			using var chat = CreateChat(devExpress);
			chat.CreateControl();

			MenuInput(chat).ShowMenuButton.ShouldBeTrue();
			MenuInput(chat).MenuButtonText.ShouldBe(chat.DisplayTexts.InputMenu);

			chat.DisplayTexts.InputMenu = "Mehr";
			MenuInput(chat).MenuButtonText.ShouldBe("Mehr");
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Is_Hidden_When_Switched_Off_Or_Without_Entries(bool devExpress)
		{
			using var chat = CreateChat(devExpress);
			chat.CreateControl();

			chat.ShowInputMenuButton = false;
			MenuInput(chat).ShowMenuButton.ShouldBeFalse();

			chat.ShowInputMenuButton = true;
			chat.AllowFileAttachments = false;
			MenuInput(chat).ShowMenuButton.ShouldBeFalse();

			EventHandler<ChatInputMenuOpeningEventArgs> handler = (_, _) => { };
			chat.InputMenuOpening += handler;
			MenuInput(chat).ShowMenuButton.ShouldBeTrue();
			chat.InputMenuOpening -= handler;
			MenuInput(chat).ShowMenuButton.ShouldBeFalse();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Lies_Over_The_Lower_Left_Corner_Without_Changing_The_Text_Layout(bool devExpress)
		{
			using var host = new Form { Size = new Size(500, 300), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
			using var input = devExpress ? (Control)new DXChatInputControl() : new ChatInputControl();
			input.Size = new Size(320, 90);
			host.Controls.Add(input);
			host.Show();
			var editor = EditorOf(input);
			var textBeforeMenu = TextBoundsInEditor(editor);
			((IChatInputMenuControl)input).ShowMenuButton = true;
			input.PerformLayout();

			var glyph = GlyphOf(input);
			var sendButton = input.Controls.Cast<Control>().Single(control => control != glyph && control.Width <= 24);
			var editorBounds = BoundsInInput(input, editor);
			glyph.Visible.ShouldBeTrue();
			glyph.Left.ShouldBeLessThan(editorBounds.Left + editorBounds.Width / 4);
			glyph.Bottom.ShouldBeLessThanOrEqualTo(editorBounds.Bottom);
			Math.Abs(glyph.Top + glyph.Height / 2 - (sendButton.Top + sendButton.Height / 2)).ShouldBeLessThanOrEqualTo(1);
			TextBoundsInEditor(editor).ShouldBe(textBeforeMenu);

			((IChatInputMenuControl)input).ShowMenuButton = false;
			glyph.Visible.ShouldBeFalse();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Hides_While_The_Draft_Has_Text_And_The_Pointer_Is_Elsewhere(bool devExpress)
		{
			using var host = new Form { Size = new Size(500, 300), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
			using var input = devExpress ? (Control)new DXChatInputControl() : new ChatInputControl();
			input.Size = new Size(320, 90);
			host.Controls.Add(input);
			host.Show();
			((IChatInputMenuControl)input).ShowMenuButton = true;
			var glyph = GlyphOf(input);
			glyph.Visible.ShouldBeTrue();

			EditorOf(input).Text = "typed text";
			glyph.Visible.ShouldBeFalse();

			EditorOf(input).Text = string.Empty;
			glyph.Visible.ShouldBeTrue();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Appears_Over_Text_Only_While_The_Pointer_Is_Over_The_Symbol(bool devExpress)
		{
			using var host = new Form { Size = new Size(500, 300), ShowInTaskbar = false };
			using var input = devExpress ? (Control)new DXChatInputControl() : new ChatInputControl();
			input.Size = new Size(320, 90);
			host.Controls.Add(input);
			host.CreateControl();
			input.CreateControl();
			((IChatInputMenuControl)input).ShowMenuButton = true;
			var glyph = GlyphOf(input);
			var editor = EditorOf(input);
			var textBox = editor is MemoEdit memo ? memo.MaskBox ?? editor : editor;
			var pointer = Point.Empty;
			input.GetType().GetField("_pointerPositionProvider", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
				.SetValue(input, (Func<Point>)(() => pointer));
			editor.Text = "typed text";

			pointer = input.PointToScreen(new Point(input.Width / 2, input.Height / 2));
			RaiseMouseMove(textBox);
			IsShown(glyph).ShouldBeFalse();

			pointer = input.PointToScreen(new Point(glyph.Left + glyph.Width / 2, glyph.Top + glyph.Height / 2));
			RaiseMouseMove(textBox);
			IsShown(glyph).ShouldBeTrue();
		}

		[Test]
		public void Works_With_DevExpress_Advanced_Mode_Editors_Without_An_Embedded_Text_Box()
		{
			var previous = DevExpress.XtraEditors.WindowsFormsSettings.UseAdvancedTextEdit;
			DevExpress.XtraEditors.WindowsFormsSettings.UseAdvancedTextEdit = DevExpress.Utils.DefaultBoolean.True;
			try
			{
				using var form = new Form { Size = new Size(500, 300), ShowInTaskbar = false };
				using var chat = new DXChatControl { Dock = DockStyle.Fill };
				form.Controls.Add(chat);
				form.Show();
				Application.DoEvents();

				var editor = Descendants(chat.InputControl!).OfType<MemoEdit>().Single();
				editor.MaskBox.ShouldBeNull();
				((IChatInputDraftEditorControl)chat.InputControl!).SetDraft("draft", []);
				editor.Text.ShouldBe("draft");
				RaiseMouseMove(editor);
				GlyphOf(chat.InputControl!).Invalidate();
				Application.DoEvents();
			}
			finally
			{
				DevExpress.XtraEditors.WindowsFormsSettings.UseAdvancedTextEdit = previous;
			}
		}

		/// <summary>Gets the visibility the control was given, independent of whether its form is shown.</summary>
		private static bool IsShown(Control control) => (bool)typeof(Control)
			.GetMethod("GetState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
			.Invoke(control, [0x00000002])!;

		private static void RaiseMouseMove(Control control) =>
			typeof(Control).GetMethod("OnMouseMove", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
				.Invoke(control, [new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0)]);

		private static Control EditorOf(Control input) => Descendants(input).First(control => control is TextBox || control is MemoEdit);

		private static Rectangle BoundsInInput(Control input, Control control) => input.RectangleToClient(control.Parent!.RectangleToScreen(control.Bounds));

		/// <summary>Gets the area the text is laid out in, in client coordinates of the editor.</summary>
		private static Rectangle TextBoundsInEditor(Control editor)
		{
			if (editor is MemoEdit memo)
				return memo.MaskBox?.Bounds ?? memo.ClientRectangle;

			var rect = new int[4];
			SendMessage(editor.Handle, 0x00B2, IntPtr.Zero, rect);
			return Rectangle.FromLTRB(rect[0], rect[1], rect[2], rect[3]);
		}

		private static InputMenuGlyph GlyphOf(Control input) => (InputMenuGlyph)input.GetType()
			.GetField("_menuGlyph", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
			.GetValue(input)!;

		[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, int[] lParam);

		[TestCase(false)]
		[TestCase(true)]
		public void Keeps_The_Input_High_Enough_For_The_Buttons(bool devExpress)
		{
			using var chat = CreateChat(devExpress);
			chat.SplitterPosition = 10;
			chat.CreateControl();
			chat.SplitterPosition = 10;
			var input = chat.InputControl!;
			var editorBounds = BoundsInInput(input, EditorOf(input));

			chat.SplitterPosition.ShouldBe(((IChatInputMinimumHeightControl)input).MinimumHeight);
			foreach (var button in input.Controls.Cast<Control>().Where(control => control.Width <= 24))
			{
				button.Top.ShouldBeGreaterThanOrEqualTo(editorBounds.Top);
				button.Bottom.ShouldBeLessThanOrEqualTo(editorBounds.Bottom);
			}
		}
		private static IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var descendant in Descendants(child))
					yield return descendant;
			}
		}
	}

	public class MenuEntries : ChatInputMenuTests
	{
		[Test]
		public void Opens_A_Skinned_DevExpress_Menu_Even_On_A_Plain_Form()
		{
			using var form = new Form { Size = new Size(500, 300), ShowInTaskbar = false };
			using var chat = new DXChatControl { Dock = DockStyle.Fill };
			form.Controls.Add(chat);
			form.Show();
			Application.DoEvents();

			chat.OpenInputMenu();
			Application.DoEvents();
			try
			{
				Application.OpenForms.OfType<DevExpress.XtraBars.Forms.SubMenuControlForm>().ShouldContain(menu => menu.Visible);
				Application.OpenForms.OfType<ContextMenuStrip>().ShouldBeEmpty();
			}
			finally
			{
				HideMenu(chat);
			}
		}

		/// <summary>Moves and clicks the real mouse, so it must not run alongside other UI tests.</summary>
		[Test]
		[Explicit("Clicks the real mouse; run on its own.")]
		public void Closes_The_DevExpress_Menu_On_A_Click_Beside_It()
		{
			using var form = new Form { Size = new Size(500, 300), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(100, 100), TopMost = true };
			using var chat = new DXChatControl { Dock = DockStyle.Fill };
			form.Controls.Add(chat);
			form.Show();
			Application.DoEvents();

			chat.OpenInputMenu();
			Application.DoEvents();
			var original = Cursor.Position;
			try
			{
				Application.OpenForms.OfType<DevExpress.XtraBars.Forms.SubMenuControlForm>().ShouldContain(menu => menu.Visible);

				Cursor.Position = form.PointToScreen(new Point(form.ClientSize.Width - 20, 20));
				mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
				mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
				for (var i = 0; i < 20; i++)
				{
					Application.DoEvents();
					Thread.Sleep(15);
				}

				Application.OpenForms.OfType<DevExpress.XtraBars.Forms.SubMenuControlForm>().ShouldNotContain(menu => menu.Visible);
			}
			finally
			{
				Cursor.Position = original;
				HideMenu(chat);
			}
		}

		private static void HideMenu(ChatControl chat)
		{
			var menu = (DevExpress.Utils.Menu.DXPopupMenu?)chat.InputControl!.GetType()
				.GetField("_menu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
				.GetValue(chat.InputControl);
			menu?.HidePopup();
		}

		private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
		private const int MOUSEEVENTF_LEFTUP = 0x0004;

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern void mouse_event(int flags, int dx, int dy, int data, IntPtr extraInfo);

		[TestCase(false)]
		[TestCase(true)]
		public void Can_Be_Extended_Replaced_Or_Cancelled_In_Code(bool devExpress)
		{
			using var chat = CreateChat(devExpress);
			chat.CreateControl();
			IList<ChatInputMenuItem>? offered = null;
			chat.InputMenuOpening += (_, e) =>
			{
				offered = e.Items.ToList();
				e.Items.Add(new ChatInputMenuItem(ChatInputMenuItemKind.Custom, "Custom", () => { }) { BeginGroup = true });
				e.Cancel = true;
			};

			chat.OpenInputMenu();

			offered.ShouldNotBeNull().Single().Kind.ShouldBe(ChatInputMenuItemKind.AttachFiles);
			offered.Single().Text.ShouldBe(chat.DisplayTexts.AttachFiles);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Omit_Attachments_When_They_Are_Not_Allowed(bool devExpress)
		{
			using var chat = CreateChat(devExpress);
			chat.AllowFileAttachments = false;
			chat.CreateControl();
			IList<ChatInputMenuItem>? offered = null;
			chat.InputMenuOpening += (_, e) => offered = e.Items.ToList();

			chat.OpenInputMenu();

			offered.ShouldNotBeNull().ShouldBeEmpty();
		}
	}

	public class AttachingFiles : ChatInputMenuTests
	{
		[TestCase(false)]
		[TestCase(true)]
		public async Task Adds_The_Files_Selected_In_The_Dialog(bool devExpress)
		{
			var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
			File.WriteAllText(file, "text");
			try
			{
				using var chat = CreateChat(devExpress);
				chat.CreateControl();
				var dialog = (IDialogStub)chat;
				dialog.Result = [file];

				(await chat.AttachFilesFromDialogAsync()).ShouldBe(1);

				dialog.Title.ShouldBe(chat.DisplayTexts.AttachFilesDialogTitle);
				dialog.Filter.ShouldStartWith(chat.DisplayTexts.SupportedAttachmentFiles + "|");
				dialog.Filter.ShouldContain("*.txt");
				dialog.Filter.ShouldEndWith(chat.DisplayTexts.AllFiles + "|*.*");
				((IChatAttachmentInputControl)chat.InputControl!).PendingAttachments.Single().Name.ShouldBe(Path.GetFileName(file));
			}
			finally
			{
				File.Delete(file);
			}
		}

		[TestCase(false)]
		[TestCase(true)]
		public async Task Does_Not_Open_The_Dialog_When_Attachments_Are_Not_Allowed(bool devExpress)
		{
			using var chat = CreateChat(devExpress);
			chat.AllowFileAttachments = false;
			chat.CreateControl();

			(await chat.AttachFilesFromDialogAsync()).ShouldBe(0);
			((IDialogStub)chat).Title.ShouldBeNull();
		}
	}

	private interface IDialogStub
	{
		IReadOnlyList<string>? Result { get; set; }
		string? Title { get; }
		string? Filter { get; }
	}

	private sealed class DialogStubChatControl : ChatControl, IDialogStub
	{
		private IReadOnlyList<string>? _result;
		private string? _title;
		private string? _filter;

		IReadOnlyList<string>? IDialogStub.Result { get => _result; set => _result = value; }
		string? IDialogStub.Title => _title;
		string? IDialogStub.Filter => _filter;

		protected override IReadOnlyList<string>? ShowAttachFilesDialog(string title, string filter)
		{
			_title = title;
			_filter = filter;
			return _result;
		}
	}

	private sealed class DialogStubDXChatControl : DXChatControl, IDialogStub
	{
		private IReadOnlyList<string>? _result;
		private string? _title;
		private string? _filter;

		IReadOnlyList<string>? IDialogStub.Result { get => _result; set => _result = value; }
		string? IDialogStub.Title => _title;
		string? IDialogStub.Filter => _filter;

		protected override IReadOnlyList<string>? ShowAttachFilesDialog(string title, string filter)
		{
			_title = title;
			_filter = filter;
			return _result;
		}
	}
}
