import assert from "node:assert/strict";
import test from "node:test";

import {
  isShiftHandoverId,
  shiftHandoverDepartmentLabel,
  shiftHandoverStatusLabel,
} from "../src/features/shift-handover/domain/shift-handover-constants";
import {
  getCommercialWorkspaceEvidenceLabel,
  resolveCommercialProvenance,
} from "../src/features/commercial-intelligence/domain/commercial-intelligence-labels";
import {
  resolveActiveWorkflowHome,
  resolveContextualNavigationSection,
} from "../src/lib/navigation/workflow-primary-navigation";

test("shift handover labels are Turkish and unknown values stay neutral", () => {
  assert.equal(shiftHandoverStatusLabel("draft"), "Taslak");
  assert.equal(shiftHandoverStatusLabel("ready-for-handover"), "Devre Hazır");
  assert.equal(shiftHandoverStatusLabel("something-else"), "Durum belirsiz");
});

test("department enum values are shown in Turkish while typed names stay as typed", () => {
  assert.equal(shiftHandoverDepartmentLabel("production"), "Üretim");
  assert.equal(shiftHandoverDepartmentLabel(" Quality "), "Kalite");
  assert.equal(shiftHandoverDepartmentLabel("Boyahane"), "Boyahane");
  assert.equal(shiftHandoverDepartmentLabel(""), "Belirtilmedi");
});

test("only canonical GUIDs can be shift handover records", () => {
  assert.equal(isShiftHandoverId("71deba03-1b2c-4d3e-8f90-123456789abc"), true);
  assert.equal(isShiftHandoverId("not-an-id"), false);
  assert.equal(isShiftHandoverId(""), false);
});

test("commercial intelligence never claims sample data for a tenant without pilot data", () => {
  assert.equal(getCommercialWorkspaceEvidenceLabel(false), "Henüz kanıt yok");
  assert.equal(getCommercialWorkspaceEvidenceLabel(false, true), "Hesaplanmış");
  assert.equal(getCommercialWorkspaceEvidenceLabel(true), "Örnek veri");
  assert.equal(
    resolveCommercialProvenance("Deterministic local Development commercial-intelligence data — simulated", false),
    "Kayıtlı veri yok",
  );
  assert.equal(resolveCommercialProvenance("Örnek veri", true), "Örnek veri");
});

test("Ticari Zekâ routes resolve to their own navigation section", () => {
  assert.equal(resolveActiveWorkflowHome("/ticari-zeka/urun-karliligi"), "ticari-zeka");
  const section = resolveContextualNavigationSection("/ticari-zeka/teklif-riskleri");
  assert.equal(section?.label, "Ticari Zekâ");
  assert.ok(section?.items.some((item) => item.href === "/ticari-zeka/teklif-riskleri"));
  assert.equal(resolveActiveWorkflowHome("/vardiya-devir/abc"), "bugun");
});
