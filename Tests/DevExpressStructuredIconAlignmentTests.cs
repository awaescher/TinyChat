using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Shouldly;
using TinyChat;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class DevExpressStructuredIconAlignmentTests
{
	[TestCase(9, 260)]
	[TestCase(18, 180)]
	public void Function_Call_Icons_Stay_Aligned_With_The_First_Text_Line_When_Content_Wraps(
		int fontSize,
		int width)
	{
		using var font = new Font(SystemFonts.MessageBoxFont!.FontFamily, fontSize);
		using var host = new Panel { Size = new Size(width, 700) };
		using var chatControl = CreateChatControl(host, font);
		var control = CreateFunctionCallControl(chatControl);
		PerformLayout(host, chatControl);
		control.PerformLayout();
		InvokeToggle(control);

		var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
		var icons = DescendantsAndSelf(control)
			.OfType<LabelControl>()
			.Where(label => label.ImageOptions.SvgImage is not null && label.Visible)
			.ToArray();
		var title = DescendantsAndSelf(control)
			.OfType<LabelControl>()
			.Single(label => label.Text.StartsWith("lookup_with", StringComparison.Ordinal));
		var resultEditor = DescendantsAndSelf(control)
			.OfType<HtmlContentControl>()
			.Single(editor => editor.Text.StartsWith("A deliberately", StringComparison.Ordinal));

		icons.Length.ShouldBe(2);
		GetRenderedFontHeight(title).ShouldBe(font.Height);
		GetRenderedFontHeight(resultEditor).ShouldBe(font.Height);
		AssertDpiScaledIconColumn(tableLayout, icons, control.DeviceDpi);
		AssertAlignedWithFirstLine(tableLayout, icons.Single(icon => tableLayout.GetPositionFromControl(icon).Row == 0), title);
		AssertAlignedWithFirstLine(
			tableLayout,
			icons.Single(icon => tableLayout.GetPositionFromControl(icon).Row == tableLayout.GetPositionFromControl(resultEditor).Row),
			resultEditor);
		title.Height.ShouldBeGreaterThan(GetRenderedFontHeight(title));
		resultEditor.Height.ShouldBeGreaterThan(GetRenderedFontHeight(resultEditor));
	}

	[TestCase(9)]
	[TestCase(18)]
	public void Reasoning_Icon_Stays_Aligned_With_Its_Status_Line(int fontSize)
	{
		using var font = new Font(SystemFonts.MessageBoxFont!.FontFamily, fontSize);
		using var host = new Panel { Size = new Size(180, 400) };
		using var chatControl = CreateChatControl(host, font);
		var control = CreateReasoningControl(chatControl);
		PerformLayout(host, chatControl);
		control.PerformLayout();

		var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
		var icon = DescendantsAndSelf(control)
			.OfType<LabelControl>()
			.Single(label => label.ImageOptions.SvgImage is not null);
		var title = DescendantsAndSelf(control)
			.OfType<LabelControl>()
			.Single(label => label.Text == "...");

		GetRenderedFontHeight(title).ShouldBe(font.Height);
		AssertDpiScaledIconColumn(tableLayout, [icon], control.DeviceDpi);
		AssertAlignedWithFirstLine(tableLayout, icon, title);
	}

	private static DXChatControl CreateChatControl(Panel host, Font messageFont)
	{
		var control = new DXChatControl
		{
			Dock = DockStyle.Fill,
			MessageFont = messageFont
		};
		host.Controls.Add(control);
		host.CreateControl();
		control.CreateControl();
		return control;
	}

	private static Control CreateFunctionCallControl(DXChatControl chatControl)
	{
		return (Control)chatControl.AddMessage(
			new NamedSender("Tool"),
			new FunctionCallMessageContent(
				"call-1",
				"lookup_with_a_deliberately_long_function_name_that_wraps",
				new Dictionary<string, object?> { ["query"] = "TinyChat" },
				"A deliberately long result that wraps across several lines so the arrow remains aligned with the first line instead of drifting into the middle of the complete result row."));
	}

	private static Control CreateReasoningControl(DXChatControl chatControl)
	{
		chatControl.MessageFormatter = new PlainTextMessageFormatter();
		return (Control)chatControl.AddMessage(
			new NamedSender("Assistant"),
			new ReasoningMessageContent("Reasoning detail"));
	}

	private static void InvokeToggle(Control control)
	{
		control.GetType()
			.GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(control, [control, EventArgs.Empty]);
		PerformLayout(control.Parent!, control);
	}

	private static void PerformLayout(Control host, Control control)
	{
		host.PerformLayout();
		control.PerformLayout();
		Application.DoEvents();
	}

	private static void AssertAlignedWithFirstLine(
		TableLayoutPanel tableLayout,
		Control icon,
		Control textControl)
	{
		var iconPosition = tableLayout.GetPositionFromControl(icon);
		var textPosition = tableLayout.GetPositionFromControl(textControl);
		iconPosition.Row.ShouldBe(textPosition.Row);

		var iconCenter = icon.Top + (icon.Height / 2f);
		var firstLineCenter = textControl.Top + (GetRenderedFontHeight(textControl) / 2f);
		Math.Abs(iconCenter - firstLineCenter).ShouldBeLessThanOrEqualTo(2f);
	}

	private static void AssertDpiScaledIconColumn(
		TableLayoutPanel tableLayout,
		IEnumerable<LabelControl> icons,
		int deviceDpi)
	{
		var expectedColumnWidth = ScaleLogicalPixels(20, deviceDpi);
		var expectedIconSize = new Size(ScaleLogicalPixels(14, deviceDpi), ScaleLogicalPixels(14, deviceDpi));
		tableLayout.GetColumnWidths()[0].ShouldBe(expectedColumnWidth);
		icons.ShouldAllBe(icon => icon.Width == expectedColumnWidth);
		icons.ShouldAllBe(icon => icon.ImageOptions.SvgImageSize == expectedIconSize);
	}

	private static int ScaleLogicalPixels(int logicalPixels, int deviceDpi)
	{
		return Math.Max(1, (int)Math.Round(logicalPixels * deviceDpi / 96f));
	}

	private static int GetRenderedFontHeight(Control control)
	{
		return control switch
		{
			LabelControl label => label.Appearance.Font.Height,
			HtmlContentControl editor => editor.Appearance.Font.Height,
			_ => control.Font.Height
		};
	}

	private static IEnumerable<Control> DescendantsAndSelf(Control control)
	{
		yield return control;

		foreach (Control child in control.Controls)
		{
			foreach (var descendant in DescendantsAndSelf(child))
				yield return descendant;
		}
	}
}
