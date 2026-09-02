const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  const username = 'dbg5_' + Date.now();
  const phone = '0912' + Date.now().toString().slice(-7);
  
  // Use page.route to intercept ALL requests
  page.on('request', req => {
    console.log('REQ:', req.method(), req.url());
  });
  
  await page.goto('http://localhost:3000/register');
  await page.waitForTimeout(3000);
  
  // Check what the page thinks API_GATEWAY_URL is
  const apiUrl = await page.evaluate(() => {
    // Try to find any reference to the API URL in the page
    return window.location.href;
  });
  console.log('Page URL:', apiUrl);
  
  await page.fill('#username', username);
  await page.fill('#firstName', 'Test');
  await page.fill('#lastName', 'User');
  await page.fill('#email', username + '@test.com');
  await page.fill('#phone', phone);
  await page.fill('#password', 'TestPass123!');
  await page.fill('#city', 'تهران');
  await page.fill('#postalCode', '1234567890');
  await page.fill('#address', 'تهران، ایران');
  
  console.log('Form filled, clicking submit...');
  
  // Use page.evaluate to trigger the form submit and capture the fetch
  await page.evaluate(() => {
    const form = document.querySelector('form');
    if (form) {
      const event = new Event('submit', { cancelable: true, bubbles: true });
      form.dispatchEvent(event);
    } else {
      console.log('No form found');
    }
  });
  
  await page.waitForTimeout(5000);
  
  console.log('Final URL:', page.url());
  
  await browser.close();
})();
