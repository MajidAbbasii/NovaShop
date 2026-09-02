const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  const username = 'dbg9_' + Date.now();
  const phone = '0912' + Date.now().toString().slice(-7);

  page.on('request', req => {
    if (req.url().includes('api/auth')) {
      console.log('AUTH REQ:', req.method(), req.url(), 'DATA:', req.postData());
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

  // Try form.dispatchEvent(new Event('submit', { cancelable: true, bubbles: true }))
  console.log('=== Dispatching submit event ===');
  await page.dispatchEvent('form', 'submit', { cancelable: true, bubbles: true });
  await page.waitForTimeout(5000);

  console.log('Final URL:', page.url());
  console.log('document.cookie:', await page.evaluate(() => document.cookie));

  // Also try clicking the button directly
  console.log('=== Trying direct click ===');
  await page.click('button[type="submit"]');
  await page.waitForTimeout(3000);
  console.log('URL after click:', page.url());
  console.log('cookie after click:', await page.evaluate(() => document.cookie));

  await browser.close();
})();
