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

    // Sidebar tóm tắt sự kiện phải hiển thị
    const summaryCard = await driver.wait(until.elementLocated(By.css('.summary-card')), 10000);
    assert.ok(await summaryCard.isDisplayed(), '.summary-card (sidebar) phải hiển thị');

    // Tiêu đề sự kiện trong sidebar phải có nội dung
    const summaryTitle = await driver.findElement(By.css('.summary-title'));
    const titleText = await summaryTitle.getText();
    assert.ok(titleText.trim().length > 0, 'Tiêu đề sự kiện trong sidebar phải có nội dung');
  });

  it('Phần thông tin vé hiển thị với ít nhất một loại vé', async function () {
    await driver.get(detailUrl);

    // Section "Thông Tin Vé" phải tồn tại
    const ticketSection = await driver.wait(until.elementLocated(By.id('ticket-section')), 10000);
    assert.ok(await ticketSection.isDisplayed(), '#ticket-section phải hiển thị');

    // Phải có ít nhất một dòng vé (.tkt-row)
    const ticketRows = await driver.findElements(By.css('.tkt-row'));
    assert.ok(ticketRows.length > 0, 'Phải có ít nhất một loại vé trong #ticket-section');
  });

  it('Hiển thị các section Giới Thiệu, Lịch Diễn, Thông Tin Vé', async function () {
    await driver.get(detailUrl);
    await driver.wait(until.elementLocated(By.css('.sec-title')), 10000);

    const secTitles = await driver.findElements(By.css('.sec-title'));
    const texts = await Promise.all(secTitles.map(el => el.getText()));

    const hasIntro    = texts.some(t => t.includes('Giới Thiệu'));
    const hasSchedule = texts.some(t => t.includes('Lịch Diễn'));
    const hasTickets  = texts.some(t => t.includes('Thông Tin Vé'));

    assert.ok(hasIntro,    'Phải có section "Giới Thiệu"');
    assert.ok(hasSchedule, 'Phải có section "Lịch Diễn"');
    assert.ok(hasTickets,  'Phải có section "Thông Tin Vé"');
  });
});
