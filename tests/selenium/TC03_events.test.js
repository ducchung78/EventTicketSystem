const { Builder, By, until } = require('selenium-webdriver');
const chrome = require('selenium-webdriver/chrome');
const assert = require('assert');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5134';

describe('TC03 - Danh Sách Sự Kiện', function () {
  this.timeout(30000);
  let driver;

  before(async function () {
    const options = new chrome.Options();
    options.addArguments('--headless=new', '--no-sandbox', '--disable-dev-shm-usage', '--window-size=1280,800');
    driver = await new Builder()
      .forBrowser('chrome')
      .setChromeOptions(options)
      .build();
  });

  after(async function () {
    if (driver) await driver.quit();
  });

  it('Trang sự kiện load thành công với heading đúng', async function () {
    await driver.get(`${BASE_URL}/Events`);
    const h1 = await driver.wait(until.elementLocated(By.css('h1')), 10000);
    const text = await h1.getText();
    assert.ok(text.includes('Sự Kiện'), `H1 phải chứa "Sự Kiện", nhận được: "${text}"`);
  });

  it('Trang sự kiện hiển thị event card hoặc thông báo trống', async function () {
    await driver.get(`${BASE_URL}/Events`);
    // Chờ đến khi có event-card hoặc thông báo "Không tìm thấy"
    await driver.wait(
      until.elementLocated(By.css('.event-card, .text-muted')),
      10000
    );
    const cards = await driver.findElements(By.css('.event-card'));
    const emptyMsg = await driver.findElements(By.css('.text-muted'));
    assert.ok(
      cards.length > 0 || emptyMsg.length > 0,
      'Phải hiển thị danh sách sự kiện hoặc thông báo không có sự kiện'
    );
  });

  it('Thanh tìm kiếm sự kiện hiển thị', async function () {
    await driver.get(`${BASE_URL}/Events`);
    const searchInput = await driver.wait(
      until.elementLocated(By.css('input[name="search"]')),
      10000
    );
    assert.ok(await searchInput.isDisplayed(), 'Thanh tìm kiếm phải hiển thị');
  });
});
