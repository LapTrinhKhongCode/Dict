"""
test_data.py - Centralized test data for Selenium E2E tests.
All test scripts should import constants from this file.
"""

# =============================================================================
# BASE CONFIGURATION
# =============================================================================
BASE_URL = "http://localhost:3000"
HYDRATION_WAIT_TIME = 7  # Seconds to wait for Nuxt/Vue hydration
SHORT_WAIT_TIME = 0.2    # Short wait for Vue re-render
MEDIUM_WAIT_TIME = 0.5   # Medium wait
LONG_WAIT_TIME = 1       # Longer wait for page transitions


# =============================================================================
# LOGIN CREDENTIALS
# =============================================================================
class LoginData:
    """Valid and invalid login credentials."""
    
    # Valid admin credentials
    ADMIN_USERNAME = "admin"
    ADMIN_PASSWORD = "SuperPassword123!"
    
    # Invalid credentials for negative tests
    WRONG_PASSWORD = "SuperPassword123!!"  # Extra '!' at the end
    EMPTY_PASSWORD = ""
    
    # Expected success text after login
    LOGIN_SUCCESS_TEXT = "admin !"


# =============================================================================
# DECK DATA
# =============================================================================
class DeckData:
    """Data for deck creation and editing tests."""
    
    # Create deck test
    DECK_NAME_PREFIX = "Bộ thẻ Test"
    DECK_DESCRIPTION = "Đây là mô tả được tạo tự động bởi Selenium."
    
    # Edit deck test
    EDITED_DECK_NAME_PREFIX = "ĐÃ SỬA"
    EDITED_DECK_DESCRIPTION_SUFFIX = " (đã chỉnh sửa)."
    
    # Delete deck test
    DELETE_DECK_NAME_PREFIX = "Deck To Delete"
    DELETE_DECK_DESCRIPTION = "Bộ thẻ này sẽ bị xóa."


# =============================================================================
# CARD DATA
# =============================================================================
class CardData:
    """Data for flashcard tests."""
    
    # Manual card entry
    CARD_FRONT_1 = "Thẻ Mặt Trước 1"
    CARD_BACK_1 = "Thẻ Mặt Sau 1"
    
    # Edit deck - new card
    NEW_CARD_FRONT = "Thẻ Mới 2"
    NEW_CARD_BACK = "Nghĩa của Thẻ Mới 2"
    NEW_CARD_TAGS = "pinyin2"
    
    # Create deck helper
    HELPER_CARD_FRONT = "Thẻ 1"
    HELPER_CARD_BACK = "Nghĩa 1"


# =============================================================================
# IMPORT DATA - COMMA DELIMITER
# =============================================================================
class ImportDataComma:
    """Data for import with comma delimiter."""
    
    # Valid import data (comma separated)
    VALID_DATA = """apple, quả táo
banana, quả chuối
orange, quả cam
grape, quả nho
watermelon, quả dưa hấu"""
    
    VALID_CARD_COUNT = 5
    
    # Invalid import data (no comma - should fail)
    INVALID_DATA = """apple quả táo
banana quả chuối
orange quả cam
grape quả nho
watermelon quả dưa hấu"""
    
    INVALID_CARD_COUNT = 0
    
    # Deck info for import tests
    DECK_NAME_PREFIX = "Import Test"
    DECK_DESCRIPTION = "Bộ thẻ được tạo bằng chức năng Import (dấu phẩy)."


# =============================================================================
# IMPORT DATA - TAB DELIMITER
# =============================================================================
class ImportDataTab:
    """Data for import with tab delimiter."""
    
    # Valid import data (tab separated)
    VALID_DATA = "apple\tquả táo\nbanana\tquả chuối\norange\tquả cam\ngrape\tquả nho\nwatermelon\tquả dưa hấu"
    
    VALID_CARD_COUNT = 5
    
    # Invalid import data (spaces instead of tabs - should fail)
    INVALID_DATA = """apple quả táo
banana quả chuối
orange quả cam
grape quả nho
watermelon quả dưa hấu"""
    
    INVALID_CARD_COUNT = 0
    
    # Deck info for import tests
    DECK_NAME_PREFIX = "Import Tab Test"
    DECK_DESCRIPTION = "Bộ thẻ được tạo bằng chức năng Import (Tab)."


