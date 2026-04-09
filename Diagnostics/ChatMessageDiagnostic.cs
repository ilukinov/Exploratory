using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUIApplication = FlaUI.Core.Application;

namespace HPAICOmpanionTester.Diagnostics;

[TestFixture]
public class ChatMessageDiagnostic
{
    [Test]
    public void ExploreMessageBubble()
    {
        using var automation = new UIA3Automation();

        var processes = System.Diagnostics.Process.GetProcessesByName("AICompanion");
        var app = processes.Length > 0
            ? FlaUIApplication.Attach(processes[0])
            : FlaUIApplication.LaunchStoreApp("AD2F1837.HPAIExperienceCenter_v10z8vjag6ke6!App");

        System.Threading.Thread.Sleep(8000);

        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(60));
        Assert.That(window, Is.Not.Null);

        // Navigate to Home
        var homeNav = window.FindFirstDescendant(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem).And(cf.ByName("Home")));
        homeNav?.Click();
        System.Threading.Thread.Sleep(1000);

        var input = FlaUI.Core.Tools.Retry.WhileNull(
            () => window.FindFirstDescendant(cf => cf.ByAutomationId("richEditBox")),
            TimeSpan.FromSeconds(10)).Result;
        Assert.That(input, Is.Not.Null);

        // Type a test message
        window.SetForeground();
        input.Click();
        System.Threading.Thread.Sleep(300);
        var sta = new System.Threading.Thread(() =>
            System.Windows.Forms.Clipboard.SetText("test message for chat"));
        sta.SetApartmentState(System.Threading.ApartmentState.STA);
        sta.Start(); sta.Join();
        FlaUI.Core.Input.Keyboard.TypeSimultaneously(
            FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL,
            FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);
        System.Threading.Thread.Sleep(500);

        var sendBtn = window.FindFirstDescendant(cf => cf.ByAutomationId("SendOrPauseUserPromptEvent"));
        Console.WriteLine($"Before Send: {window.FindAllDescendants().Length} descendants");

        if (sendBtn?.Patterns.Invoke.IsSupported == true)
        {
            sendBtn.Patterns.Invoke.Pattern.Invoke();
            System.Threading.Thread.Sleep(2000);

            Console.WriteLine($"\n=== 2s after Send ===");
            Console.WriteLine($"Descendants: {window.FindAllDescendants().Length}");

            // Look for message bubbles/items
            var allDescendants = window.FindAllDescendants();
            var itemsWithContent = new List<string>();
            foreach (var el in allDescendants)
            {
                try
                {
                    if (el.Properties.Name.IsSupported && el.Properties.Name.Value?.Contains("test message") == true)
                        itemsWithContent.Add($"[{el.ControlType}] Name: '{el.Properties.Name.Value}'");
                    if (el.Properties.AutomationId.IsSupported && el.Properties.AutomationId.Value?.Contains("message") == true)
                        itemsWithContent.Add($"[{el.ControlType}] ID: '{el.Properties.AutomationId.Value}'");
                }
                catch { }
            }

            if (itemsWithContent.Any())
            {
                Console.WriteLine("\n--- Elements containing 'message' or with name matching sent text ---");
                foreach (var item in itemsWithContent)
                    Console.WriteLine(item);
            }
            else
            {
                Console.WriteLine("\nNo message bubbles found yet. Dumping all text elements:");
                foreach (var el in allDescendants)
                {
                    try
                    {
                        var name = el.Properties.Name.IsSupported ? el.Properties.Name.Value : "";
                        var aid = el.Properties.AutomationId.IsSupported ? el.Properties.AutomationId.Value : "";
                        if (el.ControlType.ToString().Contains("Text") && !string.IsNullOrWhiteSpace(name))
                            Console.WriteLine($"  [{el.ControlType}] id='{aid}' name='{name}'");
                    }
                    catch { }
                }
            }

            // Wait for response
            Console.WriteLine("\n--- Waiting for AI response (polling for 10s) ---");
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            int pollCount = 0;
            while (DateTime.UtcNow < deadline)
            {
                System.Threading.Thread.Sleep(1000);
                pollCount++;
                var responseItems = window.FindAllDescendants()
                    .Where(el =>
                    {
                        try
                        {
                            var name = el.Properties.Name.IsSupported ? el.Properties.Name.Value : "";
                            return name?.Contains("test message") == true ||
                                   name?.Length > 50; // Likely an AI response (longer text)
                        }
                        catch { }
                        return false;
                    })
                    .ToList();

                if (responseItems.Count > 1) // User message + AI response
                {
                    Console.WriteLine($"\n[+{pollCount}s] Found {responseItems.Count} message items (likely including AI response)");
                    foreach (var item in responseItems.Take(3))
                    {
                        var name = item.Properties.Name.IsSupported ? item.Properties.Name.Value : "";
                        Console.WriteLine($"  - [{item.ControlType}] {name?.Substring(0, Math.Min(80, name.Length))}...");
                    }
                    break;
                }
            }
        }
    }
}
