const { Builder, By, until } = require('selenium-webdriver');
const chrome = require('selenium-webdriver/chrome');
const assert = require('assert');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5134';

describe('TC02 - Đăng Nhập', function () {
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

  it('Trang đăng nhập hiển thị form với đầy đủ các input', async function () {
    await driver.get(`${BASE_URL}/Account/Login`);
    const emailInput = await driver.wait(until.elementLocated(By.id('tbEmail')), 10000);
    const passwordInput = await driver.findElement(By.id('tbPassword'));
    assert.ok(await emailInput.isDisplayed(), 'Input email phải hiển thị');
    assert.ok(await passwordInput.isDisplayed(), 'Input password phải hiển thị');
  });

  it('Đăng nhập thành công với tài khoản demo admin', async function () {
    await driver.get(`${BASE_URL}/Account/Login`);
    const emailInput = await driver.wait(until.elementLocated(By.id('tbEmail')), 10000);
    await emailInput.clear();
    await emailInput.sendKeys('admin@tickethub.vn');

    const passwordInput = await driver.findElement(By.id('tbPassword'));
    await passwordInput.clear();
    await passwordInput.sendKeys('Admin@123456');

    await driver.findElement(By.css('#loginForm [type="submit"]')).click();

    // Chờ redirect khỏi trang login
    await driver.wait(async () => {
      const url = await driver.getCurrentUrl();
      return !url.includes('/Account/Login');
    }, 10000, 'Timeout: Không redirect sau khi đăng nhập');

    const currentUrl = await driver.getCurrentUrl();
    assert.ok(
      !currentUrl.includes('/Account/Login'),
      `Sau đăng nhập phải redirect khỏi trang login, URL hiện tại: ${currentUrl}`
    );
  });
});
