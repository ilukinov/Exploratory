using HPAICOmpanionTester.Configuration;
using HPAICOmpanionTester.Pages;

namespace HPAICOmpanionTester.StepDefinitions;

[Binding]
public sealed class ChatSteps
{
    private readonly ChatInputBar _chatInput;
    private readonly ChatHistory _chatHistory;
    private readonly TestSettings _settings = TestSettings.Load();
    private readonly ScenarioContext _scenarioContext;

    public ChatSteps(ChatInputBar chatInput, ChatHistory chatHistory, ScenarioContext scenarioContext)
    {
        _chatInput = chatInput;
        _chatHistory = chatHistory;
        _scenarioContext = scenarioContext;
    }

    [Then("the chat input should be visible and enabled")]
    public void ThenChatInputIsVisible()
    {
        _chatInput.IsVisible().Should().BeTrue(
            because: "the chat input must be present on the home screen for the user to ask questions");
    }

    [When("I type {string} into the chat input")]
    public void WhenIType(string message)
    {
        _chatInput.TypeMessage(message);
        _scenarioContext["LastSentMessage"] = message; // Store for use in later steps
    }

    [When("I submit the message")]
    public void WhenISubmit()
    {
        _chatInput.Submit();
    }

    [Then("the message should be accepted for processing")]
    public void ThenMessageShouldBeAccepted()
    {
        _chatInput.WaitForInputToClear(TimeSpan.FromSeconds(5))
            .Should().BeTrue(
                because: "the Send button should switch to Pause mode when the AI starts processing the request");
    }

    [Then("the message {string} should appear in the chat")]
    public void ThenMessageShouldAppearInChat(string messageText)
    {
        _chatHistory.WaitForMessageBubble(messageText, TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds))
            .Should().BeTrue(
                because: $"the message '{messageText}' must appear as a bubble in the chat history after sending");
    }

    [Then("the agent should respond within {int} seconds")]
    public void ThenAgentShouldRespond(int timeoutSeconds)
    {
        var lastMessage = _scenarioContext.TryGetValue("LastSentMessage", out var msg)
            ? msg.ToString() ?? "test message"
            : "test message";

        _chatHistory.WaitForAgentResponse(lastMessage, TimeSpan.FromSeconds(timeoutSeconds))
            .Should().BeTrue(
                because: $"the AI assistant should post a response within {timeoutSeconds} seconds");
    }

    [When("I click edit on the message {string}")]
    public void WhenIClickEditOnMessage(string messageText)
    {
        _chatHistory.ClickEditOnMessage(messageText, TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds));
        _scenarioContext["LastSentMessage"] = messageText;
    }

    [Then("there should be at least {int} messages in the chat")]
    public void ThenChatShouldHaveMessages(int minimumCount)
    {
        _chatHistory.MessageCount().Should().BeGreaterThanOrEqualTo(minimumCount,
            because: $"the chat should contain at least {minimumCount} message(s)");
    }

    [Then("the message {string} should no longer be in the chat")]
    public void ThenMessageShouldDisappear(string messageText)
    {
        _chatHistory.WaitForMessageToDisappear(messageText, TimeSpan.FromSeconds(_settings.ActionTimeoutSeconds))
            .Should().BeTrue(
                because: $"the message '{messageText}' should have been removed after editing an earlier message");
    }

    [Then("the message {string} should still be in the chat")]
    public void ThenMessageShouldStillBeInChat(string messageText)
    {
        _chatHistory.WaitForMessageBubble(messageText, TimeSpan.FromSeconds(3))
            .Should().BeTrue(
                because: $"the message '{messageText}' should still be visible in the chat after editing an earlier message");
    }
}
