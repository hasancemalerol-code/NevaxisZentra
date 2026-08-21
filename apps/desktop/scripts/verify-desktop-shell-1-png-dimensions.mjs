#!/usr/bin/env node
/**
 * verify-desktop-shell-1-png-dimensions.mjs
 * Validates closure screenshot IHDR dimensions.
 */

import { existsSync, readFileSync } from "node:fs";

const args = process.argv.slice(2);
if (args.length !== 3) {
  console.error("Usage: node verify-desktop-shell-1-png-dimensions.mjs <path> <expectedWidth> <expectedHeight>");
  process.exit(2);
}

const [filePath, expectedWidthText, expectedHeightText] = args;
const expectedWidth = Number(expectedWidthText);
const expectedHeight = Number(expectedHeightText);

if (!existsSync(filePath)) {
  console.error(`PNG not found: ${filePath}`);
  process.exit(1);
}

const bytes = readFileSync(filePath);
const width = bytes.readUInt32BE(16);
const height = bytes.readUInt32BE(20);

console.log(`PNG IHDR: ${filePath} -> ${width}x${height} (${bytes.length} bytes)`);

if (width !== expectedWidth || height !== expectedHeight) {
  console.error(`Expected ${expectedWidth}x${expectedHeight}, got ${width}x${height}`);
  process.exit(1);
}

console.log("PNG dimension verification passed.");
