import { CalendarClock } from "lucide-react";

import { ShiftWorkforceView } from "@/features/shift-workforce/components/shift-workforce-view";
import "@/features/shift-workforce/components/shift-workforce.css";
import {
  getDefaultShiftAssignmentSelection,
  readShiftAssignmentContextOptions,
  readShiftAssignmentPermissions,
} from "@/features/shift-assignment/server/shift-assignment-queries";
import { readShiftRiskPermissions } from "@/features/shift-risk/server/shift-risk-queries";
import { readShiftWorkforceWorkspace } from "@/features/shift-workforce/server/shift-workforce-queries";

export default async function VardiyalarPage() {
  const [result, assignmentPermissions, assignmentContext, riskPermissions] = await Promise.all([
    readShiftWorkforceWorkspace({
      page: 1,
      pageSize: 20,
    }),
    readShiftAssignmentPermissions(),
    readShiftAssignmentContextOptions(),
    readShiftRiskPermissions(),
  ]);

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="shift-workforce-page">
        <header className="shift-workforce-executive">
          <div className="shift-workforce-executive__intro">
            <p className="shift-workforce-executive__eyebrow">VARDİYA YÖNETİMİ</p>
            <h1>Vardiyalar</h1>
            <p className="shift-workforce-executive__subtitle">
              ERP/HR kaynaklı personel verilerini ve operasyonel yetkinlik zenginleştirmesini izleyin.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "shift-workforce-unauthorized" : "shift-workforce-error-page"}
        >
          <CalendarClock aria-hidden="true" />
          <div>
            <h3>
              {isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Vardiya personel verisi yüklenemedi"}
            </h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  const assignmentCanRead =
    assignmentPermissions.status === "success" && assignmentPermissions.capabilities.canRead;
  const assignmentCanManage =
    assignmentPermissions.status === "success" && assignmentPermissions.capabilities.canManage;
  const riskCanRead =
    riskPermissions.status === "success" && riskPermissions.capabilities.canRead;
  const riskCanManage =
    riskPermissions.status === "success" && riskPermissions.capabilities.canManage;
  const assignmentContextOptions =
    assignmentContext.status === "success"
      ? assignmentContext.options
      : {
          productionOrders: [],
          machines: [],
          shifts: [],
          competencyCodes: [],
        };

  return (
    <ShiftWorkforceView
      overview={result.overview}
      persons={result.persons}
      competencyDefinitions={result.competencyDefinitions}
      canManage={result.canManage}
      assignmentCanRead={assignmentCanRead}
      assignmentCanManage={assignmentCanManage}
      assignmentContextOptions={assignmentContextOptions}
      assignmentInitialSelection={getDefaultShiftAssignmentSelection()}
      riskCanRead={riskCanRead}
      riskCanManage={riskCanManage}
    />
  );
}
