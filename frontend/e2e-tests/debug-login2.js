const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage', '--disable-features=SameSiteByDefaultCookies,CookiesWithoutSameSiteMustBeSecure'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  const USERNAME = 'e2e_debug2_' + Date.now();
  
  page.on('console', msg => console.log('PAGE:', msg.text()));
  page.on('pageerror', err => console.log('PAGEERROR:', err.message));
  
  // Register
  await page.goto('http://localhost:3000/register');
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
  await page.click('button[type="submit"]');
  await page.waitForTimeout(5000);
  
  console.log('URL:', page.url());
  
  // Check cookie via JS
  const cookieVal = await page.evaluate(() => document.cookie);
  console.log('document.cookie:', cookieVal);
  
  // Check via context
  const cookies = await page.context().cookies();
  console.log('Context cookies:', cookies.map(c => ({ name: c.name, domain: c.domain, value: c.value.substring(0, 30) })));
  
  // Try navigating to orders (protected)
  await page.goto('http://localhost:3000/orders');
  await page.waitForTimeout(2000);
  console.log('Orders URL:', page.url());
  
  // Check localStorage too
  const lsToken = await page.evaluate(() => localStorage.getItem('token'));
  console.log('localStorage token:', lsToken ? 'exists' : 'none');
  
  await browser.close();
})();
