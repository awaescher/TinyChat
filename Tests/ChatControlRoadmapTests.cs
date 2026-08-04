using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.Utils.Html;
using DevExpress.Utils.Html.Internal;
using DevExpress.XtraEditors;
using Shouldly;
using TinyChat;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ChatControlRoadmapTests
{
	public class DesignerProperties
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Preserves_Splitter_Position_Set_Before_Control_Creation(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress, createControl: false);
			control.Size = new Size(400, 420);
			control.SplitterPosition = 96;

			InitializeControl(control);

			control.SplitterPosition.ShouldBe(96);
			((ISplitContainerControl)control.SplitContainerControl!).SplitterPosition.ShouldBe(96);
		}

		[Test]
		public void Exposes_Appearance_Properties_To_The_Windows_Forms_Designer()
		{
			using var control = new ChatControl();
			var properties = TypeDescriptor.GetProperties(control);

			foreach (var propertyName in new[]
			{
				nameof(ChatControl.MessageFont),
				nameof(ChatControl.InputFont),
				nameof(ChatControl.WelcomeFont),
				nameof(ChatControl.WelcomeMessage),
				nameof(ChatControl.HistoryPadding),
				nameof(ChatControl.MessagePadding),
				nameof(ChatControl.MessageMargin),
				nameof(ChatControl.InputPadding),
				nameof(ChatControl.WelcomeTextAlign),
				nameof(ChatControl.IsSplitterFixed),
				nameof(ChatControl.SplitterPosition)
			})
			{
				var property = properties[propertyName];
				property.ShouldNotBeNull();
				property.IsBrowsable.ShouldBeTrue();
				property.SerializationVisibility.ShouldBe(DesignerSerializationVisibility.Visible);
			}
		}

		[Test]
		public void Resets_Specialized_Fonts_To_Their_Documented_Defaults()
		{
			using var control = new ChatControl();
			using var customFont = new Font("Consolas", 11f);
			var properties = TypeDescriptor.GetProperties(control);

			foreach (var propertyName in new[]
			{
				nameof(ChatControl.MessageFont),
				nameof(ChatControl.InputFont),
				nameof(ChatControl.WelcomeFont)
			})
			{
				var property = properties[propertyName]!;
				property.ShouldSerializeValue(control).ShouldBeFalse();

				property.SetValue(control, customFont);

				property.ShouldSerializeValue(control).ShouldBeTrue();
				property.CanResetValue(control).ShouldBeTrue();

				property.ResetValue(control);

				property.ShouldSerializeValue(control).ShouldBeFalse();
			}

			control.MessageFont.ShouldBe(control.Font);
			control.InputFont.ShouldBe(control.Font);
			control.WelcomeFont.Name.ShouldBe("Tahoma");
			control.WelcomeFont.Size.ShouldBe(14f);
		}

		[Test]
		public void Resets_Layout_Properties_To_Their_Suite_Neutral_Defaults()
		{
			using var control = new ChatControl();
			var properties = TypeDescriptor.GetProperties(control);
			var cases = new (string PropertyName, object CustomValue, object DefaultValue)[]
			{
				(nameof(ChatControl.HistoryPadding), new Padding(1, 2, 3, 4), Padding.Empty),
				(nameof(ChatControl.MessagePadding), new Padding(9), new Padding(8)),
				(nameof(ChatControl.MessageMargin), new Padding(5), new Padding(3)),
				(nameof(ChatControl.InputPadding), new Padding(12), new Padding(8)),
				(nameof(ChatControl.WelcomeTextAlign), ContentAlignment.BottomRight, ContentAlignment.MiddleCenter),
				(nameof(ChatControl.IsSplitterFixed), true, false),
				(nameof(ChatControl.SplitterPosition), 91, 60)
			};

			foreach (var (propertyName, customValue, defaultValue) in cases)
			{
				var property = properties[propertyName]!;
				property.ShouldSerializeValue(control).ShouldBeFalse();

				property.SetValue(control, customValue);

				property.ShouldSerializeValue(control).ShouldBeTrue();
				property.CanResetValue(control).ShouldBeTrue();

				property.ResetValue(control);

				property.GetValue(control).ShouldBe(defaultValue);
				property.ShouldSerializeValue(control).ShouldBeFalse();
			}
		}

		[Test]
		public void Leaves_Custom_Controls_Without_Layout_Capabilities_Untouched()
		{
			using var control = new NonOptInLayoutChatControl
			{
				Size = new Size(400, 420),
				InputPadding = new Padding(15),
				MessagePadding = new Padding(16),
				MessageMargin = new Padding(17)
			};
			InitializeControl(control);

			var messageControl = (Control)control.AddMessage(
				new NamedSender("Custom"),
				new StringMessageContent("Unchanged"));

			control.InputControl!.Padding.ShouldBe(new Padding(5));
			messageControl.Padding.ShouldBe(new Padding(6));
			messageControl.Margin.ShouldBe(new Padding(7));
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Applies_Fonts_And_Live_Welcome_Text_To_Both_Control_Suites(bool useDevExpress)
		{
			using var messageFont = new Font("Segoe UI", 11f);
			using var updatedMessageFont = new Font("Consolas", 10f);
			using var inputFont = new Font("Segoe UI", 10f);
			using var welcomeFont = new Font("Segoe UI", 14f);
			using var control = CreateChatControl(useDevExpress, createControl: false);
			control.MessageFont = messageFont;
			control.InputFont = inputFont;
			control.WelcomeFont = welcomeFont;
			control.WelcomeMessage = "Start here";
			InitializeControl(control);

			var messageControl = (Control)control.AddMessage(
				new NamedSender("Assistant"),
				new StringMessageContent("Hello"));
			var functionControl = (Control)control.AddMessage(
				new NamedSender("Tool"),
				new FunctionCallMessageContent(
					"call-1",
					"lookup",
					new Dictionary<string, object?>()));
			var reasoningControl = (Control)control.AddMessage(
				new NamedSender("Reasoner"),
				new ReasoningMessageContent("Thinking"));

			messageControl.Font.ShouldBe(messageFont);
			functionControl.Font.ShouldBe(messageFont);
			reasoningControl.Font.ShouldBe(messageFont);
			AssertSenderFont(messageControl, "Assistant", messageFont, useDevExpress);
			AssertSenderFont(functionControl, "Tool", messageFont, useDevExpress);
			AssertSenderFont(reasoningControl, "Reasoner", messageFont, useDevExpress);
			control.InputControl!.Font.ShouldBe(inputFont);
			GetInputEditor(control.InputControl, useDevExpress).Font.ShouldBe(inputFont);
			control.WelcomeControl!.Font.ShouldBe(welcomeFont);
			var welcomeContent = DescendantsAndSelf(control.WelcomeControl)
				.Single(child => child.Text == "Start here");
			welcomeContent.Font.ShouldBe(welcomeFont);

			control.MessageFont = updatedMessageFont;
			control.WelcomeMessage = "Updated welcome";

			messageControl.Font.ShouldBe(updatedMessageFont);
			functionControl.Font.ShouldBe(updatedMessageFont);
			reasoningControl.Font.ShouldBe(updatedMessageFont);
			AssertSenderFont(messageControl, "Assistant", updatedMessageFont, useDevExpress);
			AssertSenderFont(functionControl, "Tool", updatedMessageFont, useDevExpress);
			AssertSenderFont(reasoningControl, "Reasoner", updatedMessageFont, useDevExpress);
			GetSelectableMessageEditor(messageControl).Font.ShouldBe(updatedMessageFont);
			DescendantsAndSelf(control.WelcomeControl).ShouldContain(child => child.Text == "Updated welcome");
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Applies_Layout_And_Global_Colors_To_All_Built_In_Control_Types(bool useDevExpress)
		{
			var initialForeColor = Color.DarkSlateBlue;
			var initialBackColor = Color.AliceBlue;
			var initialMessagePadding = new Padding(7, 8, 9, 10);
			var initialMessageMargin = new Padding(4);
			using var control = CreateChatControl(useDevExpress, createControl: false);
			control.ForeColor = initialForeColor;
			control.BackColor = initialBackColor;
			control.HistoryPadding = new Padding(6);
			control.MessagePadding = initialMessagePadding;
			control.MessageMargin = initialMessageMargin;
			control.InputPadding = new Padding(11);
			control.WelcomeTextAlign = ContentAlignment.BottomRight;
			control.IsSplitterFixed = true;
			InitializeControl(control);

			var splitContainerControl = control.SplitContainerControl!;
			var splitContainer = (ISplitContainerControl)splitContainerControl;
			var inputControl = control.InputControl!;
			splitContainer.HistoryPanel!.Padding.ShouldBe(new Padding(6));
			((IChatSplitContainerLayoutControl)splitContainerControl).IsSplitterFixed.ShouldBeTrue();
			control.MessageHistoryControl!.ForeColor.ShouldBe(initialForeColor);
			control.MessageHistoryControl.BackColor.ShouldBe(initialBackColor);
			((IChatInputLayoutControl)inputControl).ContentPadding.ShouldBe(new Padding(11));
			inputControl.ForeColor.ShouldBe(initialForeColor);
			inputControl.BackColor.ShouldBe(initialBackColor);
			var inputEditor = GetInputEditor(inputControl, useDevExpress);
			inputEditor.ForeColor.ShouldBe(initialForeColor);
			inputEditor.BackColor.ShouldBe(initialBackColor);
			AssertWelcomeAlignment(control.WelcomeControl!, useDevExpress, ContentAlignment.BottomRight);

			var messageControls = new[]
			{
				(Control)control.AddMessage(
					new NamedSender("Assistant"),
					new StringMessageContent("Regular")),
				(Control)control.AddMessage(
					new NamedSender("Tool"),
					new FunctionCallMessageContent(
						"call-1",
						"lookup",
						new Dictionary<string, object?> { ["query"] = "TinyChat" })),
				(Control)control.AddMessage(
					new NamedSender("Assistant"),
					new ReasoningMessageContent("Thinking"))
			};

			foreach (var messageControl in messageControls)
			{
				messageControl.Margin.ShouldBe(initialMessageMargin);
				((IChatMessageLayoutControl)messageControl).ContentPadding.ShouldBe(initialMessagePadding);
				messageControl.ForeColor.ShouldBe(initialForeColor);
				messageControl.BackColor.ShouldBe(initialBackColor);
				DescendantsAndSelf(messageControl)
					.Where(child => child is Label or LabelControl or TextBox or MemoEdit or HtmlContentControl)
					.ShouldAllBe(child => child.ForeColor == initialForeColor);
			}

			var updatedForeColor = Color.DarkGreen;
			var updatedBackColor = Color.Honeydew;
			var updatedMessagePadding = new Padding(12);
			var updatedMessageMargin = new Padding(2, 3, 4, 5);
			control.ForeColor = updatedForeColor;
			control.BackColor = updatedBackColor;
			control.HistoryPadding = new Padding(3, 4, 5, 6);
			control.MessagePadding = updatedMessagePadding;
			control.MessageMargin = updatedMessageMargin;
			control.InputPadding = new Padding(5);
			control.WelcomeTextAlign = ContentAlignment.TopLeft;
			control.IsSplitterFixed = false;

			splitContainer.HistoryPanel.Padding.ShouldBe(new Padding(3, 4, 5, 6));
			((IChatSplitContainerLayoutControl)splitContainerControl).IsSplitterFixed.ShouldBeFalse();
			((IChatInputLayoutControl)inputControl).ContentPadding.ShouldBe(new Padding(5));
			inputEditor.ForeColor.ShouldBe(updatedForeColor);
			inputEditor.BackColor.ShouldBe(updatedBackColor);
			AssertWelcomeAlignment(control.WelcomeControl!, useDevExpress, ContentAlignment.TopLeft);
			foreach (var messageControl in messageControls)
			{
				messageControl.Margin.ShouldBe(updatedMessageMargin);
				((IChatMessageLayoutControl)messageControl).ContentPadding.ShouldBe(updatedMessagePadding);
				messageControl.ForeColor.ShouldBe(updatedForeColor);
				messageControl.BackColor.ShouldBe(updatedBackColor);
			}
		}
	}

	public class StartNewChatMethod
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Clears_Conversation_And_Complete_Input_Draft(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress);
			var inputControl = control.InputControl!;
			var attachmentInput = (IChatAttachmentInputControl)inputControl;
			var draftInput = (IChatInputDraftControl)inputControl;
			var editor = GetInputEditor(inputControl, useDevExpress);
			editor.Text = "Unsent draft";
			attachmentInput.AddAttachments(
			[
				new ChatFileAttachment("notes.txt", "text/plain", "draft"u8.ToArray())
			]);
			var oldMessageControl = (Control)control.AddMessage(
				new NamedSender("User"),
				new StringMessageContent("Old conversation"));
			Application.DoEvents();
			var eventCount = 0;
			var eventObservedClearedState = false;
			control.NewChatStarted += (_, _) =>
			{
				eventCount++;
				eventObservedClearedState =
					!control.Messages.Any() &&
					!attachmentInput.PendingAttachments.Any() &&
					string.IsNullOrEmpty(editor.Text);
			};

			control.StartNewChat();
			Application.DoEvents();

			control.Messages.ShouldBeEmpty();
			attachmentInput.PendingAttachments.ShouldBeEmpty();
			editor.Text.ShouldBeEmpty();
			DescendantsAndSelf(control.MessageHistoryControl!)
				.OfType<IChatMessageControl>()
				.ShouldBeEmpty();
			oldMessageControl.IsDisposed.ShouldBeTrue();
			control.WelcomeControl!.Visible.ShouldBeTrue();
			eventCount.ShouldBe(1);
			eventObservedClearedState.ShouldBeTrue();
			draftInput.ShouldNotBeNull();
		}

		[Test]
		public void Cancels_The_Current_Operation_Before_Starting_The_New_Chat()
		{
			using var control = new TrackingChatControl();

			control.StartNewChat();

			control.CancelWasRequested.ShouldBeTrue();
		}

		[Test]
		public void Superseded_Operation_Cannot_Complete_A_Newer_Operation()
		{
			using var control = new ChatControl();
			using var firstOperation = new CancellationTokenSource();
			using var secondOperation = new CancellationTokenSource();
			var registerOperation = typeof(ChatControl).GetMethod(
				"RegisterCancellationSource",
				BindingFlags.Instance | BindingFlags.NonPublic)!;
			var completeOperation = typeof(ChatControl).GetMethod(
				"ClearCancellationSource",
				BindingFlags.Instance | BindingFlags.NonPublic)!;
			registerOperation.Invoke(control, [firstOperation, CancellationToken.None]);
			registerOperation.Invoke(control, [secondOperation, CancellationToken.None]);

			var firstStillOwnedTheInput = (bool)completeOperation.Invoke(control, [firstOperation])!;
			var secondStillOwnedTheInput = (bool)completeOperation.Invoke(control, [secondOperation])!;

			firstStillOwnedTheInput.ShouldBeFalse();
			secondStillOwnedTheInput.ShouldBeTrue();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Detaches_Function_Call_Updates_When_The_Chat_Is_Cleared(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress);
			var content = new FunctionCallMessageContent(
				"call-1",
				"lookup",
				new Dictionary<string, object?> { ["value"] = 42 });
			var messageControl = (Control)control.AddMessage(new NamedSender("Assistant"), content);
			GetPropertyChangedSubscribers(content).ShouldNotBeEmpty();

			control.StartNewChat();

			messageControl.IsDisposed.ShouldBeTrue();
			GetPropertyChangedSubscribers(content).ShouldBeEmpty();
		}
	}

	public class SelectableDevExpressMessageText
	{
		[Test]
		public void Uses_A_Permanently_Visible_DevExpress_Html_Control_And_Selects_A_Substring()
		{
			using var control = new DXChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new ChatMessage(
					new NamedSender("Assistant"),
					new StringMessageContent("Alpha beta gamma"))
			};
			control.CreateControl();
			var editor = control.Controls.OfType<HtmlContentControl>().Single();

			editor.AllowContentSelection.ShouldBe(DefaultBoolean.True);
			editor.AllowFocus.ShouldBeTrue();
			editor.AutoScroll.ShouldBeFalse();
			editor.Text.ShouldBe("Alpha beta gamma");
			control.Controls
				.OfType<LabelControl>()
				.ShouldNotContain(label => label.Text == "Alpha beta gamma");

			SelectHtmlText(editor, "beta");

			editor.Visible.ShouldBeTrue();
			editor.SelectedText.ShouldBe("beta");
		}

		[Test]
		public void Keeps_The_Same_Editor_And_Selection_While_Streaming()
		{
			var builder = new NotifyingStringBuilder().Append("**Started**");
			using var host = new Panel { Size = new Size(190, 500) };
			using var control = new DXChatMessageControl
			{
				MessageFormatter = new SimplifiedHtmlMessageFormatter("b"),
				Message = new ChatMessage(
					new NamedSender("Assistant"),
					new ChangingMessageContent(builder))
			};
			control.MinimumSize = new Size(190, 0);
			control.MaximumSize = new Size(190, 0);
			control.Width = 190;
			host.Controls.Add(control);
			host.CreateControl();
			control.CreateControl();
			var editor = control.Controls.OfType<HtmlContentControl>().Single();
			editor.Text.ShouldBe("Started");
			editor.Visible.ShouldBeTrue();
			editor.FindElementsByTag("b").ShouldNotBeEmpty();
			SelectHtmlText(editor, "Started");
			var initialHeight = editor.Height;

			builder.Append(" ").Append(string.Join(" ", Enumerable.Repeat("and continued", 20)));
			host.PerformLayout();
			control.PerformLayout();
			Application.DoEvents();

			control.Controls.OfType<HtmlContentControl>().Single().ShouldBeSameAs(editor);
			editor.Text.ShouldNotContain("<b>");
			editor.Text.ShouldContain("and continued");
			editor.FindElementsByTag("b").ShouldNotBeEmpty();
			editor.SelectedText.ShouldBe("Started");
			editor.Visible.ShouldBeTrue();
			editor.Height.ShouldBeGreaterThan(initialHeight);
		}

		[Test]
		public void Preserves_Formatted_Line_Breaks_In_Selectable_Copy_Text()
		{
			using var control = new DXChatMessageControl
			{
				MessageFormatter = new SimplifiedHtmlMessageFormatter("br"),
				Message = new ChatMessage(
					new NamedSender("Assistant"),
					new StringMessageContent("First<br>Second"))
			};
			var editor = control.Controls.OfType<HtmlContentControl>().Single();

			editor.Text.ShouldBe($"First{Environment.NewLine}Second");
			control.ToString().ShouldBe(editor.Text);
		}

		[Test]
		public void Renders_Supported_Formatting_Inside_The_Selectable_Html_Control()
		{
			using var control = new DXChatMessageControl
			{
				MessageFormatter = new SimplifiedHtmlMessageFormatter("b", "i", "font", "color", "size"),
				Message = new ChatMessage(
					new NamedSender("Assistant"),
					new StringMessageContent("**Bold** *italic* `code` <color=red>red</color> <size=+2>large</size>"))
			};
			control.CreateControl();
			var editor = control.Controls.OfType<HtmlContentControl>().Single();
			_ = editor.GetContentSize();

			editor.FindElementsByTag("b").ShouldNotBeEmpty();
			editor.FindElementsByTag("i").ShouldNotBeEmpty();
			editor.FindElementsByTag("span").Count().ShouldBeGreaterThanOrEqualTo(3);
			editor.HtmlTemplate.Template.ShouldNotContain("<color=");
			editor.HtmlTemplate.Template.ShouldNotContain("<font=");
			editor.Text.ShouldBe("Bold italic code red large");
		}

		[Test]
		public void Keeps_Only_The_Most_Recent_Html_Text_Selection()
		{
			using var first = new DXChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new ChatMessage(
					new NamedSender("Assistant"),
					new StringMessageContent("Alpha selection"))
			};
			using var second = new DXChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new ChatMessage(
					new NamedSender("Assistant"),
					new StringMessageContent("Beta selection"))
			};
			first.CreateControl();
			second.CreateControl();
			var firstEditor = first.Controls.OfType<HtmlContentControl>().Single();
			var secondEditor = second.Controls.OfType<HtmlContentControl>().Single();

			SelectHtmlText(firstEditor, "Alpha");
			firstEditor.SelectedText.ShouldBe("Alpha");

			SelectHtmlText(secondEditor, "Beta");

			firstEditor.SelectedText.ShouldBeEmpty();
			secondEditor.SelectedText.ShouldBe("Beta");
		}
	}

	public class SelectableStructuredMessageText
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Uses_Selectable_Editors_For_Function_Call_And_Reasoning_Details(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress);
			var functionControl = (Control)control.AddMessage(
				new NamedSender("Tool"),
				new FunctionCallMessageContent(
					"call-1",
					"lookup",
					new Dictionary<string, object?> { ["query"] = "TinyChat" },
					"result"));
			var reasoningControl = (Control)control.AddMessage(
				new NamedSender("Assistant"),
				new ReasoningMessageContent("Detailed reasoning"));

			var functionEditors = DescendantsAndSelf(functionControl)
				.Where(child => IsSelectableMessageEditor(child, useDevExpress))
				.ToArray();
			var reasoningEditors = DescendantsAndSelf(reasoningControl)
				.Where(child => IsSelectableMessageEditor(child, useDevExpress))
				.ToArray();

			functionEditors.Length.ShouldBe(2);
			reasoningEditors.Length.ShouldBe(1);
			functionEditors.ShouldContain(editor => editor.Text.Contains("query", StringComparison.Ordinal));
			functionEditors.ShouldContain(editor => editor.Text.Contains("result", StringComparison.Ordinal));
			DescendantsAndSelf(functionControl)
				.Where(IsNonSelectableLabel)
				.ShouldNotContain(label => label.Text.Contains("TinyChat", StringComparison.Ordinal));
			DescendantsAndSelf(reasoningControl)
				.Where(IsNonSelectableLabel)
				.ShouldNotContain(label => label.Text.Contains("Detailed reasoning", StringComparison.Ordinal));
			foreach (var editor in functionEditors.Concat(reasoningEditors))
				AssertUnfocusedSelectionIsHidden(editor);
		}

		[Test]
		public void DevExpress_Function_Call_Details_Grow_And_Do_Not_Toggle_From_The_Editor()
		{
			using var host = new Panel { Size = new Size(260, 500) };
			using var control = CreateDevExpressFunctionCallMessageControl();
			HostStructuredMessageControl(host, control, 260);
			var editors = DescendantsAndSelf(control).OfType<HtmlContentControl>().ToArray();

			editors.Length.ShouldBe(2);
			editors.ShouldAllBe(editor => !editor.Visible);
			var collapsedHeight = control.Height;

			InvokeToggle(control);

			editors.ShouldAllBe(editor => editor.Visible);
			var argumentsEditor = editors.Single(editor => editor.Text.Contains("TinyChat", StringComparison.Ordinal));
			var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
			var argumentsRow = tableLayout.GetPositionFromControl(argumentsEditor).Row;
			argumentsEditor.Height.ShouldBeGreaterThan(argumentsEditor.Font.Height);
			tableLayout.GetRowHeights()[argumentsRow].ShouldBeGreaterThanOrEqualTo(argumentsEditor.Height);
			control.Height.ShouldBeGreaterThan(collapsedHeight);
			SelectHtmlText(argumentsEditor, "TinyChat");
			argumentsEditor.SelectedText.ShouldBe("TinyChat");

			var parentMouseDownCount = 0;
			control.MouseDown += (_, _) => parentMouseDownCount++;
			InvokeMouseDown(argumentsEditor);
			InvokeClick(argumentsEditor);

			parentMouseDownCount.ShouldBe(0);
			editors.ShouldAllBe(editor => editor.Visible);
		}

		[Test]
		public void DevExpress_Reasoning_Detail_Grows_And_Does_Not_Toggle_From_The_Editor()
		{
			using var host = new Panel { Size = new Size(260, 500) };
			using var control = CreateDevExpressReasoningMessageControl();
			HostStructuredMessageControl(host, control, 260);
			var editor = DescendantsAndSelf(control).OfType<HtmlContentControl>().Single();

			editor.Visible.ShouldBeFalse();

			InvokeToggle(control);

			editor.Visible.ShouldBeTrue();
			editor.Text.ShouldStartWith("Thinking through the answer");
			var tableLayout = DescendantsAndSelf(control).OfType<TableLayoutPanel>().Single();
			var detailRow = tableLayout.GetPositionFromControl(editor).Row;
			editor.Height.ShouldBeGreaterThan(editor.Font.Height);
			tableLayout.GetRowHeights()[detailRow].ShouldBeGreaterThanOrEqualTo(editor.Height);
			SelectHtmlText(editor, "through");
			editor.SelectedText.ShouldBe("through");

			var parentMouseDownCount = 0;
			control.MouseDown += (_, _) => parentMouseDownCount++;
			InvokeMouseDown(editor);
			InvokeClick(editor);

			parentMouseDownCount.ShouldBe(0);
			editor.Visible.ShouldBeTrue();
		}
	}

	private static ChatControl CreateChatControl(bool useDevExpress, bool createControl = true)
	{
		var control = useDevExpress ? (ChatControl)new DXChatControl() : new ChatControl();
		control.ShowCopyButton = false;
		control.Size = new Size(400, 420);
		if (createControl)
			InitializeControl(control);
		return control;
	}

	private static void InitializeControl(ChatControl control)
	{
		if (control is not DXChatControl)
		{
			typeof(ChatControl)
				.GetMethod("OnCreateControl", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(control, null);
			return;
		}

		var host = new Panel { Size = control.Size };
		control.Dock = DockStyle.Fill;
		host.Controls.Add(control);
		host.CreateControl();
		control.CreateControl();
		control.Disposed += (_, _) => host.Dispose();
	}

	private static Control GetInputEditor(Control inputControl, bool useDevExpress)
	{
		return useDevExpress
			? DescendantsAndSelf(inputControl).OfType<MemoEdit>().Single()
			: DescendantsAndSelf(inputControl).OfType<TextBox>().Single();
	}

	private static Control GetSelectableMessageEditor(Control messageControl)
	{
		return messageControl.Controls.Cast<Control>()
			.Single(child => child is TextBox or HtmlContentControl);
	}

	private static bool IsSelectableMessageEditor(Control control, bool useDevExpress)
	{
		return useDevExpress
			? control is HtmlContentControl
			: control is TextBox;
	}

	private static bool IsNonSelectableLabel(Control control) => control is Label or LabelControl;

	private static void AssertUnfocusedSelectionIsHidden(Control control)
	{
		switch (control)
		{
			case TextBox textBox:
				textBox.HideSelection.ShouldBeTrue();
				break;
			case HtmlContentControl htmlContentControl:
				htmlContentControl.AllowContentSelection.ShouldBe(DefaultBoolean.True);
				break;
			default:
				throw new InvalidOperationException($"Unsupported selectable editor type: {control.GetType().FullName}");
		}
	}

	private static Control CreateDevExpressFunctionCallMessageControl()
	{
		var type = typeof(DXChatControl).Assembly.GetType("TinyChat.DXFunctionCallMessageControl")!;
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

	private static Control CreateDevExpressReasoningMessageControl()
	{
		var type = typeof(DXChatControl).Assembly.GetType("TinyChat.DXReasoningMessageControl")!;
		var control = (Control)Activator.CreateInstance(type, nonPublic: true)!;
		type.GetProperty(nameof(DXChatMessageControl.MessageFormatter))!
			.SetValue(control, new PlainTextMessageFormatter());
		((IChatMessageControl)control).Message = new ChatMessage(
			new NamedSender("Assistant"),
			new ReasoningMessageContent(
				"Thinking through the answer " + string.Join(" ", Enumerable.Repeat("with wrapped reasoning detail", 12))));
		return control;
	}

	private static void HostStructuredMessageControl(Control host, Control control, int width)
	{
		control.MinimumSize = new Size(width, 0);
		control.MaximumSize = new Size(width, 0);
		control.Width = width;
		host.Controls.Add(control);
		host.CreateControl();
		control.CreateControl();
		foreach (var editor in DescendantsAndSelf(control).OfType<HtmlContentControl>())
			editor.CreateControl();
		PerformStructuredMessageLayout(host, control);
	}

	private static void SelectHtmlText(HtmlContentControl control, string selectedText)
	{
		control.CreateControl();
		_ = control.GetContentSize();
		var contentElement = control.FindElementById("message-content");
		contentElement.ShouldNotBeNull();
		var textElement = DescendantHtmlNodes(contentElement)
			.OfType<DxHtmlTextElementBase>()
			.FirstOrDefault(element => element.Text.Contains(selectedText, StringComparison.Ordinal));
		textElement.ShouldNotBeNull();

		var selectionStart = textElement.Text.IndexOf(selectedText, StringComparison.Ordinal);
		var selectionRange = new DxHtmlSelectionRange(
			textElement,
			textElement,
			selectionStart,
			selectionStart + selectedText.Length);
		((IDxHtmlSelection)textElement.RootElement.Selection).Current = selectionRange;
	}

	private static IEnumerable<DxHtmlNode> DescendantHtmlNodes(DxHtmlNode parent)
	{
		foreach (DxHtmlNode child in parent.ChildNodes)
		{
			yield return child;

			foreach (var descendant in DescendantHtmlNodes(child))
				yield return descendant;
		}
	}

	private static void PerformStructuredMessageLayout(Control host, Control control)
	{
		host.PerformLayout();
		control.PerformLayout();
		Application.DoEvents();
	}

	private static void InvokeToggle(Control control)
	{
		control.GetType()
			.GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(control, [control, EventArgs.Empty]);
		PerformStructuredMessageLayout(control.Parent!, control);
	}

	private static void InvokeMouseDown(Control control)
	{
		var onMouseDown = typeof(Control).GetMethod(
			"OnMouseDown",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		onMouseDown.Invoke(control, [new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0)]);
	}

	private static void InvokeClick(Control control)
	{
		var onClick = typeof(Control).GetMethod(
			"OnClick",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		onClick.Invoke(control, [EventArgs.Empty]);
	}

	private static void AssertSenderFont(
		Control messageControl,
		string senderName,
		Font expectedFont,
		bool useDevExpress)
	{
		if (!useDevExpress)
		{
			var label = DescendantsAndSelf(messageControl)
				.OfType<Label>()
				.Single(child => child.Text == senderName);
			label.Font.Name.ShouldBe(expectedFont.Name);
			label.Font.Size.ShouldBe(expectedFont.Size);
			label.Font.Bold.ShouldBeTrue();
			return;
		}

		var devExpressLabel = DescendantsAndSelf(messageControl)
			.OfType<LabelControl>()
			.Single(child => child.Text == senderName);
		devExpressLabel.Appearance.Font.Name.ShouldBe(expectedFont.Name);
		devExpressLabel.Appearance.Font.Size.ShouldBe(expectedFont.Size);
		devExpressLabel.Appearance.FontStyleDelta.ShouldBe(FontStyle.Bold);
	}

	private static void AssertWelcomeAlignment(
		Control welcomeControl,
		bool useDevExpress,
		ContentAlignment expectedAlignment)
	{
		if (!useDevExpress)
		{
			DescendantsAndSelf(welcomeControl)
				.OfType<Label>()
				.Single()
				.TextAlign
				.ShouldBe(expectedAlignment);
			return;
		}

		var label = DescendantsAndSelf(welcomeControl).OfType<LabelControl>().Single();
		var expectedHorizontalAlignment = expectedAlignment switch
		{
			ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => HorzAlignment.Near,
			ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => HorzAlignment.Center,
			_ => HorzAlignment.Far
		};
		var expectedVerticalAlignment = expectedAlignment switch
		{
			ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => VertAlignment.Top,
			ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight => VertAlignment.Center,
			_ => VertAlignment.Bottom
		};

		label.Appearance.TextOptions.HAlignment.ShouldBe(expectedHorizontalAlignment);
		label.Appearance.TextOptions.VAlignment.ShouldBe(expectedVerticalAlignment);
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

	private static Delegate[] GetPropertyChangedSubscribers(FunctionCallMessageContent content)
	{
		var eventField = typeof(FunctionCallMessageContent).GetField(
			nameof(System.ComponentModel.INotifyPropertyChanged.PropertyChanged),
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		return (eventField.GetValue(content) as Delegate)?.GetInvocationList() ?? [];
	}

	private sealed class NonOptInLayoutChatControl : ChatControl
	{
		public NonOptInLayoutChatControl()
		{
			ShowCopyButton = false;
		}

		protected override IChatInputControl CreateChatInputControl() => new NonOptInInputControl
		{
			Padding = new Padding(5)
		};

		protected override IChatMessageControl CreateMessageControl(IChatMessage message) => new NonOptInMessageControl
		{
			Margin = new Padding(7),
			Message = message,
			Padding = new Padding(6)
		};
	}

	private sealed class NonOptInInputControl : Control, IChatInputControl
	{
		public event EventHandler? CancellationRequested { add { } remove { } }

		public event EventHandler<MessageSendingEventArgs> MessageSending { add { } remove { } }

		public void SetIsReceivingStream(bool isReceiving, bool allowCancellation) { }
	}

	private sealed class NonOptInMessageControl : Panel, IChatMessageControl
	{
		public event EventHandler? BeforeLayoutChange { add { } remove { } }

		public event EventHandler? AfterLayoutChange { add { } remove { } }

		public event EventHandler? SizeUpdatedWhileStreaming { add { } remove { } }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IChatMessage? Message { get; set; }

		public void SetIsReceivingStream(bool isReceiving) { }

		public void ShowSenderHeader(bool show) { }
	}

	private sealed class TrackingChatControl : ChatControl
	{
		public bool CancelWasRequested { get; private set; }

		public override void CancelCurrentOperation()
		{
			CancelWasRequested = true;
			base.CancelCurrentOperation();
		}
	}
}
