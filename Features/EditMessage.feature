Feature: Edit Sent Message
  As a user of HP AI Companion
  I want to understand how editing a sent message behaves
  So that I can report unexpected side effects

  Background:
    Given HP AI Companion is launched
    And I navigate to the "Home" section

  Scenario: Editing a message without changes still regenerates the response
    When I type "What day is it today?" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    When I click edit on the message "What day is it today?"
    And I submit the message
    Then the agent should respond within 15 seconds

  Scenario: Editing a message removes all subsequent messages from the chat
    When I type "Tell me a fun fact" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    When I type "Tell me another fun fact" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And there should be at least 4 messages in the chat
    When I click edit on the message "Tell me a fun fact"
    And I submit the message
    Then the agent should respond within 15 seconds
    And the message "Tell me another fun fact" should no longer be in the chat
