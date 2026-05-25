'use strict';
const fs   = require('fs');
const path = require('path');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  HeadingLevel, AlignmentType, WidthType, BorderStyle,
  ShadingType, TableLayoutType, convertInchesToTwip
} = require('docx');

// ──────────────────────────────────────────────
// Config
// ──────────────────────────────────────────────
const FONT   = 'Times New Roman';
const SZ     = 24;          // half-points → 12pt
const SZ_H1  = 36;          // 18pt
const SZ_H2  = 28;          // 14pt
const SZ_H3  = 26;          // 13pt
const COLOR_H1 = '1F3864';  // dark blue
const COLOR_H2 = '2E4A7A';
const COLOR_H3 = '365F91';
const COLOR_TH = 'D9E2F3';  // table header fill
const COLOR_TC = 'F5F8FF';  // test-case table alt row

// ──────────────────────────────────────────────
// Inline markdown → TextRun[]
// ──────────────────────────────────────────────
function parseInline(text, baseOptions = {}) {
  const runs = [];
  // Regex: **bold**, `code`, plain
  const re = /\*\*(.+?)\*\*|`([^`]+)`/g;
  let last = 0, m;
  while ((m = re.exec(text)) !== null) {
    if (m.index > last) {
      runs.push(new TextRun({ text: text.slice(last, m.index), font: FONT, size: SZ, ...baseOptions }));
    }
    if (m[1] !== undefined) {
      runs.push(new TextRun({ text: m[1], bold: true, font: FONT, size: SZ, ...baseOptions }));
    } else if (m[2] !== undefined) {
      runs.push(new TextRun({ text: m[2], font: 'Courier New', size: SZ - 2, ...baseOptions }));
    }
    last = re.lastIndex;
  }
  if (last < text.length) {
    runs.push(new TextRun({ text: text.slice(last), font: FONT, size: SZ, ...baseOptions }));
  }
  return runs.length ? runs : [new TextRun({ text: '', font: FONT, size: SZ })];
}

// ──────────────────────────────────────────────
// Cell helpers
// ──────────────────────────────────────────────
function makeCell(text, { isHeader = false, shade = null, widthPct = null } = {}) {
  const cellOpts = {
    children: [
      new Paragraph({
        children: parseInline(text.trim(), isHeader ? { bold: true } : {}),
        spacing: { before: 40, after: 40 },
        alignment: AlignmentType.LEFT,
      })
    ],
    borders: {
      top:    { style: BorderStyle.SINGLE, size: 4, color: '8EAADB' },
      bottom: { style: BorderStyle.SINGLE, size: 4, color: '8EAADB' },
      left:   { style: BorderStyle.SINGLE, size: 4, color: '8EAADB' },
      right:  { style: BorderStyle.SINGLE, size: 4, color: '8EAADB' },
    },
    margins: { top: 60, bottom: 60, left: 80, right: 80 },
  };
  if (isHeader) {
    cellOpts.shading = { type: ShadingType.SOLID, fill: COLOR_TH };
  } else if (shade) {
    cellOpts.shading = { type: ShadingType.SOLID, fill: shade };
  }
  if (widthPct) {
    cellOpts.width = { size: widthPct, type: WidthType.PERCENTAGE };
  }
  return new TableCell(cellOpts);
}

// ──────────────────────────────────────────────
// Parse markdown table rows → string[][]
// ──────────────────────────────────────────────
function parseMdTableRow(line) {
  return line.replace(/^\||\|$/g, '').split('|').map(c => c.trim());
}

function isSeparatorRow(line) {
  return /^\|[\s\-:|]+\|$/.test(line.trim());
}

// ──────────────────────────────────────────────
// Build Word Table from raw rows (string[][])
// first row = header
// ──────────────────────────────────────────────
function buildTable(rows) {
  if (!rows.length) return null;

  // Detect test-case table by header
  const headerCells = rows[0];
  const isTestCase  = headerCells.some(h => /Kết quả mong đợi|Mô tả|Tiêu đề/i.test(h));

  const colCount = headerCells.length;
  // Percentage widths – try to distribute sensibly
  const pcts = distributeWidths(headerCells, isTestCase);

  const wordRows = rows.map((cells, ri) => {
    const isHeader = ri === 0;
    const isAlt    = !isHeader && ri % 2 === 0;
    const shade    = isAlt ? 'EEF3FB' : null;

    // pad or trim cells to colCount
    const padded = Array.from({ length: colCount }, (_, i) => cells[i] ?? '');
    return new TableRow({
      tableHeader: isHeader,
      children: padded.map((c, ci) =>
        makeCell(c, { isHeader, shade, widthPct: pcts[ci] })
      ),
    });
  });

  return new Table({
    rows: wordRows,
    width: { size: 100, type: WidthType.PERCENTAGE },
    layout: TableLayoutType.FIXED,
    margins: { top: 0, bottom: 0, left: 0, right: 0 },
  });
}

function distributeWidths(headers, isTestCase) {
  const n = headers.length;
  if (n === 0) return [];
  // Test-case table: ID(5), Tiêu đề(15), Mô tả(30), KQ mong đợi(22), KQ thực tế(14), Ghi chú(14)
  if (isTestCase && n === 6) return [5, 15, 30, 22, 14, 14];
  if (isTestCase && n === 5) return [5, 20, 35, 25, 15];
  // Decision table – spread equally
  const base = Math.floor(100 / n);
  const pcts = Array(n).fill(base);
  pcts[pcts.length - 1] += 100 - base * n;
  return pcts;
}

// ──────────────────────────────────────────────
// Convert blockquote block → Paragraph[]
// ──────────────────────────────────────────────
function makeBlockquote(lines) {
  const paragraphs = [];
  for (const line of lines) {
    const text = line.replace(/^>\s*/, '');
    if (/^[-*]\s+/.test(text)) {
      const content = text.replace(/^[-*]\s+/, '');
      paragraphs.push(new Paragraph({
        children: parseInline(content, { italics: true }),
        bullet:   { level: 0 },
        indent:   { left: convertInchesToTwip(0.4) },
        spacing:  { before: 30, after: 30 },
        font: FONT, size: SZ,
      }));
    } else if (text.trim()) {
      paragraphs.push(new Paragraph({
        children: parseInline(text, { italics: true }),
        indent:   { left: convertInchesToTwip(0.35) },
        spacing:  { before: 30, after: 30 },
        border:   { left: { style: BorderStyle.SINGLE, size: 12, color: '4472C4', space: 6 } },
      }));
    }
  }
  return paragraphs;
}

// ──────────────────────────────────────────────
// Main parser
// ──────────────────────────────────────────────
function mdToDocx(mdText) {
  const lines = mdText.split('\n');
  const children = [];

  // Add cover info (before parsing)
  children.push(new Paragraph({
    children: [new TextRun({ text: '', font: FONT, size: SZ })],
    spacing: { before: 0, after: 200 },
  }));

  let i = 0;
  while (i < lines.length) {
    const raw = lines[i];
    const line = raw.trimEnd();

    // ── Heading 1 ──
    if (/^#\s+/.test(line) && !/^##/.test(line)) {
      const text = line.replace(/^#\s+/, '');
      children.push(new Paragraph({
        children: [new TextRun({ text, bold: true, font: FONT, size: SZ_H1, color: COLOR_H1 })],
        heading:  HeadingLevel.HEADING_1,
        alignment: AlignmentType.CENTER,
        spacing:  { before: 400, after: 200 },
        border: {
          bottom: { style: BorderStyle.THICK, size: 8, color: '1F3864', space: 4 }
        },
      }));
      i++; continue;
    }

    // ── Heading 2 ──
    if (/^##\s+/.test(line) && !/^###/.test(line)) {
      const text = line.replace(/^##\s+/, '');
      // section separator before each feature
      children.push(new Paragraph({
        children: [new TextRun({ text: '', font: FONT, size: 4 })],
        spacing: { before: 200, after: 0 },
        border: { top: { style: BorderStyle.SINGLE, size: 6, color: '2E4A7A', space: 4 } },
      }));
      children.push(new Paragraph({
        children: [new TextRun({ text, bold: true, font: FONT, size: SZ_H2, color: COLOR_H2 })],
        heading:  HeadingLevel.HEADING_2,
        spacing:  { before: 160, after: 120 },
        shading:  { type: ShadingType.SOLID, fill: 'D9E2F3' },
      }));
      i++; continue;
    }

    // ── Heading 3 ──
    if (/^###\s+/.test(line) && !/^####/.test(line)) {
      const text = line.replace(/^###\s+/, '');
      children.push(new Paragraph({
        children: [new TextRun({ text, bold: true, font: FONT, size: SZ_H3, color: COLOR_H3 })],
        heading:  HeadingLevel.HEADING_3,
        spacing:  { before: 200, after: 80 },
      }));
      i++; continue;
    }

    // ── Horizontal rule (---) ──
    if (/^---+$/.test(line.trim())) {
      children.push(new Paragraph({
        children: [new TextRun({ text: '', font: FONT, size: 4 })],
        spacing:  { before: 80, after: 80 },
        border:   { bottom: { style: BorderStyle.SINGLE, size: 4, color: 'AAAAAA', space: 4 } },
      }));
      i++; continue;
    }

    // ── Blockquote block ──
    if (/^>\s*/.test(line)) {
      const bqLines = [];
      while (i < lines.length && /^>\s*/.test(lines[i])) {
        bqLines.push(lines[i]);
        i++;
      }
      // Box border around blockquote
      const bqParas = makeBlockquote(bqLines);
      children.push(...bqParas);
      continue;
    }

    // ── Markdown table ──
    if (/^\|/.test(line)) {
      const tableRows = [];
      while (i < lines.length && /^\|/.test(lines[i].trim())) {
        const l = lines[i].trim();
        if (!isSeparatorRow(l)) {
          tableRows.push(parseMdTableRow(l));
        }
        i++;
      }
      if (tableRows.length > 0) {
        const tbl = buildTable(tableRows);
        if (tbl) {
          children.push(tbl);
          // spacing after table
          children.push(new Paragraph({
            children: [new TextRun({ text: '', font: FONT, size: SZ })],
            spacing: { before: 80, after: 80 },
          }));
        }
      }
      continue;
    }

    // ── Ordered list item ──
    if (/^\d+\.\s+/.test(line)) {
      const text = line.replace(/^\d+\.\s+/, '');
      children.push(new Paragraph({
        children: parseInline(text),
        numbering: { reference: 'my-numbering', level: 0 },
        spacing:   { before: 40, after: 40 },
      }));
      i++; continue;
    }

    // ── Unordered list item ──
    if (/^[-*]\s+/.test(line)) {
      const text = line.replace(/^[-*]\s+/, '');
      children.push(new Paragraph({
        children: parseInline(text),
        bullet:   { level: 0 },
        spacing:  { before: 40, after: 40 },
      }));
      i++; continue;
    }

    // ── Empty line → spacing ──
    if (!line.trim()) {
      children.push(new Paragraph({
        children: [new TextRun({ text: '', font: FONT, size: SZ })],
        spacing: { before: 0, after: 60 },
      }));
      i++; continue;
    }

    // ── Regular paragraph ──
    const cleaned = line.replace(/\s{2,}$/, ''); // strip trailing double-space
    children.push(new Paragraph({
      children: parseInline(cleaned),
      spacing:  { before: 40, after: 60 },
    }));
    i++;
  }

  return children;
}

// ──────────────────────────────────────────────
// Build & save document
// ──────────────────────────────────────────────
async function main() {
  const mdPath  = path.join(__dirname, 'testcase_all.md');
  const outPath = path.join(__dirname, 'testcase_all.docx');

  console.log('Reading markdown file...');
  const mdText = fs.readFileSync(mdPath, 'utf-8');

  console.log('Converting to Word elements...');
  const docChildren = mdToDocx(mdText);

  const doc = new Document({
    numbering: {
      config: [{
        reference: 'my-numbering',
        levels: [{
          level: 0, format: 'decimal', text: '%1.',
          alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 360, hanging: 260 } } },
        }],
      }],
    },
    styles: {
      default: {
        document: {
          run:       { font: FONT, size: SZ },
          paragraph: { spacing: { line: 276 } },
        },
      },
      paragraphStyles: [
        {
          id: 'Heading1', name: 'Heading 1',
          basedOn: 'Normal', next: 'Normal',
          run: { size: SZ_H1, bold: true, color: COLOR_H1, font: FONT },
          paragraph: { spacing: { before: 400, after: 200 }, alignment: AlignmentType.CENTER },
        },
        {
          id: 'Heading2', name: 'Heading 2',
          basedOn: 'Normal', next: 'Normal',
          run: { size: SZ_H2, bold: true, color: COLOR_H2, font: FONT },
          paragraph: { spacing: { before: 300, after: 120 } },
        },
        {
          id: 'Heading3', name: 'Heading 3',
          basedOn: 'Normal', next: 'Normal',
          run: { size: SZ_H3, bold: true, color: COLOR_H3, font: FONT },
          paragraph: { spacing: { before: 200, after: 80 } },
        },
      ],
    },
    sections: [{
      properties: {
        page: {
          margin: {
            top:    convertInchesToTwip(1),
            bottom: convertInchesToTwip(1),
            left:   convertInchesToTwip(1.25),
            right:  convertInchesToTwip(1.25),
          },
        },
      },
      children: docChildren,
    }],
  });

  console.log('Generating .docx file...');
  const buf = await Packer.toBuffer(doc);
  fs.writeFileSync(outPath, buf);
  console.log(`Done! Saved to: ${outPath} (${(buf.length / 1024).toFixed(1)} KB)`);
}

main().catch(err => { console.error(err); process.exit(1); });
