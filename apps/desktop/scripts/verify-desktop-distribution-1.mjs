#!/usr/bin/env node
/**
 * verify-desktop-distribution-1.mjs
 * CREVAFACTOS-DESKTOP-DISTRIBUTION-1 permanent verifiers.
 */

import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const DESKTOP_ROOT = join(ROOT, "apps/desktop");
const ARTIFACTS_ROOT = join(ROOT, "artifacts/desktop-distribution");

let passed = 0;
let failed = 0;

function read(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) {
    throw new Error(`File not found: ${rel}`);
  }

  return readFileSync(abs, "utf8");
}

function assert(label, condition, detail = "") {
  if (condition) {
    console.log(`  ✓ ${label}`);
    passed++;
  } else {
    console.error(`  ✗ ${label}${detail ? `: ${detail}` : ""}`);
    failed++;
  }
}

function sha256File(path) {
  const hash = createHash("sha256");
  hash.update(readFileSync(path));
  return hash.digest("hex");
}

function latestVersionDir() {
  if (!existsSync(ARTIFACTS_ROOT)) {
    return null;
  }

  const entries = readdirSync(ARTIFACTS_ROOT, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)
    .sort();

  return entries.length > 0 ? join(ARTIFACTS_ROOT, entries.at(-1)) : null;
}

function scanForForbiddenSecrets(rootDir) {
  const forbiddenPatterns = [
    /\.env(\.|$)/i,
    /appsettings\.Local\.json$/i,
    /secrets\.json$/i,
    /\.pem$/i,
    /\.pfx$/i,
    /\.git[\\/]/,
    /id_rsa/i,
    /\.pdb$/i,
  ];
  const forbiddenContent = [
    "AKIA",
    "BEGIN PRIVATE KEY",
    "Password = \"",
    "connectionString",
    "CREVAFACTOS_DEV_",
  ];

  const stack = [rootDir];
  const hits = [];

  while (stack.length > 0) {
    const current = stack.pop();
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      const fullPath = join(current, entry.name);
      if (entry.isDirectory()) {
        stack.push(fullPath);
        continue;
      }

      if (forbiddenPatterns.some((pattern) => pattern.test(fullPath))) {
        hits.push(fullPath);
        continue;
      }

      if (/\.(exe|dll|zip|msi)$/i.test(entry.name)) {
        continue;
      }

      const content = readFileSync(fullPath, "utf8");
      for (const needle of forbiddenContent) {
        if (content.includes(needle)) {
          hits.push(`${fullPath} (${needle})`);
        }
      }
    }
  }

  return hits;
}

console.log("\n[1] Version contract and packaging scripts");
const versionScript = read("apps/desktop/distribution/DesktopDistributionVersion.ps1");
const publishScript = read("apps/desktop/scripts/publish-desktop-distribution.ps1");
const props = read("apps/desktop/distribution/DesktopDistribution.props");
assert("Desktop distribution props exist", props.includes("DesktopDistributionUpgradeCode"));
assert("Publish script accepts -Version", publishScript.includes("-Version"));
assert("Publish script reads CREVAFACTOS_DESKTOP_PRODUCTION_WEB_URL", publishScript.includes("CREVAFACTOS_DESKTOP_PRODUCTION_WEB_URL"));
assert("Semantic version validation exists", versionScript.includes("Test-DesktopSemanticVersion"));
assert("UpgradeCode is stable", props.includes("{8C4E2F91-6D3A-4B17-9E2C-1A5F00000001}"));

console.log("\n[2] Production URL rejection and packaging hygiene");
assert("Production URL HTTPS validation exists", versionScript.includes("https-required"));
assert("Localhost rejected for production", versionScript.includes("localhost-not-allowed"));
assert("Unsafe schemes rejected", versionScript.includes("unsafe-scheme"));
assert("Development-only fallback is explicit", publishScript.includes("AllowDevelopmentDistribution"));
assert("WebView2 bootstrapper uses ignored cache directory", publishScript.includes("installer\\.cache\\webview2"));
assert("WebView2 bootstrapper hash is fail-closed", publishScript.includes("pinned SHA-256"));
const gitignore = read(".gitignore");
assert("WiX cache ignored", gitignore.includes(".wix/"));
assert("Installer cache ignored", gitignore.includes("apps/desktop/installer/.cache/"));
assert(
  "WebView2 bootstrapper binary not in tracked redistributables",
  !existsSync(join(ROOT, "apps/desktop/installer/redistributables/MicrosoftEdgeWebview2Setup.exe")),
);

