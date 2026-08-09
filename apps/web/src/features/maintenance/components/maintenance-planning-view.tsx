"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { CalendarClock, Wrench } from "lucide-react";
import { useState } from "react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type {
  PreventiveMaintenanceDueSummary,
  PreventiveMaintenancePlanSummary,
} from "../domain/maintenance";
import {
  formatMaintenanceDate,
  getMaintenanceIntervalLabel,
  getMaintenanceStatusLabel,
  getPreventiveDueStatusLabel,
  getPreventiveDueStatusTone,
  isOverduePreventiveDue,
  isUpcomingPreventiveDue,
} from "../presentation/maintenance-labels";
import type {
  createWorkOrderFromPreventiveDueAction,
  generatePreventiveNextDueAction,
} from "../server/maintenance-writer";
import {
  MaintenanceAssetCell,
  MaintenanceEmptyState,
  MaintenanceSectionHeader,
  MaintenanceTableActionGroup,
  MaintenanceTableActionLink,
  MaintenanceTableIdentifierCell,
} from "./maintenance-table-primitives";
import { MaintenanceWorkspaceNav } from "./maintenance-workspace-nav";
import "./maintenance-workspace.css";

type MaintenancePlanningViewProps = Readonly<{
  dues: readonly PreventiveMaintenanceDueSummary[];
  plans: readonly PreventiveMaintenancePlanSummary[];
  onGenerateNextDue: typeof generatePreventiveNextDueAction;
  onCreateWorkOrderFromDue: typeof createWorkOrderFromPreventiveDueAction;
}>;

function sortByDueDate(
  items: readonly PreventiveMaintenanceDueSummary[],
): PreventiveMaintenanceDueSummary[] {
  return [...items].sort((a, b) => a.dueAtUtc.localeCompare(b.dueAtUtc));
}