# =============================================================================
# UI LOCATORS (XPath, CSS selectors)
# =============================================================================
class Locators:
    """Common UI element locators."""
    
    # Login page
    LOGIN_HEADING = "//h2[contains(text(), 'ĐĂNG NHẬP')]"
    USERNAME_INPUT = "username"  # ID
    PASSWORD_INPUT = "password"  # ID
    LOGIN_BUTTON = "//button[normalize-space()='Login']"
    ERROR_MESSAGE = ".text-red-600"  # CSS
    
    # Explore page
    EXPLORE_HEADING = "//h2[contains(text(), 'Khám phá')]"
    CREATE_DECK_BUTTON = "//button[normalize-space()='+']"
    
    # Deck Creator page
    DECK_CREATOR_HEADING = "//h1[normalize-space()='Tạo sổ tay mới']"
    DECK_NAME_INPUT = "deckName"  # ID
    DECK_DESC_INPUT = "deckDesc"  # ID
    CARD_FRONT_INPUT = "input[placeholder='Ký tự *']"  # CSS
    CARD_BACK_INPUT = "input[placeholder='Nghĩa *']"  # CSS
    ADD_CARD_BUTTON = "//button[normalize-space()='Thêm vào danh sách']"
    CREATE_DECK_SUBMIT = "//button[normalize-space()='Tạo bộ thẻ']"
    CARD_COUNT_HEADING = "//h2[contains(text(), 'Thêm thẻ thủ công')]"
    
    # Import modal
    IMPORT_BUTTON = "//button[contains(., 'Import')]"
    IMPORT_MODAL_HEADING = "//h2[contains(text(), 'Import dữ liệu thẻ')]"
    COMMA_RADIO = "//input[@type='radio' and @value='comma']"
    TAB_RADIO = "//input[@type='radio' and @value='tab']"
    IMPORT_TEXTAREA = "//textarea[@placeholder]"
    PREVIEW_HEADING = "//h3[contains(text(), 'Xem trước')]"
    IMPORT_SUBMIT = "//button[contains(text(), 'Import') and contains(text(), 'thẻ')]"
    
    # Deck Editor page
    DECK_EDITOR_HEADING = "//h1[normalize-space()='Chỉnh sửa bộ thẻ']"
    EDIT_CARD_FRONT_INPUT = "input[placeholder='Mặt trước (Ký tự *)']"  # CSS
    EDIT_CARD_BACK_INPUT = "input[placeholder='Mặt sau (Nghĩa *)']"  # CSS
    EDIT_CARD_TAGS_INPUT = "input[placeholder='Tags (Pinyin, optional)']"  # CSS
    SAVE_ALL_BUTTON = "//button[normalize-space()='Lưu tất cả thay đổi']"
    DELETE_DECK_BUTTON = "//button[contains(text(), 'Xóa vĩnh viễn bộ thẻ này')]"
    DELETE_CONFIRM_MODAL = "//*[contains(text(), 'Xác nhận XÓA Bộ Thẻ')]"
    CONFIRM_BUTTON = "//button[contains(text(), 'Xác nhận') or contains(text(), 'Đồng ý') or contains(text(), 'OK')]"
    
    # Card List page
    EDIT_DECK_ICON = "//button[@title='Chỉnh sửa bộ thẻ']"


# =============================================================================
# ERROR MESSAGES
# =============================================================================
class ErrorMessages:
    """Expected error messages for validation."""
    
    IMPORT_NO_VALID_CARDS = "Không có thẻ hợp lệ nào để import."
