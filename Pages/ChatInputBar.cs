using System.Diagnostics;
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
    private readonly TestSettings _settings;

    // WinUI 3 RichEditBox needs time to settle after focus and after clipboard paste
    private const int FocusSettleMs = 300;
    private const int PasteSettleMs = 500;
    // Maximum number of paste retry attempts before giving up
    private const int MaxPasteRetries = 3;

    public ChatInputBar(MainPage mainPage, TestSettings settings)
    {
        _window = mainPage.Window;
        _settings = settings;
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
    ///
    /// Because clipboard paste can silently fail (lost focus, timing issues), the method
    /// verifies that text actually reached the input and retries if it didn't.
    /// </summary>
    public void TypeMessage(string message)
    {
        for (var attempt = 1; attempt <= MaxPasteRetries; attempt++)
        {
            var input = GetInput();

            // First attempt: lean path (no extra sleeps).
            // Retries: bring window to foreground first to recover from lost focus.
            if (attempt > 1)
            {
                _window.SetForeground();
                Thread.Sleep(FocusSettleMs);
            }

            input.Click();
            Thread.Sleep(FocusSettleMs);

            SetClipboardText(message);

            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            Thread.Sleep(PasteSettleMs);

            // Verify the paste worked — the Send button appears when text is present
            var sendButton = _window.FindFirstDescendant(cf =>
                cf.ByAutomationId(AutomationIds.SendButton));
            if (sendButton is not null)
                return;

            // Send button may be slow to appear; check input text directly
            if (HasText(input))
                return;

            Trace.TraceWarning(
                $"[ChatInputBar] Paste attempt {attempt}/{MaxPasteRetries} failed — " +
                "Send button not found and input appears empty. Retrying...");

            // Clear any partial state before retrying
            if (attempt < MaxPasteRetries)
            {
                _window.SetForeground();
                input.Click();
                Thread.Sleep(FocusSettleMs);
                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
                Thread.Sleep(100);
                Keyboard.Press(VirtualKeyShort.DELETE);
                Thread.Sleep(FocusSettleMs);
            }
        }

        throw new InvalidOperationException(
            $"Failed to type message after {MaxPasteRetries} attempts. " +
            "The clipboard paste did not reach the RichEditBox input.");
    }

    private static void SetClipboardText(string text)
    {
        var sta = new Thread(() => System.Windows.Forms.Clipboard.SetText(text));
        sta.SetApartmentState(ApartmentState.STA);
        sta.Start();
        sta.Join();
    }

    private static bool HasText(AutomationElement input)
    {
        try
        {
            if (input.Patterns.Text.IsSupported)
            {
                var text = input.Patterns.Text.Pattern.DocumentRange.GetText(-1);
                return !string.IsNullOrWhiteSpace(text);
            }

            if (input.Patterns.Value.IsSupported)
            {
                var value = input.Patterns.Value.Pattern.Value.Value;
                return !string.IsNullOrWhiteSpace(value);
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Clicks the Send button (AutomationId='SendOrPauseUserPromptEvent').
    /// This button only appears in the UI after text has been typed into the input.
    /// </summary>
    public void Submit()
    {
        var sendButton = WaitHelper.ForElement(
            () => _window.FindFirstDescendant(cf =>
                cf.ByAutomationId(AutomationIds.SendButton)),
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
                    cf.ByAutomationId(AutomationIds.SendButton));
                if (btn is null || btn.Name != AutomationIds.SendButtonLabel) return true;

                // Secondary signal: input text was cleared after submission
                var input = _window.FindFirstDescendant(cf =>
                    cf.ByAutomationId(AutomationIds.ChatInput));
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
            () => _window.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.ChatInput)),
            TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds)) is not null;

    private AutomationElement GetInput() =>
        WaitHelper.ForElement(
            () => _window.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.ChatInput)),
            TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds))
        ?? throw new InvalidOperationException("Chat input (richEditBox) not found.");
}
