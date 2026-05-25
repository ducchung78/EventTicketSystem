const axios = require('axios');
const fs = require('fs');
const path = require('path');

const JIRA_BASE_URL = process.env.JIRA_BASE_URL;
const JIRA_EMAIL = process.env.JIRA_EMAIL;
const JIRA_API_TOKEN = process.env.JIRA_API_TOKEN;
const JIRA_PROJECT_KEY = process.env.JIRA_PROJECT_KEY;

const RESULTS_FILE = path.join(process.cwd(), 'test-results', 'results.json');

function buildHeaders() {
  const token = Buffer.from(`${JIRA_EMAIL}:${JIRA_API_TOKEN}`).toString('base64');
  return {
    Authorization: `Basic ${token}`,
    'Content-Type': 'application/json',
    Accept: 'application/json',
  };
}

// Tìm issue đã tồn tại dựa vào title của test
async function searchExistingIssue(testTitle) {
  // Escape ký tự đặc biệt trong JQL
  const safeTitle = testTitle.replace(/["\[\]\\]/g, ' ').trim();
  const jql = `project = "${JIRA_PROJECT_KEY}" AND summary ~ "${safeTitle}" AND labels = "automated-test" ORDER BY created DESC`;
  const url = `${JIRA_BASE_URL}/rest/api/3/search?jql=${encodeURIComponent(jql)}&maxResults=1`;

  const res = await axios.get(url, { headers: buildHeaders() });
  return res.data.issues?.[0] || null;
}

// Tạo issue mới loại Bug
async function createIssue(testTitle, errorMessage, fullTitle) {
  const url = `${JIRA_BASE_URL}/rest/api/3/issue`;
  const now = new Date().toISOString();

  const body = {
    fields: {
      project: { key: JIRA_PROJECT_KEY },
      summary: `[Auto-Test FAIL] ${testTitle}`,
      description: {
        type: 'doc',
        version: 1,
        content: [
          {
            type: 'paragraph',
            content: [{ type: 'text', text: `Test case thất bại: ${fullTitle}` }],
          },
          {
            type: 'paragraph',
            content: [{ type: 'text', text: `Lỗi: ${errorMessage}` }],
          },
          {
            type: 'paragraph',
            content: [{ type: 'text', text: `Thời gian phát hiện: ${now}` }],
          },
        ],
      },
      issuetype: { name: 'Bug' },
      labels: ['automated-test'],
    },
  };

  const res = await axios.post(url, body, { headers: buildHeaders() });
  return res.data.key;
}

// Thêm comment vào issue đã tồn tại
async function addComment(issueKey, errorMessage) {
  const url = `${JIRA_BASE_URL}/rest/api/3/issue/${issueKey}/comment`;
  const now = new Date().toISOString();

  const body = {
    body: {
      type: 'doc',
      version: 1,
      content: [
        {
          type: 'paragraph',
          content: [
            { type: 'text', text: `[Auto-Test] Test tiếp tục thất bại lúc ${now}` },
          ],
        },
        {
          type: 'paragraph',
          content: [{ type: 'text', text: `Lỗi: ${errorMessage}` }],
        },
      ],
    },
  };

  await axios.post(url, body, { headers: buildHeaders() });
}

async function main() {
  // Kiểm tra biến môi trường
  if (!JIRA_BASE_URL || !JIRA_EMAIL || !JIRA_API_TOKEN || !JIRA_PROJECT_KEY) {
    console.log('Thiếu biến môi trường Jira (JIRA_BASE_URL, JIRA_EMAIL, JIRA_API_TOKEN, JIRA_PROJECT_KEY).');
    console.log('Bỏ qua việc tạo Jira issue.');
    process.exit(0);
  }

  // Kiểm tra file kết quả test
  if (!fs.existsSync(RESULTS_FILE)) {
    console.log(`Không tìm thấy file ${RESULTS_FILE}. Bỏ qua.`);
    process.exit(0);
  }

  const results = JSON.parse(fs.readFileSync(RESULTS_FILE, 'utf-8'));
  const failures = results.failures || [];

  if (failures.length === 0) {
    console.log(`Tất cả ${results.stats?.passes || 0} test đều pass. Không cần tạo Jira issue.`);
    process.exit(0);
  }

  console.log(`Tìm thấy ${failures.length} test thất bại. Đang xử lý Jira...`);

  for (const failure of failures) {
    const testTitle = failure.title;
    const fullTitle = failure.fullTitle;
    const errorMessage = failure.err?.message || 'Không có mô tả lỗi';

    console.log(`\nXử lý: "${fullTitle}"`);

    try {
      const existing = await searchExistingIssue(testTitle);

      if (existing) {
        console.log(`  Issue đã tồn tại: ${existing.key} - "${existing.fields.summary}"`);
        await addComment(existing.key, errorMessage);
        console.log(`  Đã thêm comment vào ${existing.key}`);
      } else {
        const newKey = await createIssue(testTitle, errorMessage, fullTitle);
        console.log(`  Đã tạo issue mới: ${newKey}`);
      }
    } catch (err) {
      const detail = err.response?.data
        ? JSON.stringify(err.response.data)
        : err.message;
      console.error(`  Lỗi khi xử lý Jira cho "${testTitle}": ${detail}`);
    }
  }

  console.log('\nHoàn tất xử lý Jira.');
}

main();
