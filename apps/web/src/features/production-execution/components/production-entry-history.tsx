import { displayActor } from "@/lib/presentation/display-label";
import { History } from "lucide-react";
import type { ProductionExecutionEntry } from "../domain/production-execution-entry";
import { getEntryTypeLabel, getScrapReasonLabel } from "../domain/production-execution-entry";

function formatTime(iso: string): string {
  try {
    return new Date(iso).toLocaleString("tr-TR", {
      day: "2-digit",
      month: "short",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

function formatSubjectId(id: string): string {
  return displayActor(id);
  return id;
}

function formatQuantityLabel(entry: ProductionExecutionEntry): string {
  const qty = entry.quantity.toLocaleString("tr-TR", { maximumFractionDigits: 0 });
  switch (entry.entryType) {
    case "good-production":
      return `+${qty} adet İyi Üretim`;
    case "scrap":
      return `+${qty} adet Fire`;
    case "reversal":
      return `${qty} adet — Düzeltme / Geri Alma`;
    default:
      return `${qty} ${entry.unit}`;
  }
}

type ProductionEntryHistoryProps = Readonly<{
  entries: ProductionExecutionEntry[];
}>;

export function ProductionEntryHistory({ entries }: ProductionEntryHistoryProps) {
  return (
    <div className="production-entry-history">
      <div className="production-entry-history__header">
        <History aria-hidden="true" />

        <div>
          <p className="production-entry-history__header-eyebrow">
            KAYIT GEÇMİŞİ
          </p>
          <h2>Üretim Geçmişi</h2>
        </div>
      </div>

      {entries.length === 0 ? (
        <p className="production-entry-history__empty">
          Henüz üretim kaydı yok.
        </p>
      ) : (
        <div className="production-entry-history__list" role="list">
          {entries.map((entry) => (
            <div
              key={entry.id}
              role="listitem"
              className={[
                "production-entry-history__item",
                entry.isReversed
                  ? "production-entry-history__item--reversed"
                  : "",
                entry.entryType === "reversal"
                  ? "production-entry-history__item--reversal"
                  : "",
              ]
                .filter(Boolean)
                .join(" ")}
            >
              <div className="production-entry-history__item-type">
                <span
                  className={`entry-type-badge entry-type-badge--${entry.entryType}`}
                >
                  {getEntryTypeLabel(entry.entryType)}
                </span>

                {entry.isReversed && (
                  <span className="entry-type-badge entry-type-badge--cancelled">
                    İptal
                  </span>
                )}
              </div>

              <div className="production-entry-history__item-details">
                <strong>{formatQuantityLabel(entry)}</strong>

                {entry.reasonCode && (
                  <span className="production-entry-history__reason">
                    {getScrapReasonLabel(entry.reasonCode)}
                  </span>
                )}

                {entry.comment && (
                  <span className="production-entry-history__comment">
                    &ldquo;{entry.comment}&rdquo;
                  </span>
                )}
              </div>

              <div className="production-entry-history__item-meta">
                <time dateTime={entry.recordedAtUtc}>
                  {formatTime(entry.recordedAtUtc)}
                </time>
                <span title={entry.recordedBySubjectId}>
                  {formatSubjectId(entry.recordedBySubjectId)}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
