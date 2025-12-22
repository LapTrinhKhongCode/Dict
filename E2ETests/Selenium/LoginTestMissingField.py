from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

# Import test data
from test_data import BASE_URL, HYDRATION_WAIT_TIME, SHORT_WAIT_TIME, LoginData, Locators

class LoginTestMissingPassword(unittest.TestCase):
    
    def setUp(self):
        self.driver = webdriver.Chrome() 
        self.driver.maximize_window()
        self.base_url = BASE_URL

    def test_login_wrong_password(self):
        driver = self.driver
        wait = WebDriverWait(driver, 10) 
        
        driver.get(self.base_url + "/login")

        heading_locator = (By.XPATH, Locators.LOGIN_HEADING)
        wait.until(EC.visibility_of_element_located(heading_locator))
        
        print("\nĐang chờ Nuxt/Vue hydration...")
        time.sleep(HYDRATION_WAIT_TIME)
        print("Bắt đầu điền form...")
        
        username_locator = (By.ID, Locators.USERNAME_INPUT)
        username_field = wait.until(EC.element_to_be_clickable(username_locator))
        
        password_locator = (By.ID, Locators.PASSWORD_INPUT)
        password_field = driver.find_element(*password_locator)

        # Thiếu mật khẩu
        username_field.send_keys(LoginData.ADMIN_USERNAME)
        password_field.send_keys(LoginData.EMPTY_PASSWORD)
        
        driver.find_element(*heading_locator).click()
        time.sleep(SHORT_WAIT_TIME)
        
        login_button_locator = (By.XPATH, Locators.LOGIN_BUTTON)
        login_button = wait.until(EC.element_to_be_clickable(login_button_locator))
        login_button.click()
        
        # Kiểm tra login thất bại
        error_locator = (By.CSS_SELECTOR, Locators.ERROR_MESSAGE)
        try:
            error_element = wait.until(EC.visibility_of_element_located(error_locator))
            print(f"Error message found: {error_element.text}")
            self.assertTrue(error_element.is_displayed())
            self.assertNotEqual(error_element.text, "", "Error message should not be empty")
        except Exception as e:
            self.fail(f"Không tìm thấy thông báo lỗi: {e}")

    def tearDown(self):
        # Đóng trình duyệt
        print("Test complete. Closing browser.")
        self.driver.quit()

if __name__ == '__main__':
    unittest.main()