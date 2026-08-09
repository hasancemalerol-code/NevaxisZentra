"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";

import type { LinkedNonconformanceSummary, QualityInspectionDetail } from "../domain/quality-control";
import type { createQualityNcrAction } from "../server/quality-control-writer";

export type QualityInspectionNcrLinkProps = Readonly<{
  inspection: QualityInspectionDetail;
  linkedNonconformances: readonly LinkedNonconformanceSummary[];
  onCreateNcr: typeof createQualityNcrAction;
}>;

export function QualityInspectionNcrLink({
  inspection,
  linkedNonconformances,
  onCreateNcr,
}: QualityInspectionNcrLinkProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const failedResults = inspection.results.filter((row) => row.result === "fail");
  const hasLinkedNcr = linkedNonconformances.length > 0;

  async function createFromInspection() {
    const failedResult = failedResults[0];
    setPending(true);
    setMessage(null);
    const result = await onCreateNcr({
      productionOrderId: inspection.productionOrderId,
      qualityInspectionId: inspection.id,
      qualityInspectionResultId: failedResult?.id,
      productReference: inspection.productionContext.productReference,
      title: `${inspection.inspectionNumber} uygunsuzluk`,
      description: `Başarısız kalite kontrolü: ${inspection.planCode}`,
      severity: failedResults.some((row) => row.critical) ? "critical" : "major",
      category: "inspection-failure",
    });
    setPending(false);
    if (result.status === "failure") {
      setMessage(result.message ?? "NCR oluşturulamadı.");
      return;
    }
    if (result.id) {
      router.push(`/kalite/ncr/${result.id}`);
      return;
    }
    router.refresh();
  }

  if (hasLinkedNcr) {
    return (
      <div className="production-execution-form-row">
        <strong>Bağlı NCR</strong>
        <ul className="production-execution-list">
          {linkedNonconformances.map((linked) => (
            <li key={linked.id}>
              <Link href={`/kalite/ncr/${linked.id}`}>{linked.ncrNumber}</Link>
              <span>{linked.status}</span>
            </li>
          ))}
        </ul>
      </div>
    );
  }

  if (failedResults.length === 0 && inspection.overallResult !== "fail") {
    return null;
  }

  return (
    <div className="production-execution-form-row">
      {message ? (
        <div role="alert" className="state-panel error-state">
          <p>{message}</p>
        </div>
      ) : null}
      <button
        type="button"
        className="production-execution-action-button"
        disabled={pending}
        onClick={() => void createFromInspection()}
      >
        NCR Oluştur
      </button>
    </div>
  );
}
