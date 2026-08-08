import Link from "next/link";
import {
  AlertTriangle,
  ArrowRight,
  CalendarClock,
  Globe2,
  Package,
  UserRound,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type {
  ExportCaseStatus,
  ExportCaseSummary,
  ExportReadinessStatus,
} from "../domain/export-case";
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

function getCaseStatusTone(status: ExportCaseStatus): BadgeTone {
  switch (status) {
    case "ready":
      return "success";
    case "blocked":
      return "danger";
    case "preparing":
      return "information";
    case "closed":
      return "neutral";
    case "open":
    default:
      return "warning";
  }
}

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

function formatUtcDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString("tr-TR", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

type ExportCaseCardProps = Readonly<{
  exportCase: ExportCaseSummary;
}>;

export function ExportCaseCard({ exportCase }: ExportCaseCardProps) {
  const progressWidth = Math.min(Math.max(exportCase.readinessPercent, 0), 100);

  return (
    <Card className="export-case-card">
      <div className="export-case-card__heading">
        <div className="export-case-card__badges">
          <Badge tone={getCaseStatusTone(exportCase.status)}>
            {getExportCaseStatusLabel(exportCase.status)}
          </Badge>
          <Badge tone={getReadinessTone(exportCase.readinessStatus)}>
            {getExportReadinessStatusLabel(exportCase.readinessStatus)}
          </Badge>
        </div>

        <h2>{exportCase.caseNumber}</h2>
        <p>{exportCase.customerName}</p>
      </div>

      <div className="export-case-card__summary">
        <div>
          <Globe2 aria-hidden="true" />
          <div>
            <span>Hedef ülke</span>
            <strong>{exportCase.destinationCountryCode}</strong>
            <small>
              {exportCase.incotermCode
                ? `Incoterm: ${exportCase.incotermCode}`
                : "Incoterm belirtilmedi"}
            </small>
          </div>
        </div>

        <div>
          <UserRound aria-hidden="true" />
          <div>
            <span>Müşteri</span>
            <strong>{exportCase.customerName}</strong>
          </div>
        </div>

        <div>
          <Package aria-hidden="true" />
          <div>
            <span>Kaynak sipariş</span>
            <strong>{exportCase.sourceOrderReference}</strong>
          </div>
        </div>

        <div>
          <CalendarClock aria-hidden="true" />
          <div>
            <span>Güncellendi</span>
            <strong>{formatUtcDate(exportCase.updatedAtUtc)}</strong>
          </div>
        </div>
      </div>

      <div className="export-case-card__readiness">
        <div className="export-case-card__readiness-row">
          <div>
            <div className="export-case-card__readiness-label">Hazırlık</div>
            <div className="export-case-card__readiness-values">
              <span className="export-case-card__readiness-percent">
                %{exportCase.readinessPercent}
              </span>
            </div>
          </div>

          <div className="export-case-card__readiness-meta">
            {exportCase.missingRequirementCount > 0 && (
              <span className="export-case-card__missing">
                <AlertTriangle aria-hidden="true" />
                Eksik: {exportCase.missingRequirementCount}
              </span>
            )}
            {exportCase.blockingIssueCount > 0 && (
              <span className="export-case-card__blocking">
                Bloke: {exportCase.blockingIssueCount}
              </span>
            )}
          </div>
        </div>

        <div
          className="export-case-card__progress-bar-container"
          role="progressbar"
          aria-valuenow={progressWidth}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label={`Hazırlık: %${progressWidth}`}
        >
          <div
            className="export-case-card__progress-bar"
            style={{ width: `${progressWidth}%` }}
          />
        </div>
      </div>

      <div className="export-case-card__footer">
        <Link
          className="export-case-card__link"
          href={`/ihracat/${exportCase.id}`}
        >
          İhracat dosyasını aç
          <ArrowRight aria-hidden="true" />
        </Link>
      </div>
    </Card>
  );
}
