Feature: Chat Interaction
  As a user of HP AI Companion
  I want to type and send messages through the chat input
  So that I can interact with the AI assistant on any chat-enabled screen

  Scenario Outline: Assistant responds to a user message
    Given HP AI Companion is launched
    And I navigate to the "<section>" section
    Then the chat input should be visible and enabled
    When I type "<message>" into the chat input
    And I submit the message
    Then the message should be accepted for processing
    And the message "<message>" should appear in the chat
    And the agent should respond within <timeout> seconds

    Examples:
      | section | message                      | timeout |
      | Home    | What time is it?             | 15      |
      | Perform | What is my current hardware? | 15      |
