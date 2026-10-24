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

		control.AutoSizeMode.ShouldBe(AutoSizeMode.GrowAndShrink);
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
	public void Preserves_And_Selects_Supplementary_Unicode_Text()
	{
		const string text = "Emoji 🔍 👩🏽‍💻 🇩🇪";
		using var control = CreateMessageControl(text);
		control.CreateControl();
		var textControl = GetTextControl(control);
		textControl.CreateControl();

		textControl.Text.ShouldBe(text);
		var selectionStart = text.IndexOf("👩🏽‍💻", StringComparison.Ordinal);
		textControl.Select(selectionStart, "👩🏽‍💻".Length);
		textControl.SelectedText.ShouldBe("👩🏽‍💻");
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
	public void Shrinks_To_The_Reflowed_Content_Height_When_History_Becomes_Wider()
	{
		var text = string.Join(" ", Enumerable.Repeat("wrapped message content", 12));
		using var history = new FlowLayoutMessageHistoryControl { Size = new Size(190, 1200) };
		using var control = CreateMessageControl(text);
		control.Dock = DockStyle.Fill;

		history.CreateControl();
		history.AppendMessageControl(control);
		Application.DoEvents();
		var textControl = GetTextControl(control);
		var narrowTextHeight = textControl.Height;
		var narrowControlHeight = control.Height;

		history.Width = 460;
		history.PerformLayout();
		Application.DoEvents();

		var occupiedBottom = control.Controls
			.Cast<Control>()
			.Where(child => child.Visible)
			.Max(child => child.Bottom) + control.Padding.Bottom;

		textControl.Width.ShouldBeGreaterThan(300);
		textControl.Height.ShouldBeLessThan(narrowTextHeight);
		control.Height.ShouldBeLessThan(narrowControlHeight);
		control.Height.ShouldBeLessThanOrEqualTo(occupiedBottom + 1);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Collapsing_A_Structured_Message_Releases_Height_And_Repositions_The_Following_Message(
		bool reasoning)
	{
		using var history = new FlowLayoutMessageHistoryControl { Size = new Size(300, 1200) };
		using var structuredControl = reasoning
			? CreateReasoningMessageControl()
			: CreateFunctionCallMessageControl();
		using var followingControl = CreateMessageControl("Following answer");
		structuredControl.Dock = DockStyle.Fill;
		followingControl.Dock = DockStyle.Fill;

		history.CreateControl();
		history.AppendMessageControl((IChatMessageControl)structuredControl);
		history.AppendMessageControl(followingControl);
		Application.DoEvents();

		structuredControl.ShouldBeAssignableTo<Panel>().AutoSizeMode.ShouldBe(AutoSizeMode.GrowAndShrink);
		DescendantsAndSelf(structuredControl)
			.OfType<TableLayoutPanel>()
			.Single()
			.AutoSizeMode
			.ShouldBe(AutoSizeMode.GrowAndShrink);

		InvokeToggleWithoutExternalLayout(structuredControl);
		Application.DoEvents();
		var expandedHeight = structuredControl.Height;
		var followingTopWhileExpanded = followingControl.Top;

		InvokeToggleWithoutExternalLayout(structuredControl);
		Application.DoEvents();

		structuredControl.Height.ShouldBeLessThan(expandedHeight);
		followingControl.Top.ShouldBeLessThan(followingTopWhileExpanded);
		structuredControl.Bottom.ShouldBeLessThanOrEqualTo(followingControl.Top);
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
	public void Late_Function_Result_Gets_Its_Own_Fully_Laid_Out_Row()
	{
		var content = new FunctionCallMessageContent(
			"call-late",
			"get_weather_with_a_deliberately_long_function_name_that_wraps",
			new Dictionary<string, object?>
			{
				["city"] = "San Francisco, California",
				["unit"] = "Celsius"
			})
		{
			IsFunctionExecuting = true
		};
		var resultText = string.Join(" ", Enumerable.Repeat("Weather result arrived after the call started.", 8));
		using var history = new FlowLayoutMessageHistoryControl { Size = new Size(240, 1000) };
		using var control = CreateFunctionCallMessageControl(content);
		using var followingControl = CreateMessageControl("Following assistant answer");
		control.Dock = DockStyle.Fill;
		followingControl.Dock = DockStyle.Fill;

		history.CreateControl();
		history.AppendMessageControl((IChatMessageControl)control);
		history.AppendMessageControl(followingControl);
		Application.DoEvents();
		InvokeToggleWithoutExternalLayout(control);
		Application.DoEvents();

		var textControls = GetTextControls(control);
		var argumentsControl = textControls.Single(textControl => textControl.Text.Contains("San Francisco", StringComparison.Ordinal));
		var resultControl = textControls.Single(textControl => textControl != argumentsControl);
		resultControl.Visible.ShouldBeFalse();

		content.SetResult(resultText);
		Application.DoEvents();

		var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
		var argumentsRow = tableLayout.GetPositionFromControl(argumentsControl).Row;
		var resultRow = tableLayout.GetPositionFromControl(resultControl).Row;
		var rowHeights = tableLayout.GetRowHeights();
		var resultRowTop = rowHeights.Take(resultRow).Sum();
		var resultIcon = tableLayout.GetControlFromPosition(0, resultRow)!;
		var toolIcon = tableLayout.GetControlFromPosition(0, 0)!;
		var title = tableLayout.GetControlFromPosition(1, 0)!;

		resultControl.Visible.ShouldBeTrue();
		resultControl.Text.ShouldBe(resultText + " ");
		resultControl.Height.ShouldBeGreaterThan(resultControl.Font.Height);
		title.Height.ShouldBeGreaterThan(title.Font.Height);
		resultIcon.ShouldNotBeNull();
		toolIcon.ShouldNotBeNull();
		title.ShouldNotBeNull();
		tableLayout.ColumnStyles[0].Width.ShouldBe((float)toolIcon.Width);
		resultRow.ShouldBe(argumentsRow + 1);
		resultControl.Top.ShouldBeGreaterThanOrEqualTo(argumentsControl.Bottom);
		resultControl.Top.ShouldBeGreaterThanOrEqualTo(resultRowTop);
		resultControl.Bottom.ShouldBeLessThanOrEqualTo(resultRowTop + rowHeights[resultRow]);
		rowHeights[resultRow].ShouldBeGreaterThanOrEqualTo(resultControl.Height);
		AssertAlignedWithFirstTextLine(resultIcon, resultControl);
		AssertAlignedWithFirstTextLine(toolIcon, title);
		control.Bottom.ShouldBeLessThanOrEqualTo(followingControl.Top);
	}

	[Test]
	public void Short_Late_Function_Result_Has_A_Fully_Visible_Text_Line()
	{
		var content = new FunctionCallMessageContent(
			"call-short",
			"getCurrentWeather",
			new Dictionary<string, object?> { ["city"] = "San Francisco" })
		{
			IsFunctionExecuting = true
		};
		using var history = new FlowLayoutMessageHistoryControl { Size = new Size(360, 800) };
		using var control = CreateFunctionCallMessageControl(content);
		control.Dock = DockStyle.Fill;
		((IChatMessageControl)control).ShowSenderHeader(false);

		history.CreateControl();
		history.AppendMessageControl((IChatMessageControl)control);
		Application.DoEvents();
		InvokeToggleWithoutExternalLayout(control);
		content.SetResult("Weather in San Francisco: 22°C, cloudy.");
		history.PerformLayout();
		Application.DoEvents();

		var textControls = GetTextControls(control);
		var resultControl = textControls.Single(textControl => textControl.Text.StartsWith("Weather", StringComparison.Ordinal));
		var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
		var resultRow = tableLayout.GetPositionFromControl(resultControl).Row;
		var rowHeight = tableLayout.GetRowHeights()[resultRow];
		var requiredLineHeight = (int)Math.Ceiling(resultControl.Font.GetHeight(resultControl.DeviceDpi));

		resultControl.Visible.ShouldBeTrue();
		resultControl.ClientSize.Height.ShouldBeGreaterThanOrEqualTo(requiredLineHeight);
		rowHeight.ShouldBeGreaterThanOrEqualTo(resultControl.Height + resultControl.Margin.Vertical);
		resultControl.Bottom.ShouldBeLessThanOrEqualTo(tableLayout.ClientSize.Height);
	}

	[Test]
	public void Multiple_Short_Function_Results_Keep_Their_Full_Row_After_Expansion()
	{
		using var history = new FlowLayoutMessageHistoryControl { Size = new Size(360, 800) };
		var cases = new[]
		{
			("San Francisco", "Weather in San Francisco: 22°C, cloudy."),
			("Los Angeles", "Weather in Los Angeles: 24°C, sunny."),
			("Berlin", "Weather in Berlin: 18°C, rain.")
		};
		var controls = new List<Control>();
		var pendingResults = new List<(FunctionCallMessageContent Content, string Result)>();

		history.CreateControl();
		foreach (var (city, result) in cases)
		{
			var content = new FunctionCallMessageContent(
				$"call-{city}",
				"getCurrentWeather",
				new Dictionary<string, object?> { ["city"] = city })
			{
				IsFunctionExecuting = true
			};
			var control = CreateFunctionCallMessageControl(content);
			control.Dock = DockStyle.Fill;
			((IChatMessageControl)control).ShowSenderHeader(false);
			history.AppendMessageControl((IChatMessageControl)control);
			controls.Add(control);
			pendingResults.Add((content, result));
		}
		Application.DoEvents();

		try
		{
			foreach (var control in controls)
				InvokeToggleWithoutExternalLayout(control);
			Application.DoEvents();
			foreach (var (content, result) in pendingResults)
				content.SetResult(result);
			Application.DoEvents();
			AssertResultRows();

			history.PerformLayout();
			Application.DoEvents();
			AssertResultRows();

			void AssertResultRows()
			{
				foreach (var control in controls)
				{
					var resultControl = GetTextControls(control)
						.Single(textControl => textControl.Text.StartsWith("Weather", StringComparison.Ordinal));
					var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
					var resultRow = tableLayout.GetPositionFromControl(resultControl).Row;
					var rowHeights = tableLayout.GetRowHeights();
					var rowTop = rowHeights.Take(resultRow).Sum();
					var requiredHeight = resultControl.Font.Height + 2;
					var lastCharacterPosition = resultControl.GetPositionFromCharIndex(resultControl.TextLength - 1);

					resultControl.Visible.ShouldBeTrue();
					resultControl.Height.ShouldBeGreaterThanOrEqualTo(requiredHeight);
					resultControl.MinimumSize.Height.ShouldBeGreaterThanOrEqualTo(requiredHeight);
					resultControl.Height.ShouldBeGreaterThanOrEqualTo(
						resultControl.GetPreferredSize(new Size(Math.Max(1, resultControl.Width), 0)).Height);
					rowHeights[resultRow].ShouldBeGreaterThanOrEqualTo(resultControl.Height + resultControl.Margin.Vertical);
					resultControl.Top.ShouldBeGreaterThanOrEqualTo(rowTop);
					resultControl.Bottom.ShouldBeLessThanOrEqualTo(rowTop + rowHeights[resultRow]);
					resultControl.Bottom.ShouldBeLessThanOrEqualTo(tableLayout.ClientSize.Height);
					(lastCharacterPosition.Y + resultControl.Font.Height)
						.ShouldBeLessThanOrEqualTo(resultControl.ClientSize.Height + 1);
				}
			}
		}
		finally
		{
			foreach (var control in controls)
				control.Dispose();
		}
	}

	[TestCase(96, 20)]
	[TestCase(192, 40)]
	public void Function_Call_Icon_Column_Scales_From_Logical_Pixels(int dpi, int expectedWidth)
	{
		var controlType = typeof(ChatControl).Assembly.GetType("TinyChat.FunctionCallMessageControl")!;
		var scaleMethod = controlType.GetMethod("ScaleLogicalPixels", BindingFlags.Static | BindingFlags.NonPublic)!;

		var scaledWidth = (int)scaleMethod.Invoke(null, [20, dpi])!;

		scaledWidth.ShouldBe(expectedWidth);
	}

	[Test]
	public void Collapsed_Function_Call_Does_Not_Reserve_Hidden_Detail_Rows_After_Late_Result()
	{
		var content = new FunctionCallMessageContent(
			"call-collapsed",
			"get_weather",
			new Dictionary<string, object?>
			{
				["city"] = string.Join(" ", Enumerable.Repeat("San Francisco", 12))
			})
		{
			IsFunctionExecuting = true
		};
		using var history = new FlowLayoutMessageHistoryControl { Size = new Size(460, 800) };
		using var control = CreateFunctionCallMessageControl(content);
		control.Dock = DockStyle.Fill;
		((IChatMessageControl)control).ShowSenderHeader(false);

		history.CreateControl();
		history.AppendMessageControl((IChatMessageControl)control);
		Application.DoEvents();
		content.SetResult(string.Join(" ", Enumerable.Repeat("Late multiline function result", 16)));
		Application.DoEvents();

		var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
		var rowHeights = tableLayout.GetRowHeights();

		rowHeights[1].ShouldBe(0);
		rowHeights[2].ShouldBe(0);
		control.Height.ShouldBeLessThanOrEqualTo(rowHeights[0] + control.Padding.Vertical + 1);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Expanded_Reasoning_Two_Collapsed_Tool_Calls_And_Answer_Have_No_Blank_Controls(
		bool hideReasoningSender)
	{
		using var history = new FlowLayoutMessageHistoryControl { Size = new Size(460, 500) };
		using var reasoningControl = CreateReasoningMessageControl();
		var reasoningContent = new ReasoningMessageContent("Starting analysis.");
		((IChatMessageControl)reasoningControl).Message = new ChatMessage(new NamedSender("Assistant"), reasoningContent);
		((IChatMessageControl)reasoningControl).ShowSenderHeader(!hideReasoningSender);
		reasoningControl.Dock = DockStyle.Fill;
		history.CreateControl();
		history.AppendMessageControl((IChatMessageControl)reasoningControl);

		foreach (var chunk in Enumerable.Repeat(" Streaming reasoning with enough text to wrap naturally.", 12))
			reasoningContent.AppendText(chunk);
		reasoningContent.SetDone();
		var reasoningDetail = GetTextControls(reasoningControl).Single();
		var hiddenNarrowDetailWidth = reasoningDetail.Width;
		var hiddenNarrowDetailHeight = reasoningDetail.Height;
		hiddenNarrowDetailHeight.ShouldBeLessThanOrEqualTo(reasoningDetail.Font.Height + 2);
		InvokeToggleWithoutExternalLayout(reasoningControl);
		var reasoningTableAfterExpand = DescendantsAndSelf(reasoningControl).OfType<TableLayoutPanel>().Single();
		reasoningTableAfterExpand.Dock.ShouldBe(DockStyle.Top);
		var occupiedBottomAfterExpand = reasoningControl.Controls
			.Cast<Control>()
			.Where(child => child.Visible)
			.Max(child => child.Bottom) + reasoningControl.Padding.Bottom;
		reasoningDetail.Width.ShouldBeGreaterThan(hiddenNarrowDetailWidth);
		reasoningDetail.Height.ShouldBeGreaterThan(hiddenNarrowDetailHeight);
		reasoningDetail.Bottom.ShouldBeLessThanOrEqualTo(reasoningTableAfterExpand.ClientSize.Height);
		reasoningControl.Height.ShouldBeLessThanOrEqualTo(occupiedBottomAfterExpand + 1);

		var toolControls = new List<Control>();
		for (var index = 0; index < 2; index++)
		{
			var toolContent = new FunctionCallMessageContent(
				$"call-{index}",
				"GetCurrentWeather",
				new Dictionary<string, object?> { ["city"] = index == 0 ? "San Francisco, CA" : "Los Angeles, CA" })
			{
				IsFunctionExecuting = true
			};
			var toolControl = CreateFunctionCallMessageControl(toolContent);
			toolControl.Dock = DockStyle.Fill;
			((IChatMessageControl)toolControl).ShowSenderHeader(false);
			history.AppendMessageControl((IChatMessageControl)toolControl);
			toolContent.SetResult(index == 0
				? "Weather in San Francisco, CA: -2°C, partly cloudy, humidity 38%."
				: "Weather in Los Angeles, CA: -1°C, light rain, humidity 53%.");
			toolControls.Add(toolControl);
		}

		using var answerControl = CreateMessageControl("I have both weather results and will summarize them now.");
		answerControl.Dock = DockStyle.Fill;
		((IChatMessageControl)answerControl).ShowSenderHeader(false);
		history.AppendMessageControl(answerControl);
		Application.DoEvents();

		var orderedControls = new[] { reasoningControl }.Concat(toolControls).Append(answerControl).ToArray();

		var reasoningTable = DescendantsAndSelf(reasoningControl).OfType<TableLayoutPanel>().Single();
		var reasoningOccupiedBottom = reasoningControl.Controls
			.Cast<Control>()
			.Where(child => child.Visible)
			.Max(child => child.Bottom) + reasoningControl.Padding.Bottom;
		reasoningDetail.Width.ShouldBeGreaterThan(300);
		reasoningDetail.Bottom.ShouldBeLessThanOrEqualTo(reasoningTable.ClientSize.Height);
		reasoningControl.Height.ShouldBeLessThanOrEqualTo(reasoningOccupiedBottom + 1);

		foreach (var toolControl in toolControls)
		{
			var tableLayout = DescendantsAndSelf(toolControl).OfType<TableLayoutPanel>().Single();
			var title = tableLayout.GetControlFromPosition(1, 0)!;
			var rowHeights = tableLayout.GetRowHeights();
			title.Text.ShouldStartWith("GetCurrentWeather");
			title.Visible.ShouldBeTrue();
			title.Width.ShouldBeGreaterThan(0);
			title.Height.ShouldBeGreaterThan(0);
			title.Bounds.IntersectsWith(tableLayout.ClientRectangle).ShouldBeTrue();
			rowHeights[1].ShouldBe(0);
			rowHeights[2].ShouldBe(0);
			toolControl.Height.ShouldBeLessThanOrEqualTo(rowHeights[0] + toolControl.Padding.Vertical + 1);
		}

		for (var index = 1; index < orderedControls.Length; index++)
		{
			var previous = orderedControls[index - 1];
			var current = orderedControls[index];
			var expectedMaximumGap = previous.Margin.Bottom + current.Margin.Top;
			(current.Top - previous.Bottom).ShouldBeLessThanOrEqualTo(expectedMaximumGap);
		}
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

	[TestCase(false)]
	[TestCase(true)]
	public void Expanded_Reasoning_Detail_Does_Not_Overlap_The_Following_Message_In_History(
		bool expandBeforeStreaming)
	{
		using var history = new FlowLayoutMessageHistoryControl { Size = new Size(460, 500) };
		using var reasoningControl = CreateReasoningMessageControl();
		using var answerControl = CreateMessageControl(
			"That sounds like some good news for us both today. How was your day?");
		var reasoningContent = new ReasoningMessageContent("The user is greeting in German.");
		((IChatMessageControl)reasoningControl).Message = new ChatMessage(
			new NamedSender("Assistant"),
			reasoningContent);

		history.CreateControl();
		history.AppendMessageControl((IChatMessageControl)reasoningControl);
		Application.DoEvents();

		if (expandBeforeStreaming)
			InvokeToggleWithoutExternalLayout(reasoningControl);

		foreach (var chunk in Enumerable.Repeat(" This reasoning must wrap without being clipped.", 8))
			reasoningContent.AppendText(chunk);
		reasoningContent.SetDone();
		history.AppendMessageControl(answerControl);
		Application.DoEvents();

		if (!expandBeforeStreaming)
			InvokeToggleWithoutExternalLayout(reasoningControl);
		Application.DoEvents();

		var detailControl = GetTextControls(reasoningControl).Single();
		var tableLayout = DescendantsAndSelf(reasoningControl).OfType<TableLayoutPanel>().Single();
		var detailRow = tableLayout.GetPositionFromControl(detailControl).Row;
		detailControl.Visible.ShouldBeTrue();
		tableLayout.GetRowHeights()[detailRow].ShouldBeGreaterThanOrEqualTo(detailControl.Height);
		detailControl.Height.ShouldBeGreaterThan(detailControl.Font.Height);
		detailControl.Bottom.ShouldBeLessThanOrEqualTo(tableLayout.ClientSize.Height);
		tableLayout.Bottom.ShouldBeLessThanOrEqualTo(reasoningControl.ClientSize.Height - reasoningControl.Padding.Bottom);
		reasoningControl.Bottom.ShouldBeLessThanOrEqualTo(answerControl.Top);
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

	private static Control CreateFunctionCallMessageControl(FunctionCallMessageContent? content = null)
	{
		var type = typeof(ChatControl).Assembly.GetType("TinyChat.FunctionCallMessageControl")!;
		var control = (Control)Activator.CreateInstance(type, nonPublic: true)!;
		((IFunctionCallMessageControl)control).AllowExpand = true;
		((IChatMessageControl)control).Message = new ChatMessage(
			new NamedSender("Tool"),
			content ?? new FunctionCallMessageContent(
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
		InvokeToggleWithoutExternalLayout(control);
		PerformLayout(control.Parent!, control);
	}

	private static void InvokeToggleWithoutExternalLayout(Control control)
	{
		control.GetType()
			.GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(control, [control, EventArgs.Empty]);
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

	private static void AssertAlignedWithFirstTextLine(Control icon, Control textControl)
	{
		var iconCenter = icon.Top + (icon.Height / 2f);
		var firstLineHeight = (int)Math.Ceiling(textControl.Font.GetHeight(textControl.DeviceDpi));
		var firstLineCenter = textControl.Top + (firstLineHeight / 2f);

		Math.Abs(iconCenter - firstLineCenter).ShouldBeLessThanOrEqualTo(1f);
	}
}
