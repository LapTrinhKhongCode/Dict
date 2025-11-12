from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

class CreateDeckTest(unittest.TestCase):
    
    def setUp(self):
        # 1. Khởi tạo WebDriver
        self.driver = webdriver.Chrome() 
        self.driver.maximize_window()
        self.base_url = "http://localhost:3000"

    def _login_as_admin(self, driver, wait):
        """Hàm trợ giúp để đăng nhập vào tài khoản admin."""
        print("\nBắt đầu đăng nhập...")
        driver.get(self.base_url + "/login")

        heading_locator = (By.XPATH, "//h2[contains(text(), 'ĐĂNG NHẬP')]")
        wait.until(EC.visibility_of_element_located(heading_locator))
        
        # Chờ 1 giây CỐ ĐỊNH để Nuxt/Vue "hydrate"
        time.sleep(7)
        
        username_locator = (By.ID, "username") 
        password_locator = (By.ID, "password")
        
        username_field = wait.until(EC.element_to_be_clickable(username_locator))
        password_field = driver.find_element(*password_locator)

        username_field.send_keys('admin')
        password_field.send_keys('SuperPassword123!')
        
        # Click ra ngoài để trigger v-model
        driver.find_element(*heading_locator).click()
        time.sleep(0.2) # Chờ Vue cập nhật
        
        login_button_locator = (By.XPATH, "//button[normalize-space()='Login']")
        login_button = wait.until(EC.element_to_be_clickable(login_button_locator))
        login_button.click()
        
        # Chờ cho đến khi chuyển trang và thấy text của admin
        success_text_locator = (By.XPATH, "//*[contains(text(), 'admin !')]")
        wait.until(EC.visibility_of_element_located(success_text_locator))
        print("Đăng nhập thành công, đang ở trang chủ.")


    def test_successful_deck_creation(self):
        driver = self.driver
        wait = WebDriverWait(driver, 10) 
        
        # 1. Đăng nhập
        self._login_as_admin(driver, wait)
        
        # ⚠️ BƯỚC MỚI: Chuyển hướng đến trang Explore
        print("Đang chuyển hướng đến trang /explore...")
        driver.get(self.base_url + "/explore")
        
        # 2. Điều hướng đến trang Tạo bộ thẻ
        # (Nút này nằm trên trang /explore - là nút "+")
        # (Chúng ta giả định trang /explore cũng có nút "+" tương tự)
        create_deck_button_locator = (By.XPATH, "//button[normalize-space()='+']")
        
        print("Đang tìm nút '+' để tạo bộ thẻ trên trang /explore...")
        
        # Thêm chờ cho trang explore tải (ví dụ: chờ heading "Khám phá")
        explore_heading_locator = (By.XPATH, "//h2[contains(text(), 'Khám phá')]")
        wait.until(EC.visibility_of_element_located(explore_heading_locator))
        
        create_button = wait.until(EC.element_to_be_clickable(create_deck_button_locator))
        create_button.click()
        
        # 3. Chờ trang DeckCreator tải xong
        # (Đây là heading <h1> trên DeckCreator.vue)
        create_page_heading_locator = (By.XPATH, "//h1[normalize-space()='Tạo sổ tay mới']")
        wait.until(EC.visibility_of_element_located(create_page_heading_locator))
        print("Đã chuyển sang trang 'Tạo sổ tay mới'.")

        # Chờ hydration (nếu cần)
        time.sleep(1) 

        # 4. Điền form tạo bộ thẻ
        # (Đây là các trường input trên DeckCreator.vue)
        name_locator = (By.ID, "deckName")
        desc_locator = (By.ID, "deckDesc")

        # Tạo tên bộ thẻ duy nhất
        deck_name = f"Bộ thẻ Test {int(time.time())}"
        
        name_field = wait.until(EC.element_to_be_clickable(name_locator))
        desc_field = driver.find_element(*desc_locator)

        name_field.send_keys(deck_name)
        desc_field.send_keys("Đây là mô tả được tạo tự động bởi Selenium.")
        print(f"Đã điền tên bộ thẻ: {deck_name}")

        # ⚠️ BƯỚC 4.5: Thêm ít nhất một thẻ (theo yêu cầu)
        # (Để tránh hộp thoại confirm() mà Selenium không xử lý được)
        print("Đang thêm một thẻ mới...")
        front_text_locator = (By.CSS_SELECTOR, "input[placeholder='Ký tự *']")
        back_text_locator = (By.CSS_SELECTOR, "input[placeholder='Nghĩa *']")
        add_card_button_locator = (By.XPATH, "//button[normalize-space()='Thêm vào danh sách']")

        front_field = driver.find_element(*front_text_locator)
        back_field = driver.find_element(*back_text_locator)
        add_card_button = driver.find_element(*add_card_button_locator)

        front_field.send_keys("Thẻ Mặt Trước 1")
        back_field.send_keys("Thẻ Mặt Sau 1")
        
        # Click nút "Thêm vào danh sách"
        add_card_button.click()
        
        # Chờ 0.2 giây để Vue thêm thẻ vào DOM
        time.sleep(0.2) 
        
        # Xác minh thẻ đã xuất hiện trong danh sách
        card_in_list_locator = (By.XPATH, "//*[contains(@class, 'truncate') and contains(text(), 'Thẻ Mặt Trước 1')]")
        wait.until(EC.visibility_of_element_located(card_in_list_locator))
        print("Đã thêm thẻ vào danh sách thành công.")


        # 5. Gửi form
        # (Đây là nút submit trên DeckCreator.vue)
        submit_button_locator = (By.XPATH, "//button[normalize-space()='Tạo bộ thẻ']")
        
        # Click ra ngoài để Vue kịp update :disabled state
        driver.find_element(*create_page_heading_locator).click()
        time.sleep(0.2)

        submit_button = wait.until(EC.element_to_be_clickable(submit_button_locator))
        submit_button.click()

        # 6. Xác minh thành công
        # (Sau khi tạo, app.vue sẽ chuyển sang 'list' (CardListPage))
        # Chúng ta xác minh bằng cách tìm tên bộ thẻ mới (dưới dạng <h1>)
        # ⚠️ LƯU Ý: Bước này giả định CardListPage.vue hiển thị tên bộ thẻ trong <h1>
        print("Đang chờ chuyển sang trang danh sách thẻ...")
        deck_title_locator = (By.XPATH, f"//h1[normalize-space()='{deck_name}']")
        
        success_element = wait.until(EC.visibility_of_element_located(deck_title_locator))
        
        self.assertTrue(success_element.is_displayed(), "Tạo bộ thẻ không thành công, không thấy tên bộ thẻ mới.")
        print("Tạo bộ thẻ mới thành công!")


    def tearDown(self):
        # 🔑 Giữ trình duyệt mở để quan sát
        print("\n==================================================")
        print("Test đã hoàn thành/thất bại. Nhấn Enter để đóng trình duyệt.")
        input() 
        
        # Đóng trình duyệt
        self.driver.quit()

if __name__ == '__main__':
    unittest.main()