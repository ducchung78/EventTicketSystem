const { Builder, By, until } = require('selenium-webdriver');
const chrome = require('selenium-webdriver/chrome');
const assert = require('assert');

const BASE_URL      = process.env.BASE_URL || 'http://localhost:5134';
const TEST_EMAIL    = 'admin@tickethub.vn';
const TEST_PASSWORD = 'Admin@123456';

// ── Helper: khởi tạo Chrome headless ────────────────────────────────────────
function buildDriver() {
  const options = new chrome.Options();
  options.addArguments(
    '--headless=new', '--no-sandbox',
    '--disable-dev-shm-usage', '--window-size=1280,800'
  );
  return new Builder().forBrowser('chrome').setChromeOptions(options).build();
}

// ── Helper: đăng nhập tài khoản admin ───────────────────────────────────────
async function loginUser(driver) {
  await driver.get(`${BASE_URL}/Account/Login`);
  const emailInput = await driver.wait(until.elementLocated(By.id('tbEmail')), 10000);
  await emailInput.clear();
  await emailInput.sendKeys(TEST_EMAIL);
  const passwordInput = await driver.findElement(By.id('tbPassword'));
  await passwordInput.clear();
  await passwordInput.sendKeys(TEST_PASSWORD);
  await driver.findElement(By.css('#loginForm [type="submit"]')).click();
  await driver.wait(async () => {
    const url = await driver.getCurrentUrl();
    return !url.includes('/Account/Login');
  }, 10000, 'Timeout: không redirect sau đăng nhập');
}

// ── Helper: tìm event đầu tiên có seat map bằng cách thử SelectSeats URL ────
async function findSeatMapEventId(driver) {
  for (let id = 1; id <= 30; id++) {
    await driver.get(`${BASE_URL}/Events/SelectSeats/${id}`);
    const url = await driver.getCurrentUrl();
    if (url.includes(`/SelectSeats/${id}`)) {
      try {
        await driver.wait(until.elementLocated(By.css('.ss-seat')), 3000);
        return id;
      } catch (e) { /* event tồn tại nhưng không có ghế, thử tiếp */ }
    }
  }
  return null;
}

// ── Helper: tìm ghế available đầu tiên (không phải couple, không phải sold) ─
async function findFirstAvailableSeat(driver) {
  await driver.wait(until.elementLocated(By.css('#seatGrid')), 10000);
  const seats = await driver.findElements(
    By.css('.ss-seat:not(.selected):not(.sold):not(.reserved-other):not(.couple)')
  );
  return seats.length > 0 ? seats[0] : null;
}

// ── Helper: chọn ghế và bấm "Tiếp tục" → redirect sang Cart ────────────────
async function reserveSeatAndGoToCart(driver, eventId) {
  await driver.get(`${BASE_URL}/Events/SelectSeats/${eventId}`);
  await driver.wait(until.elementLocated(By.css('#seatGrid')), 10000);
  const seat = await findFirstAvailableSeat(driver);
  if (!seat) return null;
  const seatId    = await seat.getAttribute('data-seat-id');
  const seatLabel = await seat.getAttribute('data-label');
  await seat.click();
  // Chờ nút "Tiếp tục" được enable (JS bật sau khi có ghế được chọn)
  await driver.wait(async () => {
    const btn = await driver.findElement(By.id('btnContinue'));
    return !(await btn.getAttribute('disabled'));
  }, 5000, 'Nút Tiếp tục phải được enable sau khi chọn ghế');
  await driver.findElement(By.id('btnContinue')).click();
  await driver.wait(until.urlContains('/Cart'), 10000, 'Phải redirect sang Cart sau AddToCart');
  return { seatId, seatLabel };
}

// ── Helper: xóa giỏ hàng để release ghế Reserved (dùng trong afterEach) ─────
async function clearCart(driver) {
  try {
    await driver.get(`${BASE_URL}/Cart`);
    const clearBtns = await driver.findElements(By.css('.btn-clear-cart'));
    if (clearBtns.length === 0) return; // giỏ đã trống
    if (!(await clearBtns[0].isDisplayed())) return;
    // Override confirm dialog để auto-accept
    await driver.executeScript("window.confirm = () => true;");
    await clearBtns[0].click();
    await driver.sleep(500);
  } catch (e) { /* giỏ trống hoặc lỗi không nghiêm trọng */ }
}

// ─────────────────────────────────────────────────────────────────────────────