function renderDueTable(
  items: readonly PreventiveMaintenanceDueSummary[],
  options: Readonly<{
    urgent?: boolean;
    pendingKey: string | null;
    onCreateWorkOrder: (due: PreventiveMaintenanceDueSummary) => void;
    actionVariant: "primary" | "secondary";
  }>,
) {
  return (
    <div className="maintenance-table-wrap maintenance-table-wrap--desktop-fit">
      <table className="maintenance-table maintenance-table--compact maintenance-table--planning">
        <colgroup>
          <col className="maintenance-table__col--plan" />
          <col className="maintenance-table__col--asset" />
          <col className="maintenance-table__col--date" />
          <col className="maintenance-table__col--status" />
          <col className="maintenance-table__col--order" />
          <col className="maintenance-table__col--actions-wide" />
        </colgroup>
        <thead>
          <tr>
            <th>Plan</th>
            <th>Varlık</th>
            <th>Vade</th>
            <th>Durum</th>
            <th>Emir</th>
            <th>İşlem</th>
          </tr>
        </thead>
        <tbody>
          {items.map((due) => (
            <tr
              key={due.id}
              className={options.urgent ? "maintenance-table__row--urgent" : undefined}
            >
              <td>
                <MaintenanceTableIdentifierCell
                  primary={<strong>{due.preventivePlanCode ?? due.preventivePlanId}</strong>}
                  secondary={due.preventivePlanName ?? "—"}
                />
              </td>
              <td>
                <MaintenanceAssetCell
                  assetReference={due.assetReference}
                  assetType={due.assetType}
                />
              </td>
              <td>
                {options.urgent ? (
                  <Badge tone="danger">{formatMaintenanceDate(due.dueAtUtc)}</Badge>
                ) : (
                  <span className="maintenance-table__cell--compact">
                    {formatMaintenanceDate(due.dueAtUtc)}
                  </span>
                )}
              </td>
              <td>
                <Badge tone={getPreventiveDueStatusTone(due.status)}>
                  {getPreventiveDueStatusLabel(due.status)}
                </Badge>
              </td>
              <td>
                {due.workOrderId ? (
                  <MaintenanceTableActionLink href={`/bakim/emirler/${due.workOrderId}`}>
                    {due.workOrderNumber ?? "Aç"}
                  </MaintenanceTableActionLink>
                ) : (
                  "—"
                )}
              </td>
              <td>
                {due.status === "due" ? (
                  <MaintenanceTableActionGroup>
                    <Button
                      type="button"
                      variant={options.actionVariant}
                      size="small"
                      leadingIcon={<Wrench aria-hidden="true" />}
                      disabled={options.pendingKey === `due-${due.id}`}
                      onClick={() => options.onCreateWorkOrder(due)}
                    >
                      Bakım Emri Oluştur
                    </Button>
                  </MaintenanceTableActionGroup>
                ) : null}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function MaintenancePlanningView({
  dues,
  plans,
  onGenerateNextDue,
  onCreateWorkOrderFromDue,
}: MaintenancePlanningViewProps) {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [pendingKey, setPendingKey] = useState<string | null>(null);

  const overdueDues = sortByDueDate(dues.filter(isOverduePreventiveDue));
  const upcomingDues = sortByDueDate(dues.filter(isUpcomingPreventiveDue));
  const activePlans = [...plans]
    .filter((plan) => plan.status === "active")
    .sort((a, b) => a.code.localeCompare(b.code));

  async function runAction(
    key: string,
    action: () => Promise<{ status: string; message?: string; id?: string }>,
  ) {
    setPendingKey(key);
    setMessage(null);
    const result = await action();
    setPendingKey(null);

    if (result.status === "failure") {
      setMessage(result.message ?? "İşlem başarısız.");
      return;
    }

    if (result.id) {
      router.push(`/bakim/emirler/${result.id}`);
      return;
    }

    router.refresh();
  }

  function handleCreateWorkOrder(due: PreventiveMaintenanceDueSummary) {
    void runAction(`due-${due.id}`, () =>
      onCreateWorkOrderFromDue({
        dueId: due.id,
        expectedVersion: due.version,
      }),
    );
  }

  return (
    <main className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <div className="maintenance-page-header__intro">
          <p>BAKIM &amp; ARIZA</p>
          <h1>Bakım Planlama</h1>
          <span>Periyodik bakım vadelerini izleyin ve bakım emirlerini planlayın.</span>
        </div>
        <div className="maintenance-page-header__actions">
          <Link
            href="/bakim/planlar/yeni"
            className="maintenance-dashboard-action maintenance-dashboard-action--secondary"
          >
            <CalendarClock aria-hidden="true" />
            Yeni Periyodik Plan
          </Link>
        </div>
      </header>

      <MaintenanceWorkspaceNav active="planning" />

      {message ? (
        <div role="alert" className="maintenance-error-panel">
          <p>{message}</p>
        </div>
      ) : null}

      <div className="maintenance-page-content">
        <section className="maintenance-section maintenance-section--wide" id="gecikenler">
          <MaintenanceSectionHeader
            title="Gecikenler"
            className="maintenance-section__header"
          />
          {overdueDues.length === 0 ? (
            <MaintenanceEmptyState
              icon={<CalendarClock aria-hidden="true" size={18} />}
              title="Geciken periyodik bakım vadesi bulunmuyor"
              description="Tüm vadeler planlanan tarihler içinde."
            />
          ) : (
            renderDueTable(overdueDues, {
              urgent: true,
              pendingKey,
              onCreateWorkOrder: handleCreateWorkOrder,
              actionVariant: "primary",
            })
          )}
        </section>

        <section className="maintenance-section maintenance-section--wide" id="yaklasanlar">
          <MaintenanceSectionHeader title="Yaklaşanlar" className="maintenance-section__header" />
          {upcomingDues.length === 0 ? (
            <MaintenanceEmptyState
              icon={<CalendarClock aria-hidden="true" size={18} />}
              title="Yaklaşan periyodik bakım vadesi bulunmuyor"
              description="Yeni vadeler planlandığında burada listelenecek."
            />
          ) : (
            renderDueTable(upcomingDues, {
              pendingKey,
              onCreateWorkOrder: handleCreateWorkOrder,
              actionVariant: "secondary",
            })
          )}
        </section>

        <section className="maintenance-section maintenance-section--wide" id="planlar">
          <MaintenanceSectionHeader
            title="Planlar"
            actionHref="/bakim/planlar/yeni"
            actionLabel="Yeni Plan"
            className="maintenance-section__header"
          />
          {activePlans.length === 0 ? (
            <MaintenanceEmptyState
              icon={<CalendarClock aria-hidden="true" size={18} />}
              title="Aktif periyodik bakım planı bulunmuyor"
              description="Yeni plan ekleyerek periyodik bakım döngüsünü başlatın."
            />
          ) : (
            <div className="maintenance-dashboard-plans">
              {activePlans.map((plan) => (
                <div key={plan.id} className="maintenance-plan-row">
                  <div className="maintenance-plan-row__meta">
                    <strong>
                      {plan.code} · {plan.name}
                    </strong>
                    <MaintenanceAssetCell
                      assetReference={plan.assetReference}
                      assetType={plan.assetType}
                    />
                    <div className="maintenance-plan-row__tags">
                      <span>{getMaintenanceIntervalLabel(plan.intervalType, plan.intervalValue)}</span>
                      <span>{getMaintenanceStatusLabel(plan.status)}</span>
                      {plan.nextDueAtUtc ? (
                        <span>Sonraki: {formatMaintenanceDate(plan.nextDueAtUtc)}</span>
                      ) : null}
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant="secondary"
                    size="small"
                    disabled={pendingKey === `plan-${plan.id}`}
                    onClick={() =>
                      void runAction(`plan-${plan.id}`, () => onGenerateNextDue(plan.id))
                    }
                  >
                    Sonraki Vade Oluştur
                  </Button>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </main>
  );
}
