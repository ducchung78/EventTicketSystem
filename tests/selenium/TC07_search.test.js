const { Builder, By, until } = require('selenium-webdriver');
const chrome = require('selenium-webdriver/chrome');
const assert = require('assert');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5134';

describe('TC07 - Tìm Kiếm & Lọc Sự Kiện', function () {
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

  it('Tìm kiếm theo từ khóa: input giữ nguyên giá trị và hiển thị kết quả hoặc thông báo trống', async function () {
    await driver.get(`${BASE_URL}/Events?search=nh%E1%BA%A1c`);

    // Dùng input.form-control để tránh nhầm với ô search trong navbar
    const searchInput = await driver.wait(
      until.elementLocated(By.css('input.form-control[name="search"]')),
      10000
    );
    const value = await searchInput.getAttribute('value');
    assert.ok(
      value === 'nhạc',
      `Input search phải giữ giá trị "nhạc", nhận được: "${value}"`
    );

    // Phải hiển thị kết quả sự kiện hoặc thông báo không tìm thấy
    await driver.wait(
      until.elementLocated(By.css('.event-card, .text-muted')),
      10000
    );
    const cards    = await driver.findElements(By.css('.event-card'));
    const emptyMsg = await driver.findElements(By.css('.text-muted'));
    assert.ok(
      cards.length > 0 || emptyMsg.length > 0,
      'Phải hiển thị event card hoặc thông báo không tìm thấy'
    );
  });

  it('Lọc theo danh mục: dropdown giữ nguyên giá trị đã chọn', async function () {
    await driver.get(`${BASE_URL}/Events?category=%C3%82m+nh%E1%BA%A1c`); // category=Âm nhạc

    await driver.wait(until.elementLocated(By.css('select[name="category"]')), 10000);
    const select = await driver.findElement(By.css('select[name="category"]'));

    // Lấy option đang được chọn
    const selectedOption = await driver.executeScript(
      'return arguments[0].options[arguments[0].selectedIndex].value;',
      select
    );
    assert.ok(
      selectedOption === 'Âm nhạc',
      `Danh mục đã chọn phải là "Âm nhạc", nhận được: "${selectedOption}"`
    );

    // Phải hiển thị kết quả hoặc thông báo trống
    await driver.wait(
      until.elementLocated(By.css('.event-card, .text-muted')),
      10000
    );
    const cards = await driver.findElements(By.css('.event-card'));
    const empty = await driver.findElements(By.css('.text-muted'));
    assert.ok(cards.length > 0 || empty.length > 0, 'Phải hiển thị kết quả hoặc thông báo trống');
  });

  it('Nút "Xóa Lọc" hiển thị khi đang áp dụng bộ lọc', async function () {
    await driver.get(`${BASE_URL}/Events?search=concert`);

    await driver.wait(until.elementLocated(By.css('input[name="search"]')), 10000);

    // Khi search có giá trị, nút "Xóa Lọc" (btn-outline-secondary) phải hiển thị
    const clearBtn = await driver.wait(
      until.elementLocated(By.css('a.btn-outline-secondary')),
      10000
    );
    assert.ok(await clearBtn.isDisplayed(), 'Nút "Xóa Lọc" phải hiển thị khi có bộ lọc');
    const href = await clearBtn.getAttribute('href');
    assert.ok(
      href && href.includes('/Events'),
      `Nút "Xóa Lọc" phải trỏ về /Events, nhận được: "${href}"`
    );
  });
});
