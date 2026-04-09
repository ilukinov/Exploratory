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

    // Minimum Name length to consider a Group element as an agent response
    private const int AgentResponseMinLength = 10;
    // Time for scroll-into-view to settle before interacting
    private const int ScrollSettleMs = 300;

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
    ///
    /// Detection uses structural tree walking (like nextSibling in WebDriver):
    ///   1. Find the user message bubble (Group with Name == AutomationId == messageText)
    ///   2. Walk up to its parent ListViewItem in the ChatListView
    ///   3. Get the next sibling ListViewItem — this is the agent response
    ///   4. Check if it has any children (response content has rendered)
    ///
    /// This is O(1) per poll — no full-tree scans. Falls back to Send/Stop button
    /// tracking if the tree structure doesn't match expectations.
    /// </summary>
    public bool WaitForAgentResponse(string userMessage, TimeSpan timeout)
    {
        // Wait for the user message bubble so we can anchor detection
        var userBubble = WaitHelper.ForElement(
            () => FindMessageBubble(userMessage),
            TimeSpan.FromSeconds(5));

        if (userBubble is null)
            return false;

        // Track Send/Stop button transitions as a fallback
        var sawStopState = false;
        // Track consecutive polls where the button is absent without ever seeing Stop.
        // For edge-case inputs the app may handle them locally (no AI call), so the
        // button goes Send → absent without a Stop phase. After enough absent polls
        // we can safely assume the response is done.
        var consecutiveAbsentPolls = 0;
        const int absentPollsThreshold = 7; // ~1 second at 150 ms

        // Use a faster poll interval — agent responses can appear quickly
        // and the default 500ms can miss transient button states entirely.
        var fastPoll = TimeSpan.FromMilliseconds(150);

        return WaitHelper.Until(
            () =>
            {
                // Strategy 1 (O(1), preferred): walk to the next sibling of the user
                // message's ListViewItem. If it exists and has children, the agent responded.
                if (HasNextSiblingWithContent(userBubble))
                    return true;

                // Re-find the bubble in case the original reference went stale
                // after a UI re-render, then retry Strategy 1 with the fresh reference.
                var freshBubble = FindMessageBubble(userMessage);
                if (freshBubble is not null)
                {
                    userBubble = freshBubble;
                    if (HasNextSiblingWithContent(freshBubble))
                        return true;
                }

                // Strategy 2 (fallback): Send/Stop button tracking.
                // The button shows Name='Stop' during streaming, then disappears when done.
                var btn = FindSendButton();
                if (btn is not null)
                {
                    var btnName = GetName(btn);
                    if (btnName != AutomationIds.SendButtonLabel)
                        sawStopState = true;
                    consecutiveAbsentPolls = 0;
                }
                else if (sawStopState)
                {
                    return true;
                }
                else
                {
                    // Strategy 3: Button absent without ever seeing Stop state.
                    // The app likely handled the input locally (e.g. edge-case/invalid
                    // values). After a sustained absence, treat the response as complete.
                    consecutiveAbsentPolls++;
                    if (consecutiveAbsentPolls >= absentPollsThreshold)
                        return true;
                }

                return false;
            },
            timeout,
            fastPoll);
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
        Thread.Sleep(ScrollSettleMs);

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
    /// Waits until a message bubble is no longer present in the chat.
    /// </summary>
    public bool WaitForMessageToDisappear(string messageText, TimeSpan timeout) =>
        WaitHelper.Until(
            () => !IsMessageVisible(messageText),
            timeout);

    /// <summary>
    /// Returns the last agent response text visible in the chat.
    /// Agent responses are Group elements whose Name differs from AutomationId
    /// and whose Name has substantial length.
    /// </summary>
    public string? GetLastAgentResponseText()
    {
        var agentGroups = FindGroups(el =>
        {
            var name = GetName(el);
            if (name.Length <= AgentResponseMinLength) return false;
            var autoId = GetAutoId(el);
            return name != autoId;
        });

        return agentGroups.Count > 0
            ? GetName(agentGroups[^1])
            : null;
    }

    // ── Private helpers ─────────────────────────────────────────

    /// <summary>
    /// Single traversal method for Group elements. Applies <paramref name="predicate"/>
    /// to each Group, swallowing per-element exceptions from stale/off-screen elements.
    /// </summary>
    private List<AutomationElement> FindGroups(Func<AutomationElement, bool> predicate)
    {
        try
        {
            var groups = _window.FindAllDescendants(cf => cf.ByControlType(ControlType.Group));
            var result = new List<AutomationElement>();
            foreach (var g in groups)
            {
                try
                {
                    if (predicate(g))
                        result.Add(g);
                }
                catch
                {
                    // Element may be stale or off-screen
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[ChatHistory] FindGroups failed: {ex.Message}");
            return [];
        }
    }

    private int CountGroups(Func<AutomationElement, bool> predicate) =>
        FindGroups(predicate).Count;

    /// <summary>
    /// O(1) sibling-based detection: walks the UI tree from the user message bubble
    /// up to its parent ListViewItem, then checks the next sibling ListViewItem
    /// for content (the agent response). This mirrors WebDriver's nextSibling approach.
    ///
    /// Tree structure: ChatListView (ListView) → ListViewItem (user) → ListViewItem (response)
    /// </summary>
    private bool HasNextSiblingWithContent(AutomationElement userBubble)
    {
        try
        {
            var walker = userBubble.Automation.TreeWalkerFactory.GetControlViewWalker();

            // Walk up from the bubble to find the enclosing ListViewItem
            var current = walker.GetParent(userBubble);
            while (current is not null)
            {
                if (current.ControlType == ControlType.ListItem)
                    break;
                current = walker.GetParent(current);
            }

            if (current is null)
                return false;

            // Get the next sibling ListViewItem — this is the agent response container
            var nextSibling = walker.GetNextSibling(current);
            if (nextSibling is null)
                return false;

            // The response is ready when the sibling has child content rendered
            var firstChild = walker.GetFirstChild(nextSibling);
            return firstChild is not null;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[ChatHistory] HasNextSiblingWithContent failed: {ex.Message}");
            return false;
        }
    }

    private AutomationElement? FindSendButton()
    {
        try
        {
            return _window.FindFirstDescendant(cf =>
                cf.ByAutomationId(AutomationIds.SendButton));
        }
        catch { return null; }
    }


    private static string GetName(AutomationElement el) =>
        el.Properties.Name.IsSupported ? el.Properties.Name.ValueOrDefault ?? "" : "";

    private static string GetAutoId(AutomationElement el) =>
        el.Properties.AutomationId.IsSupported ? el.Properties.AutomationId.ValueOrDefault ?? "" : "";

    /// <summary>
    /// Returns distinct message texts from all Group elements that have both Name
    /// and AutomationId set to the same non-empty value (the message text).
    /// </summary>
    private List<string> FindAllBubbleTexts()
    {
        var seen = new HashSet<string>();
        var result = new List<string>();

        foreach (var el in FindGroups(IsBubble))
        {
            var name = GetName(el);
            if (seen.Add(name))
                result.Add(name);
        }

        return result;
    }

    private int CountBubbles() => FindAllBubbleTexts().Count;

    private static bool IsBubble(AutomationElement el)
    {
        var name = GetName(el);
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name == GetAutoId(el);
    }

    private AutomationElement? FindMessageBubble(string messageText)
    {
        try
        {
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
    /// Finds the "Edit message" button closest to the given bubble element.
    /// When multiple user messages exist, each has its own edit button —
    /// picking the nearest one ensures we edit the correct message.
    /// </summary>
    private AutomationElement? FindClosestEditButton(AutomationElement bubble)
    {
        var allEditButtons = _window.FindAllDescendants(cf =>
            cf.ByAutomationId(AutomationIds.EditMessage));

        if (allEditButtons.Length == 0)
            return null;

        if (allEditButtons.Length == 1)
            return allEditButtons[0];

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
}
