from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import unittest
import time # ⚠️ 1. Import thư viện time

class AdminLoginTest(unittest.TestCase):
    
    def setUp(self):
        # 1. Khởi tạo WebDriver
        self.driver = webdriver.Chrome() 
        self.driver.maximize_window()
        self.base_url = "http://localhost:3000"

    def test_successful_admin_login(self):
        driver = self.driver
        # Thiết lập thời gian chờ tối đa 10 giây
        wait = WebDriverWait(driver, 10) 
        
        # 1. Mở trang login
        driver.get(self.base_url + "/login")

        # 2. Chờ heading login hiển thị (để xác nhận trang đã tải xong)
        heading_locator = (By.XPATH, "//h2[contains(text(), 'ĐĂNG NHẬP')]")
        wait.until(EC.visibility_of_element_located(heading_locator))
        
        # ⚠️ 2. KHẮC PHỤC RACE CONDITION (Quan trọng)
        # Chờ 1 giây CỐ ĐỊNH để Nuxt/Vue "hydrate" xong
        # trước khi tìm và gõ vào input.
        print("\nĐang chờ Nuxt/Vue hydration (1 giây)...")
        time.sleep(4)
        print("Bắt đầu điền form...")
        
        # 3. Điền form
        username_locator = (By.ID, "username") 
        
        # Bây giờ mới tìm element (sau khi đã hydrate)
        username_field = wait.until(EC.element_to_be_clickable(username_locator))
        
        # (Khi username sẵn sàng, password cũng sẽ sẵn sàng)
        password_locator = (By.ID, "password")
        password_field = driver.find_element(*password_locator)

        # Điền thông tin (Bây giờ Vue sẽ không xóa nữa)
        username_field.send_keys('admin')
        password_field.send_keys('SuperPassword123!')
        
        # 💡 BƯỚC KHẮC PHỤC QUAN TRỌNG: "Click ra ngoài"
        # Click vào heading "ĐĂNG NHẬP" để trigger 'blur' event
        # trên trường password, buộc v-model của Vue phải cập nhật.
        driver.find_element(*heading_locator).click()
        
        # (Nếu vẫn lỗi, bạn có thể thêm một khoảng chờ rất ngắn ở đây
        time.sleep(0.4) # Chờ 0.2 giây cho Vue/Nuxt re-render
        
        # 4. Đợi nút login được enable
        # Nút này có text là 'Login' khi mode='login'
        login_button_locator = (By.XPATH, "//button[normalize-space()='Login']")
        
        # Bây giờ, isFormValid() của Vue đã là true,
        # và lệnh chờ này sẽ pass ngay lập tức.
        login_button = wait.until(EC.element_to_be_clickable(login_button_locator))
        
        # 5. Click login
        login_button.click()
        
        # 6. Kiểm tra login thành công (Chuyển sang trang chủ)
        # Chờ cho text 'admin !' hiển thị (phần này ở trang chủ, không phải trang login)
        success_text_locator = (By.XPATH, "//*[contains(text(), 'admin !')]")
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