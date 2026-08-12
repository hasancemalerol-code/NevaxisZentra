import { CalendarClock } from "lucide-react";

import { ShiftWorkforceView } from "@/features/shift-workforce/components/shift-workforce-view";
import "@/features/shift-workforce/components/shift-workforce.css";
import { readShiftWorkforceWorkspace } from "@/features/shift-workforce/server/shift-workforce-queries";

export default async function VardiyalarPage() {
  const result = await readShiftWorkforceWorkspace({
    page: 1,
    pageSize: 20,
  });

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

  return (
    <ShiftWorkforceView
      overview={result.overview}
      persons={result.persons}
      competencyDefinitions={result.competencyDefinitions}
      canManage={result.canManage}
    />
  );
}
