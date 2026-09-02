const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
  
  page.on('console', msg => console.log('PAGE:', msg.text()));
  page.on('pageerror', err => console.log('ERR:', err.message));

  // First, register a test user via API
  const username = 'verify_' + Date.now();
  const phone = ('' + (10000000000 + Math.floor(Math.random() * 1000000000))).slice(0, 11);
  const regRes = await page.request.post('http://localhost:8080/api/auth/register', {
    data: { username, password: 'TestPass123!', email: username + '@test.com', phoneNumber: phone, firstName: 'T', lastName: 'U', city: 'تهران', postalCode: '1234567890', address: 'تهران' },
    headers: { 'Content-Type': 'application/json' },
  });
  const regData = await regRes.json();
  console.log('Register response:', regData.token ? 'has token' : 'NO TOKEN', 'status:', regRes.status());

  // Now test the Return URL via browser
  await page.context().clearCookies();
  await page.goto('http://localhost:3000/login?returnUrl=%2Forders');
  await page.waitForTimeout(1000);
  
  // Check what the page thinks returnUrl is
  const returnUrl = await page.evaluate(() => {
    const params = new URLSearchParams(window.location.search);
    return params.get('returnUrl');
  });
  console.log('Browser URL returnUrl param:', returnUrl);
  
  await page.fill('#username', username);
  await page.fill('#password', 'TestPass123!');
  
  // Listen for fetch requests
  page.on('request', req => {
    if (req.url().includes('api')) {
      console.log('API REQ:', req.method(), req.url(), req.postData() ? 'HAS BODY' : 'NO BODY');
    }
  });
  
  await page.click('button[type="submit"]');
  await page.waitForTimeout(5000);
  
  console.log('Final URL:', page.url());
  const cookie = await page.evaluate(() => document.cookie);
  console.log('Cookie:', cookie ? 'has ' + cookie.length + ' chars' : 'empty');

  await browser.close();
})();