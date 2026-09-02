const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH });
  const page = await browser.newPage({ viewport: { width: 375, height: 667 } });
  const urls = [
    'http://localhost:3000',
    'http://localhost:3000/login',
    'http://localhost:3000/register',
    'http://localhost:3000/products',
    'http://localhost:3000/products/3',
    'http://localhost:3000/cart',
    'http://localhost:3000/checkout',
    'http://localhost:3000/orders',
    'http://localhost:3000/custom-doll-request',
    'http://localhost:3000/admin',
    'http://localhost:3000/admin/login',
  ];
  for (const url of urls) {
    await page.goto(url, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const overflowInfo = await page.evaluate(() => {
      const elements = document.querySelectorAll('*');
      const results = [];
      elements.forEach(el => {
        const rect = el.getBoundingClientRect();
        if (rect.right > window.innerWidth + 1) {
          results.push({
            tag: el.tagName,
            className: el.className,
            id: el.id,
            right: Math.round(rect.right),
            width: Math.round(rect.width),
          });
        }
      });
      return results.slice(0, 5);
    });
    console.log(`\n${url}:`);
    if (overflowInfo.length === 0) {
      console.log('  No overflow');
    } else {
      overflowInfo.forEach(e => console.log(`  ${e.tag}.${e.className} right=${e.right} width=${e.width}`));
    }
  }
  await browser.close();
})();
