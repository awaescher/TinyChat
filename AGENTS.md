# TinyChat – Agent Instructions

Keep this AGENTS.md in English.

## UI text and presentation

- User-facing text must be localizable: use resources for defaults and do not hard-code messages or tooltips. Text must be customizable in the designer and by the host application.
- Keep appearance, colors, and dimensions customizable through existing appearance/layout settings or suitable designer properties. Respect the active WinForms or DevExpress theme.
- Prefer icons over additional labels. Follow existing icons, controls, and interaction patterns, and provide localizable tooltips and AccessibleNames.

- In DevExpress controls, visible elements, including scrollbars, must use the active skin rather than unskinned native WinForms rendering. Nonvisual layout containers are allowed.

- Display attachment errors and warnings in the input area or directly on the affected file preview. Do not create separate chat history messages for them.

## Project Overview

TinyChat is a .NET Windows Forms class library (and optional DevExpress variant) that provides a ready-to-use chat UI control integrating with `Microsoft.Extensions.AI` (`IChatClient`). It ships as two NuGet packages: `TinyChat` (core WinForms) and `TinyChat.DevExpress`.

**Solution projects:**
- `TinyChat/` – core library
- `TinyChat.DevExpress/` – DevExpress control overrides
- `tests/` – NUnit 4 test project
- `DemoApp/` – demo app (contains WinForms and DevExpress demo forms)
- `WinFormsDemo/` / `DevExpressDemo/` – placeholder directories (currently empty)

---

## Versioning

- Release versions are calculated by the existing GitVersion CI step from Git history and tags. Do not hard-code release versions in project files or set `next-version` in GitVersion.yml.
- Request increments in commit messages with `+semver: major`, `+semver: minor`, or `+semver: patch`. Preserve the marker in the final squash/merge message.
- Build and pack both TinyChat packages with the same GitVersion-derived version. Application package references select a concrete released version separately.

## Build Commands

```bash
# Restore dependencies
dotnet restore ./TinyChat/TinyChat.csproj
dotnet restore ./TinyChat.DevExpress/TinyChat.DevExpress.csproj

# Build (Debug)
dotnet build TinyChat.sln

# Build (Release)
dotnet build ./TinyChat/TinyChat.csproj --configuration Release

# Pack NuGet packages
dotnet pack ./TinyChat/TinyChat.csproj --output nupkgs --configuration Release

# Run WinForms demo
dotnet run --project WinFormsDemo
dotnet run --project WinFormsDemo -- --ichatclient
```

---

## Test Commands

**Framework:** NUnit 4 with Shouldly assertions.

```bash
# Run all tests
dotnet test ./tests/Tests.csproj

# Run a single test by method name
dotnet test ./tests/Tests.csproj --filter "FullyQualifiedName~MethodNameHere"

# Run all tests in a class
dotnet test ./tests/Tests.csproj --filter "ClassName~PlainTextMessageFormatterTests"

# Run tests matching a display name fragment
dotnet test ./tests/Tests.csproj --filter "DisplayName~Returns_Plain_Text"

# Run with verbose output
dotnet test ./tests/Tests.csproj --logger "console;verbosity=detailed"
```

**Note:** The CI pipeline does not run `dotnet test` automatically — tests are exercised locally or via NCrunch (continuous test runner, configured in `TinyChat.v3.ncrunchsolution`).

---

## Lint / Format Commands

Style is enforced by `.editorconfig` and Roslyn analyzers at build time. `IDE0055` (formatting) is treated as a **build error** — code that violates formatting rules will not compile.

```bash
# Apply auto-formatting
dotnet format ./TinyChat/TinyChat.csproj
dotnet format ./tests/Tests.csproj

# Check formatting without modifying files
dotnet format ./TinyChat/TinyChat.csproj --verify-no-changes
```

---

## Target Frameworks

All production projects target **`net9.0-windows;net10.0-windows`**. Tests use the host SDK framework. Windows Forms requires `<UseWindowsForms>true</UseWindowsForms>` and `<EnableWindowsTargeting>true</EnableWindowsTargeting>`.

---

## Code Style

### Formatting (enforced via `.editorconfig`)

