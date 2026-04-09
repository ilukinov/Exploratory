using System.Drawing;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using HPAICOmpanionTester.Drivers;
using HPAICOmpanionTester.Support;

namespace HPAICOmpanionTester.Pages;

/// <summary>
/// Page Object for the HP AI Companion main window.
///
/// Encapsulates all FlaUI element lookups so that step definitions contain
/// zero UI-automation code — they read like plain English and delegate here.
/// If the app's UI tree ever changes, only this class needs updating.
/// </summary>
public sealed class MainPage
{
    private readonly AppDriver _driver;
    private Window? _cachedWindow;

    public MainPage(AppDriver driver)
    {
        _driver = driver;
    }

    // Lazy: we fetch the window once and reuse it within the scenario.
    // Safe because Reqnroll creates new page object instances per scenario via DI,
    // so a stale reference from a relaunch never carries over.
    // Internal so NavigationPanel and ChatInputBar can access it without re-fetching.
    internal Window Window => _cachedWindow ??= _driver.GetMainWindow();

    /// <summary>Window title bar text.</summary>
    public string Title => Window.Title;

    /// <summary>
    /// True when the window exists, is on-screen, and has rendered with non-zero dimensions.
    /// This is the minimal signal that the UI is actually displayed, not just a process stub.
    /// </summary>
    public bool IsVisible()
    {
        try
        {
            var bounds = Window.BoundingRectangle;
            return Window.IsAvailable
                   && bounds != Rectangle.Empty
                   && bounds.Width > 0
                   && bounds.Height > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Current page section title (AutomationId='PageTitle').
    /// Changes when the user navigates between Home / Library / Perform / Spotlight / Help.
    /// Returns empty string if the element doesn't exist or isn't fully initialized yet.
    /// </summary>
    public string PageTitle
    {
        get
        {
            try
            {
                return Window.FindFirstDescendant(cf => cf.ByAutomationId("PageTitle"))?.Name
                       ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Polls until PageTitle equals <paramref name="expected"/>, or the timeout expires.
    /// Use after clicking a nav item to confirm the page has loaded.
    /// </summary>
    public bool WaitForPageTitle(string expected, TimeSpan timeout) =>
        WaitHelper.Until(() => PageTitle == expected, timeout);

    /// <summary>
    /// Polls until the app is past the splash screen and showing real content.
    ///
    /// The app has two interactive states:
    ///   1. Section view — NavLinksList + PageTitle visible (Home, Library, etc.)
    ///   2. Chat view — richEditBox visible (after sending a message, the app
    ///      navigates into a full-screen chat with a back button and no nav panel)
    ///
    /// Either state means the app is interactive and ready for the test.
    /// </summary>
    public bool WaitUntilInteractive(TimeSpan timeout) =>
        WaitHelper.Until(
            () =>
            {
                var hasNavAndTitle =
                    Window.FindFirstDescendant(cf => cf.ByAutomationId("NavLinksList")) is not null
                    && Window.FindFirstDescendant(cf => cf.ByAutomationId("PageTitle")) is not null;

                var hasChatInput =
                    Window.FindFirstDescendant(cf => cf.ByAutomationId("richEditBox")) is not null;

                return hasNavAndTitle || hasChatInput;
            },
            timeout);
}
