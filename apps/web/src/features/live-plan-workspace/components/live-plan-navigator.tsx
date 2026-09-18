"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";

import { getProductionPlanStatusLabel } from "@/features/production-planning/presentation/production-plan-labels";

import { buildLivePlanWorkspaceHref } from "../application/live-plan-workspace-selection";
import type { LivePlanWorkspacePayload } from "../domain/live-plan-workspace-types";

type LivePlanNavigatorProps = Readonly<{
  workspace: LivePlanWorkspacePayload;
}>;

export function LivePlanNavigator({ workspace }: LivePlanNavigatorProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { selection, productionPlans, lineContexts } = workspace;

  return (
    <aside className="live-plan-workspace__panel" data-testid="live-plan-navigator">
      <div className="live-plan-workspace__panel-header">
        <h2>Planlar</h2>
        <Link href="/planlama/yeni" className="fact-link-button">Yeni</Link>
      </div>
      <div className="live-plan-workspace__panel-body">
        <input
          type="search"
          className="live-plan-navigator__search shell-focus-ring"
          placeholder="Plan veya sipariş ara…"
          value={selection.search ?? ""}
          onChange={(event) => {
            const href = buildLivePlanWorkspaceHref(pathname, {
              ...selection,
              search: event.target.value || undefined,
            });
            router.replace(href);
          }}
          aria-label="Plan arama"
        />

        <ul className="live-plan-navigator__list">
          {productionPlans.map((plan) => {
            const active = plan.id === selection.productionPlanId;
            const href = buildLivePlanWorkspaceHref(pathname, {
              ...selection,
              productionPlanId: plan.id,
            });
            return (
              <li key={plan.id}>
                <Link
                  href={href}
                  className="live-plan-navigator__button shell-focus-ring"
                  data-active={active ? "true" : "false"}
                >
                  <strong>{plan.referenceCode}</strong>
                  <span>{plan.title}</span>
                  <span className="live-plan-navigator__meta">
                    {getProductionPlanStatusLabel(plan.status)}
                  </span>
                </Link>
              </li>
            );
          })}
        </ul>

        {lineContexts.length > 0 ? (
          <div className="live-plan-line-list" data-testid="live-plan-line-list">
            <h3 className="live-plan-navigator__meta">Hat bağlamı</h3>
            {lineContexts.map((line) => {
              const active = line.lineId === selection.lineId;
              const href = buildLivePlanWorkspaceHref(pathname, {
                ...selection,
                lineId: active ? undefined : line.lineId,
              });
              return (
                <Link
                  key={line.lineId}
                  href={href}
                  className="live-plan-line-button shell-focus-ring"
                  data-active={active ? "true" : "false"}
                >
                  <span>{line.lineCode}</span>
                  <span className="live-plan-navigator__meta">
                    {line.constraintSummary ?? line.lineName}
                  </span>
                </Link>
              );
            })}
          </div>
        ) : null}
      </div>
    </aside>
  );
}
