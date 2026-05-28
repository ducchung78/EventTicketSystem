const axios = require('axios');
const fs = require('fs');
const path = require('path');
const FormData = require('form-data');

const JIRA_BASE_URL = process.env.JIRA_BASE_URL;
const JIRA_EMAIL = process.env.JIRA_EMAIL;
const JIRA_API_TOKEN = process.env.JIRA_API_TOKEN;
const JIRA_PROJECT_KEY = process.env.JIRA_PROJECT_KEY;

const RESULTS_FILE = path.join(process.cwd(), 'test-results', 'results.json');
const LOGS_DIR = path.join(process.cwd(), 'test-results', 'logs');

function buildHeaders() {
  const token = Buffer.from(`${JIRA_EMAIL}:${JIRA_API_TOKEN}`).toString('base64');
  return {
    Authorization: `Basic ${token}`,
    'Content-Type': 'application/json',
    Accept: 'application/json',
  };
}

function extractTestcaseId(fullTitle) {
  const match = fullTitle.match(/TC(\d+)/i);
  return match ? `TC-${match[1].padStart(2, '0')}` : 'TC-XX';
}

function determinePriority(fullTitle) {
  const match = fullTitle.match(/TC(\d+)/i);
  if (!match) return 'Medium';
  const num = parseInt(match[1], 10);
  if (num <= 2) return 'High';
  if (num <= 5) return 'Medium';
  return 'Low';
}

