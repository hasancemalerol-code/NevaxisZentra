#!/usr/bin/env node
/**
 * verify-desktop-shell-1-security.mjs
 * CREVAFACTOS-DESKTOP-SHELL-1 structural and security verifier.
 */

import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const DESKTOP_ROOT = join(ROOT, "apps/desktop/CrevaFactOS.Desktop");

let passed = 0;
let failed = 0;

function readFile(rel) {
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

function assertContains(label, content, text) {
  assert(label, content.includes(text), `expected "${text}"`);
}

function assertNotContains(label, content, text) {
  assert(label, !content.includes(text), `must not contain "${text}"`);
}

function collectSourceFiles(dir) {
  const entries = readdirSync(dir, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === "bin" || entry.name === "obj") {
        continue;
      }

      files.push(...collectSourceFiles(fullPath));
      continue;
    }

    if (/\.(cs|xaml|csproj|manifest)$/i.test(entry.name)) {
      files.push(fullPath);
    }
  }

  return files;
}

console.log("\n[1] Desktop host location and technology");
const csproj = readFile("apps/desktop/CrevaFactOS.Desktop/CrevaFactOS.Desktop.csproj");
assert("Desktop project exists under apps/desktop/CrevaFactOS.Desktop", existsSync(DESKTOP_ROOT));
assert("Uses WPF", csproj.includes("<UseWPF>true</UseWPF>"));
assert("Uses WebView2 package", csproj.includes("Microsoft.Web.WebView2"));
assertNotContains("Does not use Electron", csproj, "electron");
assertNotContains("Does not use Tauri", csproj, "tauri");

console.log("\n[2] No duplicated web UI source");
const desktopSources = collectSourceFiles(DESKTOP_ROOT).map((file) => readFileSync(file, "utf8")).join("\n");
assertNotContains("No React imports", desktopSources, "from \"react\"");
assertNotContains("No Next.js imports", desktopSources, "next/");
assertNotContains("No globals.css reuse", desktopSources, "globals.css");

console.log("\n[3] URL policy and credentials");
const urlPolicy = readFile("apps/desktop/CrevaFactOS.Desktop/Security/DesktopWebUrlPolicy.cs");
assertContains("Uses CREVAFACTOS_DESKTOP_WEB_URL", urlPolicy, "CREVAFACTOS_DESKTOP_WEB_URL");
assertContains("Development default localhost", urlPolicy, "http://127.0.0.1:3001");
assertContains("Production HTTPS requirement", urlPolicy, "HttpsRequiredInProduction");
assertNotContains("No embedded password", urlPolicy, "Password = \"");
assertNotContains("No embedded token", urlPolicy, "Token = \"");

console.log("\n[4] Navigation and WebView security");
const navigationPolicy = readFile("apps/desktop/CrevaFactOS.Desktop/Security/NavigationPolicy.cs");
const mainWindow = readFile("apps/desktop/CrevaFactOS.Desktop/MainWindow.xaml.cs");
assertContains("Blocks file scheme", navigationPolicy, "\"file\"");
assertContains("Blocks javascript scheme", navigationPolicy, "\"javascript\"");
assertContains("External https browser routing", navigationPolicy, "OpenInSystemBrowser");
assertContains("Certificate errors fail closed", mainWindow, "CoreWebView2ServerCertificateErrorAction.Default");
assertContains("Permission requests denied", mainWindow, "CoreWebView2PermissionState.Deny");
assertContains(
  "Production DevTools gated",
  readFile("apps/desktop/CrevaFactOS.Desktop/Services/DesktopEnvironment.cs"),
  "CREVAFACTOS_DESKTOP_ENABLE_DEVTOOLS",
);

console.log("\n[5] Session profile outside repository");
const profilePaths = readFile("apps/desktop/CrevaFactOS.Desktop/Services/WebViewProfilePaths.cs");
assertContains("Uses LocalApplicationData", profilePaths, "LocalApplicationData");
assertContains("Product-specific profile folder", profilePaths, "CrevaFactOS");
assertNotContains("Profile not in repo", profilePaths, "Projects");

console.log("\n[6] Single instance and connection recovery");
const singleInstance = readFile("apps/desktop/CrevaFactOS.Desktop/Services/SingleInstanceManager.cs");
const mainWindowXaml = readFile("apps/desktop/CrevaFactOS.Desktop/MainWindow.xaml");
assertContains("Single-instance mutex", singleInstance, "CrevaFactOS.Desktop.SingleInstance");
assertContains("Turkish connection failure message", mainWindowXaml, "CrevaFactOS bağlantısı kurulamadı");
assertContains("Retry button wired", mainWindowXaml, "Yeniden dene");
assertContains("Retry handler exists", mainWindow, "RetryButton_Click");

console.log("\n[7] No local process supervisor");
assertNotContains("Does not start API", desktopSources, "Process.Start(\"dotnet\"");
assertNotContains("Does not start Next.js", desktopSources, "next dev");
assertNotContains("Does not bundle web server", desktopSources, "npm run");

console.log(`\nDesktop shell security verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) {
  process.exit(1);
}
