import assert from "node:assert/strict";
import test from "node:test";

import {
  describeMaintenanceActivity,
  getMaintenanceConditionReasonLabel,
  localizeMaintenanceEventText,
} from "../src/features/maintenance/presentation/maintenance-labels";

test("maintenance event codes and English summaries become Turkish sentences", () => {
  assert.equal(localizeMaintenanceEventText("maintenance.workorder.created"), "Bakım emri oluşturuldu");
  assert.equal(localizeMaintenanceEventText("Maintenance workorder created"), "Bakım emri oluşturuldu");
  assert.equal(localizeMaintenanceEventText("maintenance.request.converted"), "Arıza kaydı bakım emrine dönüştürüldü");
  assert.equal(localizeMaintenanceEventText("Bakım emri oluşturuldu"), "Bakım emri oluşturuldu");
});

test("untranslatable technical codes are hidden instead of printed", () => {
  assert.equal(localizeMaintenanceEventText("maintenance.thing.unknown"), null);
  assert.equal(localizeMaintenanceEventText(""), null);
  const activity = describeMaintenanceActivity({ eventType: "maintenance.thing.unknown", summary: "maintenance.thing.unknown" });
  assert.equal(activity.label, "Bakım olayı");
  assert.equal(activity.detail, null);
});

test("activity detail is only returned when it adds information", () => {
  assert.deepEqual(
    describeMaintenanceActivity({ eventType: "maintenance.workorder.created", summary: "Maintenance workorder created" }),
    { label: "Bakım emri oluşturuldu", detail: null },
  );
  assert.deepEqual(
    describeMaintenanceActivity({ eventType: "maintenance.workorder.completed", summary: "Pompa contası değiştirildi" }),
    { label: "Bakım emri tamamlandı", detail: "Pompa contası değiştirildi" },
  );
});

test("backend condition reasons are shown in Turkish", () => {
  assert.equal(getMaintenanceConditionReasonLabel("Critical open maintenance item"), "Kritik açık bakım kaydı var");
  assert.equal(getMaintenanceConditionReasonLabel("Insufficient maintenance history"), "Bakım geçmişi yetersiz");
  assert.equal(getMaintenanceConditionReasonLabel("Some brand new reason"), "Durum değerlendirmesinde ek bir koşul saptandı");
  assert.equal(getMaintenanceConditionReasonLabel("Kritik parça eksik"), "Kritik parça eksik");
});
