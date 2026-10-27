[![NuGet Status](https://img.shields.io/nuget/v/TinyChat.svg)](https://www.nuget.org/packages/TinyChat/)

# TinyChat 💬

A minimalistic but extensible chat control library for Windows Forms.

![Screenshot](docs/Screenshot.png)

## ✨ Features

- **Two control suites**: vendor-control-free Windows Forms controls in `TinyChat` and a skinnable DevExpress implementation in `TinyChat.DevExpress`
- **Streaming support**: real-time message streaming for AI assistants and live conversations
- **Microsoft.Extensions.AI integration**: use any registered `IChatClient`, including keyed services, with streaming, cancellation, chat options, reasoning, and function calls
- **File and image attachments**: add files through drag and drop, clipboard paste, or the public API; images are shown as previews
- **Message actions**: a small icon bar on every message offers copying (plain text, or with formatting in the DevExpress suite) and going back to that message
- **Designer-friendly appearance**: configure fonts, colors, history, message and input spacing, welcome alignment, the splitter, and behavioral options in the Windows Forms Designer
- **New-chat lifecycle**: cancel the active response and clear the conversation and input draft in one call
- **Readable message rendering**: native controls use cleaned plain text, while the DevExpress suite renders the formatting supported by its label controls
- **Extensible architecture**: interface-driven design and factory methods for custom message types and UI components

## Control suites

| NuGet package | Control | UI components | Default formatter |
|---|---|---|---|
| `TinyChat` | `ChatControl` | Native Windows Forms controls | `PlainTextMessageFormatter` |
| `TinyChat.DevExpress` | `DXChatControl` | DevExpress controls | `SimplifiedHtmlMessageFormatter` (DevExpress HTML-inspired label formatting) |

Both controls share the same public chat API. `DXChatControl` derives from `ChatControl` and replaces the primary editors, message controls, history, welcome surface, and splitter with DevExpress counterparts.
`TinyChat.DevExpress` is built and tested with DevExpress 24.2.12 and requires the corresponding DevExpress license.

## 💖 Motivation

While there are countless web-based chat controls, there are very few for the good old Windows Forms ecosystem. TinyChat provides a focused user interface for interacting with AI models inside an existing WinForms application.

DevExpress also offers a feature-rich [AI Chat Control](https://docs.devexpress.com/WindowsForms/405218). It is based on web technologies and therefore [adds a significant set of deployment dependencies](https://supportcenter.devexpress.com/ticket/details/t1294891/aichatcontrol-without-webview). TinyChat deliberately stays smaller and exposes factory methods and interfaces for applications that need to customize it.

## Known limitations

- **Full formatting specifications are not implemented.** Native Windows Forms message controls display cleaned plain text. The DevExpress controls support a practical subset of common HTML and Markdown constructs, but not every HTML, Markdown, or RTF feature an assistant may emit.
- **Image rendering currently focuses on attachments.** Local or pasted images have previews and are sent to compatible `IChatClient` implementations as multimodal content. Arbitrary remote images or image content returned by an assistant are not rendered automatically.
- **Provider capabilities still apply.** A provider or model may reject a binary attachment format even though TinyChat can load and forward it. Override `ConvertFileAttachment()` when files need extraction, upload, or RAG processing first.

## 🚀 Quick start

### Native Windows Forms

```csharp
using TinyChat;

var chatControl = new ChatControl
{
    Dock = DockStyle.Fill
};
Controls.Add(chatControl);
```

For the DevExpress suite, reference `TinyChat.DevExpress` and instantiate `DXChatControl` instead. The remaining API is the same.

### Basic message handling

The `Messages` property supplies an existing conversation. `MessageSending` can inspect or cancel a user message, while `MessageSent` runs after it was accepted.

```csharp
chatControl.MessageSent += (_, e) =>
{
    Console.WriteLine($"User sent: {e.Content}");

    chatControl.AddMessage(
        new NamedSender("Assistant"),
        new StringMessageContent("Hello! How can I help you?"));
};
```

### Starting a new chat

Use `StartNewChat()` instead of assigning an empty message collection. It requests cancellation of an active response and clears the built-in conversation, text draft, and pending attachments in one call.

```csharp
chatControl.NewChatStarted += (_, _) =>
{
    // Reset any application-specific session state here.
};

chatControl.StartNewChat();
```

Custom input controls can implement `IChatInputDraftControl` so their own draft state is cleared by the same call.

### Designer and appearance settings

Fonts, colours, paddings, the welcome text, the splitter and the visibility of function calls, skills and reasoning can be set in the Windows Forms Designer for both control suites. Changes apply to existing messages as well.

### Message actions

When the pointer rests over a message, a small icon bar appears on its top-right edge:

- **Copy**: copies the message as plain text, with Markdown and HTML removed.
- **Copy with formatting** (`DXChatControl` only): copies the message as HTML together with its plain text, so bold text, colours, links and line breaks are kept when pasting into Word, Outlook or OneNote.
- **Go back**: permanently rewinds the conversation. Every later message is removed and is no longer sent to the `IChatClient`. When the message is an assistant response, it stays and the input is cleared. When the message was sent by the user, it is removed as well and its text and attachments are placed in the input, ready to be edited and sent again. This button is not offered while a response is being received.

The bar only shows icons: the native suite uses the Windows icon font like the tool-call icons, the DevExpress suite uses Fluent UI System Icons. You can also click the sender/header or message body and press <kbd>Ctrl</kbd>+<kbd>C</kbd> to copy the complete message.

Use `ShowMessageActions` and `MessageActionsHoverDelay` to configure the bar. `RevertToMessage()` performs the revert in code, returns `false` while `IsBusy`, and raises `ConversationReverted` with the removed messages. Override `GetMessageActions()` to add or remove buttons (custom actions use `ChatMessageActionKind.Custom` with an `Image`), `CreateMessageActionButton()` to change their look, and `IsUserMessage()` to change which messages are treated as user messages. Custom input controls implement `IChatInputDraftEditorControl` to receive the reverted draft.

### File and image attachments

When `AllowFileAttachments` is enabled (the default), users can:

- drop files anywhere on the chat;
- paste copied files or a bitmap into the input;
- remove attachments from the draft before sending;
- see small image previews and document tiles.

The default per-file limit is 20 MiB and can be changed with `MaximumAttachmentFileSize`. Files can also be supplied in code:

```csharp
await chatControl.AddFilesToInputAsync(openFileDialog.FileNames);

await chatControl.SendFilesAsync(
    ["report.pdf", "diagram.png"],
    "Summarize these files.");
```

Text attachments are forwarded as named text sections. Images and other binary files are represented as typed `DataContent` for `Microsoft.Extensions.AI`.

### Streaming messages with `IAsyncEnumerable`

Pass a token stream to `AddStreamingMessage()`. TinyChat marshals display updates back to the UI synchronization context.

```csharp
IAsyncEnumerable<string> stream = GetTokenStream();
chatControl.AddStreamingMessage(new NamedSender("AI Assistant"), stream);
```

### Using `IChatClient` with dependency injection

TinyChat can resolve and call a `Microsoft.Extensions.AI.IChatClient` automatically.

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using TinyChat;

var services = new ServiceCollection();
services.AddSingleton<IChatClient>(CreateChatClient());

var chatControl = new ChatControl
{
    ServiceProvider = services.BuildServiceProvider(),
    UseStreaming = true,
    AssistantSenderName = "AI Assistant"
};
```

The complete conversation is converted and passed to the client. Override `ConvertToChatMessages()` or `DetermineChatRole()` when the default sender-to-role mapping does not fit your application. `ChatOptions` and `ChatOptionsRequested` customize each request; `IncludeFunctionCalls` and `IncludeReasoning` control whether the corresponding captured response details are visible.

Keyed clients are supported as well:

```csharp
services.AddKeyedSingleton<IChatClient>("basic", CreateBasicClient());
services.AddKeyedSingleton<IChatClient>("premium", CreatePremiumClient());

chatControl.ChatClientServiceKey = "premium";
```

### Formatting

`ChatControl` uses native labels and `PlainTextMessageFormatter` to remove common HTML and Markdown syntax while keeping the text readable.

`DXChatControl` uses DevExpress `LabelControl`s with [HTML-inspired text formatting](https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting). `SimplifiedHtmlMessageFormatter` is the default and converts Markdown and HTML into the supported bold, italic, underline, strike-through, colour, size, code-font, paragraph, line-break and link tags.

Both suites preserve Unicode emoji, including multi-code-point sequences. Their exact colour and glyph style still depend on the installed Windows fonts and the active renderer; native Windows Forms commonly displays monochrome glyphs.

To customize normalization and rendering, implement `IMessageFormatter` and override `CreateDefaultMessageFormatter()`.

## 🎬 Demos

`DemoApp` contains both native Windows Forms and DevExpress examples, including direct `IChatClient` integration:

```bash
dotnet run --project DemoApp
```

## Credits

The package icon was made by [Vectors Market](https://www.flaticon.com/authors/vectors-market) and is licensed under the [Flaticon license](https://www.flaticon.com/legal).

The DevExpress controls use icons from the [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) by Microsoft, licensed under the [MIT License](https://github.com/microsoft/fluentui-system-icons/blob/main/LICENSE).
