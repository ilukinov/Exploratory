using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using HPAICOmpanionTester.Support;

namespace HPAICOmpanionTester.Pages;

/// <summary>
/// Page Object for the chat message history area.
///
/// Message bubbles in HP AI Companion are Group elements whose Name and
/// AutomationId are both set to the full message text. The chat history
/// accumulates across scenarios (app is reused), so detection must match
/// specific messages rather than counting totals.
/// </summary>
public sealed class ChatHistory
{
    private readonly Window _window;

    public ChatHistory(MainPage mainPage)
    {
        _window = mainPage.Window;
    }

    /// <summary>
    /// Waits until the sent message appears as a bubble in the chat history.
    /// </summary>
    public bool WaitForMessageBubble(string messageText, TimeSpan timeout) =>
        WaitHelper.Until(
            () => FindMessageBubble(messageText) is not null,
            timeout);

    /// <summary>
    /// Waits until the agent has posted a response after the user's message.
    /// Snapshots the current bubble count, then polls until a new bubble appears
    /// whose text differs from the user's message.
    /// </summary>
    public bool WaitForAgentResponse(string userMessage, TimeSpan timeout)
    {
        if (!WaitForMessageBubble(userMessage, TimeSpan.FromSeconds(5)))
            return false;

        // Snapshot AFTER confirming the user message is visible, so the count
        // is accurate even after an edit that removed subsequent messages.
        var countAfterSend = CountBubbles();
        var longGroupsBefore = CountLongNameGroups();

        return WaitHelper.Until(
            () =>
            {
                // Primary: a new user-style bubble appeared (Name == AutomationId)
                var bubbles = FindAllBubbles();
                if (bubbles.Count > countAfterSend && bubbles.Any(b => b != userMessage))
                    return true;

                // Fallback for agent responses that don't match the strict pattern
                // (e.g. Perform Assistant). Detect when a new Group with substantial
                // text appeared since the snapshot.
                return CountLongNameGroups() > longGroupsBefore;
            },
            timeout);
    }

