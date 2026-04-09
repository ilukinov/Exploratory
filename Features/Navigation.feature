Feature: Left Navigation
  As a user of HP AI Companion
  I want to click each section in the left menu
  So that I can access different parts of the application

  Background:
    Given HP AI Companion is launched

  Scenario Outline: Navigating to a section loads the correct page
    When I navigate to the "<section>" section
    Then the page title should be "<section>"

    Examples:
      | section   |
      | Home      |
      | Library   |
      | Perform   |
      | Spotlight |
      | Help      |
