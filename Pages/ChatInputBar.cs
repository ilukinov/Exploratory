using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using HPAICOmpanionTester.Configuration;
using HPAICOmpanionTester.Support;

namespace HPAICOmpanionTester.Pages;

/// <summary>
/// Page Object for the chat input bar at the bottom of the screen.
/// AutomationId 'richEditBox' (WinUI RichEditBox).
/// </summary>
public sealed class ChatInputBar
{
    private readonly Window _window;
    private readonly TestSettings _settings = TestSettings.Load();

    // WinUI 3 RichEditBox needs time to settle after focus and after clipboard paste
    private const int FocusSettleMs = 300;
    private const int PasteSettleMs = 400;

    public ChatInputBar(MainPage mainPage)
    {
        _window = mainPage.Window;
    }

    /// <summary>
    /// Types text into the chat input via clipboard paste.
    ///
    /// WinUI 3 RichEditBox does not reliably receive keyboard events dispatched via
    /// SendInput (the mechanism FlaUI.Keyboard.Type uses). Pasting via Ctrl+V is
    /// the standard workaround: it triggers the same TextChanged path in WinUI that
    /// a real keystroke would, including showing the Send button.
    ///
    /// System.Windows.Forms.Clipboard requires an STA thread; we spin one up so that
    /// NUnit's default MTA thread is not affected.
    /// </summary>
    public void TypeMessage(string message)
    {
        var input = GetInput();
        _window.SetForeground();
        input.Click();
        Thread.Sleep(FocusSettleMs);

        // Set clipboard on a dedicated STA thread (WinForms Clipboard requirement)
        var sta = new Thread(() => System.Windows.Forms.Clipboard.SetText(message));
        sta.SetApartmentState(ApartmentState.STA);
        sta.Start();
        sta.Join();

        // Paste into the focused input
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
        Thread.Sleep(PasteSettleMs); // allow TextChanged events to update the send button
    }

    /// <summary>
    /// Clicks the Send button (AutomationId='SendOrPauseUserPromptEvent').
    /// This button only appears in the UI after text has been typed into the input.
    /// </summary>
    public void Submit()
    {
        var sendButton = WaitHelper.ForElement(
            () => _window.FindFirstDescendant(cf =>
                cf.ByAutomationId("SendOrPauseUserPromptEvent")),
            TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds))
            ?? throw new InvalidOperationException(
                "Send button not found. Make sure text has been typed into the input first.");

        // InvokePattern is required: .Click() dispatches a simulated mouse event
        // that WinUI 3 buttons intercept but don't route to their command handler
        // when the input focus is in a RichEditBox. InvokePattern goes straight
        // through the automation provider and reliably triggers the send command.
        if (sendButton.Patterns.Invoke.IsSupported)
            sendButton.Patterns.Invoke.Pattern.Invoke();
        else
            sendButton.Click();
    }

    /// <summary>
    /// Waits until the application has accepted the message for processing.
    /// After submission the app transitions to the conversation view: the richEditBox
    /// text is cleared and/or the Send button switches to "Pause" while the AI streams.
    /// Checking both conditions handles fast responses where the button may already
    /// have cycled back to "Send" by the time we poll.
    /// </summary>
    public bool WaitForInputToClear(TimeSpan timeout) =>
        WaitHelper.Until(
            () =>
            {
                // Primary signal: button switched to Pause (AI is actively streaming)
                var btn = _window.FindFirstDescendant(cf =>
                    cf.ByAutomationId("SendOrPauseUserPromptEvent"));
                if (btn is null || btn.Name != "Send") return true;

                // Secondary signal: input text was cleared after submission
                var input = _window.FindFirstDescendant(cf =>
                    cf.ByAutomationId("richEditBox"));
                if (input is null) return true;

                if (input.Patterns.Text.IsSupported)
                {
                    var text = input.Patterns.Text.Pattern.DocumentRange.GetText(-1);
                    if (string.IsNullOrWhiteSpace(text)) return true;
                }

                if (input.Patterns.Value.IsSupported)
                {
                    var value = input.Patterns.Value.Pattern.Value.Value;
                    if (string.IsNullOrWhiteSpace(value)) return true;
                }

                return false;
            },
            timeout);

    public bool IsVisible() =>
        WaitHelper.ForElement(
            () => _window.FindFirstDescendant(cf => cf.ByAutomationId("richEditBox")),
            TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds)) is not null;

    private AutomationElement GetInput() =>
        WaitHelper.ForElement(
            () => _window.FindFirstDescendant(cf => cf.ByAutomationId("richEditBox")),
            TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds))
        ?? throw new InvalidOperationException("Chat input (richEditBox) not found.");
}
