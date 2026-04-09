# HP AI Companion Test Framework

A BDD test suite for HP AI Companion v2.6.1003.0 using **Reqnroll** (SpecFlow successor) + **NUnit** + **FlaUI** for Windows UI Automation.

## Prerequisites

### System Requirements
- **Windows 11** or later (tested on Win11)
- **.NET 10 Runtime & SDK** (must have `net10.0-windows` TFM support)
  - Download from: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
  - Verify: `dotnet --version`
- **HP AI Companion v2.6.1003.0** installed and available in Start Menu
  - App must be logged in and functional before test runs
  - Tests will launch and close the app automatically

### Development Setup

#### 1. Visual Studio Code
- Download from: https://code.visualstudio.com/

#### 2. Install Cucumber Extension
- Open VS Code Extensions (`Ctrl+Shift+X`)
- Search for **"Cucumber"** (official by Cucumber.io)
  - **Marketplace:** https://marketplace.visualstudio.com/items?itemName=CucumberOpen.cucumber-official
  - **Do NOT use** `alexkrechik.cucumberautocomplete` (community extension, conflicts with official)
- Install and reload VS Code

#### 3. Verify `.vscode/settings.json`
The project root includes `.vscode/settings.json` which configures the Cucumber extension to find feature files and step definitions:
```json
{
  "cucumber.features": [
    "HPAICOmpanionTester/Features/**/*.feature"
  ],
  "cucumber.glue": [
    "HPAICOmpanionTester/StepDefinitions/**/*.cs",
    "HPAICOmpanionTester/Hooks/**/*.cs"
  ]
}
```

With this setup, you can **Ctrl+click** on steps in `.feature` files to jump to C# implementations.

## Running Tests

### All Tests
```bash
cd HPAICOmpanionTester
dotnet test
```

### Specific Feature
```bash
dotnet test --filter "FullyQualifiedName~Chat"
```

### Single Scenario
```bash
dotnet test --filter "FullyQualifiedName~SendingAMessageSubmitsItToTheAssistant"
```

### With Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Results

### Console Output
Test results and step execution logs print to the console during runs.

### HTML Report
After each test run, an interactive HTML report is generated at:
```
HPAICOmpanionTester/bin/Debug/net10.0-windows/test-report.html
```
Open this file in any web browser to view:
- Test pass/fail status
- Execution time per scenario
- Feature file descriptions
- Step-by-step trace with assertions

### Screenshots on Failure
When a scenario fails, a screenshot is automatically captured and saved to:
```
HPAICOmpanionTester/bin/Debug/net10.0-windows/screenshots/screenshot_YYYY-MM-DD_HH-mm-ss-fff.png
```

After the test run completes, an interactive gallery index is generated at:
```
HPAICOmpanionTester/bin/Debug/net10.0-windows/screenshots/index.html
```

Open the gallery to browse all failure screenshots with timestamps. Each screenshot shows the app state at the moment of failure, helping debug UI issues

## Architecture

### Directory Structure
```
HPAICOmpanionTester/
├── Features/                 # Gherkin .feature files (BDD specs)
│   ├── AppLaunch.feature    # App startup tests
│   ├── Chat.feature         # Chat input tests (Home section)
│   ├── Navigation.feature   # Left menu navigation tests
│   └── Perform.feature      # Chat tests on Perform section
│
├── StepDefinitions/          # C# step implementations
│   ├── AppLaunchSteps.cs
│   ├── NavigationSteps.cs
│   └── ChatSteps.cs
│
├── Hooks/                    # Reqnroll lifecycle hooks
│   └── AppHooks.cs          # Scenario/test-run setup & teardown
│
├── Drivers/                  # FlaUI app automation
│   ├── AppDriver.cs         # App launch/close & process management
│   └── AppSession.cs        # Shared FlaUI session (app instance reuse)
│
├── Pages/                    # Page Object Model
│   ├── MainPage.cs          # Main window interaction (title, visibility, interactivity)
│   ├── NavigationPanel.cs   # Left nav menu (section clicks)
│   └── ChatInputBar.cs      # Chat input & send (text typing, message submission)
│
├── Configuration/
│   └── TestSettings.cs      # Timeouts & app identifiers (AUMID, process name)
│
├── Support/
│   ├── WaitHelper.cs          # Retry/polling utilities
│   ├── ScreenshotHelper.cs    # Screenshot capture on failure
│   └── ScreenshotIndexGenerator.cs # Screenshot gallery HTML
│
├── testSettings.json         # Configuration (AUMID, process name, timeouts)
├── reqnroll.json            # Reqnroll formatter config (HTML report generation)
└── README.md                # This file
```

### Key Design Patterns

#### Page Object Model (POM)
All UI automation is encapsulated in page objects (`MainPage`, `NavigationPanel`, `ChatInputBar`). Step definitions contain zero FlaUI code — they're plain English delegating to these pages.

```csharp
// Step definition (readable, no UI code)
[When("I type {string} into the chat input")]
public void WhenIType(string message)
{
    _chatInputBar.TypeMessage(message);  // delegates to page object
}

// Page object (all UI automation here)
public void TypeMessage(string message)
{
    var input = _window.FindFirstDescendant(cf => cf.ByAutomationId("richEditBox"));
    // ... clipboard + keyboard logic ...
}
```

