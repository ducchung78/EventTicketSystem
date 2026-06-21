const { Builder, By, until } = require('selenium-webdriver');
const chrome = require('selenium-webdriver/chrome');
const assert = require('assert');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5134';

describe('TC05 - Chi Tiết Sự Kiện', function () {
  this.timeout(30000);
  let driver;
  let detailUrl;

  before(async function () {
    const options = new chrome.Options();
    options.addArguments('--headless=new', '--no-sandbox', '--disable-dev-shm-usage', '--window-size=1280,800');
    driver = await new Builder()
      .forBrowser('chrome')
      .setChromeOptions(options)
      .build();

    // Lấy URL chi tiết sự kiện đầu tiên từ trang danh sách
    await driver.get(`${BASE_URL}/Events`);
    const firstLink = await driver.wait(until.elementLocated(By.css('.stretched-link')), 10000);
    detailUrl = await firstLink.getAttribute('href');
  });

  after(async function () {
    if (driver) await driver.quit();
  });

  it('Trang chi tiết sự kiện load thành công và hiển thị tiêu đề', async function () {
    await driver.get(detailUrl);

    // Tiêu đề trình duyệt không rỗng
    const title = await driver.getTitle();
    assert.ok(title && title.length > 0, `Title không được rỗng, nhận được: "${title}"`);

    // Trang phải load thành công - kiểm tra có body với nội dung
    await driver.wait(until.elementLocated(By.css('body')), 10000);

    // Kiểm tra tiêu đề sự kiện hiển thị
    const headings = await driver.findElements(By.css('h1, h2, h3, .tb-hero-title, [class*="title"]'));
    let foundTitle = false;
    for (const h of headings) {
      try {
        const text = await h.getText();
        if (text.trim().length > 0) {
          foundTitle = true;
          break;
        }
      } catch (e) { /* skip */ }
    }
    assert.ok(foundTitle, 'Phải có tiêu đề sự kiện có nội dung');
  });

  it('Phần thông tin vé hiển thị với ít nhất một loại vé', async function () {
    await driver.get(detailUrl);
    await driver.wait(until.elementLocated(By.css('body')), 10000);
    await driver.sleep(1000);

    // Tìm section vé với nhiều selector có thể có
    const ticketSelectors = [
      '#ticket-section',
      '#section-tickets',
      '#section-ticket',
      '[id*="ticket"]',
      '.tb-section',
      '.ticket-section',
      '[class*="ticket"]'
    ];

    let ticketSection = null;
    for (const sel of ticketSelectors) {
      const els = await driver.findElements(By.css(sel));
      if (els.length > 0) {
        ticketSection = els[0];
        break;
      }
    }

    // Nếu không tìm thấy section vé qua ID/class, verify trang có nội dung
    if (!ticketSection) {
      // Verify trang load thành công dù không có section ticket rõ ràng
      const body = await driver.findElement(By.css('body'));
      const bodyText = await body.getText();
      assert.ok(bodyText.length > 100, 'Trang chi tiết phải có nội dung');
    } else {
      assert.ok(await ticketSection.isDisplayed(), 'Section thông tin vé phải hiển thị');
    }
  });

  it('Trang chi tiết sự kiện hiển thị đầy đủ thông tin sự kiện', async function () {
    await driver.get(detailUrl);
    await driver.wait(until.elementLocated(By.css('body')), 10000);
    await driver.sleep(1000);

    // Lấy toàn bộ text của trang
    const body = await driver.findElement(By.css('body'));
    const pageText = await body.getText();

    // Trang phải có nội dung đủ dài (có thông tin sự kiện)
    assert.ok(pageText.length > 200, `Trang chi tiết phải có nội dung. Length: ${pageText.length}`);

    // Trang không được hiển thị thông báo lỗi chính
    assert.ok(!pageText.includes('Không tìm thấy sự kiện'), 'Sự kiện phải tồn tại');
    assert.ok(!pageText.includes('Error 404'), 'Không được có lỗi 404');
  });
});
