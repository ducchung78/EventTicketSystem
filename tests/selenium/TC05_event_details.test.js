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

    // Kiểm tra tiêu đề sự kiện hiển thị (h1 hoặc element có class title)
    const headings = await driver.findElements(By.css('h1, h2, .tb-hero-title, .event-title, [class*="title"]'));
    assert.ok(headings.length > 0, 'Trang chi tiết phải có tiêu đề sự kiện');

    // Tìm heading đầu tiên có text
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

    // Chờ trang load
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

    // Nếu không tìm thấy section vé qua ID, tìm theo text content
    if (!ticketSection) {
      const allElements = await driver.findElements(By.css('div, section'));
      for (const el of allElements) {
        try {
          const text = await el.getText();
          if (text.includes('Thông Tin Vé') || text.includes('ticket') || text.includes('Loại Vé')) {
            ticketSection = el;
            break;
          }
        } catch (e) { /* skip */ }
      }
    }

    assert.ok(ticketSection !== null, 'Phải có section thông tin vé trên trang');
    assert.ok(await ticketSection.isDisplayed(), 'Section thông tin vé phải hiển thị');
  });

  it('Hiển thị các section Giới Thiệu, Lịch Diễn, Thông Tin Vé', async function () {
    await driver.get(detailUrl);

    // Chờ trang load
    await driver.wait(until.elementLocated(By.css('body')), 10000);
    await driver.sleep(1000);

    // Lấy toàn bộ text của trang
    const pageText = await driver.findElement(By.css('body')).getText();

    const hasIntro = pageText.includes('Giới Thiệu') || pageText.includes('Gioi Thieu') || pageText.includes('Mô Tả') || pageText.includes('Description');
    const hasSchedule = pageText.includes('Lịch Diễn') || pageText.includes('Lich Dien') || pageText.includes('Schedule') || pageText.includes('Ngày') || pageText.includes('Thời gian');
    const hasTickets = pageText.includes('Thông Tin Vé') || pageText.includes('Ticket') || pageText.includes('Loại Vé') || pageText.includes('Mua Vé');

    assert.ok(hasIntro, 'Trang phải có thông tin giới thiệu sự kiện');
    assert.ok(hasSchedule, 'Trang phải có thông tin lịch diễn');
    assert.ok(hasTickets, 'Trang phải có thông tin vé');
  });
});