    /// <summary>
    /// Counts Group elements with Names longer than 20 chars. Used as a fallback
    /// to detect agent responses (like Perform Assistant) that don't follow
    /// the strict Name==AutomationId pattern.
    /// </summary>
    private int CountLongNameGroups()
    {
        try
        {
            var groups = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.Group));
            var count = 0;
            foreach (var g in groups)
            {
                try
                {
                    var name = g.Properties.Name.ValueOrDefault ?? "";
                    if (name.Length > 20) count++;
                }
                catch { }
            }
            return count;
        }
        catch { return 0; }
    }

    /// <summary>
    /// Returns the count of distinct message bubbles currently in the chat.
    /// </summary>
    public int MessageCount() => CountBubbles();

    /// <summary>
    /// Clicks the "Edit message" button associated with a specific user message bubble.
    /// Multiple edit buttons may exist (one per user message), so we find the one
    /// closest to the target bubble by comparing bounding rectangles.
    /// </summary>
    public void ClickEditOnMessage(string messageText, TimeSpan timeout)
    {
        var bubble = WaitHelper.ForElement(
            () => FindMessageBubble(messageText),
            timeout)
            ?? throw new InvalidOperationException(
                $"Message bubble '{messageText}' not found.");

        // Scroll the bubble into view so action buttons are accessible
        if (bubble.Patterns.ScrollItem.IsSupported)
            bubble.Patterns.ScrollItem.Pattern.ScrollIntoView();
        Thread.Sleep(300);

        var editButton = WaitHelper.ForElement(
            () => FindClosestEditButton(bubble),
            TimeSpan.FromSeconds(5))
            ?? throw new InvalidOperationException(
                "Edit message button not found.");

        if (editButton.Patterns.Invoke.IsSupported)
            editButton.Patterns.Invoke.Pattern.Invoke();
        else
            editButton.Click();
    }

    /// <summary>
    /// Finds the "Edit message" button closest to the given bubble element.
    /// When multiple user messages exist, each has its own edit button —
    /// picking the nearest one ensures we edit the correct message.
    /// </summary>
    private AutomationElement? FindClosestEditButton(AutomationElement bubble)
    {
        var allEditButtons = _window.FindAllDescendants(cf =>
            cf.ByAutomationId("Edit message"));

        if (allEditButtons.Length == 0)
            return null;

        if (allEditButtons.Length == 1)
            return allEditButtons[0];

        // Pick the edit button whose vertical center is closest to the bubble's
        var bubbleCenter = bubble.BoundingRectangle.Top +
                           bubble.BoundingRectangle.Height / 2.0;

        AutomationElement? closest = null;
        var minDistance = double.MaxValue;

        foreach (var btn in allEditButtons)
        {
            try
            {
                var btnCenter = btn.BoundingRectangle.Top +
                                btn.BoundingRectangle.Height / 2.0;
                var distance = Math.Abs(btnCenter - bubbleCenter);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = btn;
                }
            }
            catch
            {
                // Element may be off-screen or stale
            }
        }

        return closest;
    }

    /// <summary>
    /// Waits until a message bubble is no longer present in the chat.
    /// </summary>
    public bool WaitForMessageToDisappear(string messageText, TimeSpan timeout) =>
        WaitHelper.Until(
            () => !IsMessageVisible(messageText),
            timeout);

    private AutomationElement? FindMessageBubble(string messageText)
    {
        try
        {
            // Real message bubbles have both Name AND AutomationId set to the message text.
            // This distinguishes them from text labels or other Group elements.
            return _window.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Group)
                    .And(cf.ByName(messageText))
                    .And(cf.ByAutomationId(messageText)));
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[ChatHistory] FindMessageBubble failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks whether a message bubble is present AND visible (not off-screen or hidden).
    /// The UI tree may retain collapsed elements after edits remove messages.
    /// </summary>
    private bool IsMessageVisible(string messageText)
    {
        var element = FindMessageBubble(messageText);
        if (element is null)
            return false;

        try
        {
            if (element.Properties.IsOffscreen.ValueOrDefault)
                return false;

            var bounds = element.BoundingRectangle;
            return bounds.Width > 0 && bounds.Height > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns distinct message texts from all Group elements that have both Name
    /// and AutomationId set to the same value (the message text). This distinguishes
    /// real message bubbles from layout containers.
    /// </summary>
    private List<string> FindAllBubbles()
    {
        var result = new List<string>();
        try
        {
            var groups = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.Group));
            var seen = new HashSet<string>();

            foreach (var el in groups)
            {
                try
                {
                    if (!el.Properties.Name.IsSupported ||
                        !el.Properties.AutomationId.IsSupported)
                        continue;

                    var name = el.Properties.Name.Value;
                    var autoId = el.Properties.AutomationId.Value;

                    // Real message bubbles have Name == AutomationId == message text
                    if (!string.IsNullOrWhiteSpace(name) && name == autoId && seen.Add(name))
                        result.Add(name);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"[ChatHistory] Element inspection failed: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[ChatHistory] FindAllBubbles failed: {ex.Message}");
        }

        return result;
    }

    private int CountBubbles() => FindAllBubbles().Count;

    /// <summary>
    /// Returns the last agent response text visible in the chat.
    /// Agent responses are Group elements whose Name differs from known user messages
    /// and whose Name != AutomationId (user messages have Name == AutomationId).
    /// </summary>
    public string? GetLastAgentResponseText()
    {
        try
        {
            var groups = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.Group));
            string? lastResponse = null;

            foreach (var el in groups)
            {
                try
                {
                    if (!el.Properties.Name.IsSupported) continue;

                    var name = el.Properties.Name.Value;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var autoId = el.Properties.AutomationId.IsSupported
                        ? el.Properties.AutomationId.Value
                        : string.Empty;

                    // Agent responses have Name != AutomationId (or no AutomationId),
                    // and typically have longer text content.
                    if (name != autoId && name.Length > 10)
                        lastResponse = name;
                }
                catch { }
            }

            return lastResponse;
        }
        catch
        {
            return null;
        }
    }

}
