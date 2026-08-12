#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`PROCESS-OEE-1 functional UI verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running PROCESS-OEE-1 functional UI verifier.");

const navigation = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const page = readRequired("apps/web/src/app/(app)/proses-oee/page.tsx");
const view = readRequired("apps/web/src/features/process-oee/components/process-oee-view.tsx");
const labels = readRequired("apps/web/src/features/process-oee/presentation/process-oee-labels.ts");
const css = readRequired("apps/web/src/features/process-oee/components/process-oee.css");

assert("navigation route /proses-oee", navigation.includes('href: "/proses-oee"'));
assert("navigation label", navigation.includes("Proses & OEE"));
assert("navigation permission", navigation.includes('permissionKey: "process-oee.read"'));
assert("navigation after uretim", navigation.indexOf('href: "/uretim"') < navigation.indexOf('href: "/proses-oee"'));
assert("navigation before makineler", navigation.indexOf('href: "/proses-oee"') < navigation.indexOf('href: "/makineler"'));
assert("navigation after vardiyalar", navigation.indexOf('href: "/vardiyalar"') < navigation.indexOf('href: "/proses-oee"'));
assert("iconKey serializable", navigation.includes('iconKey: "circle-gauge"'));
forbidText(navigation, "from \"lucide-react\"", "primary navigation must not import React icons");

assert("page eyebrow", view.includes("PROSES VE VERİMLİLİK") || page.includes("PROSES VE VERİMLİLİK"));
assert("page title", view.includes("Proses &amp; OEE") || view.includes("Proses & OEE") || page.includes("Proses & OEE"));
assert("manage CTA", view.includes("OEE Değerlendir"));
assert("KPI OEE", view.includes("OEE") && view.includes("Kullanılabilirlik"));
assert("KPI Kalite", view.includes("Kalite"));
assert("KPI Veri Yeterliliği", view.includes("Veri Yeterliliği"));
assert("assessment table Makine", view.includes("Makine"));
assert("assessment table OEE", view.includes("OEE"));
assert("detail Genel Bakış", view.includes("Genel Bakış"));
assert("detail Hesaplama Kanıtları", view.includes("Hesaplama Kanıtları"));
assert("detail Zaman ve Duruş", view.includes("Zaman ve Duruş"));
assert("detail Çevrim ve Üretim", view.includes("Çevrim ve Üretim"));
assert("detail Veri Kaynakları", view.includes("Veri Kaynakları"));
assert("formatPercent dash", labels.includes('"—"') || labels.includes("'—'"));
assert("status labels Turkish", labels.includes("Veri yetersiz") || labels.includes("Hesaplandı"));
assert("1600 layout class", css.includes("max-width") || css.includes("1600") || css.includes("process-oee-page"));
forbidText(view, "{assessment.id}", "must not render raw assessment id in UI");

console.log("PROCESS-OEE-1 functional UI verification passed.");
