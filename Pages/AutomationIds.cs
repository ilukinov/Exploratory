namespace HPAICOmpanionTester.Pages;

/// <summary>
/// Centralises all AutomationId and Name strings used to locate UI elements.
/// If the app renames an element, only this file needs updating.
/// </summary>
public static class AutomationIds
{
    public const string ChatInput = "richEditBox";
    public const string SendButton = "SendOrPauseUserPromptEvent";
    public const string NavList = "NavLinksList";
    public const string PageTitle = "PageTitle";
    public const string EditMessage = "Edit message";
    public const string BackButton = "NavigationViewBackButton";
    public const string BackButtonAlt = "BackButton";
    public const string BackButtonName = "Back";
    public const string SendButtonLabel = "Send";

    // Agent response card elements
    public const string AnswerCard = "AnswerCard";
    public const string CopyAnswer = "Copy Answer";
    public const string LikeButton = "Like";
    public const string DislikeButton = "Dislike";
    public const string RetryButton = "Retry";
}
