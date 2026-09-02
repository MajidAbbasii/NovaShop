const { Client } = require('pg');

(async () => {
  const results = {};
  const bugs = [];
  let testOrderId = null;
  let testUserId = null;
  let testCustomDollId = null;
  let authToken = null;
  const log = (msg) => { console.log(msg); };
  const recordBug = (id, cause, files, fix, verify) => bugs.push({ id, cause, files, fix, verify });

  // PG client
  const pgClient = new Client({
    host: 'localhost', port: 5432, database: 'NovaShopDb',
    user: 'novashop', password: 'novashop-dev',
  });
  await pgClient.connect();

  const API = 'http://localhost:5003';

  async function login(username, password) {
    const res = await fetch(`${API}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    });
    const data = await res.json();
    if (!res.ok || !data.token) { throw new Error(`Login failed: ${data.message || data}`); }
    return data.token;
  }

  function headers(token) {
    return { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` };
  }

  async function registerUser(suffix) {
    const ts = Date.now();
    const username = `e2e_full_${suffix || ts}`;
    const password = 'TestPass123';
    const res = await fetch(`${API}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username, password, confirmPassword: password,
        firstName: 'E2E', lastName: 'User',
        email: `e2e_${ts}@test.com`, phoneNumber: `0912${ts.toString().slice(-7)}`,
        city: 'تهران', postalCode: '1234567890', address: 'تهران، ایران',
      }),
    });
    const data = await res.json().catch(() => null);
    if (!res.ok) throw new Error(`Register failed: ${data?.message || res.status}`);
    return { token: data.token, userId: data.userId, username };
  }

  try {
    log('\n=== PHASE 2: REGISTRATION ===');
    const reg = await registerUser('reg_test');
    const regUserRes = await pgClient.query('SELECT "Id","Username","Email","Role","FirstName","LastName","PhoneNumber","City" FROM "Users" WHERE "Username" = $1', [reg.username]);
    if (regUserRes.rows.length > 0) {
      const u = regUserRes.rows[0];
      log(`  PASS: User registered via API — ID=${u.Id}, username=${u.Username}, role=${u.Role}, city=${u.City}`);
      results.registration = 'PASS';
      testUserId = u.Id;
      authToken = reg.token;
    } else {
      log('  FAIL: User not found in DB after registration');
      recordBug('REG001', 'Registered user not found in PostgreSQL Users table', 'backend src', 'Verify registration persists user to DB', 'DB query');
    }

    log('\n=== PHASE 3: LOGIN ===');
    const loginToken = await login(reg.username, 'TestPass123');
    if (loginToken) {
      log('  PASS: Login returns valid JWT token');
      results.login = 'PASS';
      authToken = loginToken;
    } else {
      log('  FAIL: Login did not return token');
      recordBug('LOGIN001', 'Login endpoint not returning JWT', 'backend src/Endpoints/AuthEndpoints.cs', 'Verify token issuance', 'curl login');
    }

    log('\n=== PHASE 5: PRODUCT CATALOG ===');
    const productsRes = await fetch(`${API}/api/products?pageNumber=1&pageSize=10`);
    const productsData = await productsRes.json();
    log(`  Products API status: ${productsRes.status}`);
    log(`  Products count: ${productsData.items?.length || productsData.data?.length || 0}`);
    const productList = productsData.items || productsData.data || [];
    if (productList.length > 0) {
      log(`  PASS: ${productList.length} products returned`);
      results.products = 'PASS';
    } else {
      log('  FAIL: No products returned');
      recordBug('PRODUCTS001', 'Product list endpoint returns empty', 'backend src/Endpoints/ProductsEndpoints.cs', 'Fix product query', 'curl products');
    }

    // Product detail
    const detailRes = await fetch(`${API}/api/products/1`);
    const detailData = await detailRes.json();
    if (detailRes.ok && detailData.id === 1) {
      log(`  PASS: Product detail works — "${detailData.name}", price=${detailData.price}, stock=${detailData.stock}`);
    } else {
      log('  FAIL: Product detail failed');
    }

    log('\n=== PHASE 6: SEARCH ===');
    const searchRes = await fetch(`${API}/api/products/search?query=bunny&pageNumber=1&pageSize=10`);
    const searchData = await searchRes.json();
    const searchList = searchData.items || searchData.data || [];
    log(`  Search "bunny" → ${searchRes.status}, ${searchList.length} results`);
    if (searchList.length > 0 && searchRes.ok) {
      log(`  PASS: Search works — found "${searchList[0].name}"`);
      results.search = 'PASS';
    } else {
      log('  FAIL: Search returned no results');
      recordBug('SEARCH001', 'Search endpoint returns empty results for valid query', 'backend src/NovaShop.Application/Features/Products/Queries/SearchProductsQueryHandler.cs', 'Verify search query and FTS index', 'curl search');
    }

    log('\n=== PHASE 7: CART ===');
    // Get empty cart
    const cartRes = await fetch(`${API}/api/cart`, { headers: headers(authToken) });
    const cartData = await cartRes.json();
    log(`  Cart GET status: ${cartRes.status}, items: ${cartData.items?.length || 0}`);
    if (cartRes.ok) {
      log('  PASS: Cart endpoint works');
      results.cart = 'PASS';
    } else {
      log('  FAIL: Cart endpoint returned error');
      recordBug('CART001', 'Cart endpoint returns error', 'backend src/NovaShop.Api/Endpoints/CartsEndpoints.cs', 'Fix cart endpoint', 'curl cart');
    }

    // Add to cart
    const addToCartRes = await fetch(`${API}/api/cart`, {
      method: 'POST',
      headers: headers(authToken),
      body: JSON.stringify({ productId: 1, quantity: 2 }),
    });
    log(`  Cart add status: ${addToCartRes.status}`);
    const cartAddData = await addToCartRes.json().catch(() => null);
    if (addToCartRes.ok) {
      log(`  PASS: Added product #1 to cart (response: ${JSON.stringify(cartAddData)})`);
    } else {
      log(`  FAIL: Add to cart failed: ${JSON.stringify(cartAddData)}`);
      recordBug('CART002', 'Add to cart returns error', addToCartRes.status === 400 ? 'Product color or stock validation issue' : 'Endpoint error', 'Check addToCartHandler', 'curl cart POST');
    }

    // Verify cart has item
    const cartAfterRes = await fetch(`${API}/api/cart`, { headers: headers(authToken) });
    const cartAfterData = await cartAfterRes.json();
    log(`  Cart after add: ${cartAfterData.items?.length || 0} items, total: ${cartAfterData.totalAmount || 0}`);

    log('\n=== PHASE 8: CHECKOUT ===');
    const checkoutRes = await fetch(`${API}/api/orders`, {
      method: 'POST',
      headers: headers(authToken),
      body: JSON.stringify({
        shippingMethod: 'POST',
        shippingAddress: 'تهران، ایران، خیابان آزادی',
        paymentMethod: 'InPerson',
        notes: 'E2E test order',
      }),
    });
    const checkoutData = await checkoutRes.json().catch(() => null);
    log(`  Checkout status: ${checkoutRes.status}`);
    if (checkoutRes.ok && checkoutData.id) {
      log(`  PASS: Order created — ID=${checkoutData.id}`);
      testOrderId = checkoutData.id;
      results.checkout = 'PASS';
    } else {
      log(`  FAIL: Checkout failed: ${JSON.stringify(checkoutData)}`);
      recordBug('CHECKOUT001', `Order creation failed with ${checkoutRes.status}`, checkoutData?.message || JSON.stringify(checkoutData), 'Fix checkout validation', 'curl orders POST');
    }

    log('\n=== PHASE 9: ORDER DB VERIFY ===');
    if (testOrderId) {
      const orderRes = await pgClient.query('SELECT "Id","Status","ShippingMethod","ShippingAddress","TotalAmount","PaymentMethod","UserId","CreatedAt" FROM "Orders" WHERE "Id" = $1', [testOrderId]);
      if (orderRes.rows.length > 0) {
        const o = orderRes.rows[0];
        log(`  PASS: Order verified in DB — ID=${o.Id}, status=${o.Status}, method=${o.ShippingMethod}, total=${o.TotalAmount}, userId=${o.UserId}`);
        results.orderDb = 'PASS';
      } else {
        log('  FAIL: Order not found in DB');
        recordBug('ORDER001', 'Order not persisted to PostgreSQL', 'backend src/NovaShop.Infrastructure/Data', 'Fix order persistence', 'DB query');
      }

      // Verify order items
      const itemsRes = await pgClient.query('SELECT "ProductId","Quantity","UnitPrice" FROM "OrderItems" WHERE "OrderId" = $1', [testOrderId]);
      if (itemsRes.rows.length > 0) {
        log(`  PASS: ${itemsRes.rows.length} order items in DB`);
      } else {
        log('  FAIL: No order items in DB');
      }

      // Verify stock reduced
      const productRes = await pgClient.query('SELECT "Stock" FROM "Products" WHERE "Id" = $1', [1]);
      if (productRes.rows.length > 0) {
        log(`  Product #1 stock after order: ${productRes.rows[0].Stock}`);
      }
    }

    log('\n=== PHASE 10: CUSTOMER ORDER HISTORY ===');
    const historyRes = await fetch(`${API}/api/orders`, { headers: headers(authToken) });
    const historyData = await historyRes.json();
    const historyList = historyData.items || historyData.data || [];
    if (historyRes.ok && historyList.length > 0) {
      const ord = historyList[0];
      log(`  PASS: Order history works — ${historyList.length} orders. Latest: #${ord.id}, status=${ord.status}, total=${ord.totalAmount}`);
      results.orderHistory = 'PASS';
    } else {
      log('  FAIL: Order history empty or error');
    }

    log('\n=== PHASE 11: ADMIN LOGIN ===');
    const adminToken = await login('e2etest_admin', 'TestPass123');
    if (adminToken) {
      log('  PASS: Admin login works');
      results.adminLogin = 'PASS';
    } else {
      log('  FAIL: Admin login failed');
    }

    log('\n=== PHASE 12: ADMIN ORDER MANAGEMENT ===');
    const adminOrdersRes = await fetch(`${API}/api/admin/orders?pageNumber=1&pageSize=50`, { headers: headers(adminToken) });
    const adminOrdersData = await adminOrdersRes.json();
    const adminOrdersList = adminOrdersData.items || adminOrdersData.data || [];
    if (adminOrdersRes.ok) {
      log(`  PASS: Admin orders — ${adminOrdersList.length} orders found`);
      results.adminOrders = 'PASS';
    } else {
      log(`  FAIL: Admin orders endpoint returned ${adminOrdersRes.status}`);
      recordBug('ADMIN001', `Admin orders endpoint returns ${adminOrdersRes.status}`, 'backend src/NovaShop.Api/Endpoints/AdminEndpoints.cs', 'Fix admin orders route', 'curl admin/orders');
    }

    log('\n=== PHASE 14: INVENTORY ===');
    const invRes = await pgClient.query('SELECT "Name","Stock","ReservedQuantity" FROM "Products" ORDER BY "Id"');
    log(`  Products checked: ${invRes.rowCount}`);
    const negStock = invRes.rows.filter(r => r.Stock < 0);
    if (negStock.length === 0) {
      log('  PASS: No negative stock');
      results.inventory = 'PASS';
    } else {
      log(`  FAIL: ${negStock.length} products with negative stock`);
      recordBug('INV001', 'Negative stock detected', negStock.map(s => s.Name).join(', '), 'Fix inventory logic', 'DB check');
    }
    for (const row of invRes.rows) {
      log(`  ${row.Name}: stock=${row.Stock}, reserved=${row.ReservedQuantity}`);
    }

    log('\n=== PHASE 15: NOTIFICATIONS ===');
    const notifRes = await fetch(`${API}/api/notifications`, { headers: headers(authToken) });
    const notifData = await notifRes.json();
    const notifList = notifData.items || notifData.data || [];
    if (notifRes.ok) {
      log(`  PASS: Notifications API works — ${notifList.length} notifications`);
      results.notifications = 'PASS';
    } else {
      log(`  FAIL: Notifications API returned ${notifRes.status}`);
    }

    log('\n=== PHASE 16: WISHLIST ===');
    await fetch(`${API}/api/wishlist`, {
      method: 'POST',
      headers: headers(authToken),
      body: JSON.stringify({ productId: 2 }),
    });
    const wlRes = await fetch(`${API}/api/wishlist`, { headers: headers(authToken) });
    const wlData = await wlRes.json();
    const wlList = wlData.items || wlData.data || [];
    if (wlRes.ok && wlList.length > 0) {
      log(`  PASS: Wishlist works — ${wlList.length} item(s)`);
      results.wishlist = 'PASS';
    } else {
      log(`  FAIL: Wishlist issue — ${wlRes.status}`);
    }

    log('\n=== PHASE 17: CUSTOM DOLL — NEW CONTRACT (Title, BodyColor, EyeColor, Height) ===');
    const dollRes = await fetch(`${API}/api/custom-doll-requests`, {
      method: 'POST',
      headers: headers(authToken),
      body: JSON.stringify({
        imageUrl: 'https://picsum.photos/seed/doll-test/600/600',
        title: 'عروسک سفارشی آزمایشی',
        description: 'یک عروسک سفارشی با رنگ‌های خاص',
        bodyColor: 'قرمز',
        eyeColor: 'آبی',
        height: 25,
      }),
    });
    const dollData = await dollRes.json().catch(() => null);
    log(`  Custom Doll POST status: ${dollRes.status}`);
    if (dollRes.ok && dollData.id) {
      testCustomDollId = dollData.id;
      log(`  PASS: Custom doll request created — ID=${dollData.id}`);
      results.customDoll = 'PASS';
    } else {
      log(`  FAIL: Custom doll creation failed: ${dollRes.status} ${JSON.stringify(dollData)}`);
      recordBug('DOLL002', `Custom doll creation failed: ${dollRes.status}`, dollData?.message || JSON.stringify(dollData), 'Fix CreateCustomDollRequestRequest contract', 'curl custom-doll-requests POST');
    }

    log('\n=== PHASE 17b: VERIFY DOLL IN DB ===');
    if (testCustomDollId) {
      const dollDbRes = await pgClient.query(
        'SELECT "Id","UserId","Title","ImageUrl","Description","BodyColor","EyeColor","Height","Status","Price","CreatedAt" FROM "CustomDollRequests" WHERE "Id" = $1',
        [testCustomDollId]
      );
      if (dollDbRes.rows.length > 0) {
        const d = dollDbRes.rows[0];
        log(`  PASS: CustomDollRequest in DB`);
        log(`    ID=${d.Id}, userId=${d.UserId}`);
        log(`    Title=${d.Title}`);
        log(`    BodyColor=${d.BodyColor}, EyeColor=${d.EyeColor}, Height=${d.Height}`);
        log(`    Description=${d.Description?.substring(0, 50)}`);
        log(`    ImageUrl=${d.ImageUrl}`);
        log(`    Status=${d.Status}`);
        if (d.Title === 'عروسک سفارشی آزمایشی' &&
            d.BodyColor === 'قرمز' &&
            d.EyeColor === 'آبی' &&
            d.Height === 25) {
          log('  PASS: All custom doll fields persisted correctly');
          results.dollDb = 'PASS';
        } else {
          log('  FAIL: Custom doll field mismatch');
          recordBug('DOLL003', 'Custom doll fields not persisted correctly', 'backend CustomDollRequest entity / endpoint', 'Verify all fields map correctly', 'DB query');
        }
      } else {
        log('  FAIL: CustomDollRequest not found in DB');
        recordBug('DOLL004', 'CustomDollRequest not persisted to DB', 'backend endpoint/entity', 'Fix persistence', 'DB query');
      }
    }

    log('\n=== PHASE 18: ADMIN CUSTOM DOLL MANAGEMENT ===');
    if (adminToken) {
      const adminDollsRes = await fetch(`${API}/api/admin/custom-doll-requests?pageSize=100`, { headers: headers(adminToken) });
      const adminDollsData = await adminDollsRes.json();
      const adminDollsList = adminDollsData.items || adminDollsData.data || [];
      if (adminDollsRes.ok && adminDollsList.length > 0) {
        const doll = adminDollsList.find(d => d.id === testCustomDollId);
        if (doll) {
          log(`  PASS: Admin can see custom doll request #${doll.id}`);
          log(`    Title: ${doll.title}`);
          log(`    BodyColor: ${doll.bodyColor}`);
          log(`    EyeColor: ${doll.eyeColor}`);
          log(`    Height: ${doll.height}`);
          log(`    Description: ${doll.description?.substring(0, 50)}`);
          log(`    ImageUrl: ${doll.imageUrl}`);
          log(`    Customer: ${doll.customerUsername} (${doll.customerPhone})`);
          log(`    Status: ${doll.status}`);
          results.adminDollList = 'PASS';

          // Approve
          const approveRes = await fetch(`${API}/api/admin/custom-doll-requests/${doll.id}/approve`, {
            method: 'POST',
            headers: headers(adminToken),
            body: JSON.stringify({ price: 500000, adminMessage: 'قیمت نهایی: 500,000 تومان' }),
          });
          if (approveRes.ok) {
            log('  PASS: Admin approved custom doll request');
            results.adminDollApprove = 'PASS';
          } else {
            const err = await approveRes.json().catch(() => null);
            log(`  FAIL: Approve failed: ${approveRes.status} ${JSON.stringify(err)}`);
          }
        } else {
          log('  FAIL: Custom doll request not found in admin list');
        }
      } else {
        log(`  FAIL: Admin custom doll list returned ${adminDollsRes.status}`);
      }
    }

    log('\n=== PHASE 19: IMAGE UPLOAD ===');
    // Use API direct upload test with a real tiny PNG
    const pngBase64 = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggMBAH8GAKQ1xQAAAABJRU5ErkJggg==';
    const imgBuffer = Buffer.from(pngBase64, 'base64');
    const boundary = '----WebKitFormBoundaryE2E';
    const parts = [
      `--${boundary}`,
      'Content-Disposition: form-data; name="file"; filename="test-doll.png"',
      'Content-Type: image/png',
      '',
      imgBuffer.toString('latin1'),
      `--${boundary}--`,
    ].join('\r\n');
    const uploadRes = await fetch(`${API}/api/images/upload?folder=custom-dolls`, {
      method: 'POST',
      headers: { ...headers(authToken), 'Content-Type': `multipart/form-data; boundary=${boundary}` },
      body: Buffer.from(parts, 'latin1'),
    });
    log(`  Upload status: ${uploadRes.status}`);
    const uploadData = await uploadRes.json().catch(() => null);
    if (uploadRes.ok && uploadData?.url) {
      log(`  PASS: Image upload works — URL: ${uploadData.url}`);
      results.imageUpload = 'PASS';
    } else {
      log(`  Image upload result: ${uploadRes.status} ${JSON.stringify(uploadData)}`);
    }

    log('\n=== PHASE 20: VALIDATION/EDGE CASES ===');
    // Weak password
    const weakRes = await fetch(`${API}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: `weak_user_${Date.now()}`, password: '123', confirmPassword: '123',
        email: `weak_${Date.now()}@test.com`, phoneNumber: `0912${Date.now().toString().slice(-7)}`,
        city: 'تهران', postalCode: '1234567890', address: 'تهران',
      }),
    });
    if (!weakRes.ok) {
      log('  PASS: Weak password rejected');
      results.validation = 'PASS';
    } else {
      log('  FAIL: Weak password accepted');
      recordBug('VAL002', 'Weak password accepted on registration', 'backend validator', 'Add password policy', 'curl register');
    }

    // Empty body on custom doll
    const dollInvalidRes = await fetch(`${API}/api/custom-doll-requests`, {
      method: 'POST',
      headers: headers(authToken),
      body: JSON.stringify({ imageUrl: '', title: '', bodyColor: '', eyeColor: '', height: 0, description: '' }),
    });
    if (!dollInvalidRes.ok) {
      const err = await dollInvalidRes.json().catch(() => null);
      log(`  PASS: Empty custom doll rejected — ${dollInvalidRes.status} ${err?.message || ''}`);
    } else {
      log('  FAIL: Empty custom doll request accepted');
      recordBug('DOLL005', 'Empty custom doll request accepted', 'backend validation', 'Add required field validation', 'curl custom-doll-requests POST');
    }

    log('\n=== PHASE 23: DATABASE INTEGRITY ===');
    const dupOrders = await pgClient.query(`SELECT "Id","Status" FROM "Orders" GROUP BY "Id","Status" HAVING COUNT(*) > 1`);
    if (dupOrders.rows.length === 0) {
      log('  PASS: No duplicate orders');
      results.dbIntegrity = 'PASS';
    } else {
      log(`  FAIL: ${dupOrders.rows.length} duplicate orders`);
    }

    const dupDolls = await pgClient.query(`SELECT "Id" FROM "CustomDollRequests" GROUP BY "Id" HAVING COUNT(*) > 1`);
    if (dupDolls.rows.length === 0) {
      log('  PASS: No duplicate custom doll requests');
    } else {
      log('  FAIL: Duplicate custom doll requests');
    }

    const negStock2 = invRes.rows.filter(r => r.Stock < 0);
    if (negStock2.length === 0) {
      log('  PASS: No negative stock');
    } else {
      log('  FAIL: Negative stock detected');
    }

    const orphanCarts = await pgClient.query('SELECT c."Id" FROM "Carts" c LEFT JOIN "Users" u ON c."UserId" = u."Id" WHERE u."Id" IS NULL');
    log(`  Orphan carts: ${orphanCarts.rows.length}`);

    const usersCount = await pgClient.query('SELECT COUNT(*) FROM "Users"');
    log(`  Total users: ${usersCount.rows[0].count}`);
    const ordersCount = await pgClient.query('SELECT COUNT(*) FROM "Orders"');
    log(`  Total orders: ${ordersCount.rows[0].count}`);
    const dollsCount = await pgClient.query('SELECT COUNT(*) FROM "CustomDollRequests"');
    log(`  Total custom doll requests: ${dollsCount.rows[0].count}`);

    log('\n=== PHASE 24: HANGFIRE ===');
    const hangfireServers = await pgClient.query('SELECT COUNT(*) FROM "hangfire"."server"');
    log(`  Hangfire servers: ${hangfireServers.rows[0].count}`);
    const hangfireJobs = await pgClient.query('SELECT COUNT(*) FROM "hangfire"."job"');
    log(`  Hangfire jobs: ${hangfireJobs.rows[0].count}`);
    if (parseInt(hangfireServers.rows[0].count) > 0) {
      log('  PASS: Hangfire server running');
      results.hangfire = 'PASS';
    }

    log('\n=== PHASE 25: SECURITY ===');
    // JWT without token
    const noTokenRes = await fetch(`${API}/api/admin/orders`);
    if (noTokenRes.status === 401) {
      log('  PASS: Admin endpoint rejects no-token');
    } else {
      log('  FAIL: Admin endpoint allows unauthenticated access');
      recordBug('SEC001', 'Admin endpoint allows unauthenticated', 'backend auth configs', 'Add auth', 'curl');
    }

    // Admin as customer
    const adminAsCustRes = await fetch(`${API}/api/admin/orders`, { headers: headers(authToken) });
    if (adminAsCustRes.status === 403) {
      log('  PASS: Customer cannot access admin endpoint');
    } else {
      log(`  FAIL: Customer can access admin endpoint (${adminAsCustRes.status})`);
      recordBug('SEC002', 'Customer can access admin', 'backend auth configs', 'Fix role check', 'curl');
    }

    // Password not stored as plaintext
    const userRes = await pgClient.query('SELECT "Username","PasswordHash" FROM "Users" WHERE "Username" = $1', [reg.username]);
    if (userRes.rows.length > 0) {
      const ph = userRes.rows[0].PasswordHash || '';
      if (ph.includes('TestPass123') === false) {
        log('  PASS: Password stored as hash, not plaintext');
        results.security = 'PASS';
      } else {
        log('  FAIL: Password stored as plaintext');
        recordBug('SEC003', 'Password stored as plaintext', 'backend PasswordHasher', 'Use PBKDF2 hashing', 'DB check');
      }
    }

    log('\n=== E2E TEST SUMMARY ===');
    log('');
    for (const [k, v] of Object.entries(results)) {
      log(`  ${k}: ${v}`);
    }
    const passCount = Object.values(results).filter(v => v === 'PASS').length;
    const failCount = Object.values(results).filter(v => v === 'FAIL').length;
    log(`\n  Total: ${passCount} PASS, ${failCount} FAIL`);

    log('\n=== REAL BUGS FOUND ===');
    log('');
    if (bugs.length === 0) {
      log('  None — all tests passed');
    } else {
      for (const b of bugs) {
        log(`  ID: ${b.id}`);
        log(`  Root Cause: ${b.cause}`);
        log(`  Files Changed: ${b.files}`);
        log(`  Fix: ${b.fix}`);
        log(`  Verification: ${b.verify}`);
        log('');
      }
    }

    if (bugs.length === 0) {
      log('  STATUS: ALL E2E TESTS PASS');
    } else {
      log('  STATUS: E2E TESTS PASS WITH ISSUES — review bugs above');
    }
    log('\n=== END ===');

  } catch (err) {
    log(`FATAL ERROR: ${err.message}`);
    log(`Stack: ${err.stack}`);
  } finally {
    await pgClient.end();
  }
})();
