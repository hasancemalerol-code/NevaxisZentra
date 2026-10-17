"use client";

import Link from "next/link";
import { knownStateLabel } from "@/lib/presentation/enum-labels";

import { ContextPanel } from "@/components/workspace/context-panel";
import { getProductionResourceTypeLabel } from "@/features/production-resources/domain/production-resources-labels";
import { buildFactoryWorkspaceHref } from "../application/factory-workspace-selection";
import type { FactoryWorkspacePayload } from "../domain/factory-workspace-types";
import { FactoryMachineIntelligence } from "./factory-machine-intelligence";
import { displayLabel } from "@/lib/presentation/display-label";

type FactoryResourceInspectorProps = Readonly<{
  workspace: FactoryWorkspacePayload;
}>;

function buildReturnHref(workspace: FactoryWorkspacePayload): string {
  return buildFactoryWorkspaceHref("/fabrika", workspace.selection);
}

export function FactoryResourceInspector({ workspace }: FactoryResourceInspectorProps) {
  const resource = workspace.selectedResource;
  const returnHref = buildReturnHref(workspace);
  const risk =
    resource?.machineRegistryCode
      ? workspace.risks.find((item) => item.machineCode === resource.machineRegistryCode) ?? null
      : null;

  if (!resource) {
    return (
      <div className="factory-inspector-sections" data-testid="factory-inspector-empty">
        <ContextPanel
          title="Bağlam"
          rows={[
            { label: "Fabrika", value: displayLabel(workspace.facilityLabel, "Fabrika") },
            { label: "Kaynak", value: workspace.resources.status.total },
            { label: "Vardiya", value: workspace.shift?.shiftLabel ?? "—" },
            { label: "Açık konu", value: workspace.shift?.openItems ?? "—" },
          ]}
        />
        <p className="fact-workspace__meta">Kaynak veya istasyon seçerek detay inceleyin.</p>
      </div>
    );
  }

  const isBuffer =
    resource.resourceType.toLowerCase().includes("buffer")
    || resource.stationKind.toLowerCase().includes("buffer");
  const isHuman =
    resource.resourceType.toLowerCase().includes("human")
    || resource.stationKind.toLowerCase().includes("assembly")
    || resource.stationKind.toLowerCase().includes("manual");
  const isQuality =
    resource.stationKind.toLowerCase().includes("quality")
    || resource.stationKind.toLowerCase().includes("test");

  const identityRows = [
    { label: "Kimlik", value: `${resource.code} · ${resource.name}` },
    { label: "Tür", value: getProductionResourceTypeLabel(resource.resourceType) },
    { label: "Hat", value: `${resource.lineCode} · ${resource.lineName}` },
    { label: "İstasyon", value: `${resource.stationCode} · ${resource.stationName}` },
    { label: "Alan", value: resource.areaCode },
  ];

  const stateRows = [
    { label: "Durum", value: knownStateLabel(resource.state) ?? resource.state },
    { label: "Operasyon", value: resource.operationCode ?? "—" },
    { label: "Emir", value: resource.orderReference ?? "—" },
    { label: "İyi üretim", value: resource.goodCount },
    { label: "Fire", value: resource.rejectCount },
    { label: "Yeniden işleme", value: "—" },
    { label: "Ara stok", value: resource.bufferQuantity != null ? `${resource.bufferQuantity}/${resource.bufferCapacity ?? "—"}` : "—" },
    { label: "Kısıt", value: resource.isBlocked ? "Bloke" : resource.isStarved ? "Malzeme bekliyor" : "—" },
  ];

  const humanRows = isHuman
    ? [{ label: "Operatör", value: resource.operatorCode ?? "—" }]
    : [];

  const bufferRows = isBuffer
    ? [
        { label: "Kapasite", value: resource.bufferCapacity ?? "—" },
        { label: "Mevcut", value: resource.bufferQuantity ?? "—" },
      ]
    : [];

  const machineRows =
    workspace.machineDetail && workspace.machineRegistry
      ? [
          { label: "Makine", value: workspace.machineRegistry.machineCode },
          { label: "Bağlantı", value: workspace.machineDetail.signalProjection?.connectivity ?? "—" },
          { label: "Çevrim", value: workspace.machineDetail.cycleProjection?.lastCycleDurationMs ?? "—" },
          { label: "Aktif alarm", value: workspace.machineDetail.activeAlarms.length },
          { label: "Telemetri", value: workspace.machineDetail.latestTelemetry.length },
        ]
      : [];

  return (
    <div className="factory-inspector-sections" data-testid="factory-resource-inspector">
      <ContextPanel title="Kimlik" rows={identityRows} />
      <ContextPanel title="Mevcut durum" rows={[...stateRows, ...humanRows, ...bufferRows, ...machineRows]} />

      {workspace.machineRegistry && workspace.machineDetail ? (
        <FactoryMachineIntelligence
          detail={workspace.machineDetail}
          machineCode={workspace.machineRegistry.machineCode}
          risk={risk}
        />
      ) : null}

      {isQuality ? (
        <ContextPanel
          title="Kalite bağlamı"
          rows={[
            { label: "Kontrol", value: resource.operationCode ?? "Kalite kontrol" },
            { label: "Fire", value: resource.rejectCount },
          ]}
        />
      ) : null}

      <nav aria-label="Derin modül bağlantıları" className="factory-inspector-links">
        <Link href={`/uretim/hatlar/${resource.lineId}?return=${encodeURIComponent(returnHref)}`}>
          Hat detayı
        </Link>
        <Link href={`/uretim/kaynaklar?lineId=${resource.lineId}&return=${encodeURIComponent(returnHref)}`}>
          Kaynaklar
        </Link>
        {workspace.machineRegistry ? (
          <Link href={`/makineler?return=${encodeURIComponent(returnHref)}`}>
            Makine mühendislik
          </Link>
        ) : null}
        <Link href={`/bakim?return=${encodeURIComponent(returnHref)}`}>Bakım</Link>
        <Link href={`/kalite?return=${encodeURIComponent(returnHref)}`}>Kalite</Link>
        <Link href={`/malzemeler?return=${encodeURIComponent(returnHref)}`}>Malzeme</Link>
        <Link href={`/isler?return=${encodeURIComponent(returnHref)}`}>Aktif işler</Link>
      </nav>
    </div>
  );
}
