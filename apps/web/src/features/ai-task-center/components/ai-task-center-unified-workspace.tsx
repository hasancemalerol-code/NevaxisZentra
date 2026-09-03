"use client";

import Link from "next/link";
import { useMemo } from "react";
import { LayoutGrid, List } from "lucide-react";
import { useSearchParams } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/ui/states";

import {
  filterAiTaskCenterItems,
  hasAnyTaskCenterColumnValue,
} from "../application/ai-task-center-composition";
import type { AiTaskCenterItem } from "../domain/ai-task-center-item";
import {
  AI_TASK_CENTER_TASK_ORIGIN_FILTERS,
  AI_TASK_CENTER_VIEWS,
  parseAiTaskCenterTaskOriginFilter,
  parseAiTaskCenterView,
  type AiTaskCenterTaskOriginFilter,
  type AiTaskCenterView,
} from "../domain/ai-task-center-item";
import { AiTaskCenterItemCard } from "./ai-task-center-item-card";

type AiTaskCenterUnifiedWorkspaceProps = Readonly<{
  items: readonly AiTaskCenterItem[];
  assignedTaskIds: readonly string[];
  listPath: string;
  initialView?: AiTaskCenterView;
  initialOriginFilter?: AiTaskCenterTaskOriginFilter;
}>;

type AiTaskCenterPresentationMode = "cards" | "list";

const DATE_TIME_FORMATTER = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "short",
  timeStyle: "short",
  timeZone: "Europe/Istanbul",
});

function formatDateTime(value: string | null): string | null {
  if (!value) {
    return null;
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return null;
  }

  return DATE_TIME_FORMATTER.format(parsed);
}

function parsePresentationMode(value: string | null): AiTaskCenterPresentationMode {
  return value === "liste" ? "list" : "cards";
}

function buildCenterHref(
  listPath: string,
  view: AiTaskCenterView,
  originFilter: AiTaskCenterTaskOriginFilter,
  presentationMode: AiTaskCenterPresentationMode,
): string {
  const params = new URLSearchParams();
  params.set("gorunum", view);

  if (view === "all-tasks" || view === "assigned" || view === "completed" || view === "unassigned") {
    params.set("kaynak", originFilter);
  }

  if (presentationMode === "list") {
    params.set("gosterim", "liste");
  }

  const query = params.toString();
  return query.length > 0 ? `${listPath}?${query}` : listPath;
}

function countItemsForView(
  items: readonly AiTaskCenterItem[],
  viewKey: AiTaskCenterView,
  assignedIds: ReadonlySet<string>,
  originFilter: AiTaskCenterTaskOriginFilter,
): number {
  return filterAiTaskCenterItems(items, viewKey, assignedIds, originFilter).length;
}

type AiTaskCenterUnifiedRowProps = Readonly<{
  item: AiTaskCenterItem;
  showFacilityColumn: boolean;
  showAssigneeColumn: boolean;
  showTeamColumn: boolean;
  showSlaColumn: boolean;
  showDueDateColumn: boolean;
  showEscalationColumn: boolean;
}>;

function AiTaskCenterUnifiedRow({
  item,
  showFacilityColumn,
  showAssigneeColumn,
  showTeamColumn,
  showSlaColumn,
  showDueDateColumn,
  showEscalationColumn,
}: AiTaskCenterUnifiedRowProps) {
  const occurredLabel = formatDateTime(item.occurredAt);

  return (
    <tr data-testid={`ai-task-center-item-${item.nativeSourceKind}-${item.nativeSourceId}`}>
      <td>
        <Badge tone="neutral">{item.sourceLabel}</Badge>
      </td>
      <td>
        <Link href={item.deepLinkPath}>{item.title}</Link>
      </td>
      {showFacilityColumn ? <td>{item.facilityLabel}</td> : null}
      {showAssigneeColumn ? <td>{item.assigneeLabel ?? "—"}</td> : null}
      {showTeamColumn ? <td>{item.teamLabel ?? "—"}</td> : null}
      {showSlaColumn ? <td>{item.slaLabel ?? "—"}</td> : null}
      {showDueDateColumn ? <td>{item.dueAtLabel ?? occurredLabel ?? "—"}</td> : null}
      {showEscalationColumn ? <td>{item.escalationLabel ?? "—"}</td> : null}
      <td>{item.priorityLabel ?? "—"}</td>
      <td>{item.statusLabel ?? "—"}</td>
      <td>{item.relatedContext ?? "—"}</td>
      <td>
        <Link href={item.deepLinkPath}>Aç</Link>
      </td>
    </tr>
  );
}

