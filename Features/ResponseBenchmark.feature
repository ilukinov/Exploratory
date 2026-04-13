Feature: Agent Response Benchmark
  As a tester of HP AI Companion
  I want to verify agent responses work reliably across section switches
  So that I can confirm response detection is consistent regardless of context

  Background:
    Given HP AI Companion is launched

  Scenario: Cross-section response benchmark
    # 1. Home — send a message and wait for response
    And I navigate to the "Home" section
    When I type "what day is it today?" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds

    # 2. Perform — switch and send a simple greeting
    When I navigate to the "Perform" section
    And I type "hi" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds

    # 3. Home — switch back and send another message
    When I navigate to the "Home" section
    And I type "tell me a fun fact" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds

    # 4. Perform — switch back and issue a real command
    When I navigate to the "Perform" section
    And I type "set my volume to 50%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
