const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  const username = 'dbg7_' + Date.now();
  const phone = '0912' + Date.now().toString().slice(-7);

  page.on('console', msg => {
    const text = msg.text();
    if (text.includes('token') || text.includes('fetch') || text.includes('register') || text.includes('signIn') || text.includes('handleSubmit')) {
      console.log('PAGE CONSOLE:', text);
    }
  });
  page.on('pageerror', err => console.log('PAGE ERROR:', err.message));

  await page.goto('http://localhost:3000/register');
  await page.waitForTimeout(3000);

  await page.fill('#username', username);
  await page.fill('#firstName', 'Test');
  await page.fill('#lastName', 'User');
  await page.fill('#email', username + '@test.com');
  await page.fill('#phone', phone);
  await page.fill('#password', 'TestPass123!');
  await page.fill('#city', 'تهران');
  await page.fill('#postalCode', '1234567890');
  await page.fill('#address', 'تهران، ایران');

  // Click submit and trace ALL navigation
  const [response] = await Promise.race([
    page.waitForResponse(resp => resp.url().includes('api/auth/register'), { timeout: 10000 }),
    page.click('button[type="submit"]'),
    new Promise(resolve => setTimeout(() => resolve('timeout'), 10000)),
  ]);
  
  if (response) {
    console.log('Found register response:', response.status());
    const text = await response.text();
    console.log('Response body:', text.substring(0, 300));
  } else if (response === 'timeout') {
    console.log('No register API response found within timeout');
  }

  await page.waitForTimeout(3000);
  console.log('Final URL:', page.url());
  console.log('Cookies:', await page.context().cookies());
  console.log('document.cookie:', await page.evaluate(() => document.cookie));

  await browser.close();
})();