export function AiTaskCenterUnifiedWorkspace({
  items,
  assignedTaskIds,
  listPath,
  initialView,
  initialOriginFilter,
}: AiTaskCenterUnifiedWorkspaceProps) {
  const searchParams = useSearchParams();

  const view = parseAiTaskCenterView(initialView ?? searchParams.get("gorunum"));
  const originFilter = parseAiTaskCenterTaskOriginFilter(
    initialOriginFilter ?? searchParams.get("kaynak"),
  );
  const presentationMode = parsePresentationMode(searchParams.get("gosterim"));

  const assignedIds = useMemo(
    () => new Set(assignedTaskIds),
    [assignedTaskIds],
  );

  const visibleItems = useMemo(
    () => filterAiTaskCenterItems(items, view, assignedIds, originFilter),
    [assignedIds, items, originFilter, view],
  );

  const viewCounts = useMemo(() => {
    const counts = {} as Record<AiTaskCenterView, number>;

    for (const viewKey of Object.keys(AI_TASK_CENTER_VIEWS) as AiTaskCenterView[]) {
      counts[viewKey] = countItemsForView(items, viewKey, assignedIds, originFilter);
    }

    return counts;
  }, [assignedIds, items, originFilter]);

  const showOriginFilters =
    view === "all-tasks"
    || view === "assigned"
    || view === "completed"
    || view === "unassigned";

  const showFacilityColumn = useMemo(
    () => hasAnyTaskCenterColumnValue(visibleItems, (item) => item.facilityLabel),
    [visibleItems],
  );

  const showAssigneeColumn = useMemo(
    () => hasAnyTaskCenterColumnValue(visibleItems, (item) => item.assigneeLabel),
    [visibleItems],
  );

  const showTeamColumn = useMemo(
    () => hasAnyTaskCenterColumnValue(visibleItems, (item) => item.teamLabel),
    [visibleItems],
  );

  const showSlaColumn = useMemo(
    () => hasAnyTaskCenterColumnValue(visibleItems, (item) => item.slaLabel),
    [visibleItems],
  );

  const showDueDateColumn = useMemo(
    () => hasAnyTaskCenterColumnValue(
      visibleItems,
      (item) => item.dueAtLabel ?? item.occurredAt,
    ),
    [visibleItems],
  );

  const showEscalationColumn = useMemo(
    () => hasAnyTaskCenterColumnValue(visibleItems, (item) => item.escalationLabel),
    [visibleItems],
  );

  const cardsHref = buildCenterHref(listPath, view, originFilter, "cards");
  const listHref = buildCenterHref(listPath, view, originFilter, "list");

  return (
    <div className="ai-task-center-unified" data-testid="ai-task-center-unified-workspace">
      <div className="ai-task-center-unified__toolbar">
        <div
          aria-label="AI Görev Merkezi görünümleri"
          className="ai-task-center-unified__view-rail"
          role="tablist"
        >
          {(Object.entries(AI_TASK_CENTER_VIEWS) as Array<[AiTaskCenterView, string]>).map(
            ([viewKey, label]) => {
              const href = buildCenterHref(listPath, viewKey, originFilter, presentationMode);
              const active = viewKey === view;
              const count = viewCounts[viewKey];

              return (
                <Link
                  aria-selected={active}
                  className="ai-task-center-unified__view-tab"
                  data-testid={`ai-task-center-view-${viewKey}`}
                  href={href}
                  key={viewKey}
                  role="tab"
                >
                  <span>{label}</span>
                  <span aria-hidden="true" className="ai-task-center-unified__view-count">
                    {count}
                  </span>
                </Link>
              );
            },
          )}
        </div>

        <div
          aria-label="Görev listesi görünüm modu"
          className="ai-task-center-unified__layout-toggle"
          role="group"
        >
          <Link
            aria-pressed={presentationMode === "cards"}
            className="ai-task-center-unified__layout-button"
            data-testid="ai-task-center-view-cards"
            href={cardsHref}
          >
            <LayoutGrid aria-hidden="true" />
            Kart
          </Link>
          <Link
            aria-pressed={presentationMode === "list"}
            className="ai-task-center-unified__layout-button"
            data-testid="ai-task-center-view-list"
            href={listHref}
          >
            <List aria-hidden="true" />
            Liste görünümü
          </Link>
        </div>
      </div>

      {showOriginFilters ? (
        <div
          aria-label="Görev kaynağı filtreleri"
          className="ai-task-center-unified__origin-filters"
        >
          {(Object.entries(AI_TASK_CENTER_TASK_ORIGIN_FILTERS) as Array<
            [AiTaskCenterTaskOriginFilter, string]
          >).map(([filterKey, label]) => {
            const href = buildCenterHref(listPath, view, filterKey, presentationMode);
            const active = filterKey === originFilter;

            return (
              <Link
                aria-pressed={active}
                className="ai-task-center-unified__origin-filter"
                data-testid={`ai-task-center-origin-${filterKey}`}
                href={href}
                key={filterKey}
              >
                {label}
              </Link>
            );
          })}
        </div>
      ) : null}

      <p className="ai-task-center-unified__summary" data-testid="ai-task-center-result-count">
        {visibleItems.length} kayıt
      </p>

      {visibleItems.length > 0 ? (
        presentationMode === "list" ? (
          <div className="ai-task-center-unified__table-wrap">
            <table className="ai-task-center-unified__table">
              <thead>
                <tr>
                  <th>Kaynak</th>
                  <th>Başlık</th>
                  {showFacilityColumn ? <th>Tesis</th> : null}
                  {showAssigneeColumn ? <th>Atanan</th> : null}
                  {showTeamColumn ? <th>Ekip</th> : null}
                  {showSlaColumn ? <th>SLA</th> : null}
                  {showDueDateColumn ? <th>Son Tarih</th> : null}
                  {showEscalationColumn ? <th>Eskalasyon</th> : null}
                  <th>Öncelik</th>
                  <th>Durum</th>
                  <th>İlişkili bağlam</th>
                  <th>Bağlantı</th>
                </tr>
              </thead>
              <tbody>
                {visibleItems.map((item) => (
                  <AiTaskCenterUnifiedRow
                    item={item}
                    key={item.dedupeKey}
                    showAssigneeColumn={showAssigneeColumn}
                    showDueDateColumn={showDueDateColumn}
                    showEscalationColumn={showEscalationColumn}
                    showFacilityColumn={showFacilityColumn}
                    showSlaColumn={showSlaColumn}
                    showTeamColumn={showTeamColumn}
                  />
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div
            className="ai-task-center-unified__card-grid"
            data-testid="ai-task-center-card-grid"
          >
            {visibleItems.map((item) => (
              <AiTaskCenterItemCard item={item} key={item.dedupeKey} />
            ))}
          </div>
        )
      ) : (
        <EmptyState
          description="Seçili görünümde gösterilecek kayıt bulunmuyor."
          title="Kayıt bulunamadı"
        />
      )}
    </div>
  );
}
