#!/usr/bin/env node
/**
 * verify-fullscreen-png-pixels.mjs
 * Hard pixel acceptance for edge-to-edge fullscreen proof.
 *
 * Usage:
 *   node verify-fullscreen-png-pixels.mjs <png-path> <monitorWidth> <monitorHeight> [--expect-reject]
 */

import { existsSync, readFileSync } from "node:fs";
import zlib from "node:zlib";

const args = process.argv.slice(2);
const expectReject = args.includes("--expect-reject");
const filtered = args.filter((a) => a !== "--expect-reject");
if (filtered.length !== 3) {
  console.error(
    "Usage: node verify-fullscreen-png-pixels.mjs <png-path> <monitorWidth> <monitorHeight> [--expect-reject]",
  );
  process.exit(2);
}

const [pngPath, monitorWidthText, monitorHeightText] = filtered;
const monitorWidth = Number(monitorWidthText);
const monitorHeight = Number(monitorHeightText);

if (!existsSync(pngPath)) {
  console.error(`PNG not found: ${pngPath}`);
  process.exit(1);
}

function paeth(a, b, c) {
  const p = a + b - c;
  const pa = Math.abs(p - a);
  const pb = Math.abs(p - b);
  const pc = Math.abs(p - c);
  if (pa <= pb && pa <= pc) return a;
  if (pb <= pc) return b;
  return c;
}

function decodePngRgba(bytes) {
  if (bytes[0] !== 0x89 || bytes.toString("ascii", 1, 4) !== "PNG") {
    throw new Error("Invalid PNG signature");
  }
  let offset = 8;
  let width = 0;
  let height = 0;
  const idat = [];
  while (offset < bytes.length) {
    const length = bytes.readUInt32BE(offset);
    const type = bytes.toString("ascii", offset + 4, offset + 8);
    const data = bytes.subarray(offset + 8, offset + 8 + length);
    if (type === "IHDR") {
      width = data.readUInt32BE(0);
      height = data.readUInt32BE(4);
    } else if (type === "IDAT") {
      idat.push(data);
    } else if (type === "IEND") {
      break;
    }
    offset += 12 + length;
  }
  const inflated = zlib.inflateSync(Buffer.concat(idat));
  const stride = 1 + width * 4;
  const rgba = Buffer.alloc(width * height * 4);
  let prior = Buffer.alloc(width * 4);
  for (let y = 0; y < height; y++) {
    const filter = inflated[y * stride];
    const row = inflated.subarray(y * stride + 1, y * stride + 1 + width * 4);
    const out = Buffer.alloc(width * 4);
    for (let x = 0; x < width; x++) {
      const i = x * 4;
      for (let c = 0; c < 4; c++) {
        const raw = row[i + c];
        const a = x > 0 ? out[i + c - 4] : 0;
        const b = prior[i + c];
        const d = x > 0 ? prior[i + c - 4] : 0;
        let val = raw;
        if (filter === 1) val = (raw + a) & 255;
        else if (filter === 2) val = (raw + b) & 255;
        else if (filter === 3) val = (raw + Math.floor((a + b) / 2)) & 255;
        else if (filter === 4) val = (raw + paeth(a, b, d)) & 255;
        out[i + c] = val;
      }
    }
    out.copy(rgba, y * width * 4);
    prior = out;
  }
  return { width, height, rgba };
}

function pixel(rgba, width, x, y) {
  const i = (y * width + x) * 4;
  return { r: rgba[i], g: rgba[i + 1], b: rgba[i + 2], a: rgba[i + 3] };
}

function isNearBlack(p) {
  return p.r < 16 && p.g < 16 && p.b < 16;
}

function isMenuGray(p) {
  return p.r > 230 && p.g > 230 && p.b > 230 && Math.abs(p.r - p.g) < 8 && Math.abs(p.g - p.b) < 8;
}

function rowStats(rgba, width, y) {
  let nearBlack = 0;
  let menuGray = 0;
  let unique = new Set();
  for (let x = 0; x < width; x++) {
    const p = pixel(rgba, width, x, y);
    unique.add(`${p.r},${p.g},${p.b}`);
    if (isNearBlack(p)) nearBlack++;
    if (isMenuGray(p)) menuGray++;
  }
  return {
    nearBlack,
    nearBlackPct: nearBlack / width,
    menuGrayPct: menuGray / width,
    uniqueCount: unique.size,
  };
}

function regionNearBlackPct(rgba, width, x0, y0, w, h) {
  let nearBlack = 0;
  let total = 0;
  for (let y = y0; y < y0 + h; y++) {
    for (let x = x0; x < x0 + w; x++) {
      if (isNearBlack(pixel(rgba, width, x, y))) nearBlack++;
      total++;
    }
  }
  return nearBlack / total;
}

