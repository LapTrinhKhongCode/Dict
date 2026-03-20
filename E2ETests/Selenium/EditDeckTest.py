from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

# Import test data
from test_data import (
    BASE_URL, HYDRATION_WAIT_TIME, SHORT_WAIT_TIME, MEDIUM_WAIT_TIME, LONG_WAIT_TIME,
    LoginData, Locators, DeckData, CardData
)

class EditDeckTest(unittest.TestCase):
    
    def setUp(self):
        # 1. Khởi tạo WebDriver
        self.driver = webdriver.Chrome() 
        self.driver.maximize_window()
        self.base_url = "http://localhost:3000"

    # --- HÀM HỖ TRỢ 1: ĐĂNG NHẬP ---
    def _login_as_admin(self, driver, wait):
        """Hàm trợ giúp để đăng nhập vào tài khoản admin."""
        print("\nBắt đầu đăng nhập...")
        driver.get(self.base_url + "/login")

        heading_locator = (By.XPATH, "//h2[contains(text(), 'ĐĂNG NHẬP')]")
        wait.until(EC.visibility_of_element_located(heading_locator))
        time.sleep(6) # Chờ hydrate
        
        username_field = wait.until(EC.element_to_be_clickable((By.ID, "username")))
        password_field = driver.find_element(By.ID, "password")
        username_field.send_keys('admin')
        password_field.send_keys('SuperPassword123!')
        
        driver.find_element(*heading_locator).click()
        time.sleep(0.2)
        
        login_button = wait.until(EC.element_to_be_clickable((By.XPATH, "//button[normalize-space()='Login']")))
        login_button.click()
        
        wait.until(EC.visibility_of_element_located((By.XPATH, "//*[contains(text(), 'admin !')]")))
        print("Đăng nhập thành công.")

    # --- HÀM HỖ TRỢ 2: TẠO BỘ THẺ ---
    def _create_new_deck(self, driver, wait) -> str:
        """Hàm trợ giúp tạo một bộ thẻ mới và trả về tên của nó.
           Giả định rằng đã đăng nhập thành công.
        """
        print("Bắt đầu tạo bộ thẻ (điều kiện tiên quyết)...")
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
        time.sleep(1) # Chờ hydrate

        # Điền thông tin deck
        deck_name = f"Bộ thẻ Test {int(time.time())}"
        wait.until(EC.element_to_be_clickable((By.ID, "deckName"))).send_keys(deck_name)
        driver.find_element(By.ID, "deckDesc").send_keys("Mô tả test.")
        
        # Thêm 1 thẻ
        driver.find_element(By.CSS_SELECTOR, "input[placeholder='Ký tự *']").send_keys("Thẻ 1")
        driver.find_element(By.CSS_SELECTOR, "input[placeholder='Nghĩa *']").send_keys("Nghĩa 1")
        driver.find_element(By.XPATH, "//button[normalize-space()='Thêm vào danh sách']").click()
        time.sleep(0.2)
        
        # Click "Tạo bộ thẻ"
        driver.find_element(*create_page_heading_locator).click() # Click ra ngoài
        time.sleep(0.2)
        submit_button = wait.until(EC.element_to_be_clickable((By.XPATH, "//button[normalize-space()='Tạo bộ thẻ']")))
        submit_button.click()

        # Xác minh đã chuyển sang trang CardListPage
        deck_title_locator = (By.XPATH, f"//h1[normalize-space()='{deck_name}']")
        wait.until(EC.visibility_of_element_located(deck_title_locator))
        print(f"Đã tạo thành công bộ thẻ: {deck_name}")
        return deck_name

    # --- BÀI TEST CHÍNH: CHỈNH SỬA BỘ THẺ ---
    def test_edit_deck_and_add_card(self):
        driver = self.driver
        wait = WebDriverWait(driver, 10) 
        
        # 1. Đăng nhập
        self._login_as_admin(driver, wait)
        
        # 2. Tạo một bộ thẻ mới để làm dữ liệu test
        original_deck_name = self._create_new_deck(driver, wait)
        
        # 3. Tìm và click nút "Chỉnh sửa" trên CardListPage
        # (Đây là nút icon có title="Chỉnh sửa bộ thẻ")
        edit_button_locator = (By.XPATH, "//button[@title='Chỉnh sửa bộ thẻ']")
        print("Đang ở CardListPage, tìm nút icon 'Chỉnh sửa'...")
        wait.until(EC.element_to_be_clickable(edit_button_locator)).click()

        # 4. Chờ trang DeckEditor tải xong
        editor_heading_locator = (By.XPATH, "//h1[normalize-space()='Chỉnh sửa bộ thẻ']")
        wait.until(EC.visibility_of_element_located(editor_heading_locator))
        print("Đã vào trang 'Chỉnh sửa bộ thẻ'.")
        time.sleep(1) # Chờ DeckEditor hydrate

        # 5. Chỉnh sửa thông tin bộ thẻ
        new_deck_name = f"ĐÃ SỬA {int(time.time())}"
        name_field_locator = (By.ID, "deckName")
        
        name_field = wait.until(EC.element_to_be_clickable(name_field_locator))
        name_field.clear()
        name_field.send_keys(new_deck_name)
        
        driver.find_element(By.ID, "deckDesc").send_keys(" (đã chỉnh sửa).")
        print(f"Đã đổi tên bộ thẻ thành: {new_deck_name}")

        # 6. Thêm một thẻ mới (trong trình editor)
        print("Đang thêm một thẻ mới (Thẻ Mới 2)...")
        driver.find_element(By.CSS_SELECTOR, "input[placeholder='Mặt trước (Ký tự *)']").send_keys("Thẻ Mới 2")
        driver.find_element(By.CSS_SELECTOR, "input[placeholder='Mặt sau (Nghĩa *)']").send_keys("Nghĩa của Thẻ Mới 2")
        driver.find_element(By.CSS_SELECTOR, "input[placeholder='Tags (Pinyin, optional)']").send_keys("pinyin2")
        
        # Click "Thêm vào danh sách"
        driver.find_element(By.XPATH, "//button[normalize-space()='Thêm vào danh sách']").click()
        time.sleep(0.2)
        print("Đã thêm 'Thẻ Mới 2' vào danh sách chờ.")

        # 7. Lưu tất cả thay đổi
        save_button_locator = (By.XPATH, "//button[normalize-space()='Lưu tất cả thay đổi']")
        driver.find_element(*editor_heading_locator).click() # Click ra ngoài
        time.sleep(0.2) # Chờ Vue cập nhật
        
        wait.until(EC.element_to_be_clickable(save_button_locator)).click()
        print("Đã click 'Lưu tất cả thay đổi'.")

        # 8. Xác minh
        # (Sau khi lưu, app.vue sẽ quay lại CardListPage)
        print("Đang chờ quay lại CardListPage và xác minh tên mới...")
        final_title_locator = (By.XPATH, f"//h1[normalize-space()='{new_deck_name}']")
        
        success_element = wait.until(EC.visibility_of_element_located(final_title_locator))
        
        self.assertTrue(success_element.is_displayed(), "Chỉnh sửa bộ thẻ thất bại, không thấy tên mới.")
        print("Chỉnh sửa bộ thẻ và thêm thẻ mới thành công!")


    def tearDown(self):
        # 🔑 Giữ trình duyệt mở để quan sát
        print("\n==================================================")
        print("Test đã hoàn thành/thất bại. Nhấn Enter để đóng trình duyệt.")
        input() 
        
        # Đóng trình duyệt
        self.driver.quit()

if __name__ == '__main__':
    unittest.main()