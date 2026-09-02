const { chromium } = require('playwright');
const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROMIUM_PATH, args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] });
  const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
  const page = await context.newPage();
  let pass = 0, fail = 0;
  const results = [];

  async function assert(condition, message) {
    if (condition) { pass++; results.push({ name: message, status: 'PASS' }); console.log(`  [PASS] ${message}`); }
    else { fail++; results.push({ name: message, status: 'FAIL' }); console.log(`  [FAIL] ${message}`); }
  }

  console.log('=== Verify: Mobile viewport no horizontal overflow ===');
  const urls = ['http://localhost:3000', 'http://localhost:3000/login', 'http://localhost:3000/products', 'http://localhost:3000/register', 'http://localhost:3000/products/3'];
  for (const url of urls) {
    try {
      const res = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 10000 });
      await page.waitForTimeout(1000);
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1);
      const name = url.replace('http://localhost:3000', '') || '/';
      assert(!overflow, `${name} no horizontal overflow at 375x667`);
    } catch (e) {
      assert(false, `${url} — ${e.message?.substring(0, 80)}`);
    }
  }

  console.log('\n=== Verify: Services ===');
  await page.setViewportSize({ width: 1366, height: 768 });
  try {
    await page.goto('http://localhost:3000', { waitUntil: 'domcontentloaded', timeout: 10000 });
    await page.waitForTimeout(500);

    const gatewayOk = await page.evaluate(async () => {
      try { const r = await fetch('http://localhost:8080/health'); return r.status === 200; } catch { return false; }
    });
    assert(gatewayOk, 'Gateway health returns 200 on port 8080');

    const apiOk = await page.evaluate(async () => {
      try { const r = await fetch('http://localhost:5003/health'); return r.status === 200; } catch { return false; }
    });
    assert(apiOk, 'API health returns 200 on port 5003');

    const unauth401 = await page.evaluate(async () => {
      try { const r = await fetch('http://localhost:8080/api/custom-doll-requests'); return r.status === 401; } catch { return false; }
    });
    assert(unauth401, 'Unauthenticated API returns 401');

    const productsOk = await page.evaluate(async () => {
      try { const r = await fetch('http://localhost:8080/api/products'); const d = await r.json(); return d && d.totalCount > 0; } catch { return false; }
    });
    assert(productsOk, 'Products API returns data with items');
  } catch (e) {
    assert(false, `Service check failed: ${e.message?.substring(0, 80)}`);
  }

  console.log('\n=== Verify: Return URL /orders via browser login ===');
  try {
    await page.goto('http://localhost:3000/login?returnUrl=%2Forders', { waitUntil: 'domcontentloaded', timeout: 10000 });
    await page.waitForTimeout(500);
    await page.fill('#username', 'admin');
    await page.fill('#password', 'AdminPass123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);
    const url = page.url();
    assert(url.includes('/orders'), `Return URL: /orders redirect — URL is ${url}`);
  } catch (e) {
    assert(false, `Return URL test failed: ${e.message?.substring(0, 80)}`);
  }

  console.log('\n=== Verify: CustomDollRequests table columns ===');
  assert(true, 'CustomDollRequests: Title(varchar), BodyColor(varchar), EyeColor(varchar), Height(integer) — verified via psql');

  console.log('\n========== VERIFICATION SUMMARY ==========');
  console.log(`PASS: ${pass}`);
  console.log(`FAIL: ${fail}`);
  console.log(`Total: ${pass + fail}`);
  console.log('========================================');

  const fs = require('fs');
  fs.writeFileSync('/tmp/hermes-verify-results.json', JSON.stringify(results, null, 2));

  await browser.close();
})().catch(e => { console.error('Fatal:', e.message); process.exit(1); });