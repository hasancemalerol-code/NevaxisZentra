import {
  AlertTriangle,
  CheckCircle2,
  CircleDashed,
  ShieldAlert,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type { ExportReadiness, ExportReadinessStatus } from "../domain/export-case";
import {
  getExportCaseStatusLabel,
  getExportReadinessStatusLabel,
} from "../domain/export-case";

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

function requirementStatusLabel(status: string): string {
  switch (status) {
    case "satisfied":
    case "verified":
      return "Karşılandı";
    case "missing":
      return "Eksik";
    case "draft":
      return "Taslak";
    case "received":
      return "Alındı";
    case "rejected":
      return "Reddedildi";
    default:
      return status;
  }
}

type ExportReadinessPanelProps = Readonly<{
  readiness: ExportReadiness;
}>;

export function ExportReadinessPanel({ readiness }: ExportReadinessPanelProps) {
  const blockers = readiness.requirements.filter(
    (r) => r.isBlocking && !r.isSatisfied,
  );
  const missing = readiness.requirements.filter((r) => !r.isSatisfied);
  const tone =
    readiness.readinessStatus === "ready"
      ? "ready"
      : readiness.readinessStatus === "attention"
        ? "attention"
        : "not-ready";

  return (
    <section className={`export-readiness-panel export-readiness-panel--${tone}`}>
      <div className="export-readiness-panel__header">
        <div>
          <p>HAZIRLIK DURUMU</p>
          <h2>
            %{readiness.readinessPercent} —{" "}
            {getExportReadinessStatusLabel(readiness.readinessStatus)}
          </h2>
          <span>
            Önerilen dosya durumu:{" "}
            {getExportCaseStatusLabel(readiness.suggestedCaseStatus)}
          </span>
        </div>

        <Badge tone={getReadinessTone(readiness.readinessStatus)}>
          {getExportReadinessStatusLabel(readiness.readinessStatus)}
        </Badge>
      </div>

      <div
        className="export-readiness-panel__progress"
        role="progressbar"
        aria-valuenow={readiness.readinessPercent}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`Hazırlık yüzdesi: ${readiness.readinessPercent}`}
      >
        <div
          className="export-readiness-panel__progress-fill"
          style={{
            width: `${Math.min(Math.max(readiness.readinessPercent, 0), 100)}%`,
          }}
        />
      </div>

      <div className="export-readiness-panel__stats">
        <div>
          <ShieldAlert aria-hidden="true" />
          <div>
            <span>Bloke eden</span>
            <strong>{readiness.blockingIssueCount}</strong>
          </div>
        </div>
        <div>
          <CircleDashed aria-hidden="true" />
          <div>
            <span>Eksik gereksinim</span>
            <strong>{readiness.missingRequirementCount}</strong>
          </div>
        </div>
      </div>

      {blockers.length > 0 && (
        <div className="export-readiness-panel__blockers">
          <h3>
            <AlertTriangle aria-hidden="true" />
            Bloke eden gereksinimler
          </h3>
          <ul>
            {blockers.map((item) => (
              <li key={item.key}>
                <strong>{item.label}</strong>
                <span>
                  {requirementStatusLabel(item.status)} — belgesi doğrulanmadan
                  dosya hazır sayılamaz.
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="export-readiness-panel__requirements">
        <h3>Gereksinimler</h3>
        <ul>
          {readiness.requirements.map((item) => (
            <li
              key={item.key}
              className={
                item.isSatisfied
                  ? "export-readiness-panel__req--ok"
                  : item.isBlocking
                    ? "export-readiness-panel__req--blocking"
                    : "export-readiness-panel__req--missing"
              }
            >
              {item.isSatisfied ? (
                <CheckCircle2 aria-hidden="true" />
              ) : (
                <AlertTriangle aria-hidden="true" />
              )}
              <div>
                <strong>{item.label}</strong>
                <span>
                  {requirementStatusLabel(item.status)}
                  {item.isBlocking ? " · Bloke edici" : ""}
                  {!item.isSatisfied
                    ? " — karşılanması bekleniyor"
                    : " — tamamlandı"}
                </span>
              </div>
            </li>
          ))}
        </ul>

        {missing.length === 0 && (
          <p className="export-readiness-panel__all-clear">
            Tüm temel gereksinimler karşılandı.
          </p>
        )}
      </div>
    </section>
  );
}
