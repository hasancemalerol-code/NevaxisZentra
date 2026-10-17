"use client";

import { useCallback, useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  AlertTriangle,
  ArrowRight,
  FileStack,
  Globe2,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type {
  EnsureExportCaseFromOrderInput,
  ExportCaseDetail,
  ExportCaseSummary,
  ExportCaseWriteSuccess,
  ExportReadinessStatus,
} from "../domain/export-case";
import {
  getExportCaseStatusLabel,
  getExportReadinessStatusLabel,
} from "../domain/export-case";
import type { ensureExportCaseFromOrderAction } from "../server/export-case-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function getReadinessTone(status: ExportReadinessStatus): BadgeTone {
  switch (status) {
    case "ready":
      return "success";
    case "attention":
      return "warning";
    case "not-ready":
    default:
      return "danger";
  }
}

type OrderExportSummaryPanelProps = Readonly<{
  summary: ExportCaseSummary | ExportCaseDetail | null;
  ensureInput: EnsureExportCaseFromOrderInput;
  onEnsure: typeof ensureExportCaseFromOrderAction;
  lastEnsureResult?: ExportCaseWriteSuccess | null;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "pending" }
  | { kind: "error"; message: string }
  | { kind: "created"; exportCaseId: string };

export function OrderExportSummaryPanel({
  summary,
  ensureInput,
  onEnsure,
  lastEnsureResult = null,
}: OrderExportSummaryPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<PanelState>({ kind: "idle" });

  const handleEnsure = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      setState({ kind: "pending" });

      const result = await onEnsure(ensureInput);

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setState({
        kind: "created",
        exportCaseId: result.value.exportCaseId,
      });
      router.refresh();
    },
    [ensureInput, onEnsure, router],
  );

  const activeCaseId =
    summary?.id ??
    lastEnsureResult?.exportCaseId ??
    (state.kind === "created" ? state.exportCaseId : null);

  if (summary || activeCaseId) {
    const readinessPercent =
      summary?.readinessPercent ?? lastEnsureResult?.readinessPercent ?? 0;
    const readinessStatus =
      summary?.readinessStatus ?? lastEnsureResult?.readinessStatus ?? "not-ready";
    const caseStatus = summary?.status ?? lastEnsureResult?.status ?? "open";
    const blockingCount = summary?.blockingIssueCount ?? 0;
    const caseNumber =
      summary?.caseNumber ?? lastEnsureResult?.caseNumber ?? "—";

    return (
      <section className="order-export-summary-panel">
        <div className="order-export-summary-panel__heading">
          <FileStack aria-hidden="true" />
          <div>
            <p>İHRACAT</p>
            <h2>İhracat dosyası özeti</h2>
            <span>{caseNumber}</span>
          </div>
          <Badge tone={getReadinessTone(readinessStatus)}>
            {getExportReadinessStatusLabel(readinessStatus)}
          </Badge>
        </div>

        <div className="order-export-summary-panel__stats">
          <div>
            <span>Hazırlık</span>
            <strong>%{readinessPercent}</strong>
          </div>
          <div>
            <span>Durum</span>
            <strong>{getExportCaseStatusLabel(caseStatus)}</strong>
          </div>
          <div>
            <span>Bloke</span>
            <strong>{blockingCount}</strong>
          </div>
        </div>

        {activeCaseId ? (
          <Link
            className="order-export-summary-panel__link"
            href={`/ihracat/${activeCaseId}`}
          >
            İhracat Dosyasına Git
            <ArrowRight aria-hidden="true" />
          </Link>
        ) : null}
      </section>
    );
  }

  return (
    <section className="order-export-summary-panel order-export-summary-panel--empty">
      <div className="order-export-summary-panel__heading">
        <Globe2 aria-hidden="true" />
        <div>
          <p>İHRACAT</p>
          <h2>İhracat dosyası henüz yok</h2>
          <span>
            Bu sipariş için belge hazırlık takibini başlatmak üzere ihracat
            dosyası oluşturun.
          </span>
        </div>
      </div>

      {state.kind === "error" && (
        <div className="order-export-summary-panel__error" role="alert">
          <AlertTriangle aria-hidden="true" />
          {customerMessage(state.message)}
        </div>
      )}

      <form className="zx-inline-form" onSubmit={handleEnsure}>
        <button
          type="submit"
          className="button button-primary button-small"
          disabled={state.kind === "pending"}
        >
          {state.kind === "pending"
            ? "Oluşturuluyor…"
            : "İhracat dosyası oluştur"}
        </button>
      </form>
    </section>
  );
}
