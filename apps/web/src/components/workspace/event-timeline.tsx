import { FACILITY_TIME_ZONE } from "@/lib/presentation/format";
type EventTimelineEntry = Readonly<{
  id: string;
  occurredAtUtc: string;
  category: string;
  eventType: string;
  summary: string;
  quantity?: number | null;
}>;

type EventTimelineProps = Readonly<{
  entries: readonly EventTimelineEntry[];
  emptyMessage?: string;
}>;

export function EventTimeline({ entries, emptyMessage = "Henüz olay kaydı yok." }: EventTimelineProps) {
  if (entries.length === 0) {
    return (
      <p className="production-execution-empty__desc fact-event-timeline__empty" role="status">
        {emptyMessage}
      </p>
    );
  }

  return (
    <div className="fact-event-timeline" data-testid="fact-event-timeline">
      {entries.map((entry) => (
        <article key={entry.id} className="fact-event-row">
          <time className="fact-event-row__time" dateTime={entry.occurredAtUtc}>
            {new Date(entry.occurredAtUtc).toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit", second: "2-digit", timeZone: FACILITY_TIME_ZONE })}
          </time>
          <span className="fact-event-row__category">{entry.category}</span>
          <span className="fact-event-row__summary" title={entry.summary}>
            {entry.eventType ? `${entry.eventType} · ` : ""}{entry.summary}
          </span>
          {entry.quantity != null ? <span className="fact-numeric">{entry.quantity}</span> : <span />}
        </article>
      ))}
    </div>
  );
}
