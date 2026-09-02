const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  
  // Intercept fetch/XHR
  page.on('request', req => {
    if (req.url().includes('api/auth')) {
      console.log('AUTH REQ:', req.method(), req.url(), req.postData());
    }
  });
  page.on('response', res => {
    if (res.url().includes('api/auth')) {
      res.text().then(text => {
        console.log('AUTH RESP:', res.status(), text.substring(0, 200));
      }).catch(() => {});
    }
  });
  page.on('console', msg => console.log('PAGE:', msg.text()));
  page.on('pageerror', err => console.log('PAGEERROR:', err.message));
  
  const username = 'dbg3_' + Date.now();
  const phone = '0912' + Date.now().toString().slice(-7);
  
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
  
  console.log('Form filled. Submitting...');
  await page.click('button[type="submit"]');
  await page.waitForTimeout(5000);
  
  console.log('URL:', page.url());
  const cookieVal = await page.evaluate(() => document.cookie);
  console.log('document.cookie:', cookieVal);
  
  await browser.close();
})();
