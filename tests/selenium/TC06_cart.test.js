const { Builder, By, until } = require('selenium-webdriver');
const chrome = require('selenium-webdriver/chrome');
const assert = require('assert');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5134';

describe('TC06 - Giỏ Hàng', function () {
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

  it('Trang giỏ hàng load thành công với tiêu đề đúng', async function () {
    await driver.get(`${BASE_URL}/Cart`);

    const h3 = await driver.wait(until.elementLocated(By.css('h3')), 10000);
    const h3Text = await h3.getText();
    assert.ok(
      h3Text.includes('Giỏ Hàng'),
      `H3 phải chứa "Giỏ Hàng", nhận được: "${h3Text}"`
    );
  });

  it('Giỏ hàng trống hiển thị thông báo và nút khám phá sự kiện', async function () {
    await driver.get(`${BASE_URL}/Cart`);

    // Thông báo "Giỏ hàng trống"
    const emptyCart = await driver.wait(until.elementLocated(By.css('.empty-cart')), 10000);
    assert.ok(await emptyCart.isDisplayed(), 'Khu vực .empty-cart phải hiển thị khi giỏ trống');

    const emptyTitle = await driver.findElement(By.css('.empty-cart h5'));
    const emptyText = await emptyTitle.getText();
    assert.ok(
      emptyText.includes('Giỏ hàng trống'),
      `Phải hiển thị "Giỏ hàng trống", nhận được: "${emptyText}"`
    );
  });

  it('Giỏ hàng trống có link dẫn đến trang sự kiện', async function () {
    await driver.get(`${BASE_URL}/Cart`);

    const exploreLink = await driver.wait(
      until.elementLocated(By.css('.empty-cart .btn-buy-now')),
      10000
    );
    assert.ok(await exploreLink.isDisplayed(), 'Nút "Khám phá sự kiện" phải hiển thị');

    const href = await exploreLink.getAttribute('href');
    assert.ok(
      href && href.includes('/Events'),
      `Link phải trỏ tới /Events, nhận được: "${href}"`
    );
  });
});
