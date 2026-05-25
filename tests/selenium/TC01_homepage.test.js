const { Builder, By, until } = require('selenium-webdriver');
const chrome = require('selenium-webdriver/chrome');
const assert = require('assert');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5134';

describe('TC01 - Trang Chủ', function () {
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

  it('Trang chủ load thành công, title không rỗng', async function () {
    await driver.get(BASE_URL);
    const title = await driver.getTitle();
    assert.ok(title && title.length > 0, `Title không được rỗng, nhận được: "${title}"`);
  });

  it('Trang chủ hiển thị navbar', async function () {
    await driver.get(BASE_URL);
    const navbar = await driver.wait(until.elementLocated(By.css('nav')), 10000);
    const displayed = await navbar.isDisplayed();
    assert.ok(displayed, 'Navbar phải được hiển thị trên trang chủ');
  });
});
