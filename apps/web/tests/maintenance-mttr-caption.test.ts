import assert from "node:assert/strict";
import test from "node:test";

import { describeMeanRepairUnavailable } from "../src/features/maintenance/presentation/maintenance-labels";

test("MTTR caption says there is no completed corrective maintenance only when none is completed", () => {
  assert.equal(describeMeanRepairUnavailable([]), "Tamamlanmış düzeltici bakım yok");
  // An open corrective work order (listed in the fault table) does not feed MTTR and must not be called "none".
  assert.equal(
    describeMeanRepairUnavailable([{ type: "corrective", status: "in-progress" }]),
    "Tamamlanmış düzeltici bakım yok",
  );
  assert.equal(
    describeMeanRepairUnavailable([{ type: "preventive", status: "completed" }]),
    "Tamamlanmış düzeltici bakım yok",
  );
  assert.equal(
    describeMeanRepairUnavailable([{ type: "fault", status: "completed" }]),
    "Başlangıç ve bitiş zamanı kayıtlı onarım yok",
  );
});
