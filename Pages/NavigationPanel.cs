using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using HPAICOmpanionTester.Configuration;
using HPAICOmpanionTester.Support;

namespace HPAICOmpanionTester.Pages;

/// <summary>
/// Page Object for the left navigation list (NavLinksList).
/// Sections: Home | Library | Perform | Spotlight | Help
/// </summary>
public sealed class NavigationPanel
{
    private readonly Window _window;
    private readonly TestSettings _settings;

    // Time for the UI to settle after pressing the back button from chat view
    private const int BackButtonSettleMs = 1000;

    public NavigationPanel(MainPage mainPage, TestSettings settings)
    {
        _window = mainPage.Window;
        _settings = settings;
    }

    /// <summary>
    /// Clicks the named nav item and waits for the PageTitle element to confirm
    /// the section has fully loaded before returning.
    /// </summary>
    public void NavigateTo(string sectionName)
    {
        var item = FindNavItem(sectionName);
        item.Click();

        // Page transitions (especially on first navigation) can take longer than
        // a normal action — use LaunchTimeoutSeconds so slow system states don't
        // cause false failures.
        WaitHelper.Until(
            () => _window.FindFirstDescendant(cf =>
                cf.ByAutomationId(AutomationIds.PageTitle))?.Name == sectionName,
            TimeSpan.FromSeconds(_settings.LaunchTimeoutSeconds));
    }

    /// <summary>Returns the currently selected nav item name, or empty string.</summary>
    public string SelectedSection()
    {
        var navList = GetNavList();
        foreach (var item in navList.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)))
        {
            if (item.Patterns.SelectionItem.IsSupported &&
                item.Patterns.SelectionItem.Pattern.IsSelected.Value)
                return item.Name;
        }
        return string.Empty;
    }

    private AutomationElement GetNavList()
    {
        // If the app is in full-screen chat view, there's no NavLinksList —
        // press the back button first to return to the section view.
        var navList = _window.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.NavList));
        if (navList is null)
        {
            var backButton =
                _window.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.BackButton))
                ?? _window.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.BackButtonAlt))
                ?? _window.FindFirstDescendant(cf => cf.ByName(AutomationIds.BackButtonName));

            if (backButton is not null)
            {
                if (backButton.Patterns.Invoke.IsSupported)
                    backButton.Patterns.Invoke.Pattern.Invoke();
                else
                    backButton.Click();
                Thread.Sleep(BackButtonSettleMs);
            }
        }

        return WaitHelper.ForElement(
            () => _window.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.NavList)),
            TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds))
        ?? throw new InvalidOperationException("NavLinksList not found.");
    }

    private AutomationElement FindNavItem(string name)
    {
        var navList = GetNavList();
        return WaitHelper.ForElement(
            () => navList.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.ListItem).And(cf.ByName(name))),
            TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds))
        ?? throw new InvalidOperationException($"Nav item '{name}' not found.");
    }
}
