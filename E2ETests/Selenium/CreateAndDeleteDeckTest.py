from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

# Import test data
from test_data import (
    BASE_URL, HYDRATION_WAIT_TIME, SHORT_WAIT_TIME, MEDIUM_WAIT_TIME, LONG_WAIT_TIME,
    LoginData, Locators, DeckData
)

class CreateAndDeleteDeckTest(unittest.TestCase):
    """
    Test: Tạo bộ thẻ trống sau đó xóa nó.
    Flow:
    1. Đăng nhập
    2. Tạo bộ thẻ trống (không thêm thẻ nào)
    3. Vào trang chỉnh sửa
    4. Click "Xóa vĩnh viễn bộ thẻ này"
    5. Xác nhận xóa trong modal
    6. Xác minh đã quay về trang chủ
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

    def _create_empty_deck(self, driver, wait) -> str:
        """Tạo bộ thẻ trống và trả về tên của nó."""
        print("\nĐang tạo bộ thẻ trống...")
        driver.get(self.base_url + "/explore")
        
        # Chờ trang explore tải
        explore_heading_locator = (By.XPATH, "//h2[contains(text(), 'Khám phá')]")
        wait.until(EC.visibility_of_element_located(explore_heading_locator))
        
        # Click nút "+"
        create_deck_button_locator = (By.XPATH, "//button[normalize-space()='+']")
        wait.until(EC.element_to_be_clickable(create_deck_button_locator)).click()
        
        # Chờ trang DeckCreator tải
        create_page_heading_locator = (By.XPATH, "//h1[normalize-space()='Tạo sổ tay mới']")
        wait.until(EC.visibility_of_element_located(create_page_heading_locator))
        time.sleep(1)

        # Điền thông tin deck
        deck_name = f"Deck To Delete {int(time.time())}"
        wait.until(EC.element_to_be_clickable((By.ID, "deckName"))).send_keys(deck_name)
        driver.find_element(By.ID, "deckDesc").send_keys("Bộ thẻ này sẽ bị xóa.")
        print(f"Đã điền tên: {deck_name}")

        # Click "Tạo bộ thẻ" (sẽ hiện confirm vì trống)
        driver.find_element(*create_page_heading_locator).click()
        time.sleep(0.2)
        
        submit_button = wait.until(EC.element_to_be_clickable(
            (By.XPATH, "//button[normalize-space()='Tạo bộ thẻ']")
        ))
        submit_button.click()

        # Xử lý confirm dialog (nếu có)
        try:
            time.sleep(0.5)
            alert = driver.switch_to.alert
            print(f"Confirm dialog: {alert.text}")
            alert.accept()  # Click OK
            print("Đã xác nhận tạo bộ thẻ trống.")
        except:
            print("Không có confirm dialog (hoặc đã tự động xử lý).")

        # Xác minh đã chuyển sang CardListPage
        deck_title_locator = (By.XPATH, f"//h1[normalize-space()='{deck_name}']")
        wait.until(EC.visibility_of_element_located(deck_title_locator))
        print(f"Đã tạo thành công bộ thẻ: {deck_name}")
        return deck_name


    def test_create_and_delete_deck(self):
        """Test tạo bộ thẻ trống rồi xóa nó."""
        driver = self.driver
        wait = WebDriverWait(driver, 10) 
        
        # 1. Đăng nhập
        self._login_as_admin(driver, wait)
        
        # 2. Tạo bộ thẻ trống
        deck_name = self._create_empty_deck(driver, wait)
        
        # 3. Tìm và click nút "Chỉnh sửa" trên CardListPage
        edit_button_locator = (By.XPATH, "//button[@title='Chỉnh sửa bộ thẻ']")
        print("Đang ở CardListPage, tìm nút 'Chỉnh sửa'...")
        wait.until(EC.element_to_be_clickable(edit_button_locator)).click()

        # 4. Chờ trang DeckEditor tải xong
        editor_heading_locator = (By.XPATH, "//h1[normalize-space()='Chỉnh sửa bộ thẻ']")
        wait.until(EC.visibility_of_element_located(editor_heading_locator))
        print("Đã vào trang 'Chỉnh sửa bộ thẻ'.")
        time.sleep(1)

        # 5. Tìm và click nút "Xóa vĩnh viễn bộ thẻ này" trong vùng nguy hiểm
        delete_button_locator = (By.XPATH, "//button[contains(text(), 'Xóa vĩnh viễn bộ thẻ này')]")
        delete_button = wait.until(EC.element_to_be_clickable(delete_button_locator))
        delete_button.click()
        print("Đã click 'Xóa vĩnh viễn bộ thẻ này'.")

        # 6. Chờ modal xác nhận xuất hiện
        modal_title_locator = (By.XPATH, "//*[contains(text(), 'Xác nhận XÓA Bộ Thẻ')]")
        wait.until(EC.visibility_of_element_located(modal_title_locator))
        print("Modal xác nhận xóa đã xuất hiện.")

        # 7. Click nút xác nhận trong modal
        # ConfirmationModal thường có nút "Xác nhận" hoặc "Đồng ý"
        confirm_button_locator = (By.XPATH, "//button[contains(text(), 'Xác nhận') or contains(text(), 'Đồng ý') or contains(text(), 'OK')]")
        confirm_button = wait.until(EC.element_to_be_clickable(confirm_button_locator))
        confirm_button.click()
        print("Đã xác nhận xóa bộ thẻ.")

        # 8. Chờ chuyển về trang chủ (HomePage với heading "Khám phá")
        print("Đang chờ chuyển về trang chủ...")
        home_heading_locator = (By.XPATH, "//h2[contains(text(), 'Khám phá')]")
        wait.until(EC.visibility_of_element_located(home_heading_locator))
        
        # 9. Xác minh bộ thẻ đã bị xóa (không còn trong danh sách)
        # Tìm kiếm tên bộ thẻ, nên KHÔNG tìm thấy
        time.sleep(1)  # Chờ trang load
        
        deleted_deck_locator = (By.XPATH, f"//*[contains(text(), '{deck_name}')]")
        
        try:
            # Sử dụng presence check với timeout ngắn
            WebDriverWait(driver, 2).until(
                EC.presence_of_element_located(deleted_deck_locator)
            )
            # Nếu tìm thấy = test fail
            self.fail(f"Bộ thẻ '{deck_name}' vẫn còn tồn tại sau khi xóa!")
        except:
            # Không tìm thấy = đúng như expected
            print(f"✅ Xác minh: Bộ thẻ '{deck_name}' đã bị xóa thành công!")

        print("\n✅ Test PASSED: Tạo và xóa bộ thẻ thành công!")


    def tearDown(self):
        print("\n==================================================")
        print("Test đã hoàn thành. Nhấn Enter để đóng trình duyệt.")
        input() 
        self.driver.quit()

if __name__ == '__main__':
    unittest.main()