#### Shared App Session
Tests reuse the same FlaUI app instance across scenarios to save ~15s per test. Only on scenario failure does the app reload to ensure a clean state.

**Why:** App startup takes ~12 seconds. By reusing the same instance, subsequent scenarios are instant (0.0s), reducing total suite time from ~2.5 minutes to ~36 seconds.

**Failure Handling:** If a scenario fails, the next scenario automatically kills and relaunches the app, ensuring it starts from a known-clean state. Failed scenarios may leave the app in an unknown state (unresponsive UI, stale data, etc.), so we force a fresh launch.

```csharp
// First scenario: launch app (~12s)
GivenFreshlyLaunched();  // EnsureLaunched() → new launch

// Second scenario: reuse app (0.0s)
GivenFreshlyLaunched();  // EnsureLaunched() → no-op, reuses existing

// Test fails here ↓
// Next scenario detects failure and relaunches
AfterScenario() → MarkForRelaunch()
// Next scenario will kill and relaunch fresh app (~12s)
```

#### Context Injection
Reqnroll automatically wires dependencies via constructor injection:
```csharp
// Reqnroll creates AppDriver, MainPage, and passes them here
public ChatSteps(ChatInputBar chatInputBar, MainPage mainPage)
{
    _chatInputBar = chatInputBar;
    _mainPage = mainPage;
}
```

## Test Execution Flow

1. **Setup**: App is launched (or reused if healthy from previous scenario)
2. **Wait**: Framework waits for `NavLinksList` (shell UI) to render
3. **Navigation**: User navigates to a section (Home, Perform, etc.)
4. **Interact**: User types or clicks buttons
5. **Assert**: Step validates state (button name, text cleared, page title changed)
6. **Teardown on Failure**: If any step fails, next scenario will relaunch fresh app

## Current Test Suite

| Feature | Scenarios | Status |
|---|---|---|
| AppLaunch | 1 | ✓ Pass (explicit kill + fresh launch) |
| Chat (Home) | 2 | ✓ Pass (typing, sending, response detection) |
| Navigation | 5 | ✓ Pass (all 5 sections: Home, Library, Perform, Spotlight, Help) |
| Perform Screen | 2 | ✓ Pass (chat on Perform section) |
| **Total** | **10** | **~36 seconds** |

## Timeouts (Configurable)

See `testSettings.json`:
- **`launchTimeoutSeconds: 60`** — App startup and page transitions (slower under system load)
- **`actionTimeoutSeconds: 10`** — Element lookups and quick interactions

## Dependencies

### NuGet Packages
- **Reqnroll 3.3.4** — BDD framework (SpecFlow successor)
- **NUnit 4.5.1** — Test runner
- **NUnit3TestAdapter 6.1.0** — NUnit runner for VS Code/dotnet test
- **FlaUI.Core 4.0.0** — Windows UI Automation wrapper
- **FlaUI.UIA3 4.0.0** — UIA3 provider for FlaUI
- **FluentAssertions 6.12.0** — Readable assertions

### MSBuild Features
- `net10.0-windows` target (WinUI 3 support + Windows-specific APIs)
- `<UseWindowsForms>true</UseWindowsForms>` (System.Windows.Forms.Clipboard for WinUI 3 workaround)

## Troubleshooting

### App Not Found
- Verify HP AI Companion is installed: Start Menu → search "AI Companion"
- Verify AUMID in `testSettings.json`: `AD2F1837.HPAIExperienceCenter_v10z8vjag6ke6!App`

### Element Not Found (Timeout)
- Check if the app is responsive (click the window, type in chat)
- Increase `actionTimeoutSeconds` in `testSettings.json` if system is slow

### VS Code Cucumber Extension Not Finding Steps
- Reload VS Code (`Ctrl+R`)
- Verify `cucumber.glue` paths in `.vscode/settings.json` point to `**/*.cs` directories
- Ensure step method has `[Given]`, `[When]`, or `[Then]` attribute

### HTML Report Not Generated
- Check `bin/Debug/net10.0-windows/test-report.html` exists after `dotnet test`
- Verify `reqnroll.json` is in the project root with `outputFilePath: "test-report.html"`
- Rebuild: `dotnet clean && dotnet build`

## Next Steps for Development

### Adding a New Test
1. Create a `.feature` file in `Features/` with Gherkin scenarios
2. Run: `dotnet test` (Reqnroll auto-generates feature runner)
3. Implement step definitions in `StepDefinitions/`
4. Delegate UI logic to page objects in `Pages/`

### Example: New Feature
```gherkin
Feature: Library Search
  Scenario: User can search libraries
    Given HP AI Companion is launched
    And I navigate to the "Library" section
    When I search for "MyLibrary"
    Then the library appears in the results
```

Create `StepDefinitions/LibrarySteps.cs` and `Pages/LibraryPanel.cs` following existing patterns.

---

**Built with:** Reqnroll 3.3.4, NUnit 4.5.1, FlaUI 4.0.0, FluentAssertions 6.12.0
**Target:** .NET 10
**Last Updated:** 2026-04-08
