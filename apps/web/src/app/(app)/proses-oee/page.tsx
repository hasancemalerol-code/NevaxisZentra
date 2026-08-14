import { CircleGauge } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ProcessOeeView } from "@/features/process-oee/components/process-oee-view";
import "@/features/process-oee/components/process-oee.css";
import { readProcessOeeWorkspace } from "@/features/process-oee/server/process-oee-reader";

export default async function ProcessOeePage() {
  await forbidUnlessGranted("process-oee.read");

  const result = await readProcessOeeWorkspace();

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="process-oee-page">
        <header className="process-oee-header">
          <div className="process-oee-header__intro">
            <p className="process-oee-header__eyebrow">PROSES VE VERİMLİLİK</p>
            <h1>Proses &amp; OEE</h1>
            <p className="process-oee-header__subtitle">
              Makine ve üretim kanıtlarına dayalı OEE değerlendirmelerini izleyin.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "process-oee-unauthorized" : "process-oee-error-page"}
        >
          <CircleGauge aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Proses OEE verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <ProcessOeeView
      summary={result.summary}
      assessments={result.assessments}
      canManage={result.canManage}
    />
  );
}
