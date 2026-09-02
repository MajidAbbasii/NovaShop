const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: false, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  const USERNAME = 'e2e_debug_' + Date.now();
  
  await page.goto('http://localhost:3000/register', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1000);
  
  await page.fill('#username', USERNAME);
  await page.fill('#firstName', 'Test');
  await page.fill('#lastName', 'User');
  await page.fill('#email', USERNAME + '@test.com');
  await page.fill('#phone', '09120000000');
  await page.fill('#password', 'TestPass123!');
  await page.fill('#city', 'تهران');
  await page.fill('#postalCode', '1234567890');
  await page.fill('#address', 'تهران، ایران');
  
  // Listen for network errors
  page.on('console', msg => console.log('PAGE:', msg.text()));
  page.on('pageerror', err => console.log('ERROR:', err.message));
  
  await page.click('button[type="submit"]');
  await page.waitForTimeout(5000);
  
  console.log('URL after submit:', page.url());
  const cookies = await page.context().cookies();
  console.log('Cookies:', JSON.stringify(cookies.map(c => ({ name: c.name, value: c.value.substring(0, 20) + '...' }))));
  
  await browser.close();
})();