- **Indentation:** Tabs (width 4 for C#; width 2 for XML/JSON project files).
- **Line endings:** CRLF (`\r\n`).
- **Trailing whitespace:** Always trimmed.
- **Charset:** UTF-8.
- **Brace style:** Allman — opening brace always on its own line.
- **`else` / `catch` / `finally`:** Each on a new line.
- **Single-line statements:** Not preserved; always expand to multiple lines.
- **`var` preference:** Use `var` for all local variables where type is inferable.
- **`this.` qualifier:** Omit unless required to disambiguate.
- **Language keywords:** Prefer `int` over `Int32`, `string` over `String`, etc.
- **Expression-bodied members:** Preferred for properties, indexers, and accessors. **Not** preferred for methods, constructors, or operators.
- **Pattern matching:** Prefer over `is`-cast or `as`-with-null-check.
- **Object/collection initializers:** Preferred where applicable.
- **Null coalescing / propagation:** Preferred (`?.`, `??`, `??=`).

### Imports / `using` Directives

- **System.* namespaces first**, then Microsoft.*, then project namespaces (enforced by `dotnet_sort_system_directives_first = true`).
- **File-scoped namespaces** are required — use `namespace TinyChat;` not a block namespace.
- **`ImplicitUsings` is enabled** — do not add explicit `using` statements for common BCL types (`System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`, etc.).
- Remove unused `using` directives (`IDE0005` is a warning).

Example ordering:
```csharp
using System.ComponentModel;
using System.Threading.Channels;
using Microsoft.Extensions.AI;
using TinyChat.Messages;
using TinyChat.SubControls;
```

### Nullable Reference Types

- **Nullable is enabled** in all projects (`<Nullable>enable</Nullable>`).
- Annotate all reference types as nullable (`string?`, `IChatMessage?`) wherever a null value is possible.
- Use the null-forgiving operator (`!`) only when you are certain a value is non-null and the compiler cannot infer it (e.g., in test code testing null arguments).
- Use `required` on properties that must be set during object initialization.

---

## Naming Conventions

| Symbol | Convention | Example |
|---|---|---|
| Classes, structs, enums, delegates | PascalCase | `ChatMessageControl` |
| Interfaces | `I` prefix + PascalCase | `IChatMessage`, `ISender` |
| Public/internal methods, properties, events | PascalCase | `AddMessage`, `MessageSent` |
| Parameters | camelCase | `sender`, `cancellationToken` |
| Local variables | camelCase | `chatClient`, `textStarted` |
| Private/protected instance fields | `_camelCase` | `_messages`, `_sendButton` |
| Constants | `ALL_UPPER_WITH_UNDERSCORES` | `ROBOT_WELCOME`, `SEND_CHAR` |
| Namespaces | PascalCase | `TinyChat`, `TinyChat.Messages` |
| Files | Match primary type name | `ChatControl.cs`, `IChatMessage.cs` |
| Test methods | `Pascal_Case_With_Underscores` | `Returns_Plain_Text_From_Simple_String` |

---

## Architecture & Patterns

- **Template / Factory Method pattern:** `ChatControl` provides default WinForms implementations. Override any of the following in a subclass (as `DXChatControl` does) to substitute custom controls:
  - `CreateMessageHistoryControl()` – the scrollable message list container
  - `CreateWelcomeControl()` – the placeholder shown when no messages exist
  - `CreateMessageControl()` – individual chat message bubbles
  - `CreateFunctionCallMessageControl()` – controls for function-call messages
  - `CreateReasoningMessageControl()` – controls for reasoning/thinking messages
  - `CreateChatInputControl()` – the text input + send button area
  - `CreateSplitContainerControl()` – the splitter between history and input
  - `CreateDefaultMessageFormatter()` – the `IMessageFormatter` used for rendering
  - `CreateChatMessage()` – factory for new `IChatMessage` instances
- **Interfaces for all extensibility seams:** `IChatMessage`, `IChatMessageContent`, `ISender`, `IChatInputControl`, `IChatMessageHistoryControl`, `IChatMessageControl`, `ISplitContainerControl`, `IMessageFormatter`.
- **Records** for simple value objects: e.g., `ChatMessage` (positional record), `NamedSender` (positional record).
- **WinForms threading:** Use `BeginInvoke(...)` or `SynchronizationContext.Post(...)` for cross-thread UI updates. Check `InvokeRequired` before accessing controls from non-UI threads.

---

## Error Handling Patterns

1. **Guard returns** for null/missing dependencies:
   ```csharp
   if (ServiceProvider is null)
       return;
   ```

2. **Separate `OperationCanceledException`** from general `Exception` — treat cancellation as expected, not an error:
   ```csharp
   catch (OperationCanceledException)
   {
       // expected – user cancelled the operation
   }
   catch (Exception ex)
   {
       AddMessage(new NamedSender("System"), new StringMessageContent($"Error: {ex.Message}"));
   }
   ```

3. **`async void`** is allowed only for top-level WinForms event handlers (fire-and-forget). Always wrap the entire body in a try/catch that handles both `OperationCanceledException` and `Exception` to prevent unhandled exceptions from crashing the application.

4. **Silent outer catch** as a last resort inside `async void` event handlers to prevent application crash if even error reporting fails:
   ```csharp
   catch
   {
       // If we can't even add an error message, just ignore to prevent further issues
   }
   ```

5. **`NotSupportedException`** for unrecognised content types in formatter `Switch` paths.

6. **`InvalidOperationException`** when a required precondition is unmet (e.g., missing `SynchronizationContext`).

---

## Test Organisation

Tests use **nested inner classes** to group tests by the method under test:

```csharp
public class PlainTextMessageFormatterTests
{
    [OneTimeSetUp]
    public void SetUp() { ... }

    public class FormatMethod : PlainTextMessageFormatterTests
    {
        [Test]
        public void Returns_Plain_Text_From_Simple_String() { ... }
    }

    public class PerformanceTests : PlainTextMessageFormatterTests
    {
        [Test]
        public void Is_Fast_Enough() { ... }
    }
}
```

Use **Shouldly** for assertions (`result.ShouldBe(...)`, `result.ShouldContain(...)`) rather than `Assert.*`.

---

## Key Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Extensions.AI.Abstractions` | 10.3.0 | `IChatClient`, `ChatRole`, streaming |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.3 | `IServiceProvider`, keyed services |
| `DevExpress.Win` | 20.1.* | DevExpress controls (optional project only) |
| NUnit 4 | 4.2.2 | Test framework |
| Shouldly | 4.3.0 | Fluent assertions |
