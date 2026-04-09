using HPAICOmpanionTester.Configuration;
using HPAICOmpanionTester.Pages;

namespace HPAICOmpanionTester.StepDefinitions;

[Binding]
public sealed class NavigationSteps
{
    private readonly NavigationPanel _nav;
    private readonly MainPage _mainPage;
    private readonly TestSettings _settings;

    public NavigationSteps(NavigationPanel nav, MainPage mainPage, TestSettings settings)
    {
        _nav = nav;
        _mainPage = mainPage;
        _settings = settings;
    }

    [Given("I navigate to the {string} section")]
    [When("I navigate to the {string} section")]
    public void WhenINavigateTo(string section)
    {
        _nav.NavigateTo(section);
    }

    [Then("the page title should be {string}")]
    public void ThenPageTitleShouldBe(string expectedTitle)
    {
        _mainPage
            .WaitForPageTitle(expectedTitle, TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds))
            .Should().BeTrue(
                because: $"navigating to '{expectedTitle}' must update the page title");

        _mainPage.PageTitle.Should().Be(expectedTitle);
    }
}
