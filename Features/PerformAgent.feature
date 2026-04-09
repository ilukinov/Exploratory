Feature: Perform Agent Device Control
  As a tester of HP AI Companion
  I want to issue device control commands through the Perform agent
  So that I can verify the NLP pipeline correctly changes system settings

  Background:
    Given HP AI Companion is launched
    And I navigate to the "Perform" section

  # ── Volume Control ──────────────────────────────────────────────

  Scenario: Set volume to a specific level
    Given I note the current system volume
    And I bookmark the app log
    When I type "set volume to 50%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And the app log should contain intent "set_audio_volume_speaker" with value "50"
    And the system volume should be approximately 50 within 5 seconds

  Scenario: Set volume to maximum
    And I bookmark the app log
    When I type "set volume to 100%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And the app log should contain intent "set_audio_volume_speaker" with value "100"
    And the system volume should be approximately 100 within 5 seconds

  Scenario: Set volume to minimum
    And I bookmark the app log
    When I type "set volume to 0%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And the app log should contain intent "set_audio_volume_speaker" with value "0"
    And the system volume should be approximately 0 within 5 seconds

  # ── Brightness Control ──────────────────────────────────────────

  Scenario: Set brightness to a specific level
    Given I note the current screen brightness
    And I bookmark the app log
    When I type "set brightness to 50%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And the app log should contain intent "set_display_brightness" with value "50"
    And the screen brightness should be approximately 50 within 5 seconds

  Scenario: Set brightness to maximum
    And I bookmark the app log
    When I type "set brightness to 100%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And the app log should contain intent "set_display_brightness" with value "100"
    And the screen brightness should be approximately 100 within 5 seconds

  # ── Edge Cases / Boundary Values ────────────────────────────────

  Scenario: Out-of-range volume value is handled gracefully
    And I bookmark the app log
    When I type "set volume to 500%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And there should be no errors in the app log since the bookmark

  Scenario: Negative brightness value is handled gracefully
    And I bookmark the app log
    When I type "set brightness to -50%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And there should be no errors in the app log since the bookmark

  # ── Non-Numeric Value Bugs ──────────────────────────────────────

  @bug
  Scenario Outline: Non-numeric volume value defaults to 50 instead of being rejected
    Given I note the current system volume
    And I bookmark the app log
    When I type "set volume to 100%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And the system volume should be approximately 100 within 5 seconds
    And I bookmark the app log
    When I type "set volume to <value>" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And the app log should contain intent "set_audio_volume_speaker"
    And the system volume should not have changed from 100

    Examples:
      | value |
      | A     |
      | hello |
      | 😀    |

  # ── Restore ─────────────────────────────────────────────────────

  Scenario: Restore original volume after tests
    Given I note the current system volume
    When I type "set volume to 50%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
