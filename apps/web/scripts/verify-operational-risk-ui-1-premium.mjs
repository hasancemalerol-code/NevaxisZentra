#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`OPERATIONAL-RISK-UI-1 premium UI verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running OPERATIONAL-RISK-UI-1 premium UI verifier.");

const page = read("apps/web/src/app/(app)/operasyonel-riskler/page.tsx");
const view = read("apps/web/src/features/operational-risk/components/operational-risk-workspace-view.tsx");
const css = read("apps/web/src/features/operational-risk/components/operational-risk-workspace.css");
const labels = read("apps/web/src/features/operational-risk/domain/operational-risk-labels.ts");
const queries = read("apps/web/src/features/operational-risk/server/operational-risk-queries.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const presentation = read("apps/web/src/features/operational-risk/components/operational-risk-presentation.ts");
const bundle = `${page}\n${view}\n${css}\n${labels}\n${queries}\n${presentation}`;

assert("dedicated /operasyonel-riskler page", page.includes('forbidUnlessGranted("operational-risk.read")'));
assert("operasyonel-riskler removed from catch-all", catchAll.includes('item.href !== "/operasyonel-riskler"'));
assert("navigation permission operational-risk.read", navigation.includes('permissionKey: "operational-risk.read"'));
assert("navigation route /operasyonel-riskler", navigation.includes('href: "/operasyonel-riskler"'));
assert("navigation label Operasyonel Riskler", navigation.includes('label: "Operasyonel Riskler"'));
assert("navigation first in operational risk group", navigation.indexOf('href: "/operasyonel-riskler"') < navigation.indexOf('href: "/riskler/malzeme-uygunlugu"'));

assert("executive eyebrow", bundle.includes("OPERASYONEL RİSK YÖNETİMİ"));
assert("executive title", view.includes("<h1>Operasyonel Riskler</h1>"));
assert("development provenance strip", view.includes('data-testid="operational-risk-provenance-strip"'));
assert("readonly note", view.includes('data-testid="operational-risk-readonly-note"'));
assert("six KPI grid", view.includes('data-testid="operational-risk-kpi-grid"'));
assert("KPI open cases label", queries.includes('"Açık risk vakaları"'));
assert("KPI critical high", queries.includes('"Kritik / yüksek öncelik"'));
assert("KPI new detections", queries.includes('"Yeni tespitler"'));
assert("KPI under review", queries.includes('"İncelemede"'));
assert("KPI insufficient data", queries.includes('"Veri yetersiz"'));
assert("KPI source conflict", queries.includes('"Kaynak uyuşmazlığı"'));

assert("four tabs in order", labels.includes("Genel Bakış") && labels.includes("Risk Vakaları") && labels.includes("Tespit Profilleri") && labels.includes("Tespit Çalıştırmaları"));
assert("accessible tab rail", view.includes('role="tablist"') && view.includes('data-testid="operational-risk-tab-rail"'));
assert("donut distribution", view.includes('data-testid="operational-risk-distribution-donut"'));
assert("source coverage panel", view.includes('data-testid="operational-risk-source-coverage"'));
assert("priority list testid", view.includes('data-testid="operational-risk-priority-list"'));
assert("detection summary testid", view.includes('data-testid="operational-risk-detection-summary"'));
assert("detection summary empty testid", view.includes('data-testid="operational-risk-detection-summary-empty"'));
assert("detection summary hasRuns gate", view.includes("detectionSummary.hasRuns"));
assert("workspace testid", view.includes('data-testid="operational-risk-workspace"'));

assert("scoped CSS prefix", css.includes(".oprisk-page") && css.includes(".oprisk-kpi-grid"));
assert("responsive KPI grid", css.includes("repeat(3, minmax(0, 1fr))"));
assert("overflow protection", css.includes("overflow-x: clip"));
assert("no global css edits", !read("apps/web/src/app/globals.css").includes(".oprisk-"));

assert("case detail ready testid", view.includes('data-testid="operational-risk-case-detail-ready"'));
assert("case detail loading testid", view.includes('data-testid="operational-risk-case-detail-loading"'));
assert("case detail error testid", view.includes('data-testid="operational-risk-case-detail-error"'));
assert("profile workspace testid", view.includes('data-testid="operational-risk-profile-workspace"'));
assert("profile hero testid", view.includes('data-testid="operational-risk-profile-hero"'));
assert("profile rule grid testid", view.includes('data-testid="operational-risk-profile-rule-grid"'));
assert("runs workspace testid", view.includes('data-testid="operational-risk-runs-workspace"'));
assert("runs history testid", view.includes('data-testid="operational-risk-runs-history"'));
assert("runs empty state testid", view.includes('data-testid="operational-risk-runs-empty"'));
assert("compact KPI min-height", css.includes("min-height: 6.2rem"));
assert("larger donut geometry", css.includes("width: 160px"));
assert("overview lower row grid areas", css.includes("grid-template-areas"));
assert("profile workspace layout", css.includes("oprisk-profile-workspace__layout"));
assert("case title localization helper", read("apps/web/src/features/operational-risk/components/operational-risk-presentation.ts").includes("localizeOperationalRiskCaseTitle"));
assert("case drawer detail grid", css.includes("oprisk-detail-grid"));
assert("runs composer title", view.includes("Yeni tespit çalıştırması"));
assert("runs history title", view.includes("Son tespit çalıştırmaları"));
assert("visible token localization helper", presentation.includes("localizeOperationalRiskVisibleToken"));
assert("unknown technical reason fallback", presentation.includes("Tanımlanmamış teknik neden"));
assert("technical audit details collapse", view.includes("Teknik ayrıntılar"));
assert("technical details css", css.includes(".oprisk-technical-details"));
assert("detection empty css", css.includes(".oprisk-detection-empty"));
assert("runs empty premium css", css.includes("oprisk-empty-state--runs"));
assert("evidence summary localization in queries", queries.includes("localizeOperationalRiskEvidenceSummary"));
assert("hasRuns in detection summary", queries.includes("hasRuns:"));

console.log("OPERATIONAL-RISK-UI-1 premium UI verification passed.");
