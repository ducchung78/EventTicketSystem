const { Builder, By, until } = require('selenium-webdriver');
const chrome = require('selenium-webdriver/chrome');
const assert = require('assert');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5134';

describe('TC04 - Đăng Ký Tài Khoản', function () {
  this.timeout(30000);
  let driver;

  function buildDriver() {
    const options = new chrome.Options();
    options.addArguments('--headless=new', '--no-sandbox', '--disable-dev-shm-usage', '--window-size=1280,800');
    return new Builder().forBrowser('chrome').setChromeOptions(options).build();
  }

  // Mỗi test dùng driver riêng để tránh ảnh hưởng state đăng nhập
  beforeEach(async function () { driver = await buildDriver(); });
  afterEach(async function () { if (driver) await driver.quit(); });

  it('Trang đăng ký hiển thị form với đầy đủ các trường bắt buộc', async function () {
    await driver.get(`${BASE_URL}/Account/Register`);

    // ASP.NET Core renders asp-for="Input.Ho" → id="Input_Ho"
    const fieldIds = ['Input_Ho', 'Input_Ten', 'Input_UserName', 'Input_Email', 'regPassword', 'regConfirm'];
    for (const id of fieldIds) {
      const el = await driver.wait(until.elementLocated(By.id(id)), 10000);
      assert.ok(await el.isDisplayed(), `Trường #${id} phải hiển thị`);
    }

    const submitBtn = await driver.findElement(By.css('button.tb-btn-continue'));
    assert.ok(await submitBtn.isDisplayed(), 'Nút Đăng Ký phải hiển thị');
  });

  it('Đăng ký thành công với tài khoản mới và redirect khỏi trang register', async function () {
    const ts = Date.now();
    await driver.get(`${BASE_URL}/Account/Register`);
    await driver.wait(until.elementLocated(By.id('Input_Ho')), 10000);

    await driver.findElement(By.id('Input_Ho')).sendKeys('Nguyễn');
    await driver.findElement(By.id('Input_Ten')).sendKeys('Dùng Thử');
    await driver.findElement(By.id('Input_UserName')).sendKeys(`testuser${ts}`);
    await driver.findElement(By.id('Input_Email')).sendKeys(`testuser${ts}@example.com`);
    await driver.findElement(By.id('regPassword')).sendKeys('Test@123456');
    await driver.findElement(By.id('regConfirm')).sendKeys('Test@123456');

    // Scroll button vào viewport trước khi click (form dài, button có thể nằm ngoài màn hình)
    const submitBtn = await driver.findElement(By.css('button.tb-btn-continue'));
    await driver.executeScript('arguments[0].scrollIntoView({block:"center"});', submitBtn);
    await driver.executeScript('arguments[0].click();', submitBtn);

    await driver.wait(async () => {
      const url = await driver.getCurrentUrl();
      return !url.includes('/Account/Register');
    }, 10000, 'Timeout: Không redirect sau khi đăng ký thành công');

    const url = await driver.getCurrentUrl();
    assert.ok(
      !url.includes('/Account/Register'),
      `Sau đăng ký phải redirect khỏi trang register, URL hiện tại: ${url}`
    );
  });

  it('Hiển thị lỗi khi đăng ký với tên đăng nhập đã tồn tại trong hệ thống', async function () {
    // UserName "admin@tickethub.vn" đã được seed sẵn → Identity sẽ từ chối
    await driver.get(`${BASE_URL}/Account/Register`);
    await driver.wait(until.elementLocated(By.id('Input_Ho')), 10000);

    await driver.findElement(By.id('Input_Ho')).sendKeys('Test');
    await driver.findElement(By.id('Input_Ten')).sendKeys('Trùng Username');
    await driver.findElement(By.id('Input_UserName')).sendKeys('admin@tickethub.vn'); // username đã tồn tại
    await driver.findElement(By.id('Input_Email')).sendKeys(`unique${Date.now()}@example.com`);
    await driver.findElement(By.id('regPassword')).sendKeys('Test@123456');
    await driver.findElement(By.id('regConfirm')).sendKeys('Test@123456');

    // Scroll button vào viewport trước khi click
    const submitBtn = await driver.findElement(By.css('button.tb-btn-continue'));
    await driver.executeScript('arguments[0].scrollIntoView({block:"center"});', submitBtn);
    await driver.executeScript('arguments[0].click();', submitBtn);

    // Chờ page re-render với lỗi (Input_Ho vẫn có mặt trên trang)
    await driver.wait(until.elementLocated(By.id('Input_Ho')), 10000);

    // Xác nhận vẫn ở trang đăng ký (đăng ký thất bại = không redirect)
    const url = await driver.getCurrentUrl();
    assert.ok(
      url.includes('/Account/Register'),
      `Phải ở lại trang đăng ký khi username đã tồn tại, URL: ${url}`
    );

    // Lỗi được add vào field-level (Input.UserName), hiển thị qua asp-validation-for
    // TagHelper renders span có data-valmsg-for="Input.UserName" với text lỗi
    const userNameError = await driver.findElement(
      By.css('span[data-valmsg-for="Input.UserName"]')
    );
    const errorText = await userNameError.getText();
    assert.ok(
      errorText.trim().length > 0,
      `Phải hiển thị lỗi cạnh trường Username, nhận được: "${errorText}"`
    );
  });
});
