using System.Drawing;
using System.Windows.Forms;
using Shouldly;
using TinyChat;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class NativeMessageResizeLayoutTests
{
	private const string LongText =
		"This is a fairly long chat message that must wrap across multiple lines when the " +
		"chat history is narrow and collapse back to fewer lines when the history is widened " +
		"again after the user resizes the window several times in a row.";

	public class TextHeightTracksWidth
	{
		[Test]
		public void Shrinks_Message_Height_When_History_Is_Widened_After_Being_Narrow()
		{
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(250, 400) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(LongText))
			};

			history.AppendMessageControl(messageControl);
			history.PerformLayout();
			messageControl.PerformLayout();
			var narrowHeight = messageControl.Height;

			// Simulate the user enlarging the window.
			history.ClientSize = new Size(600, 400);
			history.PerformLayout();
			messageControl.PerformLayout();
			var wideHeight = messageControl.Height;

			wideHeight.ShouldBeLessThan(narrowHeight);
		}

		[Test]
		public void Restores_The_Height_After_Repeated_Resizes()
		{
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(600, 400) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(LongText))
			};

			history.AppendMessageControl(messageControl);
			history.PerformLayout();
			messageControl.PerformLayout();
			var initialWideHeight = messageControl.Height;

			foreach (var width in new[] { 250, 600, 300, 550, 240, 600 })
			{
				history.ClientSize = new Size(width, 400);
				history.PerformLayout();
				messageControl.PerformLayout();
			}

			messageControl.Height.ShouldBe(initialWideHeight);
		}

		[Test]
		public void Message_Text_Never_Exceeds_The_Available_Width_After_Resize()
		{
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(600, 400) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(LongText))
			};

			history.AppendMessageControl(messageControl);

			foreach (var width in new[] { 250, 600, 300, 550, 240 })
			{
				history.ClientSize = new Size(width, 400);
				history.PerformLayout();
				messageControl.PerformLayout();

				var textControl = messageControl.Controls.OfType<TextBox>().Single();
				textControl.Width.ShouldBeLessThanOrEqualTo(messageControl.ClientSize.Width);
			}
		}

		[Test]
		public void Message_Control_Fully_Contains_Its_Text_After_Repeated_Resizes()
		{
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(600, 400) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(LongText))
			};

			history.AppendMessageControl(messageControl);

			foreach (var width in new[] { 600, 250, 600, 300, 240, 600 })
			{
				history.ClientSize = new Size(width, 400);
				history.PerformLayout();
				messageControl.PerformLayout();
			}

			var senderLabel = messageControl.Controls.OfType<Label>().First();
			var textControl = messageControl.Controls.OfType<TextBox>().Single();

			// The docked text must sit below the sender header (no vertical overlap) and the panel
			// must be tall enough to fully contain the wrapped text. A stale height cache would leave
			// the text overflowing past the panel bottom, painting over the following message.
			textControl.Top.ShouldBeGreaterThanOrEqualTo(senderLabel.Bottom);
			textControl.Bottom.ShouldBeLessThanOrEqualTo(messageControl.ClientSize.Height);
		}
	}
}
