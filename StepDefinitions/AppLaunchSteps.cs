using HPAICOmpanionTester.Configuration;
using HPAICOmpanionTester.Drivers;
using HPAICOmpanionTester.Pages;

namespace HPAICOmpanionTester.StepDefinitions;

/// <summary>
/// Step definitions for the Application Launch feature.
///
/// Steps are kept intentionally thin — they contain no UI-automation code.
/// All element interactions are delegated to MainPage (Page Object Model),
/// keeping step definitions readable and decoupled from FlaUI specifics.
/// </summary>
[Binding]
public sealed class AppLaunchSteps
{
    private readonly AppDriver _driver;
    private readonly MainPage _mainPage;
    private readonly TestSettings _settings;

    public AppLaunchSteps(AppDriver driver, MainPage mainPage, TestSettings settings)
    {
        _driver = driver;
        _mainPage = mainPage;
        _settings = settings;
    }

    [Given("HP AI Companion is not already running")]
    public void GivenTheAppIsNotRunning()
    {
        _driver.EnsureNotRunning();
    }

    [When("I launch HP AI Companion")]
    public void WhenILaunchTheApp()
    {
        _driver.Launch();
    }

    [Then("the window title should contain {string}")]
    public void ThenTheWindowTitleShouldContain(string expectedTitle)
    {
        _mainPage.Title.Should().Contain(expectedTitle,
            because: "the title bar identifies the application to the user");
    }

    [Then("the window should be visible on screen")]
    public void ThenTheWindowShouldBeVisible()
    {
        _mainPage.IsVisible().Should().BeTrue(
            because: "a launched application must render its window before the user can interact with it");
    }

    [Then("the Home page should load within {int} seconds")]
    public void ThenHomePageShouldLoad(int timeoutSeconds)
    {
        _mainPage.WaitForPageTitle("Home", TimeSpan.FromSeconds(timeoutSeconds))
            .Should().BeTrue(
                because: "the app should finish its splash screen and land on the Home page");

        _mainPage.PageTitle.Should().Be("Home");
    }

    /// <summary>
    /// Shared Background step for Navigation, Chat, and Perform features.
    /// Reuses the running app when healthy; only kills and relaunches when the
    /// previous scenario failed or the process has unexpectedly died.
    /// </summary>
    [Given("HP AI Companion is launched")]
    public void GivenFreshlyLaunched()
    {
        _driver.EnsureLaunched();
        _mainPage.WaitUntilInteractive(TimeSpan.FromSeconds(_settings.LaunchTimeoutSeconds))
            .Should().BeTrue(because: "the app must be fully ready before the test can proceed");
    }
}
