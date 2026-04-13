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
    /// UI tree structure (from FlaUInspect):
    ///   AnswerCard
    ///   ├── questionGrid   ← created immediately on send (holds user bubble)
    ///   └── AnswerGrid     ← appears when the agent response arrives
    ///       └── ResponseButtonsGrid  ← Copy/Like/Dislike/Retry buttons
    ///
    /// Detection: ResponseButtonsGrid only appears with the final rendered response
    /// (not during "Thinking..." state). We compare the current count of
    /// ResponseButtonsGrid elements against a baseline taken before submission.
    /// A new one means a new completed response — immune to duplicate message texts
    /// in chat history and section switches.
    /// </summary>
    /// <summary>
    /// Waits until the agent has posted a response after the user's message.
    ///
    /// UI tree during "Thinking..." state:
    ///   AnswerGrid → ResponseButtonsGrid + Text "Thinking"
    ///
    /// UI tree when response is complete:
    ///   AnswerGrid → ResponseButtonsGrid + Text "assistant name" + Group "response text" + ...
    ///                (no "Thinking" text)
    ///
    /// Detection: find the last AnswerCard, check its AnswerGrid has children
    /// AND the "Thinking" text is gone (replaced by actual response content).
    /// </summary>
    public bool WaitForAgentResponse(TimeSpan timeout)
    {
        var fastPoll = TimeSpan.FromMilliseconds(200);

        return WaitHelper.Until(
            () =>
            {
                var cards = FindAllAnswerCards();
                if (cards.Length == 0)
                    return false;

                var lastCard = cards[^1];
                var answerGrid = FindChildByAutomationId(lastCard, AutomationIds.AnswerGrid);
                if (answerGrid is null)
                    return false;

                // Response is complete when AnswerGrid exists but no longer shows "Thinking"
                return !HasThinkingIndicator(answerGrid);
            },
            timeout,
            fastPoll);
    }

    /// <summary>
    /// Checks whether the AnswerGrid contains a "Thinking" text element,
    /// indicating the response is still being generated.
    /// </summary>
    private static bool HasThinkingIndicator(AutomationElement answerGrid)
    {
        try
        {
            var thinkingEl = answerGrid.FindFirstChild(cf =>
                cf.ByControlType(ControlType.Text).And(cf.ByName("Thinking")));
            return thinkingEl is not null;
        }
        catch
        {
            return false;
        }
    }

    private AutomationElement[] FindAllAnswerCards()
    {
        try { return _window.FindAllDescendants(cf => cf.ByAutomationId(AutomationIds.AnswerCard)); }
        catch { return []; }
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

    /// <summary>
    /// Finds the AnswerCard that contains the given user message.
    /// Walks up from the user bubble (Group "hi" "hi") through questionGrid
    /// to the enclosing AnswerCard.
    /// </summary>
    private AutomationElement? FindAnswerCardForMessage(string messageText)
    {
        try
        {
            var bubble = FindMessageBubble(messageText);
            if (bubble is null)
                return null;

            // Walk up: bubble → questionGrid → AnswerCard
            var walker = bubble.Automation.TreeWalkerFactory.GetControlViewWalker();
            var current = walker.GetParent(bubble);
            while (current is not null)
            {
                if (GetAutoId(current) == AutomationIds.AnswerCard)
                    return current;
                current = walker.GetParent(current);
            }

            return null;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[ChatHistory] FindAnswerCardForMessage failed: {ex.Message}");
            return null;
        }
    }

    private static AutomationElement? FindChildByAutomationId(AutomationElement parent, string automationId)
    {
        try
        {
            return parent.FindFirstChild(cf => cf.ByAutomationId(automationId));
        }
        catch
        {
            return null;
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

    /// <summary>
    /// Returns the last (most recent) message bubble matching <paramref name="messageText"/>.
    /// Chat history accumulates across scenarios — the same text may appear multiple
    /// times, so we need the last match (latest message), not the first.
    /// </summary>
    private AutomationElement? FindMessageBubble(string messageText)
    {
        try
        {
            var all = _window.FindAllDescendants(cf =>
                cf.ByControlType(ControlType.Group)
                    .And(cf.ByName(messageText))
                    .And(cf.ByAutomationId(messageText)));
            return all.Length > 0 ? all[^1] : null;
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
