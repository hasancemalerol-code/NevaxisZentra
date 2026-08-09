"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import type { QualityInspectionDetail } from "../domain/quality-control";
import type {
  completeQualityInspectionAction,
  recordQualityResultAction,
  releaseQualityInspectionAction,
  rejectQualityInspectionAction,
  startQualityInspectionAction,
} from "../server/quality-control-writer";

export type QualityInspectionActionsProps = Readonly<{
  inspection: QualityInspectionDetail;
  onStart: typeof startQualityInspectionAction;
  onRecordResult: typeof recordQualityResultAction;
  onComplete: typeof completeQualityInspectionAction;
  onRelease: typeof releaseQualityInspectionAction;
  onReject: typeof rejectQualityInspectionAction;
}>;

export function QualityInspectionActions({
  inspection,
  onStart,
  onRecordResult,
  onComplete,
  onRelease,
  onReject,
}: QualityInspectionActionsProps) {
  const router = useRouter();
  const [version, setVersion] = useState(inspection.version);
  const [message, setMessage] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function run(action: () => Promise<{ status: string; message?: string; version?: number }>) {
    setPending(true);
    setMessage(null);
    const result = await action();
    setPending(false);
    if (result.status === "failure") {
      setMessage(result.message ?? "İşlem başarısız.");
      return;
    }
    if (result.version) {
      setVersion(result.version);
    }
    router.refresh();
  }

  return (
    <section className="production-execution-panel" aria-label="Kalite kontrol işlemleri">
      <header>
        <h2>İşlemler</h2>
        <p>Sürüm: {version}</p>
      </header>

      {message ? (
        <div role="alert" className="state-panel error-state">
          <p>{message}</p>
        </div>
      ) : null}

      {inspection.status === "draft" ? (
        <button
          type="button"
          className="production-execution-action-button"
          disabled={pending}
          onClick={() => run(() => onStart(inspection.id, version))}
        >
          Kontrolü başlat
        </button>
      ) : null}

      {inspection.status === "in-progress"
        ? inspection.results.map((row) => (
            <form
              key={row.inspectionDefinitionId}
              className="production-execution-form-row"
              onSubmit={(event) => {
                event.preventDefault();
                const form = event.currentTarget;
                const formData = new FormData(form);
                const numericRaw = formData.get("numeric");
                const textRaw = formData.get("text");
                const numeric =
                  typeof numericRaw === "string" && numericRaw.trim()
                    ? Number(numericRaw)
                    : undefined;
                const text =
                  typeof textRaw === "string" && textRaw.trim() ? textRaw.trim() : undefined;

                void run(() =>
                  onRecordResult(
                    inspection.id,
                    version,
                    row.inspectionDefinitionId,
                    numeric,
                    text,
                    undefined,
                    typeof formData.get("comment") === "string"
                      ? formData.get("comment")?.toString()
                      : undefined,
                  ),
                );
              }}
            >
              <div>
                <strong>{row.definitionCode}</strong> — {row.definitionName}
                <p>
                  Hedef: {row.nominalValue ?? "—"} {row.unit ?? ""} (
                  {row.lowerLimit ?? "—"} – {row.upperLimit ?? "—"})
                </p>
                <p>Mevcut sonuç: {row.result}</p>
              </div>
              {row.inspectionType === "measurement" ? (
                <input name="numeric" type="number" step="0.001" placeholder="Ölçüm" />
              ) : (
                <input name="text" type="text" placeholder="pass / fail / n/a" />
              )}
              <input name="comment" type="text" placeholder="Yorum" />
              <button type="submit" className="production-execution-action-button" disabled={pending}>
                Kaydet
              </button>
            </form>
          ))
        : null}

      {inspection.status === "in-progress" ? (
        <button
          type="button"
          className="production-execution-action-button"
          disabled={pending}
          onClick={() => run(() => onComplete(inspection.id, version))}
        >
          Kontrolü tamamla
        </button>
      ) : null}

      {inspection.status === "completed" &&
      inspection.releaseStatus === "ready-for-review" ? (
        <div className="production-execution-form-row">
          <button
            type="button"
            className="production-execution-action-button"
            disabled={pending}
            onClick={() =>
              run(() => onRelease(inspection.id, version, "Kalite serbest bırakma"))
            }
          >
            Serbest bırak
          </button>
          <button
            type="button"
            className="production-execution-action-button production-execution-action-button--danger"
            disabled={pending}
            onClick={() => run(() => onReject(inspection.id, version, "Kalite reddi"))}
          >
            Reddet
          </button>
        </div>
      ) : null}
    </section>
  );
}
