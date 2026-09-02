const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext();
  const page = await context.newPage();
  const username = 'dbg2_' + Date.now();
  
  // Enable request tracing
  page.on('request', req => console.log('REQ:', req.method(), req.url()));
  
  await page.goto('http://localhost:3000/register');
  await page.waitForTimeout(2000);
  
  await page.fill('#username', username);
  await page.fill('#firstName', 'Test');
  await page.fill('#lastName', 'User');
  await page.fill('#email', username + '@test.com');
  const phone = '0912' + Date.now().toString().slice(-7);
  await page.fill('#phone', phone);
  await page.fill('#password', 'TestPass123!');
  await page.fill('#city', 'تهران');
  await page.fill('#postalCode', '1234567890');
  await page.fill('#address', 'تهران، ایران');
  
  await page.click('button[type="submit"]');
  await page.waitForTimeout(5000);
  
  console.log('URL:', page.url());
  const cookieVal = await page.evaluate(() => document.cookie);
  console.log('document.cookie:', cookieVal);
  const cookies = await context.cookies();
  console.log('Context cookies:', JSON.stringify(cookies.map(c => c.name)));
  
  // Check localStorage
  const lsToken = await page.evaluate(() => localStorage.getItem('token'));
  console.log('localStorage token:', lsToken ? 'exists' : 'none');
  
  // Check what the register API endpoint actually is
  const apiMatch = cookieVal.match(/token=(.+)/);
  console.log('Token in cookie:', apiMatch ? 'YES' : 'NO');
  
  await browser.close();
})();
