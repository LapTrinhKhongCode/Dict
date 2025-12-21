from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

# Import test data
from test_data import (
    BASE_URL, HYDRATION_WAIT_TIME, SHORT_WAIT_TIME, MEDIUM_WAIT_TIME, LONG_WAIT_TIME,
    LoginData, Locators, ImportDataComma, ErrorMessages
)

class CreateDeckWithImportInvalidTest(unittest.TestCase):
    """
    Test: Thử import thẻ với định dạng không hợp lệ (thiếu dấu phẩy).
    Expected: Hiển thị thông báo lỗi hoặc không có thẻ nào được parse.
    """
    
    def setUp(self):
        self.driver = webdriver.Chrome() 
        self.driver.maximize_window()
        self.base_url = "http://localhost:3000"

    def _login_as_admin(self, driver, wait):
        """Hàm trợ giúp để đăng nhập vào tài khoản admin."""
        print("\nBắt đầu đăng nhập...")
        driver.get(self.base_url + "/login")

        heading_locator = (By.XPATH, "//h2[contains(text(), 'ĐĂNG NHẬP')]")
        wait.until(EC.visibility_of_element_located(heading_locator))
        
        time.sleep(7)  # Chờ Nuxt/Vue hydrate
        
        username_locator = (By.ID, "username") 
        password_locator = (By.ID, "password")
        
        username_field = wait.until(EC.element_to_be_clickable(username_locator))
        password_field = driver.find_element(*password_locator)

        username_field.send_keys('admin')
        password_field.send_keys('SuperPassword123!')
        
        driver.find_element(*heading_locator).click()
        time.sleep(0.2)
        
        login_button_locator = (By.XPATH, "//button[normalize-space()='Login']")
        login_button = wait.until(EC.element_to_be_clickable(login_button_locator))
        login_button.click()
        
        success_text_locator = (By.XPATH, "//*[contains(text(), 'admin !')]")
        wait.until(EC.visibility_of_element_located(success_text_locator))
        print("Đăng nhập thành công, đang ở trang chủ.")


    def test_import_with_invalid_format_no_comma(self):
        """Test import thẻ với dữ liệu không có dấu phẩy - expected lỗi."""
        driver = self.driver
        wait = WebDriverWait(driver, 10) 
        
        # 1. Đăng nhập
        self._login_as_admin(driver, wait)
        
        # 2. Chuyển hướng đến trang Explore
        print("Đang chuyển hướng đến trang /explore...")
        driver.get(self.base_url + "/explore")
        
        # 3. Chờ trang explore tải và click nút "+"
        explore_heading_locator = (By.XPATH, "//h2[contains(text(), 'Khám phá')]")
        wait.until(EC.visibility_of_element_located(explore_heading_locator))
        
        create_deck_button_locator = (By.XPATH, "//button[normalize-space()='+']")
        print("Đang tìm nút '+' để tạo bộ thẻ...")
        create_button = wait.until(EC.element_to_be_clickable(create_deck_button_locator))
        create_button.click()
        
        # 4. Chờ trang DeckCreator tải xong
        create_page_heading_locator = (By.XPATH, "//h1[normalize-space()='Tạo sổ tay mới']")
        wait.until(EC.visibility_of_element_located(create_page_heading_locator))
        print("Đã chuyển sang trang 'Tạo sổ tay mới'.")
        time.sleep(1)

        # 5. Click nút "Import" để mở modal
        import_button_locator = (By.XPATH, "//button[contains(., 'Import')]")
        import_button = wait.until(EC.element_to_be_clickable(import_button_locator))
        import_button.click()
        print("Đã click nút 'Import', đang chờ modal...")

        # 6. Chờ modal Import xuất hiện
        modal_heading_locator = (By.XPATH, "//h2[contains(text(), 'Import dữ liệu thẻ')]")
        wait.until(EC.visibility_of_element_located(modal_heading_locator))
        print("Modal Import đã xuất hiện.")
        time.sleep(0.5)

        # 7. Đảm bảo chọn "Dấu phẩy" làm phân cách
        comma_radio_locator = (By.XPATH, "//input[@type='radio' and @value='comma']")
        comma_radio = driver.find_element(*comma_radio_locator)
        if not comma_radio.is_selected():
            comma_radio.click()
        print("Đã chọn 'Dấu phẩy' làm phân cách.")

        # 8. Nhập dữ liệu KHÔNG HỢP LỆ (thiếu dấu phẩy)
        import_textarea_locator = (By.XPATH, "//textarea[@placeholder]")
        import_textarea = driver.find_element(*import_textarea_locator)
        
        # Dữ liệu import KHÔNG CÓ dấu phẩy - sẽ không parse được
        invalid_import_data = """apple quả táo
banana quả chuối
orange quả cam
grape quả nho
watermelon quả dưa hấu"""
        
        import_textarea.clear()
        import_textarea.send_keys(invalid_import_data)
        print("Đã nhập dữ liệu KHÔNG HỢP LỆ (không có dấu phẩy).")
        time.sleep(0.5)  # Chờ Vue parse dữ liệu

        # 9. Xác minh preview hiển thị 0 thẻ
        preview_heading_locator = (By.XPATH, "//h3[contains(text(), 'Xem trước')]")
        preview_heading = wait.until(EC.visibility_of_element_located(preview_heading_locator))
        preview_text = preview_heading.text
        print(f"Preview: {preview_text}")
        
        # Kiểm tra có "(0 thẻ)" trong heading - vì dữ liệu không hợp lệ
        self.assertIn("0 thẻ", preview_text, "Preview nên hiển thị 0 thẻ khi dữ liệu không hợp lệ!")
        print("✅ Xác minh: Preview hiển thị 0 thẻ (đúng như expected).")

        # 10. Click nút "Import 0 thẻ" và kiểm tra toast error
        import_submit_button_locator = (By.XPATH, "//button[contains(text(), 'Import') and contains(text(), 'thẻ')]")
        import_submit_button = wait.until(EC.element_to_be_clickable(import_submit_button_locator))
        import_submit_button.click()
        print("Đã click 'Import 0 thẻ'...")

        # 11. Chờ và xác minh toast error xuất hiện
        # Toast có class chứa "bg-red" hoặc text "Không có thẻ hợp lệ"
        toast_error_locator = (By.XPATH, "//*[contains(@class, 'toast') or contains(@class, 'bg-red') or contains(text(), 'Không có thẻ hợp lệ')]")
        
        try:
            toast_error = wait.until(EC.visibility_of_element_located(toast_error_locator))
            toast_text = toast_error.text
            print(f"Toast error hiển thị: {toast_text}")
            self.assertTrue(True, "Toast error đã hiển thị!")
        except:
            # Nếu không tìm thấy toast, kiểm tra modal vẫn còn mở
            modal_still_open = driver.find_element(*modal_heading_locator).is_displayed()
            self.assertTrue(modal_still_open, "Modal nên vẫn mở khi import thất bại!")
            print("✅ Modal vẫn mở (không import được do dữ liệu không hợp lệ).")

        print("\n✅ Test PASSED: Hệ thống xử lý đúng khi dữ liệu import không hợp lệ!")


    def tearDown(self):
        print("\n==================================================")
        print("Test đã hoàn thành. Nhấn Enter để đóng trình duyệt.")
        input() 
        self.driver.quit()

if __name__ == '__main__':
    unittest.main()
