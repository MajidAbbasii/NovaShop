const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';
(async () => {
  const browser = await chromium.launch({
    headless: true,
    executablePath: CHROMIUM_PATH,
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage']
  });
  const page = await browser.newPage();
  await page.goto('http://localhost:3000', { timeout: 15000 });
  console.log('Title:', await page.title());
  const h1 = await page.textContent('h1') || 'no h1';
  console.log('H1:', h1.substring(0, 100));
  await browser.close();
  console.log('Browser test PASSED');
})().catch(e => {
  console.error('Browser test FAILED:', e.message);
  process.exit(1);
});
