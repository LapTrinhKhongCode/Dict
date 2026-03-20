from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

# Import test data
from test_data import BASE_URL, HYDRATION_WAIT_TIME, SHORT_WAIT_TIME, LoginData, Locators

class AdminLoginTest(unittest.TestCase):
    
    def setUp(self):
        # 1. Khởi tạo WebDriver
        self.driver = webdriver.Chrome() 
        self.driver.maximize_window()
        self.base_url = BASE_URL

    def test_successful_admin_login(self):
        driver = self.driver
        # Thiết lập thời gian chờ tối đa 10 giây
        wait = WebDriverWait(driver, 10) 
        
        # 1. Mở trang login
        driver.get(self.base_url + "/login")

        # 2. Chờ heading login hiển thị (để xác nhận trang đã tải xong)
        heading_locator = (By.XPATH, Locators.LOGIN_HEADING)
        wait.until(EC.visibility_of_element_located(heading_locator))
        
        # Chờ Nuxt/Vue hydration
        print("\nĐang chờ Nuxt/Vue hydration...")
        time.sleep(HYDRATION_WAIT_TIME)
        print("Bắt đầu điền form...")
        
        # 3. Điền form
        username_locator = (By.ID, Locators.USERNAME_INPUT) 
        
        # Bây giờ mới tìm element (sau khi đã hydrate)
        username_field = wait.until(EC.element_to_be_clickable(username_locator))
        
        password_locator = (By.ID, Locators.PASSWORD_INPUT)
        password_field = driver.find_element(*password_locator)

        # Điền thông tin từ test data
        username_field.send_keys(LoginData.ADMIN_USERNAME)
        password_field.send_keys(LoginData.ADMIN_PASSWORD)
        
        # Click ra ngoài để trigger blur event
        driver.find_element(*heading_locator).click()
        time.sleep(SHORT_WAIT_TIME)
        
        # 4. Đợi nút login được enable
        login_button_locator = (By.XPATH, Locators.LOGIN_BUTTON)
        login_button = wait.until(EC.element_to_be_clickable(login_button_locator))
        
        # 5. Click login
        login_button.click()
        
        # 6. Kiểm tra login thành công
        success_text_locator = (By.XPATH, f"//*[contains(text(), '{LoginData.LOGIN_SUCCESS_TEXT}')]")
        success_text_element = wait.until(EC.visibility_of_element_located(success_text_locator))
        
        self.assertTrue(success_text_element.is_displayed(), "Đăng nhập không thành công, text 'admin !' không hiển thị.")

    def tearDown(self):
        # 🔑 Giữ trình duyệt mở để quan sát
        print("\n==================================================")
        print("Test đã hoàn thành/thất bại. Nhấn Enter để đóng trình duyệt.")
        input() 
        
        # Đóng trình duyệt
        self.driver.quit()

if __name__ == '__main__':
    unittest.main()