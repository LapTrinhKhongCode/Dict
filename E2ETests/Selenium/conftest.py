"""
conftest.py - Pytest configuration for Selenium E2E tests
Patches input() to allow unattended test execution.
"""

import pytest
import builtins

# Store original input
_original_input = builtins.input

def mock_input(prompt=""):
    """Mock input() to skip user prompts during automated test runs."""
    print(f"[AUTO] Skipping input prompt: {prompt}")
    return ""

# Apply mock before tests run
builtins.input = mock_input


def pytest_configure(config):
    """Configure pytest with custom markers."""
    config.addinivalue_line("markers", "login: Login related tests")
    config.addinivalue_line("markers", "deck: Deck CRUD tests")
    config.addinivalue_line("markers", "import_feature: Import feature tests")


def pytest_collection_modifyitems(config, items):
    """Order tests in a specific sequence."""
    # Define the desired test order
    test_order = [
        'test_successful_admin_login',
        'test_login_wrong_password', 
        'test_login_missing_password',
        'test_successful_deck_creation',
        'test_edit_deck_and_add_card',
        'test_create_deck_with_import_comma_delimiter',
        'test_import_with_invalid_format_no_comma',
        'test_create_deck_with_import_tab_delimiter',
        'test_import_tab_with_invalid_format_no_tab',
        'test_create_and_delete_deck',
    ]
    
    def get_order(item):
        test_name = item.name
        try:
            return test_order.index(test_name)
        except ValueError:
            return len(test_order)  # Unknown tests go last
    
    items.sort(key=get_order)


@pytest.hookimpl(tryfirst=True)
def pytest_html_report_title(report):
    """Set custom HTML report title."""
    report.title = "Selenium E2E Test Report - Dict Application"
