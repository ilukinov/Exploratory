using HPAICOmpanionTester.Configuration;
using HPAICOmpanionTester.Support;

namespace HPAICOmpanionTester.StepDefinitions;

[Binding]
public sealed class PerformAgentSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly TestSettings _settings = TestSettings.Load();

    public PerformAgentSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    // ── Bookmarks & Snapshots ────────────────────────────────────

    [Given("I bookmark the app log")]
    [When("I bookmark the app log")]
    [Then("I bookmark the app log")]
    public void GivenIBookmarkTheAppLog()
    {
        _scenarioContext["LogBookmark"] = AppLogReader.Bookmark();
    }

    [Given("I note the current system volume")]
    public void GivenINoteTheCurrentVolume()
    {
        _scenarioContext["OriginalVolume"] = SystemStateReader.GetVolume();
    }

    [Given("I note the current screen brightness")]
    public void GivenINoteTheCurrentBrightness()
    {
        _scenarioContext["OriginalBrightness"] = SystemStateReader.GetBrightness();
    }

    // ── Log Assertions ───────────────────────────────────────────

    [Then("the app log should contain intent {string} with value {string}")]
    public void ThenLogShouldContainIntent(string expectedIntent, string expectedValue)
    {
        var bookmark = (long)_scenarioContext["LogBookmark"];

        var result = AppLogReader.WaitForIntent(
            bookmark, expectedIntent, TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds));

        result.Should().NotBeNull(
            because: $"the app log should contain intent '{expectedIntent}' after the command");

        result!.Value.Value.Should().Be(expectedValue,
            because: $"the intent '{expectedIntent}' should have value '{expectedValue}'");
    }

    [Then("the app log should contain intent {string}")]
    public void ThenLogShouldContainIntent(string expectedIntent)
    {
        var bookmark = (long)_scenarioContext["LogBookmark"];

        var result = AppLogReader.WaitForIntent(
            bookmark, expectedIntent, TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds));

        result.Should().NotBeNull(
            because: $"the app log should contain intent '{expectedIntent}' after the command");
    }

    [Then("there should be no errors in the app log since the bookmark")]
    public void ThenNoLogErrors()
    {
        // Give the app a moment to finish processing
        Thread.Sleep(2000);

        var bookmark = (long)_scenarioContext["LogBookmark"];
        var errors = AppLogReader.GetErrorsSince(bookmark);

        errors.Should().BeEmpty(
            because: "the command should not produce any errors in the app log");
    }

    // ── System State Assertions ──────────────────────────────────

    [Then("the system volume should be approximately {int} within {int} seconds")]
    public void ThenVolumeShouldBe(int expected, int timeoutSeconds)
    {
        var matched = WaitHelper.Until(
            () => Math.Abs(SystemStateReader.GetVolume() - expected) <= 5,
            TimeSpan.FromSeconds(timeoutSeconds));

        var actual = SystemStateReader.GetVolume();
        actual.Should().BeCloseTo(expected, 5,
            because: $"the system volume should be ~{expected}% after the agent command (actual: {actual}%)");
    }

    [Then("the screen brightness should be approximately {int} within {int} seconds")]
    public void ThenBrightnessShouldBe(int expected, int timeoutSeconds)
    {
        var brightness = SystemStateReader.GetBrightness();
        if (brightness == -1)
        {
            // No built-in display — skip this assertion
            Assert.Inconclusive("Screen brightness not available (no built-in display or WMI access).");
            return;
        }

        var matched = WaitHelper.Until(
            () =>
            {
                var b = SystemStateReader.GetBrightness();
                return b >= 0 && Math.Abs(b - expected) <= 5;
            },
            TimeSpan.FromSeconds(timeoutSeconds));

        brightness = SystemStateReader.GetBrightness();
        brightness.Should().BeCloseTo(expected, 5,
            because: $"the screen brightness should be ~{expected}% after the agent command (actual: {brightness}%)");
    }

    [Then("I print the intents and errors from the app log")]
    public void ThenPrintIntentsAndErrors()
    {
        // Allow the agent to finish processing
        Thread.Sleep(1000);

        var bookmark = (long)_scenarioContext["LogBookmark"];
        var intents = AppLogReader.GetIntentsSince(bookmark);
        var errors = AppLogReader.GetErrorsSince(bookmark);

        if (intents.Count > 0)
        {
            Console.WriteLine("  Intents detected:");
            foreach (var (intent, value) in intents)
                Console.WriteLine($"    {intent} = {(string.IsNullOrEmpty(value) ? "(empty)" : value)}");
        }
        else
        {
            Console.WriteLine("  No intents detected (cloud-only response or default_fallback)");
        }

        if (errors.Count > 0)
        {
            Console.WriteLine($"  Errors ({errors.Count}):");
            foreach (var err in errors.Take(5))
                Console.WriteLine($"    {err}");
        }
    }

    [Then("the system volume should not have changed from {int}")]
    public void ThenVolumeShouldNotHaveChanged(int expected)
    {
        // Wait briefly to allow any unintended change to take effect
        Thread.Sleep(2000);
        var actual = SystemStateReader.GetVolume();
        actual.Should().BeCloseTo(expected, 5,
            because: $"a non-numeric value should not change the volume from {expected}% (actual: {actual}%) — the agent should reject invalid input, not default to 50%");
    }

    [Then("the system volume should have changed from the original")]
    public void ThenVolumeShouldHaveChanged()
    {
        var original = (int)_scenarioContext["OriginalVolume"];
        var current = SystemStateReader.GetVolume();
        current.Should().NotBe(original,
            because: "the agent command should have changed the volume");
    }

    [Then("the screen brightness should have changed from the original")]
    public void ThenBrightnessShouldHaveChanged()
    {
        var original = (int)_scenarioContext["OriginalBrightness"];
        var current = SystemStateReader.GetBrightness();
        if (current == -1)
        {
            Assert.Inconclusive("Screen brightness not available.");
            return;
        }
        current.Should().NotBe(original,
            because: "the agent command should have changed the brightness");
    }
}
