

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ProcessOeeView } from "@/features/process-oee/components/process-oee-view";
import "@/features/process-oee/components/process-oee.css";
import { readProcessOeeWorkspace } from "@/features/process-oee/server/process-oee-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function ProcessOeePage() {
  await forbidUnlessGranted("process-oee.read");

  const result = await readProcessOeeWorkspace();

  if (result.status === "failure") {
    return (
      <div className="process-oee-page">
        <header className="process-oee-header">
          <div className="process-oee-header__intro">
            <p className="process-oee-header__eyebrow">PROSES VE VERİMLİLİK</p>
            <h1>Proses ve OEE</h1>
            <p className="process-oee-header__subtitle">
              Makine ve üretim kanıtlarına dayalı OEE değerlendirmelerini izleyin.
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Proses OEE verisi yüklenemedi" inline />
      </div>
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
