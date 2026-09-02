const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  const username = 'dbg8_' + Date.now();
  const phone = '0912' + Date.now().toString().slice(-7);

  // Track ALL fetch requests
  let fetchCalled = false;
  await page.route('**/*', route => {
    const url = route.request().url();
    const method = route.request().method();
    console.log('ALL REQ:', method, url);
    if (url.includes('api/auth')) {
      fetchCalled = true;
      console.log('  POST DATA:', route.request().postData());
    }
    route.continue();
  });

  page.on('console', msg => {
    const text = msg.text();
    console.log('PAGE:', text.substring(0, 200));
  });
  page.on('pageerror', err => console.log('PAGE ERROR:', err.message));

  await page.goto('http://localhost:3000/register');
  await page.waitForTimeout(2000);

  await page.fill('#username', username);
  await page.fill('#firstName', 'Test');
  await page.fill('#lastName', 'User');
  await page.fill('#email', username + '@test.com');
  await page.fill('#phone', phone);
  await page.fill('#password', 'TestPass123!');
  await page.fill('#city', 'تهران');
  await page.fill('#postalCode', '1234567890');
  await page.fill('#address', 'تهران، ایران');

  console.log('=== Clicking submit ===');
  await page.click('button[type="submit"]');
  await page.waitForTimeout(5000);
  
  console.log('fetchCalled:', fetchCalled);
  console.log('Final URL:', page.url());
  console.log('document.cookie:', await page.evaluate(() => document.cookie));

  await browser.close();
})();
