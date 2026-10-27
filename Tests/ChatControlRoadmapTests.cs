using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using Shouldly;
using TinyChat;
using TinyChat.Messages;

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
				nameof(ChatControl.InputEditorBackColor),
				nameof(ChatControl.InputEditorForeColor),
				nameof(ChatControl.InputEditorBorderStyle),
				nameof(ChatControl.InputPlaceholderText),
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
		public void Resets_Input_Editor_Appearance_To_The_Platform_Defaults()
		{
			using var control = new ChatControl();
			var properties = TypeDescriptor.GetProperties(control);
			var cases = new (string PropertyName, object CustomValue, object DefaultValue)[]
			{
				(nameof(ChatControl.InputEditorBackColor), Color.LemonChiffon, Color.Empty),
				(nameof(ChatControl.InputEditorForeColor), Color.DarkRed, Color.Empty),
				(nameof(ChatControl.InputEditorBorderStyle), BorderStyle.None, BorderStyle.Fixed3D),
				(nameof(ChatControl.InputPlaceholderText), "Ask TinyChat", string.Empty)
			};

			foreach (var (propertyName, customValue, defaultValue) in cases)
			{
				var property = properties[propertyName]!;
				property.ShouldSerializeValue(control).ShouldBeFalse(propertyName);

				property.SetValue(control, customValue);

				property.ShouldSerializeValue(control).ShouldBeTrue(propertyName);
				property.CanResetValue(control).ShouldBeTrue(propertyName);

				property.ResetValue(control);

				property.GetValue(control).ShouldBe(defaultValue, propertyName);
				property.ShouldSerializeValue(control).ShouldBeFalse(propertyName);
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
			if (useDevExpress)
				DescendantsAndSelf(messageControl).OfType<LabelControl>().Single(label => label.Text == "Hello").Appearance.Font.ShouldBe(updatedMessageFont);
			else
				DescendantsAndSelf(messageControl).OfType<Label>().Single(label => label.Text == "Hello").Font.ShouldBe(updatedMessageFont);
			DescendantsAndSelf(control.WelcomeControl).ShouldContain(child => child.Text == "Updated welcome");
		}

		[Test]
		public void Renders_The_Default_DevExpress_Robot_In_The_Welcome_Font_Like_The_Native_Suite()
		{
			using var control = CreateChatControl(useDevExpress: true);
			var welcomeLabel = DescendantsAndSelf(control.WelcomeControl!)
				.OfType<LabelControl>()
				.Single();

			welcomeLabel.AllowHtmlString.ShouldBeTrue();
			welcomeLabel.Appearance.Font.Name.ShouldBe(control.WelcomeFont.Name);
			welcomeLabel.Text.ShouldNotContain("<font");
			welcomeLabel.Text.ShouldContain("●‿●");
			welcomeLabel.Text.ShouldContain("How can I help you today?");
		}

		[Test]
		public void Leaves_Characters_Of_The_Welcome_Font_Untouched()
		{
			using var control = CreateChatControl(useDevExpress: true);

			control.WelcomeMessage = "Hello & welcome";

			DescendantsAndSelf(control.WelcomeControl!).OfType<LabelControl>().Single().Text
				.ShouldBe("Hello &amp; welcome");
		}

		[Explicit("Writes screenshots for a visual check")]
		[TestCase(false)]
		[TestCase(true)]
		public void Renders_The_Welcome_Robot(bool useDevExpress)
		{
			using var form = new Form { ClientSize = new Size(360, 320), StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) };
			using var control = useDevExpress ? new DXChatControl { Dock = DockStyle.Fill } : new ChatControl { Dock = DockStyle.Fill };
			form.Controls.Add(control);
			form.Show();
			form.Activate();
			for (var index = 0; index < 10; index++)
			{
				Application.DoEvents();
				Thread.Sleep(20);
			}

			var bounds = control.WelcomeControl!.RectangleToScreen(control.WelcomeControl.ClientRectangle);
			using var bitmap = new Bitmap(bounds.Width, bounds.Height);
			using (var graphics = Graphics.FromImage(bitmap))
				graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
			bitmap.Save(Path.Combine(Path.GetTempPath(), "opencode", $"welcome_{(useDevExpress ? "dx" : "native")}.png"));
		}

		[Test]
		public void Html_Encodes_Custom_DevExpress_Welcome_Text()
		{
			using var control = CreateChatControl(useDevExpress: true);

			control.WelcomeMessage = "<b>Wide awake</b> & ready";

			var welcomeLabel = DescendantsAndSelf(control.WelcomeControl!)
				.OfType<LabelControl>()
				.Single();
			welcomeLabel.Text.ShouldBe("&lt;b&gt;Wide awake&lt;/b&gt; &amp; ready");
			welcomeLabel.PlainText.ShouldBe("<b>Wide awake</b> & ready");
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
			AssertInputEditorUsesEditableDefaults(inputEditor, useDevExpress);
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
					.Where(child => child is Label or LabelControl or TextBox or MemoEdit)
					.Where(child => child.Font.Name != ChatIcons.IconFontName)
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
			AssertInputEditorUsesEditableDefaults(inputEditor, useDevExpress);
			AssertWelcomeAlignment(control.WelcomeControl!, useDevExpress, ContentAlignment.TopLeft);
			foreach (var messageControl in messageControls)
			{
				messageControl.Margin.ShouldBe(updatedMessageMargin);
				((IChatMessageLayoutControl)messageControl).ContentPadding.ShouldBe(updatedMessagePadding);
				messageControl.ForeColor.ShouldBe(updatedForeColor);
				messageControl.BackColor.ShouldBe(updatedBackColor);
			}
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Applies_Input_Editor_Appearance_Before_Creation_And_Live(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress, createControl: false);
			control.InputEditorBackColor = Color.LemonChiffon;
			control.InputEditorForeColor = Color.DarkRed;
			control.InputEditorBorderStyle = BorderStyle.FixedSingle;
			control.InputPlaceholderText = "Ask TinyChat";

			InitializeControl(control);

			var inputEditor = GetInputEditor(control.InputControl!, useDevExpress);
			AssertInputEditorAppearance(
				inputEditor,
				useDevExpress,
				Color.LemonChiffon,
				Color.DarkRed,
				BorderStyle.FixedSingle,
				"Ask TinyChat");

			control.InputEditorBackColor = Color.Azure;
			control.InputEditorForeColor = Color.DarkGreen;
			control.InputEditorBorderStyle = BorderStyle.None;
			control.InputPlaceholderText = "Continue";

			AssertInputEditorAppearance(
				inputEditor,
				useDevExpress,
				Color.Azure,
				Color.DarkGreen,
				BorderStyle.None,
				"Continue");
		}

		[Test]
		public void Uses_A_Custom_Cue_Text_For_The_Native_Multiline_Editor()
		{
			using var control = CreateChatControl(useDevExpress: false, createControl: false);
			control.InputPlaceholderText = "Ask TinyChat";

			InitializeControl(control);

			var inputEditor = (TextBox)GetInputEditor(control.InputControl!, useDevExpress: false);
			inputEditor.Multiline.ShouldBeTrue();
			inputEditor.PlaceholderText.ShouldBe("Ask TinyChat");
			inputEditor.GetType()
				.GetProperty("CueText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
				.GetValue(inputEditor)
				.ShouldBe("Ask TinyChat");
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

	private static ChatControl CreateChatControl(bool useDevExpress, bool createControl = true)
	{
		var control = useDevExpress ? (ChatControl)new DXChatControl() : new ChatControl();
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

	private static void AssertInputEditorUsesEditableDefaults(Control inputEditor, bool useDevExpress)
	{
		if (!useDevExpress)
		{
			inputEditor.ShouldBeAssignableTo<TextBox>();
			var textBox = (TextBox)inputEditor;
			textBox.ReadOnly.ShouldBeFalse();
			textBox.BackColor.ShouldBe(SystemColors.Window);
			textBox.ForeColor.ShouldBe(SystemColors.WindowText);
			textBox.BorderStyle.ShouldBe(BorderStyle.Fixed3D);
			return;
		}

		inputEditor.ShouldBeAssignableTo<MemoEdit>();
		var memoEdit = (MemoEdit)inputEditor;
		memoEdit.Properties.ReadOnly.ShouldBeFalse();
		memoEdit.Properties.BorderStyle.ShouldBe(DevExpress.XtraEditors.Controls.BorderStyles.Default);
		memoEdit.Properties.Appearance.Options.UseBackColor.ShouldBeFalse();
		memoEdit.Properties.Appearance.Options.UseForeColor.ShouldBeFalse();
		memoEdit.Properties.AppearanceFocused.Options.UseBackColor.ShouldBeFalse();
		memoEdit.Properties.AppearanceFocused.Options.UseForeColor.ShouldBeFalse();
	}

	private static void AssertInputEditorAppearance(
		Control inputEditor,
		bool useDevExpress,
		Color backColor,
		Color foreColor,
		BorderStyle borderStyle,
		string placeholderText)
	{
		if (!useDevExpress)
		{
			inputEditor.ShouldBeAssignableTo<TextBox>();
			var textBox = (TextBox)inputEditor;
			textBox.ReadOnly.ShouldBeFalse();
			textBox.BackColor.ShouldBe(backColor);
			textBox.ForeColor.ShouldBe(foreColor);
			textBox.BorderStyle.ShouldBe(borderStyle);
			textBox.PlaceholderText.ShouldBe(placeholderText);
			return;
		}

		inputEditor.ShouldBeAssignableTo<MemoEdit>();
		var memoEdit = (MemoEdit)inputEditor;
		var expectedBorderStyle = borderStyle switch
		{
			BorderStyle.None => DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
			BorderStyle.FixedSingle => DevExpress.XtraEditors.Controls.BorderStyles.Simple,
			_ => DevExpress.XtraEditors.Controls.BorderStyles.Default
		};
		memoEdit.Properties.ReadOnly.ShouldBeFalse();
		memoEdit.Properties.BorderStyle.ShouldBe(expectedBorderStyle);
		memoEdit.Properties.NullValuePrompt.ShouldBe(placeholderText);
		memoEdit.Properties.Appearance.BackColor.ShouldBe(backColor);
		memoEdit.Properties.Appearance.ForeColor.ShouldBe(foreColor);
		memoEdit.Properties.Appearance.Options.UseBackColor.ShouldBeTrue();
		memoEdit.Properties.Appearance.Options.UseForeColor.ShouldBeTrue();
		memoEdit.Properties.AppearanceFocused.BackColor.ShouldBe(backColor);
		memoEdit.Properties.AppearanceFocused.ForeColor.ShouldBe(foreColor);
		memoEdit.Properties.AppearanceFocused.Options.UseBackColor.ShouldBeTrue();
		memoEdit.Properties.AppearanceFocused.Options.UseForeColor.ShouldBeTrue();
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
