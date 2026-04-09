Feature: Perform Agent Command Probe
  As a tester of HP AI Companion
  I want to send every known device control command through the Perform agent
  So that I can map what the agent can actually control on this system

  Background:
    Given HP AI Companion is launched
    And I navigate to the "Perform" section

  # ── Audio: Volume ───────────────────────────────────────────────

  Scenario: Get current volume
    And I bookmark the app log
    When I type "what is my current volume?" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Increase volume
    And I bookmark the app log
    When I type "increase the volume" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Decrease volume
    And I bookmark the app log
    When I type "decrease the volume" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Audio: Mute/Unmute ──────────────────────────────────────────

  Scenario: Mute speakers
    And I bookmark the app log
    When I type "mute my speakers" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Unmute speakers
    And I bookmark the app log
    When I type "unmute my speakers" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Mute microphone
    And I bookmark the app log
    When I type "mute my microphone" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Unmute microphone
    And I bookmark the app log
    When I type "unmute my microphone" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Audio: Microphone Volume ────────────────────────────────────

  Scenario: Set microphone volume
    And I bookmark the app log
    When I type "set microphone volume to 80%" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Audio: Noise Cancellation ───────────────────────────────────

  Scenario: Turn on noise cancellation
    And I bookmark the app log
    When I type "turn on noise cancellation" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Turn off noise cancellation
    And I bookmark the app log
    When I type "turn off noise cancellation" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Display ─────────────────────────────────────────────────────

  Scenario: Get current brightness
    And I bookmark the app log
    When I type "what is my current brightness?" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Increase brightness
    And I bookmark the app log
    When I type "increase the brightness" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Decrease brightness
    And I bookmark the app log
    When I type "decrease the brightness" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Enable HDR
    And I bookmark the app log
    When I type "enable HDR" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Disable HDR
    And I bookmark the app log
    When I type "disable HDR" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Camera ──────────────────────────────────────────────────────

  Scenario: Turn on camera
    And I bookmark the app log
    When I type "turn on my camera" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Turn off camera
    And I bookmark the app log
    When I type "turn off my camera" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Turn on camera blur
    And I bookmark the app log
    When I type "turn on camera blur" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Enable auto frame
    And I bookmark the app log
    When I type "enable auto frame" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Mouse ───────────────────────────────────────────────────────

  Scenario: Set mouse speed
    And I bookmark the app log
    When I type "set mouse speed to 10" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Increase mouse speed
    And I bookmark the app log
    When I type "increase mouse speed" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Swap mouse buttons
    And I bookmark the app log
    When I type "swap mouse buttons" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Power / BIOS ────────────────────────────────────────────────

  Scenario: Check power mode
    And I bookmark the app log
    When I type "what is my current power mode?" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Set high performance mode
    And I bookmark the app log
    When I type "set power mode to high performance" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Enable fast charge
    And I bookmark the app log
    When I type "enable fast charge" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Enable fast boot
    And I bookmark the app log
    When I type "enable fast boot" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Experience Modes ────────────────────────────────────────────

  Scenario: Turn on gaming mode
    And I bookmark the app log
    When I type "turn on gaming mode" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Turn on movie mode
    And I bookmark the app log
    When I type "turn on movie mode" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Turn on conference mode
    And I bookmark the app log
    When I type "turn on conference mode" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Turn on music mode
    And I bookmark the app log
    When I type "turn on music mode" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── System Settings ─────────────────────────────────────────────

  Scenario: Enable dynamic lock
    And I bookmark the app log
    When I type "enable dynamic lock" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Disable toast notifications
    And I bookmark the app log
    When I type "disable toast notifications" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  # ── Audio Presets ───────────────────────────────────────────────

  Scenario: Set audio to music preset
    And I bookmark the app log
    When I type "set audio preset to music" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log

  Scenario: Set audio to movie preset
    And I bookmark the app log
    When I type "set audio preset to movie" into the chat input
    And I submit the message
    Then the agent should respond within 15 seconds
    And I print the intents and errors from the app log