const bytes = readFileSync(pngPath);
const { width, height, rgba } = decodePngRgba(bytes);
const failures = [];

console.log(`PNG: ${pngPath}`);
console.log(`IHDR: ${width}x${height} (${bytes.length} bytes)`);

if (width !== monitorWidth || height !== monitorHeight) {
  failures.push(`IHDR ${width}x${height} does not match monitor ${monitorWidth}x${monitorHeight}`);
}

const topRows = Math.min(32, height);
const topRightBlackPct = regionNearBlackPct(
  rgba,
  width,
  Math.floor(width * 0.8),
  0,
  Math.floor(width * 0.2),
  topRows,
);
console.log(`Top-right near-black (first ${topRows} rows): ${(topRightBlackPct * 100).toFixed(2)}%`);

if (topRightBlackPct > 0.05) {
  failures.push(`Top-right near-black region exceeds 5% (${(topRightBlackPct * 100).toFixed(2)}%)`);
}

let stripRows = 0;
for (let y = 0; y < Math.min(40, height); y++) {
  const s = rowStats(rgba, width, y);
  console.log(
    `  row ${y}: unique=${s.uniqueCount} nearBlackPct=${(s.nearBlackPct * 100).toFixed(1)}% menuGrayPct=${(s.menuGrayPct * 100).toFixed(1)}%`,
  );
  if (s.uniqueCount <= 8 && s.menuGrayPct > 0.75 && s.uniqueCount > 1) {
    stripRows++;
  } else if (s.uniqueCount <= 1 && s.menuGrayPct > 0.99) {
    break;
  } else {
    break;
  }
}

console.log(`Detected foreign top strip rows: ${stripRows}`);
if (stripRows > 2) {
  failures.push(`Foreign horizontal strip detected: ${stripRows} rows (~menu/chrome band)`);
}

let contentStartRow = stripRows;
console.log(`Detected web content start row: ${contentStartRow}`);
if (contentStartRow > 2) {
  failures.push(`Foreign top strip detected: web content starts at row ${contentStartRow} (>2px unused edge)`);
}

let topBandBlackRows = 0;
for (let y = 0; y < Math.min(24, height); y++) {
  const s = rowStats(rgba, width, y);
  if (s.nearBlackPct > 0.15) topBandBlackRows++;
}
if (topBandBlackRows >= 8) {
  failures.push(`Continuous near-black top band across ${topBandBlackRows} rows in first 24 rows`);
}

let bottomBandRows = 0;
for (let y = Math.max(0, height - 80); y < height; y++) {
  const s = rowStats(rgba, width, y);
  if (s.nearBlackPct > 0.35 || (s.uniqueCount <= 12 && s.menuGrayPct < 0.2)) {
    bottomBandRows++;
  }
}
if (bottomBandRows >= 20) {
  failures.push(`Taskbar/desktop chrome detected in bottom ${bottomBandRows} rows`);
}

const topLeft = pixel(rgba, width, 0, 0);
const topMid = pixel(rgba, width, Math.floor(width / 2), 0);
if (isNearBlack(topLeft) && isNearBlack(topMid)) {
  failures.push("Top edge is near-black across left/center (desktop chrome, not web surface)");
}

if (stripRows === 0 && !expectReject) {
  let blueRows = 0;
  for (let y = 0; y < Math.min(120, height); y++) {
    const p = pixel(rgba, width, Math.floor(width / 2), y);
    if (p.b >= p.r && p.b >= 120 && p.g >= 100) blueRows++;
  }
  if (blueRows < 20) {
    failures.push("NevaxisFactOS web surface not detected (insufficient blue-gradient rows; page may not be loaded)");
  }
}

const corners = [
  [0, 0],
  [width - 1, 0],
  [0, height - 1],
  [width - 1, height - 1],
];
for (const [x, y] of corners) {
  const p = pixel(rgba, width, x, y);
  if (isNearBlack(p)) {
    failures.push(`Corner (${x},${y}) is near-black`);
  }
}

const passed = failures.length === 0;
console.log(`Pixel validation: ${passed ? "PASS" : "FAIL"}`);
for (const f of failures) console.error(`  - ${f}`);

if (expectReject) {
  if (passed) {
    console.error("Expected rejection but pixel validation passed.");
    process.exit(1);
  }
  console.log("Expected rejection confirmed.");
  process.exit(0);
}

process.exit(passed ? 0 : 1);