describe('TC08 - Seat Reservation State Management (VĐ3)', function () {
  this.timeout(30000);
  let driver;
  let eventId = null;

  before(async function () {
    // Khởi tạo driver chính, đăng nhập, tìm event có seat map
    driver = await buildDriver();
    await loginUser(driver);
    eventId = await findSeatMapEventId(driver);
    if (!eventId) {
      console.warn('⚠️  Không tìm thấy event có seat map — bỏ qua toàn bộ TC08');
      this.skip();
    }
  });

  after(async function () {
    if (driver) await driver.quit();
  });

  afterEach(async function () {
    // Dọn dẹp sau mỗi test: clear cart để release ghế đang Reserved
    await clearCart(driver);
  });

  // ── TC08.1: Click ghế → selected class xuất hiện, chip hiển thị ──────────
  it('TC08.1 – Click ghế available → chuyển sang selected (xanh lá), chip xuất hiện', async function () {
    await driver.get(`${BASE_URL}/Events/SelectSeats/${eventId}`);
    await driver.wait(until.elementLocated(By.css('#seatGrid')), 10000);

    // Tìm ghế available đầu tiên
    const seat = await findFirstAvailableSeat(driver);
    assert.ok(seat, 'Phải có ít nhất 1 ghế available để test');

    const seatLabel = await seat.getAttribute('data-label');
    await seat.click();

    // Verify ghế có class "selected" sau khi click
    const classes = await seat.getAttribute('class');
    assert.ok(
      classes.includes('selected'),
      `Ghế "${seatLabel}" phải có class "selected" sau khi click, class hiện tại: "${classes}"`
    );

    // Verify chip xuất hiện trong panel bên phải
    const chip = await driver.wait(
      until.elementLocated(By.css('#seatChips .ss-chip')),
      5000,
      'Chip ghế phải xuất hiện sau khi click'
    );
    const chipText = await chip.getText();
    assert.ok(
      chipText.includes(seatLabel),
      `Chip phải chứa label "${seatLabel}", text nhận được: "${chipText}"`
    );

    // Verify nút "Tiếp tục" được enable
    const continueBtn = await driver.findElement(By.id('btnContinue'));
    const isDisabled  = await continueBtn.getAttribute('disabled');
    assert.strictEqual(isDisabled, null, 'Nút "Tiếp tục" phải được enable khi có ghế được chọn');
  });

  // ── TC08.2: User ở session khác thấy ghế màu vàng (reserved-other) ────────
  it('TC08.2 – Ghế đang bị giữ bởi session khác hiển thị màu vàng (reserved-other), không click được', async function () {
    this.timeout(45000);

    // === Driver 1: reserve ghế bằng cách bấm "Tiếp tục" (lưu vào DB) ===
    const reserved = await reserveSeatAndGoToCart(driver, eventId);
    assert.ok(reserved, 'Phải reserve được ghế để test TC08.2');
    const { seatId: reservedSeatId, seatLabel: reservedLabel } = reserved;

    // === Driver 2 (browser mới = session mới): kiểm tra ghế hiện màu vàng ===
    const driver2 = await buildDriver();
    try {
      await loginUser(driver2);
      await driver2.get(`${BASE_URL}/Events/SelectSeats/${eventId}`);
      await driver2.wait(until.elementLocated(By.css('#seatGrid')), 10000);

      // Tìm ghế vừa được reserve bởi driver1 qua data-seat-id
      const reservedEl = await driver2.wait(
        until.elementLocated(By.css(`[data-seat-id="${reservedSeatId}"]`)),
        8000,
        `Không tìm thấy ghế ${reservedLabel} (data-seat-id=${reservedSeatId})`
      );

      // Ghế phải có class "reserved-other" (màu vàng/cam)
      const classes2 = await reservedEl.getAttribute('class');
      assert.ok(
        classes2.includes('reserved-other'),
        `Ghế "${reservedLabel}" phải có class "reserved-other" khi nhìn từ session khác. Class: "${classes2}"`
      );

      // Click vào ghế reserved-other → KHÔNG được chuyển thành selected
      await reservedEl.click();
      const classesAfterClick = await reservedEl.getAttribute('class');
      assert.ok(
        !classesAfterClick.includes('selected'),
        `Ghế reserved-other không được chuyển sang "selected" khi click. Class: "${classesAfterClick}"`
      );
    } finally {
      await driver2.quit();
    }
  });

  // ── TC08.3: Countdown timer hiển thị khi có ghế đang Reserved ────────────
  it('TC08.3 – Vào lại SelectSeats khi có ghế đang giữ: ghế pre-selected + timer đếm ngược', async function () {
    this.timeout(35000);

    // Bước 1: Reserve ghế (click "Tiếp tục" để lưu vào DB)
    const reserved = await reserveSeatAndGoToCart(driver, eventId);
    assert.ok(reserved, 'Phải reserve được ghế để test TC08.3');

    // Bước 2: Quay lại SelectSeats bằng GET (không phải Release)
    await driver.get(`${BASE_URL}/Events/SelectSeats/${eventId}`);
    await driver.wait(until.elementLocated(By.css('#seatGrid')), 10000);

    // DEBUG: đọc trạng thái server để diagnose session/reservation issue
    try {
      const dbg = await driver.findElement(By.id('dbg-session'));
      const sid      = await dbg.getAttribute('data-sid');
      const res      = await dbg.getAttribute('data-reserved');
      const expires  = await dbg.getAttribute('data-expires');
      console.log(`[DBG TC08.3] sid="${sid}" reserved=${res} expires="${expires}"`);
    } catch(e) { console.log('[DBG TC08.3] dbg-session not found'); }

    // Verify timer container tồn tại và hiển thị
    const timerEl = await driver.wait(
      until.elementLocated(By.id('seatTimer')),
      8000,
      '#seatTimer phải xuất hiện khi user có ghế đang Reserved'
    );
    assert.ok(await timerEl.isDisplayed(), '#seatTimer phải visible');

    // Verify #timerCount hiển thị format MM:SS (chờ JS cập nhật từ --:--)
    const timerCount = await driver.findElement(By.id('timerCount'));
    await driver.wait(async () => {
      const text = await timerCount.getText();
      return /^\d{2}:\d{2}$/.test(text);
    }, 5000, 'timerCount phải hiển thị format MM:SS sau khi JS khởi chạy');

    const t1 = await timerCount.getText();
    assert.match(t1, /^\d{2}:\d{2}$/, `Timer text phải có format MM:SS, nhận được: "${t1}"`);

    // Đợi 3 giây rồi verify timer đã giảm (đang đếm ngược)
    await driver.sleep(3000);
    const t2 = await timerCount.getText();
    assert.notStrictEqual(t1, t2, `Timer phải đang đếm ngược: t1="${t1}", t2="${t2}"`);

    // Verify ghế đang Reserved của mình hiển thị như pre-selected (có class "selected")
    const preSelected = await driver.findElements(By.css('.ss-seat.selected'));
    assert.ok(
      preSelected.length > 0,
      'Ghế đang Reserved phải được pre-select (class "selected") khi vào lại trang'
    );
  });

  // ── TC08.4: Bấm "← Trở về" → release ghế, session khác thấy available ────
  it('TC08.4 – Bấm "← Trở về" (Release) → ghế released, session khác thấy Available trở lại', async function () {
    this.timeout(45000);

    // Bước 1: Reserve ghế
    const reserved = await reserveSeatAndGoToCart(driver, eventId);
    assert.ok(reserved, 'Phải reserve được ghế để test TC08.4');
    const { seatId: releasedSeatId, seatLabel: releasedLabel } = reserved;

    // Bước 2: Quay lại SelectSeats rồi bấm "← Trở về" (form POST → Release handler)
    await driver.get(`${BASE_URL}/Events/SelectSeats/${eventId}`);
    const backBtn = await driver.wait(
      until.elementLocated(By.css('.ss-back')),
      8000,
      'Nút "← Trở về" (.ss-back) phải xuất hiện'
    );

    // Click button → POST /Events/SelectSeats?handler=Release → redirect sang Details
    await backBtn.click();
    await driver.wait(
      until.urlContains('/Events/Details'),
      10000,
      'Phải redirect sang Events/Details sau khi release'
    );

    // Bước 3: Từ session khác, verify ghế đã trở về Available
    const driver2 = await buildDriver();
    try {
      await loginUser(driver2);
      await driver2.get(`${BASE_URL}/Events/SelectSeats/${eventId}`);
      await driver2.wait(until.elementLocated(By.css('#seatGrid')), 10000);

      // Tìm ghế vừa được release
      const seatEl = await driver2.wait(
        until.elementLocated(By.css(`[data-seat-id="${releasedSeatId}"]`)),
        5000,
        `Không tìm thấy ghế ${releasedLabel} sau release`
      );
      const classes = await seatEl.getAttribute('class');

      // Ghế phải không còn là reserved-other hoặc sold
      assert.ok(
        !classes.includes('reserved-other') && !classes.includes('sold'),
        `Ghế "${releasedLabel}" phải về Available sau release, không phải "reserved-other" hay "sold". Class: "${classes}"`
      );
      // Phải có class available (nrm-avail hoặc vip-avail)
      assert.ok(
        classes.includes('avail'),
        `Ghế "${releasedLabel}" phải có class *-avail sau release. Class: "${classes}"`
      );
    } finally {
      await driver2.quit();
    }
  });

  // ── TC08.5: Checkout thành công → ghế chuyển sang Sold (khóa) ────────────
  it('TC08.5 – Checkout thành công → redirect Confirmation, ghế chuyển trạng thái Sold', async function () {
    this.timeout(45000);

    // Bước 1: Reserve ghế
    const reserved = await reserveSeatAndGoToCart(driver, eventId);
    assert.ok(reserved, 'Phải reserve được ghế để test TC08.5');
    const { seatId: soldSeatId, seatLabel: soldLabel } = reserved;

    // Bước 2: Điền form checkout và bấm "Thanh Toán"
    const nameInput = await driver.wait(
      until.elementLocated(By.css('input[name="customerName"]')),
      10000
    );
    await nameInput.clear();
    await nameInput.sendKeys('Test User TC08');
    const emailInput = await driver.findElement(By.css('input[name="customerEmail"]'));
    await emailInput.clear();
    await emailInput.sendKeys('tc08test@example.com');
    await driver.findElement(By.css('#checkoutForm [type="submit"]')).click();

    // Chờ redirect sang Confirmation
    await driver.wait(
      until.urlContains('/Cart/Confirmation'),
      15000,
      'Phải redirect sang /Cart/Confirmation sau khi checkout thành công'
    );
    const confirmUrl = await driver.getCurrentUrl();
    assert.ok(
      confirmUrl.includes('/Cart/Confirmation'),
      `Sau checkout phải ở Confirmation. URL: ${confirmUrl}`
    );

    // Bước 3: Session khác verify ghế đã Sold (class "sold", icon 🔒)
    const driver2 = await buildDriver();
    try {
      await loginUser(driver2);
      await driver2.get(`${BASE_URL}/Events/SelectSeats/${eventId}`);
      await driver2.wait(until.elementLocated(By.css('#seatGrid')), 10000);

      const seatEl = await driver2.wait(
        until.elementLocated(By.css(`[data-seat-id="${soldSeatId}"]`)),
        5000,
        `Không tìm thấy ghế ${soldLabel} (data-seat-id=${soldSeatId}) sau checkout`
      );
      const classes = await seatEl.getAttribute('class');
      assert.ok(
        classes.includes('sold'),
        `Ghế "${soldLabel}" phải có class "sold" sau khi checkout. Class: "${classes}"`
      );
    } finally {
      await driver2.quit();
    }
  });

  // ── TC08.6: Regression VĐ2 — checkout không hiện lỗi "ghế đã bị đặt" ─────
  it('TC08.6 – Regression VĐ2: checkout chính ghế mình đã Reserve → không báo lỗi "đã bị đặt"', async function () {
    this.timeout(35000);

    // Bước 1: Reserve ghế (seat status = Reserved, ReservedBySessionId = session hiện tại)
    const reserved = await reserveSeatAndGoToCart(driver, eventId);
    assert.ok(reserved, 'Phải reserve được ghế để test TC08.6');

    // Bước 2: Điền form và bấm Thanh Toán
    const nameInput = await driver.wait(
      until.elementLocated(By.css('input[name="customerName"]')),
      10000
    );
    await nameInput.clear();
    await nameInput.sendKeys('Regression Test User');
    const emailInput = await driver.findElement(By.css('input[name="customerEmail"]'));
    await emailInput.clear();
    await emailInput.sendKeys('regression@tc08.com');
    await driver.findElement(By.css('#checkoutForm [type="submit"]')).click();

    // Bước 3: Chờ kết quả — Confirmation (thành công) hoặc vẫn ở Cart (có lỗi)
    await driver.wait(async () => {
      const url  = await driver.getCurrentUrl();
      const body = await driver.findElement(By.css('body')).getText();
      // Dừng chờ khi đã về Confirmation hoặc thấy thông báo lỗi
      return url.includes('/Cart/Confirmation') || body.includes('đã bị đặt') || body.includes('không còn khả dụng');
    }, 15000, 'Timeout chờ kết quả checkout');

    // Verify không xuất hiện lỗi "đã bị đặt"
    const bodyText = await driver.findElement(By.css('body')).getText();
    assert.ok(
      !bodyText.includes('đã bị đặt'),
      `BUG VĐ2 còn tồn tại: xuất hiện lỗi "đã bị đặt" khi checkout ghế của chính mình. Body: "${bodyText.substring(0, 400)}"`
    );

    // Verify đã redirect sang Confirmation thành công
    const currentUrl = await driver.getCurrentUrl();
    assert.ok(
      currentUrl.includes('/Cart/Confirmation'),
      `Phải redirect sang Confirmation, URL hiện tại: ${currentUrl}`
    );
  });
});
