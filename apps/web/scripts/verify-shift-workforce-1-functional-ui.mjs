#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`SHIFT-WORKFORCE-1 functional UI verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running SHIFT-WORKFORCE-1 functional UI verifier.");

const page = readRequired("apps/web/src/app/(app)/vardiyalar/page.tsx");
const view = readRequired("apps/web/src/features/shift-workforce/components/shift-workforce-view.tsx");
const labels = readRequired("apps/web/src/features/shift-workforce/domain/shift-workforce-labels.ts");
const api = readRequired("apps/web/src/features/shift-workforce/server/shift-workforce-api.ts");
const queries = readRequired("apps/web/src/features/shift-workforce/server/shift-workforce-queries.ts");
const actions = readRequired("apps/web/src/features/shift-workforce/server/shift-workforce-actions.ts");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const ui = `${page}\n${view}`;

requireText(page, "ShiftWorkforceView", "vardiyalar page");
requireText(ui, "Personel ve Yetkinlikler", "workspace tab");
requireText(ui, "Toplam Personel", "overview KPI");
requireText(ui, "Aktif", "active KPI");
requireText(ui, "Müsait", "available KPI");
requireText(ui, "İzinli / Müsait Değil", "unavailable KPI");
requireText(ui, "Bayat Kayıt", "stale KPI");
requireText(ui, "Yetkinlik Dikkati", "competency attention KPI");
requireText(labels, "LOCAL_PILOT_DISCLAIMER", "pilot disclaimer constant");
requireText(labels, "Yerel pilot veri", "pilot disclaimer text");
requireText(ui, "LOCAL_PILOT_DISCLAIMER", "pilot disclaimer rendered");
requireText(ui, "ERP/HR bilgileri (salt okunur)", "ERP read-only marker");
requireText(ui, "formatShiftWorkforceCount", "null-safe KPI formatting");
requireText(ui, "sonuç", "result count");
requireText(ui, "Yetkinlik Ekle", "add competency action");
requireText(api, "/api/shift-workforce/overview", "overview API");
requireText(api, "/api/shift-workforce/persons", "persons API");
requireText(api, "/api/shift-workforce/competency-definitions", "definitions API");
requireText(api, "/api/shift-workforce/synchronize", "synchronize API");
requireText(queries, "readShiftWorkforceWorkspace", "workspace reader");
requireText(actions, "loadShiftWorkforcePersonsAction", "persons action");
requireText(actions, "createShiftWorkforceCompetencyAction", "create competency action");
requireText(nav, 'href: "/vardiyalar"', "navigation route");
requireText(nav, 'permissionKey: "shift-workforce.read"', "navigation permission");

forbidText(ui, "operator assignment", "assignment feature");
forbidText(ui, "staffing risk", "risk feature");
forbidText(ui, "risk score", "risk score");
forbidText(view, "{person.id}", "raw person UUID");
forbidText(view, "{selectedPerson.id}", "raw selected UUID");
forbidText(view, "{competency.availabilityState}", "raw availability enum");
forbidText(view, "{competency.freshnessState}", "raw freshness enum");
forbidText(view, "{assignment.verificationStatus}", "raw verification enum");

assert("uses availability labels", view.includes("availabilityLabel") || view.includes("getAvailabilityLabel"));
assert("uses freshness labels", view.includes("freshnessLabel") || view.includes("getFreshnessLabel"));
assert("loading state", view.includes("LoaderCircle") || view.includes("loading"));
assert("empty state", view.includes("Kayıt bulunamadı") || view.includes("empty"));
assert("error state", view.includes("error") || view.includes("Hata"));
assert("read-only manage guard", view.includes("canManage"));

console.log("SHIFT-WORKFORCE-1 functional UI verification passed.");
