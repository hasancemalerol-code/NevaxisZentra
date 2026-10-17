import assert from "node:assert/strict";
import test from "node:test";

import { getOperationsCommandCenterStatusParts } from "../src/features/operations-command-center/domain/operations-command-center-labels";
import { localizeAiTaskAuditActorLabel } from "../src/features/ai-task-center/presentation/ai-task-audit-labels";
import { formatLossMinutes } from "../src/features/factory-learning/domain/factory-learning-labels";

test("command center status labels never expose English tokens", () => {
  const parts = getOperationsCommandCenterStatusParts("Majör · Action required");
  assert.deepEqual(parts.map((part) => part.label), ["Majör", "Aksiyon Gerekiyor"]);
  assert.equal(parts[1]?.tone, "critical");
});

test("English system actors are shown with Turkish names", () => {
  assert.equal(localizeAiTaskAuditActorLabel("AI Task Center"), "Görev Merkezi");
  assert.equal(localizeAiTaskAuditActorLabel("Ayşe Yılmaz"), "Ayşe Yılmaz");
});

test("loss minutes use spaced Turkish units", () => {
  assert.equal(formatLossMinutes(0), "0 dk");
  assert.equal(formatLossMinutes(135), "2 sa 15 dk");
});
