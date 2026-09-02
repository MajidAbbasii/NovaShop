const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
  
  page.on('request', req => {
    if (req.url().includes('api/auth/login')) {
      console.log('LOGIN API REQ:', req.method(), req.url(), req.postData());
    }
  });
  page.on('response', res => {
    if (res.url().includes('api/auth/login')) {
      console.log('LOGIN API RESP:', res.status());
    }
  });

  await page.goto('http://localhost:3000/login');
  await page.waitForTimeout(2000);
  
  // Check the form element
  const formInfo = await page.evaluate(() => {
    const form = document.querySelector('form');
    const submitBtn = document.querySelector('button[type="submit"]');
    return {
      formExists: !!form,
      onSubmit: form ? form.onsubmit : null,
      submitBtnExists: !!submitBtn,
      submitBtnType: submitBtn ? submitBtn.type : null,
      submitBtnText: submitBtn ? submitBtn.textContent : null,
    };
  });
  console.log('Form info:', JSON.stringify(formInfo, null, 2));
  
  // Try using page.locator to click
  await page.fill('#username', 'admin');
  await page.fill('#password', 'AdminPass123!');
  
  // Use page.locator().click() instead of page.click()
  await page.locator('button[type="submit"]').click();
  await page.waitForTimeout(3000);
  
  console.log('URL:', page.url());
  
  await browser.close();
})();