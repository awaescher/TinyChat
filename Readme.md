# TinyChat 💬

A very minimal but extensible chat control library for Windows Forms.

![Screenshot](docs/Screenshot.png)

## ✨ Features

- **Simplistic Chat Interface**: Clean, responsive chat UI with message history and input controls
- **Streaming Support**: Real-time message streaming for AI assistants and live conversations
- **Extensible Architecture**: Interface-driven design allowing custom message types and custom UI components
- **Made for WinForms**: Because business applications might need AI chats, too

## 🚀 Quick Start

### Basic Usage

The chat control provides a `Messages` property that can be set to provide message history.
When the user is sending messages, the events `MessageSending` and `MessageSent` gets fired to prevent or react on user messages.

```csharp
using TinyChat;

// Create a chat control
var chatControl = new ChatControl();
chatControl.Dock = DockStyle.Fill;

// Set up message handling
chatControl.MessageSent += (sender, e) => {
    // Handle user messages
    Console.WriteLine($"User sent: {e.Content}");
    
    // Add a response
    chatControl.AddMessage(
        new NamedSender("Assistant"), 
        new StringMessageContent("Hello! How can I help you?")
    );
};

// Add to your form
this.Controls.Add(chatControl);
```

### Streaming Messages with IAsyncEnumerable

Use `AddStreamingMessage()` to pass in a stream of tokens asynchrounously. The chat component will take care of updating the UI from background threads.

```csharp
IAsyncEnumerable<string> stream = ...;
chatControl.AddStreamingMessage(new NamedSender("AI Assistant"), stream);
```

## 🎬 Demos

The repository includes two demo applications:
- WinForms Demo: A basic Windows Forms application with dependency-free WinForms controls
- DevExpress Demo: A DevExpress-themed application showcasing using skinned DevExpress components

```bash
dotnet run --project WinFormsDemo
dotnet run --project DevExpressDemo
```

## 🤷‍♂️ Missing features (not planned either)

- Rich text message support 🔸  
- File attachment handling
- Message search and filtering
- Regenerating messages
- everything else you know from chatbots

---

🔸 This will be very hard to do in Winforms. Especially because AI assistants can come up with pretty much every flavor of formatting like HTML, Markdown, RTF, etc.