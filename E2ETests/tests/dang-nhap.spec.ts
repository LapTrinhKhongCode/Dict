import { test, expect } from '@playwright/test';

test('Người dùng có thể đăng nhập thành công với tài khoản admin', async ({ page }) => {

  // 1. Mở trang login
  await page.goto('http://localhost:3000/login');

  // ⭐ Chờ Nuxt hydrate xong
  await page.waitForLoadState('networkidle');

  // 2. Xác nhận heading login hiển thị
  await expect(page.getByRole('heading', { name: 'ĐĂNG NHẬP' })).toBeVisible();

  // 3. Điền form (v-model sẽ nhận)
  await page.getByLabel('Username').fill('admin');
  await page.getByLabel('Password').fill('SuperPassword123!');

  // 4. Đợi nút login được enable từ isFormValid()
  const loginButton = page.getByRole('button', { name: 'Login' });
  await expect(loginButton).toBeEnabled(); // ✅ GIỜ SẼ PASS

  // 5. Click login
  await loginButton.click();

  // 6. Kiểm tra login thành công
  await expect(page.getByText('admin !')).toBeVisible();
});