console.log("\n[3] Installer metadata and upgrade identity");
const packageWxs = read("apps/desktop/installer/Package.wxs");
assert("WiX MSI authoring exists", packageWxs.includes("<Package"));
assert("MajorUpgrade/downgrade prevention exists", packageWxs.includes("MajorUpgrade"));
assert("UpgradeCode referenced", packageWxs.includes("UpgradeCode"));
assert("Start Menu shortcut exists", packageWxs.includes("StartMenuShortcut"));
assert("Desktop shortcut exists", packageWxs.includes("DesktopShortcut"));
assert("No cosmetic desktop shortcut checkbox property", !packageWxs.includes("CREVAFACTOSDESKTOPDESKTOPSHORTCUT"));
assert("No WixUI exit optional checkbox", !packageWxs.includes("WIXUI_EXITDIALOGOPTIONALCHECKBOX"));
assert("WebView2 prerequisite handling exists", packageWxs.includes("WEBVIEW2RUNTIMEINSTALLED"));
assert("Turkish downgrade message exists", packageWxs.includes("daha yeni bir sürümü"));
assert("Close running app prompt exists", packageWxs.includes("CloseApplication"));
assert("Uninstall does not remove WebView2 profile", !packageWxs.includes("WebView2") || !packageWxs.includes("LocalApplicationData"));

console.log("\n[4] Self-contained publish settings");
const csproj = read("apps/desktop/CrevaFactOS.Desktop/CrevaFactOS.Desktop.csproj");
assert("Targets win-x64", csproj.includes("win-x64"));
assert("Trimming not forced on", !csproj.includes("<PublishTrimmed>true</PublishTrimmed>"));
assert("Single-file not forced on", !csproj.includes("<PublishSingleFile>true</PublishSingleFile>"));
assert("Debug symbols disabled by default for publish", csproj.includes("DesktopIncludeDebugSymbolsInPublish"));

console.log("\n[5] Signing-state honesty");
assert("Signing via environment only", publishScript.includes("CREVAFACTOS_DESKTOP_CODESIGN_CERT_THUMBPRINT"));
assert("Manifest records signingState", publishScript.includes("signingState"));
assert("No committed certificate paths", !publishScript.includes(".pfx") || publishScript.includes("env:CREVAFACTOS_DESKTOP_CODESIGN_PFX_PATH"));

console.log("\n[6] Artifact reconciliation");
const artifactDir = latestVersionDir();
if (!artifactDir) {
  assert("Artifact directory exists", false, "run publish-desktop-distribution.ps1 first");
} else {
  const manifest = JSON.parse(readFileSync(join(artifactDir, "distribution-manifest.json"), "utf8"));
  const checksums = readFileSync(join(artifactDir, "SHA256SUMS.txt"), "utf8");
  const msiPath = join(artifactDir, manifest.artifacts.msi);
  const zipPath = join(artifactDir, manifest.artifacts.portableZip);

  assert("MSI artifact exists", existsSync(msiPath));
  assert("Portable ZIP exists", existsSync(zipPath));
  assert("Manifest MSI hash matches file", manifest.sha256.msi === sha256File(msiPath));
  assert("Manifest ZIP hash matches file", manifest.sha256.portableZip === sha256File(zipPath));
  assert("Checksum file contains MSI hash", checksums.includes(manifest.sha256.msi));
  assert("Checksum file contains ZIP hash", checksums.includes(manifest.sha256.portableZip));
  assert("Manifest has upgradeCode", Boolean(manifest.upgradeCode));
  assert("Manifest has sourceCommit", Boolean(manifest.sourceCommit));
  assert("Manifest has no absolute dev path", !JSON.stringify(manifest).includes("C:\\Users\\"));
  assert(
    "Production manifest origin is not localhost",
    manifest.distributionClassification !== "Production" || !/localhost|127\.0\.0\.1/i.test(manifest.configuredOrigin),
  );
  assert(
    "Unsigned artifact not labeled signed",
    manifest.signingState !== "SIGNED_PRODUCTION" || process.env.CREVAFACTOS_DESKTOP_CODESIGN_CERT_THUMBPRINT || process.env.CREVAFACTOS_DESKTOP_CODESIGN_PFX_PATH,
  );

  const secretHits = scanForForbiddenSecrets(join(artifactDir, "publish"));
  assert("Publish output secret scan clean", secretHits.length === 0, secretHits.join("; "));
}

console.log(`\nDesktop distribution verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) {
  process.exit(1);
}
