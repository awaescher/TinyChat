using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Shouldly;
using TinyChat;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class NativeChatMessageSelectionTests
{
	[Test]
	public void Uses_A_Readonly_Borderless_Multiline_Text_Control()
	{
		using var control = CreateMessageControl("Alpha beta gamma");
		var textControl = GetTextControl(control);

		textControl.ReadOnly.ShouldBeTrue();
		textControl.BorderStyle.ShouldBe(BorderStyle.None);
		textControl.HideSelection.ShouldBeTrue();
		textControl.Multiline.ShouldBeTrue();
		textControl.WordWrap.ShouldBeTrue();
		textControl.ScrollBars.ShouldBe(ScrollBars.None);
		textControl.ShortcutsEnabled.ShouldBeTrue();
	}

	[Test]
	public void Displays_Text_Immediately_Without_A_Selection_Mode_Click()
	{
		using var host = new Panel { Size = new Size(190, 500) };
		using var control = CreateMessageControl("Visible immediately");

		HostMessageControl(host, control, 190);

		var textControl = GetTextControl(control);
		textControl.Visible.ShouldBeTrue();
		textControl.Text.ShouldBe("Visible immediately");
		textControl.Height.ShouldBeGreaterThanOrEqualTo(textControl.Font.Height);
	}

	[Test]
	public void Selects_Only_A_Message_Substring()
	{
		using var control = CreateMessageControl("Alpha beta gamma");
		var textControl = GetTextControl(control);
		textControl.Select(6, 4);

		textControl.SelectedText.ShouldBe("beta");
	}

	[Test]
	public void Content_MouseDown_Does_Not_Steal_Focus_Through_Parent_Selection()
	{
		using var control = CreateMessageControl("Selectable text");
		var textControl = GetTextControl(control);
		var senderLabel = control.Controls.OfType<Label>().Single();
		var parentMouseDownCount = 0;
		control.MouseDown += (_, _) => parentMouseDownCount++;
		var onMouseDown = typeof(Control).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!;
		var mouseEvent = new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0);

		onMouseDown.Invoke(textControl, [mouseEvent]);

		parentMouseDownCount.ShouldBe(0);

		onMouseDown.Invoke(senderLabel, [mouseEvent]);

		parentMouseDownCount.ShouldBe(1);
	}

	[Test]
	public void Grows_When_Text_Wraps_And_Shrinks_When_Text_Becomes_Shorter()
	{
		using var host = new Panel { Size = new Size(190, 500) };
		using var control = CreateMessageControl("Short text");
		HostMessageControl(host, control, 190);
		var textControl = GetTextControl(control);
		var singleLineHeight = textControl.Height;

		textControl.Text = string.Join(" ", Enumerable.Repeat("wrapped message content", 12));
		PerformLayout(host, control);
		var wrappedHeight = textControl.Height;

		textControl.Text = "Short again";
		PerformLayout(host, control);

		wrappedHeight.ShouldBeGreaterThan(singleLineHeight);
		textControl.Height.ShouldBeLessThan(wrappedHeight);
		textControl.Height.ShouldBeGreaterThanOrEqualTo(textControl.Font.Height);
	}

	[Test]
	public void Updates_Text_Height_And_Parent_Size_While_Streaming()
	{
		var builder = new NotifyingStringBuilder().Append("Started");
		using var host = new Panel { Size = new Size(190, 500) };
		using var control = new ChatMessageControl
		{
			MessageFormatter = new PlainTextMessageFormatter(),
			Message = new ChatMessage(new NamedSender("Assistant"), new ChangingMessageContent(builder))
		};
		HostMessageControl(host, control, 190);
		var textControl = GetTextControl(control);
		var initialTextHeight = textControl.Height;
		var initialControlHeight = control.Height;
		var sizeUpdateCount = 0;
		control.SizeUpdatedWhileStreaming += (_, _) => sizeUpdateCount++;
		((IChatMessageControl)control).SetIsReceivingStream(true);
		textControl.Select(0, "Started".Length);

		builder.Append(" ").Append(string.Join(" ", Enumerable.Repeat("streamed response content", 16)));
		PerformLayout(host, control);

		textControl.Text.ShouldContain("streamed response content");
		textControl.Height.ShouldBeGreaterThan(initialTextHeight);
		control.Height.ShouldBeGreaterThan(initialControlHeight);
		sizeUpdateCount.ShouldBeGreaterThan(0);
		textControl.SelectionStart.ShouldBe(0);
		textControl.SelectionLength.ShouldBe("Started".Length);
		textControl.SelectedText.ShouldBe("Started");
	}

	[Test]
	public void Propagates_Font_And_Colors_To_Selectable_Text()
	{
		using var control = CreateMessageControl("Styled message");
		using var font = new Font("Consolas", 12f);
		var backColor = Color.AliceBlue;
		var foreColor = Color.DarkRed;

		control.Font = font;
		control.BackColor = backColor;
		control.ForeColor = foreColor;
		var textControl = GetTextControl(control);
		var senderLabel = control.Controls.OfType<Label>().Single();

		textControl.Font.ShouldBe(font);
		textControl.BackColor.ShouldBe(backColor);
		textControl.ForeColor.ShouldBe(foreColor);
		senderLabel.Font.Bold.ShouldBeTrue();
		senderLabel.ForeColor.ShouldBe(foreColor);
	}

	[Test]
	public void Keeps_Attachment_Preview_Separate_From_Selectable_Caption()
	{
		var imageData = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
		var attachment = new ChatFileAttachment("photo.png", "image/png", imageData);
		using var host = new Panel { Size = new Size(300, 500) };
		using var control = new ChatMessageControl
		{
			MessageFormatter = new PlainTextMessageFormatter(),
			Message = new ChatMessage(
				new NamedSender("User"),
				new FileAttachmentMessageContent([attachment], "Selectable caption"))
		};
		HostMessageControl(host, control, 300);
		var textControl = GetTextControl(control);
		var previewPanel = control.Controls.OfType<FileAttachmentPreviewPanel>().Single();

		textControl.Text.ShouldBe("Selectable caption");
		textControl.Bounds.IntersectsWith(previewPanel.Bounds).ShouldBeFalse();
		previewPanel.Controls.OfType<PictureBox>().Single().Image.ShouldNotBeNull();
	}

	[Test]
	public void Function_Call_Details_Are_Selectable_And_Do_Not_Toggle_From_The_Editor()
	{
		using var host = new Panel { Size = new Size(260, 500) };
		using var control = CreateFunctionCallMessageControl();
		HostMessageControl(host, control, 260);
		var detailControls = GetTextControls(control);

		detailControls.Length.ShouldBe(2);
		detailControls.ShouldAllBe(textControl => textControl.ReadOnly);
		detailControls.ShouldAllBe(textControl => textControl.HideSelection);
		detailControls.ShouldAllBe(textControl => !textControl.Visible);
		DescendantsAndSelf(control)
			.OfType<Label>()
			.ShouldContain(label => label.Text.StartsWith("lookup", StringComparison.Ordinal));
		var collapsedHeight = control.Height;

		InvokeToggle(control);

		detailControls.ShouldAllBe(textControl => textControl.Visible);
		var argumentsControl = detailControls.Single(textControl => textControl.Text.Contains("query:", StringComparison.Ordinal));
		var resultControl = detailControls.Single(textControl => textControl != argumentsControl);
		var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
		var argumentsRow = tableLayout.GetPositionFromControl(argumentsControl).Row;
		argumentsControl.Height.ShouldBeGreaterThan(argumentsControl.Font.Height);
		tableLayout.GetRowHeights()[argumentsRow].ShouldBeGreaterThanOrEqualTo(argumentsControl.Height);
		control.Height.ShouldBeGreaterThan(collapsedHeight);
		argumentsControl.Select(argumentsControl.Text.IndexOf("TinyChat", StringComparison.Ordinal), "TinyChat".Length);
		argumentsControl.SelectedText.ShouldBe("TinyChat");
		resultControl.Text.ShouldBe("Found ");
		resultControl.Select(0, "Found".Length);
		resultControl.SelectedText.ShouldBe("Found");

		var parentMouseDownCount = 0;
		control.MouseDown += (_, _) => parentMouseDownCount++;
		InvokeMouseDown(argumentsControl);
		InvokeClick(argumentsControl);

		parentMouseDownCount.ShouldBe(0);
		detailControls.ShouldAllBe(textControl => textControl.Visible);
	}

	[Test]
	public void Reasoning_Detail_Is_Selectable_And_Does_Not_Toggle_From_The_Editor()
	{
		using var host = new Panel { Size = new Size(260, 500) };
		using var control = CreateReasoningMessageControl();
		HostMessageControl(host, control, 260);
		var detailControl = GetTextControls(control).Single();

		detailControl.ReadOnly.ShouldBeTrue();
		detailControl.HideSelection.ShouldBeTrue();
		detailControl.Visible.ShouldBeFalse();
		DescendantsAndSelf(control).OfType<Label>().Count().ShouldBe(3);
		DescendantsAndSelf(control).OfType<Label>().ShouldContain(label => label.Text == "...");

		InvokeToggle(control);

		detailControl.Visible.ShouldBeTrue();
		detailControl.Text.ShouldStartWith("Thinking through the answer");
		var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
		var detailRow = tableLayout.GetPositionFromControl(detailControl).Row;
		detailControl.Height.ShouldBeGreaterThan(detailControl.Font.Height);
		tableLayout.GetRowHeights()[detailRow].ShouldBeGreaterThanOrEqualTo(detailControl.Height);
		detailControl.Select("Thinking ".Length, "through".Length);
		detailControl.SelectedText.ShouldBe("through");

		var reasoningContent = ((IChatMessageControl)control).Message!.Content.ShouldBeOfType<ReasoningMessageContent>();
		reasoningContent.AppendText(" streamed tail");
		PerformLayout(host, control);

		detailControl.Text.ShouldEndWith("streamed tail");
		detailControl.SelectedText.ShouldBe("through");

		reasoningContent.SetDone();
		DescendantsAndSelf(control).OfType<Label>().ShouldContain(label => label.Text == "✔");

		var parentMouseDownCount = 0;
		control.MouseDown += (_, _) => parentMouseDownCount++;
		InvokeMouseDown(detailControl);
		InvokeClick(detailControl);

		parentMouseDownCount.ShouldBe(0);
		detailControl.Visible.ShouldBeTrue();
	}

	private static ChatMessageControl CreateMessageControl(string text)
	{
		return new ChatMessageControl
		{
			MessageFormatter = new PlainTextMessageFormatter(),
			Message = new ChatMessage(new NamedSender("Sender"), new StringMessageContent(text))
		};
	}

	private static TextBox GetTextControl(ChatMessageControl control)
	{
		return control.Controls.OfType<TextBox>().Single();
	}

	private static Control CreateFunctionCallMessageControl()
	{
		var type = typeof(ChatControl).Assembly.GetType("TinyChat.FunctionCallMessageControl")!;
		var control = (Control)Activator.CreateInstance(type, nonPublic: true)!;
		((IFunctionCallMessageControl)control).AllowExpand = true;
		((IChatMessageControl)control).Message = new ChatMessage(
			new NamedSender("Tool"),
			new FunctionCallMessageContent(
				"call-1",
				"lookup",
				new Dictionary<string, object?>
				{
					["query"] = "TinyChat " + string.Join(" ", Enumerable.Repeat("wrapped function argument", 12))
				},
				"Found"));
		return control;
	}

	private static Control CreateReasoningMessageControl()
	{
		var type = typeof(ChatControl).Assembly.GetType("TinyChat.SubControls.ReasoningMessageControl")!;
		var control = (Control)Activator.CreateInstance(type, nonPublic: true)!;
		type.GetProperty("MessageFormatter")!.SetValue(control, new PlainTextMessageFormatter());
		((IChatMessageControl)control).Message = new ChatMessage(
			new NamedSender("Assistant"),
			new ReasoningMessageContent(
				"Thinking through the answer " + string.Join(" ", Enumerable.Repeat("with wrapped reasoning detail", 12))));
		return control;
	}

	private static void HostMessageControl(Control host, Control control, int width)
	{
		control.MinimumSize = new Size(width, 0);
		control.MaximumSize = new Size(width, 0);
		control.Width = width;
		host.Controls.Add(control);
		host.CreateControl();
		control.CreateControl();
		foreach (var textControl in GetTextControls(control))
			textControl.CreateControl();
		PerformLayout(host, control);
	}

	private static void PerformLayout(Control host, Control control)
	{
		host.PerformLayout();
		control.PerformLayout();
		Application.DoEvents();
	}

	private static TextBox[] GetTextControls(Control parent)
	{
		return DescendantsAndSelf(parent).OfType<TextBox>().ToArray();
	}

	private static IEnumerable<Control> DescendantsAndSelf(Control parent)
	{
		yield return parent;

		foreach (Control child in parent.Controls)
		{
			foreach (var descendant in DescendantsAndSelf(child))
				yield return descendant;
		}
	}

	private static void InvokeToggle(Control control)
	{
		control.GetType()
			.GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(control, [control, EventArgs.Empty]);
		PerformLayout(control.Parent!, control);
	}

	private static void InvokeMouseDown(Control control)
	{
		var onMouseDown = typeof(Control).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!;
		onMouseDown.Invoke(control, [new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0)]);
	}

	private static void InvokeClick(Control control)
	{
		var onClick = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!;
		onClick.Invoke(control, [EventArgs.Empty]);
	}
}
