"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { ArrowUpRight } from "lucide-react";

import { ContextPanel } from "@/components/workspace/context-panel";
import { LineTopology } from "@/components/workspace/line-topology";
import { OperationalBadge, mapOperationalStatus } from "@/components/workspace/operational-status";
import { StatusStrip } from "@/components/workspace/status-strip";

import {
  buildProductionResourcesFilterParams,
  clearProductionResourcesFilterParams,
  filterProductionResources,
  hasActiveProductionResourcesFilters,
  readProductionResourcesFilters,
} from "../application/production-resources-filter";
import {
  PRODUCTION_RESOURCES_EMPTY,
  PRODUCTION_RESOURCES_SUBTITLE,
  PRODUCTION_RESOURCES_TITLE,
  PRODUCTION_RESOURCE_STATE_FILTER_OPTIONS,
  PRODUCTION_RESOURCE_TYPE_FILTER_OPTIONS,
  getProductionResourceTypeLabel,
} from "../domain/production-resources-labels";
import type { ProductionResourceWorkspaceApi } from "../server/production-resources-api-client";

import "./production-resources-workspace.css";

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
    <main className="fact-workspace production-resources-workspace" data-testid="production-resources-workspace">
      <header className="fact-workspace__header">
        <div>
          <p className="fact-workspace__context">ÜRETİM · KAYNAKLAR</p>
          <h1 className="fact-workspace__title">{PRODUCTION_RESOURCES_TITLE}</h1>
          <p className="fact-workspace__meta">{PRODUCTION_RESOURCES_SUBTITLE}</p>
        </div>
      </header>

      <StatusStrip
        aria-label="Kaynak durum özeti"
        items={[
          { label: "Toplam", value: workspace.status.total },
          { label: "Çalışıyor", value: workspace.status.running },
          { label: "Boşta", value: workspace.status.idle },
          { label: "Bekliyor", value: workspace.status.waiting },
          { label: "Durdu", value: workspace.status.stopped },
          { label: "Arıza", value: workspace.status.fault },
          { label: "Risk", value: workspace.status.atRisk },
        ]}
      />

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
            <h2 className="fact-section__title">Kaynak grid</h2>
            <span className="production-resources-grid__secondary">{filteredResources.length} kayıt</span>
          </header>
          <div className="fact-section__body production-resources-grid-wrap">
            <table className="fact-data-grid" data-testid="production-resources-grid">
              <thead>
                <tr>
                  <th>Kaynak</th>
                  <th>Tip</th>
                  <th>Hat</th>
                  <th>İstasyon</th>
                  <th>Durum</th>
                  <th>Sipariş</th>
                  <th>GOOD</th>
                  <th>REJECT</th>
                  <th>WIP</th>
                </tr>
              </thead>
              <tbody>
                {filteredResources.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="production-resources-grid__empty">
                      {PRODUCTION_RESOURCES_EMPTY}
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
                        <td>
                          <strong>{row.code}</strong>
                          <span className="production-resources-grid__secondary">{row.name}</span>
                        </td>
                        <td>{getProductionResourceTypeLabel(row.resourceType)}</td>
                        <td>{row.lineCode}</td>
                        <td>{row.stationCode}</td>
                        <td>
                          <OperationalBadge status={mapOperationalStatus(row.state)} label={row.state} />
                        </td>
                        <td>{row.orderReference ?? "—"}</td>
                        <td className="production-resources-grid__numeric">{row.goodCount}</td>
                        <td className="production-resources-grid__numeric">{row.rejectCount}</td>
                        <td className="production-resources-grid__numeric">
                          {row.bufferQuantity ?? "—"}
                          {row.bufferCapacity ? ` / ${row.bufferCapacity}` : ""}
                        </td>
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
                rows={[
                  { label: "Kaynak", value: `${selectedResource.code} · ${selectedResource.name}` },
                  { label: "Tip", value: getProductionResourceTypeLabel(selectedResource.resourceType) },
                  { label: "Hat", value: `${selectedResource.lineCode} · ${selectedResource.lineName}` },
                  { label: "Alan", value: selectedResource.areaCode },
                  { label: "İstasyon", value: `${selectedResource.stationCode} · ${selectedResource.stationName}` },
                  { label: "Makine kaydı", value: selectedResource.machineRegistryCode },
                ]}
              />
              <ContextPanel
                title="Operasyon durumu"
                rows={[
                  { label: "Durum", value: selectedResource.state },
                  { label: "Operasyon", value: selectedResource.operationCode },
                  { label: "Operatör", value: selectedResource.operatorCode },
                  { label: "Sipariş", value: selectedResource.orderReference },
                  { label: "GOOD", value: selectedResource.goodCount },
                  { label: "REJECT", value: selectedResource.rejectCount },
                  { label: "WIP", value: selectedResource.bufferQuantity },
                  { label: "Kısıt", value: selectedResource.isBlocked ? "Bloklu" : selectedResource.isStarved ? "Aç" : "—" },
                ]}
              />
              <section className="fact-section">
                <header className="fact-section__header">
                  <h3 className="fact-section__title">Hat topolojisi</h3>
                </header>
                <div className="fact-section__body">
                <LineTopology
                  stations={selectedLineResources.map((row) => ({
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
              <div className="production-resources-inspector__links">
                <Link href={`/uretim/hatlar/${selectedResource.lineId}`} className="commint-table__link">
                  Hat detayı
                  <ArrowUpRight aria-hidden="true" size={14} />
                </Link>
                {selectedResource.machineRegistryCode ? (
                  <Link href="/makineler" className="commint-table__link">
                    Makine kaydı
                    <ArrowUpRight aria-hidden="true" size={14} />
                  </Link>
                ) : null}
              </div>
            </>
          ) : (
            <p className="production-resources-grid__empty">{PRODUCTION_RESOURCES_EMPTY}</p>
          )}
        </aside>
      </div>
    </main>
  );
}
