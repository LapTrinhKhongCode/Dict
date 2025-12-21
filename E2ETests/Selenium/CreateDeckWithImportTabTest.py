from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

# Import test data
from test_data import (
    BASE_URL, HYDRATION_WAIT_TIME, SHORT_WAIT_TIME, MEDIUM_WAIT_TIME, LONG_WAIT_TIME,
    LoginData, Locators, ImportDataTab
)

class CreateDeckWithImportTabTest(unittest.TestCase):
    """
    Test: Tạo bộ thẻ mới sử dụng chức năng Import với TAB làm phân cách.
    Flow:
    1. Đăng nhập
    2. Vào trang /explore, click "+" để tạo bộ thẻ
    3. Điền tên và mô tả bộ thẻ
    4. Click "Import" để mở modal
    5. Chọn "Tab" làm phân cách
    6. Nhập dữ liệu thẻ hợp lệ (phân cách bằng Tab)
    7. Click "Import X thẻ"
    8. Click "Tạo bộ thẻ"
    9. Xác minh tạo thành công
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


    def test_create_deck_with_import_tab_delimiter(self):
        """Test tạo bộ thẻ sử dụng Import với Tab làm phân cách."""
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
        time.sleep(1)  # Chờ hydration

        # 5. Điền thông tin bộ thẻ
        deck_name = f"Import Tab Test {int(time.time())}"
        
        name_field = wait.until(EC.element_to_be_clickable((By.ID, "deckName")))
        desc_field = driver.find_element(By.ID, "deckDesc")
        
        name_field.send_keys(deck_name)
        desc_field.send_keys("Bộ thẻ được tạo bằng chức năng Import (Tab).")
        print(f"Đã điền tên bộ thẻ: {deck_name}")

        # 6. Click nút "Import" để mở modal
        import_button_locator = (By.XPATH, "//button[contains(., 'Import')]")
        import_button = wait.until(EC.element_to_be_clickable(import_button_locator))
        import_button.click()
        print("Đã click nút 'Import', đang chờ modal...")

        # 7. Chờ modal Import xuất hiện
        modal_heading_locator = (By.XPATH, "//h2[contains(text(), 'Import dữ liệu thẻ')]")
        wait.until(EC.visibility_of_element_located(modal_heading_locator))
        print("Modal Import đã xuất hiện.")
        time.sleep(0.5)

        # 8. Chọn "Tab" làm phân cách
        tab_radio_locator = (By.XPATH, "//input[@type='radio' and @value='tab']")
        tab_radio = driver.find_element(*tab_radio_locator)
        tab_radio.click()
        print("Đã chọn 'Tab' làm phân cách.")
        time.sleep(0.3)

        # 9. Nhập dữ liệu thẻ hợp lệ vào textarea (phân cách bằng Tab)
        import_textarea_locator = (By.XPATH, "//textarea[@placeholder]")
        import_textarea = driver.find_element(*import_textarea_locator)
        
        # Dữ liệu import: mỗi dòng là "frontText\tbackText" (Tab phân cách)
        # Sử dụng \t để biểu diễn Tab character
        import_data = "apple\tquả táo\nbanana\tquả chuối\norange\tquả cam\ngrape\tquả nho\nwatermelon\tquả dưa hấu"
        
        import_textarea.clear()
        import_textarea.send_keys(import_data)
        print("Đã nhập dữ liệu import với Tab (5 thẻ).")
        time.sleep(0.5)  # Chờ Vue parse dữ liệu

        # 10. Xác minh preview hiển thị đúng số thẻ
        preview_heading_locator = (By.XPATH, "//h3[contains(text(), 'Xem trước')]")
        preview_heading = wait.until(EC.visibility_of_element_located(preview_heading_locator))
        preview_text = preview_heading.text
        print(f"Preview: {preview_text}")
        
        # Kiểm tra có "(5 thẻ)" trong heading
        self.assertIn("5 thẻ", preview_text, "Preview không hiển thị đúng số thẻ!")

        # 11. Click nút "Import X thẻ"
        import_submit_button_locator = (By.XPATH, "//button[contains(text(), 'Import') and contains(text(), 'thẻ')]")
        import_submit_button = wait.until(EC.element_to_be_clickable(import_submit_button_locator))
        import_submit_button.click()
        print("Đã click 'Import 5 thẻ'.")
        
        # 12. Chờ modal đóng và xác minh thẻ đã được thêm
        wait.until(EC.invisibility_of_element_located(modal_heading_locator))
        time.sleep(0.5)
        
        # Kiểm tra số thẻ trong danh sách
        card_count_heading_locator = (By.XPATH, "//h2[contains(text(), 'Thêm thẻ thủ công')]")
        card_count_heading = wait.until(EC.visibility_of_element_located(card_count_heading_locator))
        card_count_text = card_count_heading.text
        print(f"Số thẻ trong danh sách: {card_count_text}")
        
        self.assertIn("5", card_count_text, "Danh sách không có đủ 5 thẻ!")

        # 13. Click "Tạo bộ thẻ"
        submit_button_locator = (By.XPATH, "//button[normalize-space()='Tạo bộ thẻ']")
        
        driver.find_element(*create_page_heading_locator).click()
        time.sleep(0.2)

        submit_button = wait.until(EC.element_to_be_clickable(submit_button_locator))
        submit_button.click()
        print("Đã click 'Tạo bộ thẻ'.")

        # 14. Xác minh thành công
        print("Đang chờ chuyển sang trang danh sách thẻ...")
        deck_title_locator = (By.XPATH, f"//h1[normalize-space()='{deck_name}']")
        
        success_element = wait.until(EC.visibility_of_element_located(deck_title_locator))
        
        self.assertTrue(success_element.is_displayed(), "Tạo bộ thẻ không thành công!")
        print(f"✅ Tạo bộ thẻ '{deck_name}' với Import Tab thành công!")


    def tearDown(self):
        print("\n==================================================")
        print("Test đã hoàn thành. Nhấn Enter để đóng trình duyệt.")
        input() 
        self.driver.quit()

if __name__ == '__main__':
    unittest.main()
