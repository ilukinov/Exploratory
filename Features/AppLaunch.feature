Feature: Application Launch
  As a user of HP AI Companion
  I want the application to start successfully
  So that I can interact with the AI assistant

  Scenario: Main window appears after launch
    Given HP AI Companion is not already running
    When I launch HP AI Companion
    Then the window title should contain "AI Companion"
    And the window should be visible on screen
    And the Home page should load within 60 seconds
