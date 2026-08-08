import { Activity, Bot, Cable, UserRound } from "lucide-react";

import type { ExportActivity, ExportActivitySourceType } from "../domain/export-case";

function formatUtcDateTime(iso: string): string {
  try {
    return new Date(iso).toLocaleString("tr-TR", {
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

function sourceLabel(sourceType: ExportActivitySourceType): string {
  switch (sourceType) {
    case "user":
      return "Kullanıcı";
    case "integration":
      return "Entegrasyon";
    case "ai":
      return "AI";
    case "system":
    default:
      return "Sistem";
  }
}

function SourceIcon({ sourceType }: { sourceType: ExportActivitySourceType }) {
  switch (sourceType) {
    case "user":
      return <UserRound aria-hidden="true" />;
    case "integration":
      return <Cable aria-hidden="true" />;
    case "ai":
      return <Bot aria-hidden="true" />;
    case "system":
    default:
      return <Activity aria-hidden="true" />;
  }
}

type ExportActivityTimelineProps = Readonly<{
  activities: readonly ExportActivity[];
}>;

export function ExportActivityTimeline({
  activities,
}: ExportActivityTimelineProps) {
  const sorted = [...activities].sort((a, b) =>
    b.occurredAtUtc.localeCompare(a.occurredAtUtc),
  );

  return (
    <section className="export-activity-timeline">
      <div className="export-activity-timeline__heading">
        <div>
          <p>AKTİVİTE</p>
          <h2>Dosya zaman çizelgesi</h2>
          <span>
            İhracat dosyasındaki kayıt, doğrulama ve durum değişikliklerinin
            denetlenebilir geçmişi.
          </span>
        </div>
      </div>

      {sorted.length === 0 ? (
        <div className="export-activity-timeline__empty" role="status">
          <Activity aria-hidden="true" />
          <p>Henüz aktivite kaydı yok.</p>
        </div>
      ) : (
        <ol className="export-activity-timeline__list">
          {sorted.map((activity) => (
            <li key={activity.id} className="export-activity-timeline__item">
              <div
                className={`export-activity-timeline__icon export-activity-timeline__icon--${activity.sourceType}`}
              >
                <SourceIcon sourceType={activity.sourceType} />
              </div>

              <div className="export-activity-timeline__content">
                <div className="export-activity-timeline__meta">
                  <time dateTime={activity.occurredAtUtc}>
                    {formatUtcDateTime(activity.occurredAtUtc)}
                  </time>
                  <span>{sourceLabel(activity.sourceType)}</span>
                </div>

                <h3>{activity.title}</h3>

                {activity.summary ? <p>{activity.summary}</p> : null}

                <small>{activity.eventType}</small>
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
