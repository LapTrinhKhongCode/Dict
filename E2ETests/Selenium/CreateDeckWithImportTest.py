from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

# Import test data
from test_data import (
    BASE_URL, HYDRATION_WAIT_TIME, SHORT_WAIT_TIME, MEDIUM_WAIT_TIME, LONG_WAIT_TIME,
    LoginData, Locators, ImportDataComma
)

class CreateDeckWithImportTest(unittest.TestCase):
    """
    Test: Tạo bộ thẻ mới sử dụng chức năng Import với dấu phẩy làm phân cách.
    Flow:
    1. Đăng nhập
    2. Vào trang /explore, click "+" để tạo bộ thẻ
    3. Điền tên và mô tả bộ thẻ
    4. Click "Import" để mở modal
    5. Chọn "Dấu phẩy" làm phân cách
    6. Nhập dữ liệu thẻ hợp lệ
    7. Click "Import X thẻ"
    8. Click "Tạo bộ thẻ"
    9. Xác minh tạo thành công
    """
    
    def setUp(self):
        self.driver = webdriver.Chrome() 
        self.driver.maximize_window()
        self.base_url = BASE_URL

    def _login_as_admin(self, driver, wait):
        """Hàm trợ giúp để đăng nhập vào tài khoản admin."""
        print("\nBắt đầu đăng nhập...")
        driver.get(self.base_url + "/login")

        heading_locator = (By.XPATH, Locators.LOGIN_HEADING)
        wait.until(EC.visibility_of_element_located(heading_locator))
        
        time.sleep(HYDRATION_WAIT_TIME)
        
        username_locator = (By.ID, Locators.USERNAME_INPUT) 
        password_locator = (By.ID, Locators.PASSWORD_INPUT)
        
        username_field = wait.until(EC.element_to_be_clickable(username_locator))
        password_field = driver.find_element(*password_locator)

        username_field.send_keys(LoginData.ADMIN_USERNAME)
        password_field.send_keys(LoginData.ADMIN_PASSWORD)
        
        driver.find_element(*heading_locator).click()
        time.sleep(SHORT_WAIT_TIME)
        
        login_button_locator = (By.XPATH, Locators.LOGIN_BUTTON)
        login_button = wait.until(EC.element_to_be_clickable(login_button_locator))
        login_button.click()
        
        success_text_locator = (By.XPATH, f"//*[contains(text(), '{LoginData.LOGIN_SUCCESS_TEXT}')]")
        wait.until(EC.visibility_of_element_located(success_text_locator))
        print("Đăng nhập thành công, đang ở trang chủ.")


    def test_create_deck_with_import_comma_delimiter(self):
        """Test tạo bộ thẻ sử dụng Import với dấu phẩy."""
        driver = self.driver
        wait = WebDriverWait(driver, 10) 
        
        self._login_as_admin(driver, wait)
        
        print("Đang chuyển hướng đến trang /explore...")
        driver.get(self.base_url + "/explore")
        
        explore_heading_locator = (By.XPATH, Locators.EXPLORE_HEADING)
        wait.until(EC.visibility_of_element_located(explore_heading_locator))
        
        create_deck_button_locator = (By.XPATH, Locators.CREATE_DECK_BUTTON)
        print("Đang tìm nút '+' để tạo bộ thẻ...")
        create_button = wait.until(EC.element_to_be_clickable(create_deck_button_locator))
        create_button.click()
        
        create_page_heading_locator = (By.XPATH, Locators.DECK_CREATOR_HEADING)
        wait.until(EC.visibility_of_element_located(create_page_heading_locator))
        print("Đã chuyển sang trang 'Tạo sổ tay mới'.")
        time.sleep(LONG_WAIT_TIME)

        # Điền thông tin bộ thẻ
        deck_name = f"{ImportDataComma.DECK_NAME_PREFIX} {int(time.time())}"
        
        name_field = wait.until(EC.element_to_be_clickable((By.ID, Locators.DECK_NAME_INPUT)))
        desc_field = driver.find_element(By.ID, Locators.DECK_DESC_INPUT)
        
        name_field.send_keys(deck_name)
        desc_field.send_keys(ImportDataComma.DECK_DESCRIPTION)
        print(f"Đã điền tên bộ thẻ: {deck_name}")

        # Click nút "Import" để mở modal
        import_button_locator = (By.XPATH, Locators.IMPORT_BUTTON)
        import_button = wait.until(EC.element_to_be_clickable(import_button_locator))
        import_button.click()
        print("Đã click nút 'Import', đang chờ modal...")

        # Chờ modal Import xuất hiện
        modal_heading_locator = (By.XPATH, Locators.IMPORT_MODAL_HEADING)
        wait.until(EC.visibility_of_element_located(modal_heading_locator))
        print("Modal Import đã xuất hiện.")
        time.sleep(MEDIUM_WAIT_TIME)

        # Chọn "Dấu phẩy" làm phân cách
        comma_radio_locator = (By.XPATH, Locators.COMMA_RADIO)
        comma_radio = driver.find_element(*comma_radio_locator)
        
        if not comma_radio.is_selected():
            comma_radio.click()
            print("Đã chọn 'Dấu phẩy' làm phân cách.")
        else:
            print("'Dấu phẩy' đã được chọn sẵn.")

        # Nhập dữ liệu thẻ hợp lệ
        import_textarea_locator = (By.XPATH, Locators.IMPORT_TEXTAREA)
        import_textarea = driver.find_element(*import_textarea_locator)
        
        import_textarea.clear()
        import_textarea.send_keys(ImportDataComma.VALID_DATA)
        print(f"Đã nhập dữ liệu import ({ImportDataComma.VALID_CARD_COUNT} thẻ).")
        time.sleep(MEDIUM_WAIT_TIME)

        # Xác minh preview
        preview_heading_locator = (By.XPATH, Locators.PREVIEW_HEADING)
        preview_heading = wait.until(EC.visibility_of_element_located(preview_heading_locator))
        preview_text = preview_heading.text
        print(f"Preview: {preview_text}")
        
        self.assertIn(f"{ImportDataComma.VALID_CARD_COUNT} thẻ", preview_text, "Preview không hiển thị đúng số thẻ!")

        # Click nút "Import X thẻ"
        import_submit_button_locator = (By.XPATH, Locators.IMPORT_SUBMIT)
        import_submit_button = wait.until(EC.element_to_be_clickable(import_submit_button_locator))
        import_submit_button.click()
        print(f"Đã click 'Import {ImportDataComma.VALID_CARD_COUNT} thẻ'.")
        
        wait.until(EC.invisibility_of_element_located(modal_heading_locator))
        time.sleep(MEDIUM_WAIT_TIME)
        
        # Kiểm tra số thẻ trong danh sách
        card_count_heading_locator = (By.XPATH, Locators.CARD_COUNT_HEADING)
        card_count_heading = wait.until(EC.visibility_of_element_located(card_count_heading_locator))
        card_count_text = card_count_heading.text
        print(f"Số thẻ trong danh sách: {card_count_text}")
        
        self.assertIn(str(ImportDataComma.VALID_CARD_COUNT), card_count_text, "Danh sách không có đủ thẻ!")

        # Click "Tạo bộ thẻ"
        submit_button_locator = (By.XPATH, Locators.CREATE_DECK_SUBMIT)
        
        driver.find_element(*create_page_heading_locator).click()
        time.sleep(SHORT_WAIT_TIME)

        submit_button = wait.until(EC.element_to_be_clickable(submit_button_locator))
        submit_button.click()
        print("Đã click 'Tạo bộ thẻ'.")

        # Xác minh thành công
        print("Đang chờ chuyển sang trang danh sách thẻ...")
        deck_title_locator = (By.XPATH, f"//h1[normalize-space()='{deck_name}']")
        
        success_element = wait.until(EC.visibility_of_element_located(deck_title_locator))
        
        self.assertTrue(success_element.is_displayed(), "Tạo bộ thẻ không thành công!")
        print(f"✅ Tạo bộ thẻ '{deck_name}' với Import thành công!")


    def tearDown(self):
        print("\n==================================================")
        print("Test đã hoàn thành. Nhấn Enter để đóng trình duyệt.")
        input() 
        self.driver.quit()

if __name__ == '__main__':
    unittest.main()
