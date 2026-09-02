const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  
  // Intercept ALL requests including fetch
  await page.route('**/*', route => {
    const url = route.request().url();
    const method = route.request().method();
    if (url.includes('register') || url.includes('auth/login')) {
      console.log('ROUTE:', method, url, route.request().postData());
    }
    route.continue();
  });
  
  page.on('request', req => {
    const url = req.url();
    if (url.includes('register') || url.includes('auth')) {
      console.log('REQ:', req.method(), url, req.postData() || '');
    }
  });
  
  page.on('response', res => {
    const url = res.url();
    if (url.includes('register') || url.includes('auth')) {
      console.log('RESP:', res.status(), url);
    }
  });
  
  const username = 'dbg6_' + Date.now();
  const phone = '0912' + Date.now().toString().slice(-7);
  
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
  
  console.log('Form filled. Clicking submit button...');
  
  // Try clicking the actual submit button
  await page.evaluate(() => {
    const btn = document.querySelector('button[type="submit"]');
    if (btn) {
      console.log('Found submit button, clicking...');
      btn.click();
    } else {
      console.log('Submit button NOT found');
    }
  });
  
  await page.waitForTimeout(5000);
  
  console.log('Final URL:', page.url());
  const cookieVal = await page.evaluate(() => document.cookie);
  console.log('document.cookie:', cookieVal);
  
  await browser.close();
})();