async function searchExistingIssue(testTitle) {
  const safeTitle = testTitle.replace(/["\[\]\\]/g, ' ').trim().substring(0, 60);
  const jql = `project = "${JIRA_PROJECT_KEY}" AND summary ~ "${safeTitle}" AND labels = "automated-test" ORDER BY created DESC`;
  const url = `${JIRA_BASE_URL}/rest/api/3/search?jql=${encodeURIComponent(jql)}&maxResults=1`;
  try {
    const res = await axios.get(url, { headers: buildHeaders() });
    return res.data.issues?.[0] || null;
  } catch (e) {
    return null;
  }
}

async function createIssue(summary, errorMessage, fullTitle, testcaseId, priority) {
  const url = `${JIRA_BASE_URL}/rest/api/3/issue`;
  const now = new Date().toISOString();
  const body = {
    fields: {
      project: { key: JIRA_PROJECT_KEY },
      summary: summary,
      description: {
        type: 'doc',
        version: 1,
        content: [
          { type: 'paragraph', content: [{ type: 'text', text: `Test case that bai: ${fullTitle}`, marks: [{ type: 'strong' }] }] },
          { type: 'paragraph', content: [{ type: 'text', text: `Loi: ${errorMessage}` }] },
          { type: 'paragraph', content: [{ type: 'text', text: `Testcase ID: ${testcaseId}` }] },
          { type: 'paragraph', content: [{ type: 'text', text: `Priority: ${priority}` }] },
          { type: 'paragraph', content: [{ type: 'text', text: `Thoi gian phat hien: ${now}` }] },
          { type: 'paragraph', content: [{ type: 'text', text: 'Lan loi: 1' }] },
        ],
      },
      issuetype: { name: 'Bug' },
      labels: ['automated-test', testcaseId],
      priority: { name: priority },
    },
  };
  const res = await axios.post(url, body, { headers: buildHeaders() });
  return res.data.key;
}

async function updateExistingIssue(issueKey, errorMessage, failCount) {
  const now = new Date().toISOString();
  const commentUrl = `${JIRA_BASE_URL}/rest/api/3/issue/${issueKey}/comment`;
  const body = {
    body: {
      type: 'doc',
      version: 1,
      content: [
        { type: 'paragraph', content: [{ type: 'text', text: `[Auto-Test] Test tiep tuc that bai luc ${now}`, marks: [{ type: 'strong' }] }] },
        { type: 'paragraph', content: [{ type: 'text', text: `Loi: ${errorMessage}` }] },
        { type: 'paragraph', content: [{ type: 'text', text: `Lan loi: ${failCount}` }] },
        { type: 'paragraph', content: [{ type: 'text', text: `Cap nhat lan cuoi: ${now}` }] },
      ],
    },
  };
  await axios.post(commentUrl, body, { headers: buildHeaders() });
}

async function attachLogFile(issueKey, logFilePath) {
  if (!fs.existsSync(logFilePath)) return;
  const attachUrl = `${JIRA_BASE_URL}/rest/api/3/issue/${issueKey}/attachments`;
  const token = Buffer.from(`${JIRA_EMAIL}:${JIRA_API_TOKEN}`).toString('base64');
  const form = new FormData();
  form.append('file', fs.createReadStream(logFilePath));
  try {
    await axios.post(attachUrl, form, {
      headers: {
        Authorization: `Basic ${token}`,
        'X-Atlassian-Token': 'no-check',
        ...form.getHeaders(),
      },
    });
    console.log(`  Attached log: ${path.basename(logFilePath)}`);
  } catch (e) {
    console.log(`  Warning: Could not attach log: ${e.message}`);
  }
}

function writeLogFile(testTitle, errorMessage, fullTitle, testcaseId) {
  if (!fs.existsSync(LOGS_DIR)) fs.mkdirSync(LOGS_DIR, { recursive: true });
  const safeName = testcaseId.replace(/[^a-zA-Z0-9_-]/g, '_');
  const logPath = path.join(LOGS_DIR, `${safeName}.log`);
  const now = new Date().toISOString();
  const content = [
    `Testcase ID : ${testcaseId}`,
    `Title       : ${fullTitle}`,
    `Status      : FAILED`,
    `Timestamp   : ${now}`,
    '',
    'Error:',
    errorMessage,
  ].join('\n');
  fs.writeFileSync(logPath, content, 'utf-8');
  return logPath;
}

async function main() {
  if (!JIRA_BASE_URL || !JIRA_EMAIL || !JIRA_API_TOKEN || !JIRA_PROJECT_KEY) {
    console.log('Thieu bien moi truong Jira. Bo qua.');
    process.exit(0);
  }
  if (!fs.existsSync(RESULTS_FILE)) {
    console.log(`Khong tim thay file ${RESULTS_FILE}. Bo qua.`);
    process.exit(0);
  }
  const results = JSON.parse(fs.readFileSync(RESULTS_FILE, 'utf-8'));
  const failures = results.failures || [];
  if (failures.length === 0) {
    console.log(`Tat ca ${results.stats?.passes || 0} test deu pass. Khong can tao Jira issue.`);
    process.exit(0);
  }
  console.log(`Tim thay ${failures.length} test that bai. Dang xu ly Jira...`);
  for (const failure of failures) {
    const fullTitle = failure.fullTitle || failure.title;
    const testTitle = failure.title;
    const errorMessage = (failure.err?.message || failure.err || 'Unknown error').toString().substring(0, 500);
    const testcaseId = extractTestcaseId(fullTitle);
    const priority = determinePriority(fullTitle);
    const jiraSummary = `[Auto-Test FAIL] ${testTitle}`;
    console.log(`\nProcessing: ${testcaseId} - ${testTitle}`);
    try {
      const logPath = writeLogFile(testTitle, errorMessage, fullTitle, testcaseId);
      const existing = await searchExistingIssue(testTitle);
      if (existing) {
        const issueKey = existing.key;
        const commentsRes = await axios.get(
          `${JIRA_BASE_URL}/rest/api/3/issue/${issueKey}/comment`,
          { headers: buildHeaders() }
        );
        const failCount = (commentsRes.data.total || 0) + 2;
        await updateExistingIssue(issueKey, errorMessage, failCount);
        await attachLogFile(issueKey, logPath);
        console.log(`  Updated: ${issueKey} (lan loi: ${failCount})`);
      } else {
        const issueKey = await createIssue(jiraSummary, errorMessage, fullTitle, testcaseId, priority);
        await attachLogFile(issueKey, logPath);
        console.log(`  Created: ${issueKey}`);
      }
    } catch (e) {
      console.error(`  Error processing ${testcaseId}: ${e.message}`);
    }
  }
  console.log('\nJira reporter done.');
}

main();
