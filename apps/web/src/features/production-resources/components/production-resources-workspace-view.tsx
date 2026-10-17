"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  Factory,
  ArrowUpRight,
  Boxes,
  Cog,
  Hourglass,
  Pause,
  Play,
  ShieldAlert,
} from "lucide-react";

import { ContextPanel } from "@/components/workspace/context-panel";
import { LineTopology } from "@/components/workspace/line-topology";
import { OperationalBadge, mapOperationalStatus } from "@/components/workspace/operational-status";
import { MetricCard, MetricGrid } from "@/components/ui/metric";
import { PageHeader } from "@/components/ui/page";

import {
  buildProductionResourcesFilterParams,
  clearProductionResourcesFilterParams,
  filterProductionResources,
  hasActiveProductionResourcesFilters,
  readProductionResourcesFilters,
} from "../application/production-resources-filter";
import { BULK_DATA_ONBOARDING_ROUTE } from "@/features/bulk-data-onboarding/domain/bulk-data-onboarding-labels";

import {
  PRODUCTION_RESOURCES_EMPTY,
  PRODUCTION_RESOURCES_ZERO_FACTORY,
  PRODUCTION_RESOURCES_ZERO_FACTORY_CTA,
  PRODUCTION_RESOURCES_SUBTITLE,
  PRODUCTION_RESOURCES_TITLE,
  PRODUCTION_RESOURCE_STATE_FILTER_OPTIONS,
  PRODUCTION_RESOURCE_TYPE_FILTER_OPTIONS,
  getProductionResourceTypeLabel,
  uniqueTopologyStations,
} from "../domain/production-resources-labels";
import type { ProductionResourceWorkspaceApi } from "../server/production-resources-api-client";

import "./production-resources-workspace.css";

import "@/features/apps-catalog/apps-catalog-workspace.css";
import { enumLabel } from "@/lib/presentation/enum-labels";

/** Rows without a value are left out instead of printing a column of dashes. */
function presentRows(rows: readonly { label: string; value: string | number | null | undefined }[]) {
  return rows.filter((row) => row.value !== null && row.value !== undefined && row.value !== "");
}

type ProductionResourcesWorkspaceViewProps = Readonly<{
  workspace: ProductionResourceWorkspaceApi;
}>;

