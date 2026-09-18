#!/usr/bin/env node

import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join, relative } from "node:path";

const repoRoot = process.argv[2] ?? process.cwd();
const releaseRoot = join(repoRoot, "release", "artifacts");
const stagingRoot = join(repoRoot, "release", "staging");

const forbiddenPatterns = [
  /\.git(\/|\\|$)/i,
  /\\\.env(\.|$)/i,
  /\.map$/i,
  /\\node_modules\\\.cache\\/i,
  /\\tests\\/i,
  /NEVAXISZENTRA-.*FINAL-REPORT\.md$/i,
];

const forbiddenSecretPatterns = [
  /sk-[A-Za-z0-9]{20,}/,
  /OPENAI_API_KEY\s*=\s*\S+/,
  /ghp_[A-Za-z0-9]{20,}/,
  /local_dev_only_change_me/,
];

function sha256File(path) {
  const hash = createHash("sha256");
  hash.update(readFileSync(path));
  return hash.digest("hex");
}

function walkFiles(dir, files = []) {
  if (!existsSync(dir)) return files;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) walkFiles(full, files);
    else files.push(full);
  }
  return files;
}

function assert(name, pass, detail = "") {
  if (!pass) throw new Error(`${name}${detail ? `: ${detail}` : ""}`);
  console.log(`PASS ${name}`);
}

try {
  assert("staging exists", existsSync(stagingRoot));
  assert("desktop exe exists", existsSync(join(stagingRoot, "desktop", "NevaxisZentra.exe")));
  assert("api exe exists", existsSync(join(stagingRoot, "api", "NevaxisZentra.Api.exe")));
  assert("web server exists", existsSync(join(stagingRoot, "web", "apps", "web", "server.js")));
  assert("bundled node exists", existsSync(join(stagingRoot, "node", "node.exe")));
  assert("postgres initdb exists", existsSync(join(stagingRoot, "postgres", "bin", "initdb.exe")));
  assert("runtime manifest exists", existsSync(join(stagingRoot, "manifests", "runtime-manifest.json")));

  const manifest = JSON.parse(readFileSync(join(stagingRoot, "manifests", "runtime-manifest.json"), "utf8"));
  assert("embedded postgres architecture", manifest.databaseArchitecture === "EMBEDDED_LOCAL");

  const stagingFiles = walkFiles(stagingRoot);
  assert("staging non-empty", stagingFiles.length > 100, `${stagingFiles.length} files`);

  for (const file of stagingFiles) {
    const rel = relative(stagingRoot, file).replaceAll("\\", "/");
    for (const pattern of forbiddenPatterns) {
      assert(`forbidden path ${rel}`, !pattern.test(rel));
    }
    if (/\.(cs|tsx?|jsx?)$/i.test(file) && !/\\web\\/.test(file)) {
      assert(`raw source not packaged ${rel}`, false);
    }
    if (file.endsWith(".map")) {
      assert(`source map absent ${rel}`, false);
    }
    const sample = readFileSync(file, "utf8").slice(0, 50000);
    for (const pattern of forbiddenSecretPatterns) {
      assert(`secret scan ${rel}`, !pattern.test(sample));
    }
  }

  const setupMsi = join(releaseRoot, "Nevaxis-FactOS-Setup.msi");
  const setupExe = join(releaseRoot, "Nevaxis-FactOS-Setup.exe");
  const zip = join(releaseRoot, "Nevaxis-FactOS-Demo.zip");
  const readme = join(releaseRoot, "README-DEMO.txt");
  const checksums = join(releaseRoot, "SHA256SUMS.txt");
  const releaseManifest = join(releaseRoot, "NEVAXISZENTRA-WINDOWS-RELEASE-MANIFEST.md");

  if (existsSync(releaseRoot)) {
    assert("installer msi exists", existsSync(setupMsi));
    assert("installer artifact exists", existsSync(setupExe) || existsSync(setupMsi));
    assert("distribution zip exists", existsSync(zip));
    assert("readme demo exists", existsSync(readme));
    assert("checksums exist", existsSync(checksums));
    assert("release manifest exists", existsSync(releaseManifest));
    assert("setup msi non-empty", statSync(setupMsi).size > 1024 * 1024);
    assert("zip non-empty", statSync(zip).size > 1024 * 1024);
    const sums = readFileSync(checksums, "utf8");
    const msiHash = sha256File(setupMsi);
    assert("checksum includes msi", sums.includes(msiHash));
  }

  console.log("FACTOS_FULL_CURRENT_WINDOWS_RELEASE_VERIFIER = PASS");
} catch (error) {
  console.error(`FACTOS_FULL_CURRENT_WINDOWS_RELEASE_VERIFIER = FAIL: ${error.message}`);
  process.exit(1);
}
