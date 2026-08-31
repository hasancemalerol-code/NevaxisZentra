import { ArrowUpRight, Inbox } from "lucide-react";
import Link from "next/link";

import {
  TEAMS_WORK_INBOX_DISCLAIMER,
  TEAMS_WORK_INBOX_SOURCE_KINDS,
} from "@/features/teams/domain/teams-constants";
import { readTeamsWorkInbox } from "@/features/teams/server/teams-queries";

const MAX_PRIORITY_ITEMS = 5;
const WORK_INBOX_HREF = "/teams/is-kutum";

function sourceLabel(sourceKind: string): string {
  return TEAMS_WORK_INBOX_SOURCE_KINDS[sourceKind as keyof typeof TEAMS_WORK_INBOX_SOURCE_KINDS]
    ?? sourceKind;
}

function formatOccurredAt(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return null;
  }

  return new Intl.DateTimeFormat("tr-TR", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  }).format(parsed);
}

export async function OperasyonOncelikleriPanel() {
  const result = await readTeamsWorkInbox();
  const unavailable = result.status === "failure";
  const items = unavailable ? [] : result.value.items.slice(0, MAX_PRIORITY_ITEMS);
  const hasItems = items.length > 0;

  return (
    <section
      aria-labelledby="operasyon-oncelikleri-title"
      className="dashboard-panel dashboard-panel--operasyon-oncelikleri"
      data-testid="dashboard-operasyon-oncelikleri-panel"
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Teams iş kutusu özeti
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="operasyon-oncelikleri-title"
          >
            Operasyon Öncelikleri
          </h2>
        </div>

        {hasItems ? (
          <div className="dashboard-section-heading__meta">
            <Inbox aria-hidden="true" size={16} strokeWidth={1.8} />
            {items.length} öncelikli iş
          </div>
        ) : null}
      </div>

      {unavailable ? (
        <p
          className="command-center__operasyon-oncelikleri-empty"
          data-testid="dashboard-operasyon-oncelikleri-unavailable"
        >
          İş öncelikleri şu anda görüntülenemiyor. Kaynak erişimi doğrulandığında
          bekleyen işler burada listelenir.
        </p>
      ) : hasItems ? (
        <ul
          className="command-center__operasyon-oncelikleri-list"
          data-testid="dashboard-operasyon-oncelikleri-list"
        >
          {items.map((item) => {
            const occurredLabel = formatOccurredAt(item.occurredAt);

            return (
              <li key={`${item.sourceKind}:${item.sourceId}`}>
                <Link
                  className="command-center__operasyon-oncelikleri-row"
                  data-testid={`dashboard-operasyon-oncelikleri-item-${item.sourceKind}-${item.sourceId}`}
                  href={item.deepLinkPath}
                >
                  <span className="command-center__operasyon-oncelikleri-source">
                    {sourceLabel(item.sourceKind)}
                  </span>

                  <span className="command-center__operasyon-oncelikleri-copy">
                    <strong>{item.title}</strong>
                    <span>{item.reason}</span>
                  </span>

                  {occurredLabel ? (
                    <time
                      className="command-center__operasyon-oncelikleri-time"
                      dateTime={item.occurredAt}
                    >
                      {occurredLabel}
                    </time>
                  ) : (
                    <span
                      aria-hidden="true"
                      className="command-center__operasyon-oncelikleri-time command-center__operasyon-oncelikleri-time--empty"
                    />
                  )}

                  <ArrowUpRight
                    aria-hidden="true"
                    className="command-center__operasyon-oncelikleri-arrow"
                    size={16}
                    strokeWidth={1.8}
                  />
                </Link>
              </li>
            );
          })}
        </ul>
      ) : (
        <p
          className="command-center__operasyon-oncelikleri-empty"
          data-testid="dashboard-operasyon-oncelikleri-empty"
        >
          Bu oturumda görüntüleyebileceğiniz bekleyen iş yok. Yeni onay, vardiya
          devri, etiket veya AI görevi geldiğinde burada görünür.
        </p>
      )}

      <div className="command-center__operasyon-oncelikleri-footer">
        <Link
          data-testid="dashboard-operasyon-oncelikleri-inbox-link"
          href={WORK_INBOX_HREF}
        >
          Tüm iş kutumu aç
          <ArrowUpRight aria-hidden="true" size={14} strokeWidth={1.8} />
        </Link>

        <p className="command-center__operasyon-oncelikleri-disclaimer">
          {TEAMS_WORK_INBOX_DISCLAIMER}
        </p>
      </div>
    </section>
  );
}
