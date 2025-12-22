from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time

# Import test data
from test_data import (
    BASE_URL, HYDRATION_WAIT_TIME, SHORT_WAIT_TIME, LONG_WAIT_TIME,
    LoginData, Locators, DeckData, CardData
)

class CreateDeckTest(unittest.TestCase):
    
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


    def test_successful_deck_creation(self):
        driver = self.driver
        wait = WebDriverWait(driver, 10) 
        
        # 1. Đăng nhập
        self._login_as_admin(driver, wait)
        
        # Chuyển hướng đến trang Explore
        print("Đang chuyển hướng đến trang /explore...")
        driver.get(self.base_url + "/explore")
        
        # Điều hướng đến trang Tạo bộ thẻ
        create_deck_button_locator = (By.XPATH, Locators.CREATE_DECK_BUTTON)
        
        print("Đang tìm nút '+' để tạo bộ thẻ trên trang /explore...")
        
        explore_heading_locator = (By.XPATH, Locators.EXPLORE_HEADING)
        wait.until(EC.visibility_of_element_located(explore_heading_locator))
        
        create_button = wait.until(EC.element_to_be_clickable(create_deck_button_locator))
        create_button.click()
        
        # Chờ trang DeckCreator tải xong
        create_page_heading_locator = (By.XPATH, Locators.DECK_CREATOR_HEADING)
        wait.until(EC.visibility_of_element_located(create_page_heading_locator))
        print("Đã chuyển sang trang 'Tạo sổ tay mới'.")

        time.sleep(LONG_WAIT_TIME)

        # Điền form tạo bộ thẻ
        name_locator = (By.ID, Locators.DECK_NAME_INPUT)
        desc_locator = (By.ID, Locators.DECK_DESC_INPUT)

        deck_name = f"{DeckData.DECK_NAME_PREFIX} {int(time.time())}"
        
        name_field = wait.until(EC.element_to_be_clickable(name_locator))
        desc_field = driver.find_element(*desc_locator)

        name_field.send_keys(deck_name)
        desc_field.send_keys(DeckData.DECK_DESCRIPTION)
        print(f"Đã điền tên bộ thẻ: {deck_name}")

        # Thêm ít nhất một thẻ
        print("Đang thêm một thẻ mới...")
        front_text_locator = (By.CSS_SELECTOR, Locators.CARD_FRONT_INPUT)
        back_text_locator = (By.CSS_SELECTOR, Locators.CARD_BACK_INPUT)
        add_card_button_locator = (By.XPATH, Locators.ADD_CARD_BUTTON)

        front_field = driver.find_element(*front_text_locator)
        back_field = driver.find_element(*back_text_locator)
        add_card_button = driver.find_element(*add_card_button_locator)

        front_field.send_keys(CardData.CARD_FRONT_1)
        back_field.send_keys(CardData.CARD_BACK_1)
        
        add_card_button.click()
        
        time.sleep(SHORT_WAIT_TIME)
        
        # Xác minh thẻ đã xuất hiện trong danh sách
        card_in_list_locator = (By.XPATH, f"//*[contains(@class, 'truncate') and contains(text(), '{CardData.CARD_FRONT_1}')]")
        wait.until(EC.visibility_of_element_located(card_in_list_locator))
        print("Đã thêm thẻ vào danh sách thành công.")


        # Gửi form
        submit_button_locator = (By.XPATH, Locators.CREATE_DECK_SUBMIT)
        
        driver.find_element(*create_page_heading_locator).click()
        time.sleep(SHORT_WAIT_TIME)

        submit_button = wait.until(EC.element_to_be_clickable(submit_button_locator))
        submit_button.click()

        # Xác minh thành công
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