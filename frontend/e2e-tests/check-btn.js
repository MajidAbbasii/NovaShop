const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
  
  await page.goto('http://localhost:3000/login');
  await page.waitForTimeout(2000);
  
  // Find all submit buttons and get their text
  const buttons = await page.locator('button[type="submit"]').all();
  for (let i = 0; i < buttons.length; i++) {
    const text = await buttons[i].textContent();
    const id = await buttons[i].id;
    console.log(`Button ${i}: text="${text}", id="${id}"`);
  }
  
  await browser.close();
})();