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
  getMaintenanceAssetTypeLabel,
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
          <header className="maintenance-section__header">
            <h2>Gecikenler</h2>
          </header>
          {overdueDues.length === 0 ? (
            <p className="maintenance-empty-state">Geciken periyodik bakım vadesi bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
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
                  {overdueDues.map((due) => (
                    <tr key={due.id} className="maintenance-table__row--urgent">
                      <td>
                        <strong>{due.preventivePlanCode ?? due.preventivePlanId}</strong>
                        <small>{due.preventivePlanName ?? "—"}</small>
                      </td>
                      <td>
                        <span className="maintenance-table__cell">{due.assetReference}</span>
                        <small>{getMaintenanceAssetTypeLabel(due.assetType)}</small>
                      </td>
                      <td>
                        <Badge tone="danger">{formatMaintenanceDate(due.dueAtUtc)}</Badge>
                      </td>
                      <td>
                        <Badge tone={getPreventiveDueStatusTone(due.status)}>
                          {getPreventiveDueStatusLabel(due.status)}
                        </Badge>
                      </td>
                      <td>
                        {due.workOrderId ? (
                          <Link href={`/bakim/emirler/${due.workOrderId}`}>
                            {due.workOrderNumber ?? "Emri Aç"}
                          </Link>
                        ) : (
                          "—"
                        )}
                      </td>
                      <td>
                        {due.status === "due" ? (
                          <Button
                            type="button"
                            variant="primary"
                            disabled={pendingKey === `due-${due.id}`}
                            onClick={() =>
                              void runAction(`due-${due.id}`, () =>
                                onCreateWorkOrderFromDue({
                                  dueId: due.id,
                                  expectedVersion: due.version,
                                }),
                              )
                            }
                          >
                            <Wrench aria-hidden="true" />
                            Bakım Emri Oluştur
                          </Button>
                        ) : null}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="maintenance-section maintenance-section--wide" id="yaklasanlar">
          <header className="maintenance-section__header">
            <h2>Yaklaşanlar</h2>
          </header>
          {upcomingDues.length === 0 ? (
            <p className="maintenance-empty-state">Yaklaşan periyodik bakım vadesi bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
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
                  {upcomingDues.map((due) => (
                    <tr key={due.id}>
                      <td>
                        <strong>{due.preventivePlanCode ?? due.preventivePlanId}</strong>
                        <small>{due.preventivePlanName ?? "—"}</small>
                      </td>
                      <td>
                        <span className="maintenance-table__cell">{due.assetReference}</span>
                        <small>{getMaintenanceAssetTypeLabel(due.assetType)}</small>
                      </td>
                      <td>{formatMaintenanceDate(due.dueAtUtc)}</td>
                      <td>
                        <Badge tone={getPreventiveDueStatusTone(due.status)}>
                          {getPreventiveDueStatusLabel(due.status)}
                        </Badge>
                      </td>
                      <td>
                        {due.workOrderId ? (
                          <Link href={`/bakim/emirler/${due.workOrderId}`}>
                            {due.workOrderNumber ?? "Emri Aç"}
                          </Link>
                        ) : (
                          "—"
                        )}
                      </td>
                      <td>
                        {due.status === "due" ? (
                          <Button
                            type="button"
                            variant="secondary"
                            disabled={pendingKey === `due-${due.id}`}
                            onClick={() =>
                              void runAction(`due-${due.id}`, () =>
                                onCreateWorkOrderFromDue({
                                  dueId: due.id,
                                  expectedVersion: due.version,
                                }),
                              )
                            }
                          >
                            Bakım Emri Oluştur
                          </Button>
                        ) : null}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="maintenance-section maintenance-section--wide" id="planlar">
          <header className="maintenance-section__header">
            <h2>Planlar</h2>
            <Link href="/bakim/planlar/yeni">Yeni Plan</Link>
          </header>
          {activePlans.length === 0 ? (
            <p className="maintenance-empty-state">Aktif periyodik bakım planı bulunmuyor.</p>
          ) : (
            <div className="maintenance-dashboard-plans">
              {activePlans.map((plan) => (
                <div key={plan.id} className="maintenance-plan-row">
                  <div className="maintenance-plan-row__meta">
                    <strong>
                      {plan.code} · {plan.name}
                    </strong>
                    <span>
                      {plan.assetReference} ·{" "}
                      {getMaintenanceIntervalLabel(plan.intervalType, plan.intervalValue)}
                    </span>
                    <div className="maintenance-plan-row__tags">
                      <span>{getMaintenanceAssetTypeLabel(plan.assetType)}</span>
                      <span>{getMaintenanceStatusLabel(plan.status)}</span>
                      {plan.nextDueAtUtc ? (
                        <span>Sonraki: {formatMaintenanceDate(plan.nextDueAtUtc)}</span>
                      ) : null}
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant="secondary"
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