export function ProductionResourcesWorkspaceView({ workspace }: ProductionResourcesWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [selectedResourceId, setSelectedResourceId] = useState<string | null>(null);

  const filters = readProductionResourcesFilters(searchParams);
  const filtersActive = hasActiveProductionResourcesFilters(filters);

  const filteredResources = useMemo(
    () => filterProductionResources(workspace.resources, filters),
    [workspace.resources, filters],
  );

  const lineOptions = useMemo(() => {
    const map = new Map<string, string>();
    for (const row of workspace.resources) {
      map.set(row.lineId, `${row.lineCode} · ${row.lineName}`);
    }
    return [...map.entries()].sort((left, right) => left[1].localeCompare(right[1], "tr"));
  }, [workspace.resources]);

  const showOrderColumn = workspace.resources.some((row) => row.orderReference);
  const showBufferColumn = workspace.resources.some((row) => row.bufferQuantity !== null && row.bufferQuantity !== undefined);
  const columnCount = 7 + (showOrderColumn ? 1 : 0) + (showBufferColumn ? 1 : 0);

  const selectedResource =
    filteredResources.find((row) => row.resourceId === selectedResourceId) ??
    filteredResources[0] ??
    null;

  const selectedLineResources = useMemo(() => {
    if (!selectedResource) return [];
    return workspace.resources.filter((row) => row.lineId === selectedResource.lineId);
  }, [selectedResource, workspace.resources]);

  useEffect(() => {
    if (selectedResourceId && filteredResources.some((row) => row.resourceId === selectedResourceId)) {
      return;
    }
    setSelectedResourceId(filteredResources[0]?.resourceId ?? null);
  }, [filteredResources, selectedResourceId]);

  const applyFilterUpdates = useCallback(
    (updates: Partial<Record<"lineId" | "resourceType" | "state" | "search", string>>) => {
      const params = buildProductionResourcesFilterParams(searchParams, updates);
      router.replace(params.toString() ? `${pathname}?${params.toString()}` : pathname);
    },
    [pathname, router, searchParams],
  );

  const clearFilters = useCallback(() => {
    const params = clearProductionResourcesFilterParams(searchParams);
    router.replace(params.toString() ? `${pathname}?${params.toString()}` : pathname);
  }, [pathname, router, searchParams]);

  return (
    <div className="fact-workspace production-resources-workspace" data-testid="production-resources-workspace">
      <PageHeader
        eyebrow="ÜRETİM"
        title={PRODUCTION_RESOURCES_TITLE}
        description={PRODUCTION_RESOURCES_SUBTITLE}
        actions={
          <Link href="/uretim/hatlar" className="button button-secondary button-medium">
            <Factory aria-hidden="true" size={16} />
            Üretim Hatları
          </Link>
        }
      />

      <div className="production-resources-kpis">
        <MetricGrid label="Kaynak durum özeti">
          <MetricCard label="Toplam" value={workspace.status.total} hint="Kayıtlı" icon={<Boxes size={18} />} />
          <MetricCard label="Çalışıyor" value={workspace.status.running} hint="Üretimde" icon={<Play size={18} />} tone="low" />
          <MetricCard label="Boşta" value={workspace.status.idle} hint="İş yok" icon={<Pause size={18} />} />
          <MetricCard label="Bekliyor" value={workspace.status.waiting} hint="Beklemede" icon={<Hourglass size={18} />} tone="medium" />
          <MetricCard label="Durdu" value={workspace.status.stopped} hint="Durmuş" icon={<Cog size={18} />} tone="high" />
          <MetricCard label="Arıza" value={workspace.status.fault} hint="Müdahale" icon={<AlertTriangle size={18} />} tone="critical" />
          <MetricCard label="Risk" value={workspace.status.atRisk} hint="İzlenmeli" icon={<ShieldAlert size={18} />} tone="high" />
        </MetricGrid>
      </div>

      <div
        className="production-resources-type-chips"
        data-testid="production-resources-type-chips"
        role="toolbar"
        aria-label="Kaynak tipi filtreleri"
      >
        {PRODUCTION_RESOURCE_TYPE_FILTER_OPTIONS.map((option) => {
          const active = (filters.resourceType ?? "") === option.value;
          return (
            <button
              key={option.value || "all"}
              type="button"
              className={`production-resources-type-chips__chip${active ? " production-resources-type-chips__chip--active" : ""}`}
              data-testid={`production-resources-type-chip-${option.value || "all"}`}
              aria-pressed={active}
              onClick={() => applyFilterUpdates({ resourceType: option.value })}
            >
              {option.label}
            </button>
          );
        })}
      </div>

      <div
        className={`fact-command-bar production-resources-filter-rail${filtersActive ? " production-resources-filter-rail--active" : ""}`}
        data-testid="production-resources-filter-rail"
      >
        <div className="fact-command-bar__group">
          <label className="fact-command-bar__field">
            <span>Hat</span>
            <select
              value={filters.lineId ?? ""}
              onChange={(event) => applyFilterUpdates({ lineId: event.target.value })}
              data-testid="production-resources-line-filter"
            >
              <option value="">Tüm hatlar</option>
              {lineOptions.map(([lineId, label]) => (
                <option key={lineId} value={lineId}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label className="fact-command-bar__field">
            <span>Tip</span>
            <select
              value={filters.resourceType ?? ""}
              onChange={(event) => applyFilterUpdates({ resourceType: event.target.value })}
              data-testid="production-resources-type-filter"
            >
              {PRODUCTION_RESOURCE_TYPE_FILTER_OPTIONS.map((option) => (
                <option key={option.value || "all"} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label className="fact-command-bar__field">
            <span>Durum</span>
            <select
              value={filters.state ?? ""}
              onChange={(event) => applyFilterUpdates({ state: event.target.value })}
              data-testid="production-resources-state-filter"
            >
              {PRODUCTION_RESOURCE_STATE_FILTER_OPTIONS.map((option) => (
                <option key={option.value || "all"} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label className="fact-command-bar__field production-resources-filter-rail__search">
            <span>Ara</span>
            <input
              type="search"
              value={filters.search ?? ""}
              placeholder="Kaynak, hat, istasyon"
              onChange={(event) => applyFilterUpdates({ search: event.target.value })}
              data-testid="production-resources-search-filter"
            />
          </label>
        </div>
        <button
          type="button"
          className="commint-button commint-button--ghost"
          disabled={!filtersActive}
          onClick={clearFilters}
          data-testid="production-resources-clear-filters"
        >
          Filtreleri temizle
        </button>
      </div>

      <div className="production-resources-layout">
        <section className="fact-section production-resources-grid-panel">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Kaynak listesi</h2>
            <span className="production-resources-grid__secondary">{filteredResources.length} kayıt</span>
          </header>
          <div className="fact-section__body production-resources-grid-wrap">
            <table aria-label="Kaynak listesi" className="fact-data-grid" data-testid="production-resources-grid">
              <thead>
                <tr>
                  <th className="production-resources-grid__col--name">Kaynak</th>
                  <th className="production-resources-grid__col--type">Tip</th>
                  <th className="production-resources-grid__col--line">Hat</th>
                  <th className="production-resources-grid__col--station">İstasyon</th>
                  <th className="production-resources-grid__col--state">Durum</th>
                  {showOrderColumn ? <th>Sipariş</th> : null}
                  <th className="production-resources-grid__numeric production-resources-grid__col--num">İyi</th>
                  <th className="production-resources-grid__numeric production-resources-grid__col--num">Hatalı</th>
                  {showBufferColumn ? <th className="production-resources-grid__numeric production-resources-grid__col--num">Devam eden iş</th> : null}
                </tr>
              </thead>
              <tbody>
                {filteredResources.length === 0 ? (
                  <tr>
                    <td colSpan={columnCount} className="production-resources-grid__empty">
                      {workspace.resources.length === 0 && !filtersActive ? (
                        <div
                          className="production-resources-zero-factory"
                          data-testid="production-resources-zero-factory"
                        >
                          <p>{PRODUCTION_RESOURCES_ZERO_FACTORY}</p>
                          <div className="production-resources-zero-factory__actions">
                            <Link
                              href={BULK_DATA_ONBOARDING_ROUTE}
                              className="commint-button commint-button--primary"
                            >
                              {PRODUCTION_RESOURCES_ZERO_FACTORY_CTA}
                            </Link>
                            <Link href="/fabrika" className="commint-button commint-button--ghost">
                              Fabrika genel bakış
                            </Link>
                          </div>
                        </div>
                      ) : (
                        PRODUCTION_RESOURCES_EMPTY
                      )}
                    </td>
                  </tr>
                ) : (
                  filteredResources.map((row) => {
                    const selected = row.resourceId === selectedResource?.resourceId;
                    return (
                      <tr
                        key={row.resourceId}
                        data-selected={selected}
                        data-testid={`production-resource-row-${row.resourceId}`}
                        onClick={() => setSelectedResourceId(row.resourceId)}
                      >
                        <td className="production-resources-grid__code">
                          <strong>{row.code}</strong>
                          <span className="production-resources-grid__secondary">{row.name}</span>
                        </td>
                        <td>{getProductionResourceTypeLabel(row.resourceType)}</td>
                        <td className="production-resources-grid__code">{row.lineCode}</td>
                        <td className="production-resources-grid__code">{row.stationCode}</td>
                        <td>
                          <OperationalBadge status={mapOperationalStatus(row.state)} label={enumLabel(row.state)} />
                        </td>
                        {showOrderColumn ? <td>{row.orderReference ?? "—"}</td> : null}
                        <td className="production-resources-grid__numeric">{row.goodCount}</td>
                        <td className="production-resources-grid__numeric">{row.rejectCount}</td>
                        {showBufferColumn ? (
                          <td className="production-resources-grid__numeric">
                            {row.bufferQuantity ?? "—"}
                            {row.bufferCapacity ? ` / ${row.bufferCapacity}` : ""}
                          </td>
                        ) : null}
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </section>

        <aside className="production-resources-inspector">
          {selectedResource ? (
            <>
              <ContextPanel
                title="Kaynak kimliği"
                rows={presentRows([
                  { label: "Kaynak", value: `${selectedResource.code} · ${selectedResource.name}` },
                  { label: "Tip", value: getProductionResourceTypeLabel(selectedResource.resourceType) },
                  { label: "Hat", value: `${selectedResource.lineCode} · ${selectedResource.lineName}` },
                  { label: "Alan", value: selectedResource.areaCode },
                  { label: "İstasyon", value: `${selectedResource.stationCode} · ${selectedResource.stationName}` },
                  { label: "Makine kaydı", value: selectedResource.machineRegistryCode },
                ])}
              />
              <ContextPanel
                title={`Operasyon durumu · ${selectedResource.code}`}
                rows={presentRows([
                  { label: "Durum", value: enumLabel(selectedResource.state) },
                  { label: "Operasyon", value: selectedResource.operationCode },
                  { label: "Operatör", value: selectedResource.operatorCode },
                  { label: "Sipariş", value: selectedResource.orderReference },
                  { label: "İyi", value: selectedResource.goodCount },
                  { label: "Hatalı", value: selectedResource.rejectCount },
                  { label: "Devam eden iş", value: selectedResource.bufferQuantity },
                  { label: "Kısıt", value: selectedResource.isBlocked ? "Bloklu" : selectedResource.isStarved ? "Aç" : null },
                ])}
              />
              <section className="fact-section">
                <header className="fact-section__header">
                  <h3 className="fact-section__title">{`Hat topolojisi · ${selectedResource.lineCode}`}</h3>
                </header>
                <div className="fact-section__body">
                <LineTopology
                  stations={uniqueTopologyStations(selectedLineResources)
                    .map((row) => ({
                      id: row.stationId,
                      code: row.stationCode,
                      name: row.stationName,
                      state: row.state,
                    }))}
                  selectedStationId={selectedResource.stationId}
                  onSelectStation={(stationId) => {
                    const match = selectedLineResources.find((row) => row.stationId === stationId);
                    if (match) setSelectedResourceId(match.resourceId);
                  }}
                />
                </div>
              </section>
              <section className="fact-section production-resources-inspector__context" aria-label="İlgili Bağlam">
                <header className="fact-section__header">
                  <h3 className="fact-section__title">İlgili Bağlam</h3>
                </header>
                <div className="fact-section__body">
                  <div className="production-resources-inspector__links">
                    <Link href={`/uretim/hatlar/${selectedResource.lineId}`} className="button button-secondary button-medium">
                      Hat detayı
                      <ArrowUpRight aria-hidden="true" size={14} />
                    </Link>
                    {selectedResource.machineRegistryCode ? (
                      <Link href="/makineler" className="button button-secondary button-medium">
                        Makine kaydı
                        <ArrowUpRight aria-hidden="true" size={14} />
                      </Link>
                    ) : null}
                  </div>
                </div>
              </section>
            </>
          ) : (
            <p className="production-resources-grid__empty">{PRODUCTION_RESOURCES_EMPTY}</p>
          )}
        </aside>
      </div>
    </div>
  );
}
