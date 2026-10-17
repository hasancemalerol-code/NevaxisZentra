"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import type { QualityInspectionDetail } from "../domain/quality-control";
import type {
  completeQualityInspectionAction,
  recordQualityResultAction,
  releaseQualityInspectionAction,
  rejectQualityInspectionAction,
  startQualityInspectionAction,
} from "../server/quality-control-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

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
    <section className="quality-section quality-section--wide quality-action-bar" aria-label="Kalite kontrol işlemleri">
      <header className="quality-section__header">
        <div>
          <h2>İşlemler</h2>
          <p>Sürüm: {version}</p>
        </div>
      </header>

      {message ? (
        <div role="alert" className="quality-error-panel">
          <p>{customerMessage(message)}</p>
        </div>
      ) : null}

      {inspection.status === "draft" ? (
        <div className="quality-form-actions">
          <Button
            type="button"
            variant="primary"
            disabled={pending}
            onClick={() => run(() => onStart(inspection.id, version))}
          >
            Kontrolü Başlat
          </Button>
        </div>
      ) : null}

      {inspection.status === "in-progress" ? (
        <div className="quality-measurement-list">
          {inspection.results.map((row) => (
            <form
              key={row.inspectionDefinitionId}
              className="quality-measurement-row"
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
              <div className="quality-measurement-row__info">
                <strong>{row.definitionCode}</strong>
                <span>{row.definitionName}</span>
                <span>Mevcut sonuç: {row.result}</span>
              </div>
              <div className="quality-measurement-row__tolerance">
                <span>
                  Hedef: {row.nominalValue ?? "—"} {row.unit ?? ""}
                </span>
                <span>
                  Tolerans: {row.lowerLimit ?? "—"} – {row.upperLimit ?? "—"}
                </span>
              </div>
              {row.inspectionType === "measurement" ? (
                <div className="quality-form-field">
                  <label htmlFor={`numeric-${row.inspectionDefinitionId}`}>Ölçülen</label>
                  <input
                    id={`numeric-${row.inspectionDefinitionId}`}
                    name="numeric"
                    type="number"
                    step="0.001"
                    className="quality-form-input"
                    placeholder="Ölçüm"
                  />
                </div>
              ) : (
                <div className="quality-form-field">
                  <label htmlFor={`text-${row.inspectionDefinitionId}`}>Sonuç</label>
                  <input
                    id={`text-${row.inspectionDefinitionId}`}
                    name="text"
                    type="text"
                    className="quality-form-input"
                    placeholder="pass / fail / n/a"
                  />
                </div>
              )}
              <div className="quality-form-field">
                <label htmlFor={`comment-${row.inspectionDefinitionId}`}>Yorum</label>
                <input
                  id={`comment-${row.inspectionDefinitionId}`}
                  name="comment"
                  type="text"
                  className="quality-form-input"
                  placeholder="Yorum"
                />
              </div>
              <Button type="submit" variant="secondary" size="small" disabled={pending}>
                Kaydet
              </Button>
            </form>
          ))}
        </div>
      ) : null}

      {inspection.status === "in-progress" ? (
        <div className="quality-form-actions">
          <Button
            type="button"
            variant="primary"
            disabled={pending}
            onClick={() => run(() => onComplete(inspection.id, version))}
          >
            Kontrolü Tamamla
          </Button>
        </div>
      ) : null}

      {inspection.status === "completed" && inspection.releaseStatus === "ready-for-review" ? (
        <div className="quality-action-bar__buttons">
          <Button
            type="button"
            variant="primary"
            disabled={pending}
            onClick={() => run(() => onRelease(inspection.id, version, "Kalite serbest bırakma"))}
          >
            Serbest Bırak
          </Button>
          <Button
            type="button"
            variant="danger"
            disabled={pending}
            onClick={() => run(() => onReject(inspection.id, version, "Kalite reddi"))}
          >
            Reddet
          </Button>
        </div>
      ) : null}
    </section>
  );
}
