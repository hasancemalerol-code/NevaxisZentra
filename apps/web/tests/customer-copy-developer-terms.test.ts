import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";

// P2-C: customer-facing copy must not carry developer / implementation wording. This guard scans the label, view and
// presentation files of the customer modules for banned terms. Code comments are ignored; non-rendered usages are
// listed in ALLOWLIST with the reason.
const srcRoot = path.resolve(__dirname, "..", "src");

const SCANNED_FEATURES = [
  "maintenance",
  "commercial-intelligence",
  "erp",
  "energy",
  "operational-risk",
  "iot-plc",
  "shift-workforce",
  "shift-assignment",
  "executive-command-center",
  "production-planning",
  "operations-command-center",
  "scenario-recovery",
  "edge",
];
const SCANNED_EXTRA = ["features/ai-task-center/presentation", "app/(app)/operasyonel-riskler"];
const SKIPPED_FILE = /(api-client|api-contracts?|-types?|fixtures?|\.d)\.tsx?$|api-client/;

const BANNED: ReadonlyArray<readonly [string, RegExp, boolean?]> = [
  ["deterministik", /deterministik/i],
  ["tekrar imzası", /tekrar imzas/i],
  ["payda", /\bpayda/i],
  ["karşılaştırma motoru", /kar..la.t.rma motoru/i],
  ["projeksiyon", /projeksiyon/i],
  ["terminal olmayan", /terminal olmayan/i],
  ["tespit çalıştırma", /tespit çal.ştır/i],
  ["onaylı yoğunluk hedefi", /onayl. yo.unluk hedefi/i],
  ["Uygun kayıt:", /uygun kay.t:/i],
  // English implementation words are matched inside quoted string literals only (identifiers are fine).
  ["payload", /\bpayload\b/i, true],
  ["signature", /\bsignature\b/i, true],
  ["pipeline", /\bpipeline\b/i, true],
  ["snapshot", /\bsnapshot\b/i, true],
  ["backfill", /\bbackfill\b/i, true],
  ["heuristic", /\bheuristic\b/i],
];

// file (relative to src) -> banned term labels allowed there, with the reason.
const ALLOWLIST: Record<string, { terms: string[]; reason: string }> = {
  "features/edge/presentation/edge-labels.ts": { terms: ["projeksiyon", "payload"], reason: "EDGE_CAPABILITIES is an unrendered engineering inventory; 'payload' appears only in a reason-code map key" },
  "features/edge/presentation/edge-reliability-fixtures.ts": { terms: ["payload"], reason: "fixture, not rendered" },
};

function walk(directory: string, found: string[] = []): string[] {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(full, found);
    else if (/\.tsx?$/.test(entry.name)) found.push(full);
  }
  return found;
}

function stripComments(text: string): string {
  return text
    .replace(/\/\*[\s\S]*?\*\//g, (m) => m.replace(/[^\n]/g, " "))
    .split("\n")
    .map((line) => (/^\s*\/\//.test(line) ? "" : line))
    .join("\n");
}

/** Text inside quotes on one line (simple scanner; escapes are skipped). */
function stringLiterals(rawLine: string): string {
  // Test ids and template interpolations are identifiers, not rendered copy.
  const line = rawLine.replace(/data-testid="[^"]*"/g, "").replace(/\$\{[^}]*\}/g, "");
  const found: string[] = [];
  let quote = "";
  let current = "";
  for (let i = 0; i < line.length; i += 1) {
    const ch = line[i];
    if (quote) {
      if (ch === "\\") i += 1;
      else if (ch === quote) {
        found.push(current);
        quote = "";
        current = "";
      } else current += ch;
    } else if (ch === '"' || ch === "'" || ch === "`") quote = ch;
  }
  return found.join(" ");
}

const roots = [
  ...SCANNED_FEATURES.map((feature) => path.join(srcRoot, "features", feature)),
  ...SCANNED_EXTRA.map((extra) => path.join(srcRoot, extra)),
];
const rel = (file: string) => path.relative(srcRoot, file).split(path.sep).join("/");
const files = roots.flatMap((root) => walk(root)).filter((file) => !SKIPPED_FILE.test(path.basename(file)) && !file.includes(`${path.sep}server${path.sep}`) || /queries\.ts$/.test(file));

test("customer copy files contain no developer/implementation terms", () => {
  assert.ok(files.length > 50, "scan must cover the customer modules");
  const offenders: string[] = [];
  for (const file of files) {
    const allowed = ALLOWLIST[rel(file)]?.terms ?? [];
    const text = stripComments(readFileSync(file, "utf8"));
    text.split("\n").forEach((line, index) => {
      const literals = stringLiterals(line);
      for (const [label, pattern, literalOnly] of BANNED) {
        if (pattern.test(literalOnly ? literals : line) && !allowed.includes(label)) offenders.push(`${rel(file)}:${index + 1} [${label}] ${line.trim().slice(0, 120)}`);
      }
    });
  }
  assert.deepEqual(offenders, [], `developer terms in customer copy:\n${offenders.join("\n")}`);
});
