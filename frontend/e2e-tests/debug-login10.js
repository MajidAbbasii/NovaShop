const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  const username = 'dbg10_' + Date.now();
  const phone = '0912' + Date.now().toString().slice(-7);

  // Use page.evaluate to do everything in the browser
  await page.goto('http://localhost:3000/register');
  await page.waitForTimeout(3000);
  
  // Fill form via page.fill
  await page.fill('#username', username);
  await page.fill('#firstName', 'Test');
  await page.fill('#lastName', 'User');
  await page.fill('#email', username + '@test.com');
  await page.fill('#phone', phone);
  await page.fill('#password', 'TestPass123!');
  await page.fill('#city', 'تهران');
  await page.fill('#postalCode', '1234567890');
  await page.fill('#address', 'تهران، ایران');
  
  // Use evaluate to call the form's handleSubmit directly
  await page.evaluate(async () => {
    // Find the form and its onSubmit handler
    const form = document.querySelector('form');
    if (!form) {
      console.log('No form found');
      return;
    }
    // Get the React event handler via _reactProps
    console.log('Form found:', form.id || 'no-id');
    // Try to find the submit button and click it programmatically
    const btn = form.querySelector('button[type="submit"]');
    if (btn) {
      console.log('Button found, dispatching click...');
      btn.click();
    } else {
      console.log('Button not found');
    }
  });
  
  await page.waitForTimeout(5000);
  
  console.log('URL:', page.url());
  console.log('document.cookie:', await page.evaluate(() => document.cookie));
  
  await browser.close();
})();
