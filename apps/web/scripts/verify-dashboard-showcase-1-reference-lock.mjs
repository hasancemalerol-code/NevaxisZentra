#!/usr/bin/env node
import { readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

const KPI_ORDER = [
  "Gerçekleşen Üretim",
  "OEE",
  "Fire Oranı",
  "Çevrim Süresi",
  "Kalite Uygunluğu",
  "Hedef Gerçekleşmesi",
];

const FORBIDDEN_PATTERNS = [
  /\bKritik Arıza\b/i,
  /\bKritik risk\b/i,
  /\bAcil\b/i,
  /\balarm\b/i,
  /\bsidebar-navigation\b/i,
  /\bapp-sidebar\b/i,
  /\btop-bar\b/i,
  /\bAccountMenu\b/,
  /\bcarousel\b/i,
  /\bmodal\b/i,
  /\bbreadcrumb\b/i,
  /\bmüşteri\b/i,
  /\bsipariş no\b/i,
  /\bfatura\b/i,
  /\bödeme\b/i,
  /\bAI öner/i,
];

function fail(message) {
  console.error(`DASHBOARD-SHOWCASE-1 reference-lock verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function walkRoutes(dir, base = "") {
  const entries = [];
  for (const name of readdirSync(dir)) {
    const fullPath = join(dir, name);
    const relative = `${base}/${name}`.replace(/\\/g, "/");
    const stats = statSync(fullPath);
    if (stats.isDirectory()) {
      entries.push(...walkRoutes(fullPath, relative));
      continue;
    }
    if (name === "page.tsx") {
      entries.push(relative);
    }
  }
  return entries;
}

console.log("Running DASHBOARD-SHOWCASE-1 reference-lock verifier.");

const view = readRequired("apps/web/src/features/dashboard-showcase/components/factory-showcase-view.tsx");
const css = readRequired("apps/web/src/features/dashboard-showcase/components/factory-showcase.css");
const page = readRequired("apps/web/src/app/(app)/fabrika-vitrini/page.tsx");
const layout = readRequired("apps/web/src/app/(app)/layout.tsx");
const mapper = readRequired("apps/web/src/features/dashboard-showcase/server/dashboard-showcase-mapper.ts");

assert("route page exists", page.includes("FabrikaVitriniPage"));
assert("layout uses shell-free showcase shell", layout.includes('pathname === "/fabrika-vitrini"') && layout.includes("FactoryShowcaseShell"));
assert("no app shell on showcase route", !layout.includes('pathname === "/fabrika-vitrini"') || layout.includes("<FactoryShowcaseShell>{children}</FactoryShowcaseShell>"));

for (const label of KPI_ORDER) {
  assert(`KPI label present: ${label}`, mapper.includes(label));
}

const kpiCardMatches = view.match(/<KpiCard card=\{card\}/g) ?? [];
assert("exactly six KPI cards rendered", kpiCardMatches.length === 1);
assert("KPI card order locked", view.includes('const KPI_CARD_ORDER = ['));
assert("hourly chart title", view.includes("Saatlik Üretim Performansı"));
assert("OEE components title", view.includes("OEE Bileşenleri"));
assert("distribution title", view.includes("Üretim Dağılımı"));
assert("header brand", view.includes("NevaxisFactOS") && view.includes("Fabrika Operasyon Görünümü"));
assert("header refresh label", view.includes("Son güncelleme:"));
assert("header live label", view.includes("Operasyon akışı izleniyor"));
assert("fullscreen control only", view.includes("requestFullscreen") && view.includes("exitFullscreen"));
assert("no sidebar class", !view.includes("app-sidebar") && !css.includes("app-sidebar"));
assert("no topbar class", !view.includes("top-bar") && !css.includes("top-bar"));
assert("viewport locked layout", css.includes("height: 100dvh") && css.includes("overflow: hidden"));
assert("grid proportions", css.includes("grid-template-rows: minmax(0, 82px)") && css.includes("225px"));
assert("responsive chart plot containers", css.includes("factory-showcase-chart__plot"));
assert("presentation mode attribute", view.includes("data-presentation-mode"));
assert("three-column KPI grid", css.includes("grid-template-columns: repeat(3"));
assert("two-column bottom grid", css.includes("factory-showcase__bottom-grid"));

for (const pattern of FORBIDDEN_PATTERNS) {
  if (pattern.test(view)) {
    fail(`forbidden content pattern ${pattern} found in view`);
  }
}

const appRoutes = walkRoutes(resolve(ROOT, "apps/web/src/app/(app)"));
const showcaseRoutes = appRoutes.filter((route) => route.includes("fabrika-vitrini"));
assert("only one showcase route file", showcaseRoutes.length === 1);

const extraShowcaseRoutes = appRoutes.filter((route) =>
  route.includes("vitrin") || route.includes("andon") || route.includes("showcase"));
assert("no extra showcase-like routes", extraShowcaseRoutes.length === 1);

assert("mapper uses six KPI definitions", (mapper.match(/id: "/g) ?? []).length >= 6);
assert("no hardcoded reference numbers in view", !view.includes("42.680") && !view.includes("42680"));
assert("pending state text", view.includes("Veri bekleniyor"));

console.log("DASHBOARD-SHOWCASE-1 reference-lock verification passed.");
