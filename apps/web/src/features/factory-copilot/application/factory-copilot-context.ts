import type { FactoryCopilotContextInput, FactoryCopilotEntryPoint } from "../domain/factory-copilot-types";

function readParam(
  searchParams: Readonly<URLSearchParams>,
  key: string,
): string | undefined {
  const value = searchParams.get(key);
  return value?.trim() ? value.trim() : undefined;
}

function resolveEntryPoint(pathname: string): FactoryCopilotEntryPoint {
  if (pathname === "/" || pathname.startsWith("/bugun")) return "BUGUN";
  if (pathname.startsWith("/fabrika")) return "FABRIKA";
  if (pathname.startsWith("/planlama")) return "PLAN";
  if (pathname.startsWith("/isler")) return "ISLER";
  if (pathname.startsWith("/senaryo-kurtarma")) return "SCENARIO";
  return "BUGUN";
}

export function resolveFactoryCopilotContext(
  pathname: string,
  searchParams: Readonly<URLSearchParams>,
): FactoryCopilotContextInput {
  const entryPoint = resolveEntryPoint(pathname);
  const areaCode = readParam(searchParams, "alan");
  const lineId = readParam(searchParams, "hat");
  const resourceId = readParam(searchParams, "kaynak");
  const workCaseId = readParam(searchParams, "is");
  const scenarioRunId = readParam(searchParams, "run") ?? readParam(searchParams, "senaryo");
  const orderReference =
    readParam(searchParams, "siparis") ??
    readParam(searchParams, "order") ??
    undefined;

  return {
    entryPoint,
    areaCode,
    lineId,
    resourceId,
    digitalWorkCaseId: workCaseId,
    scenarioRunId,
    orderReference,
    searchQuery: readParam(searchParams, "q") ?? readParam(searchParams, "ara"),
  };
}

const ENTRY_POINT_LABELS: Readonly<Record<string, string>> = {
  BUGUN: "Bugün",
  FABRIKA: "Fabrika",
  PLAN: "Plan",
  ISLER: "İşler",
  SCENARIO: "Senaryo",
  MACHINE: "Makine",
  LINE: "Hat",
  ORDER: "Sipariş",
};

export function formatFactoryCopilotContextHeader(
  header: Readonly<{
    entryPoint: string;
    factoryLabel?: string | null;
    areaLabel?: string | null;
    lineLabel?: string | null;
    resourceLabel?: string | null;
    orderLabel?: string | null;
    workLabel?: string | null;
    scenarioLabel?: string | null;
  }>,
): string {
  const parts = [
    header.factoryLabel ? `Fabrika: ${header.factoryLabel}` : null,
    header.areaLabel ? `Alan: ${header.areaLabel}` : null,
    header.lineLabel ? `Hat: ${header.lineLabel}` : null,
    header.resourceLabel ? `Kaynak: ${header.resourceLabel}` : null,
    header.orderLabel ? `Sipariş: ${header.orderLabel}` : null,
    header.workLabel ? `İş: ${header.workLabel}` : null,
    header.scenarioLabel ? `Senaryo: ${header.scenarioLabel}` : null,
  ].filter(Boolean);

  return parts.length > 0 ? parts.join(" · ") : (ENTRY_POINT_LABELS[header.entryPoint] ?? "Genel bağlam");
}

export function isFactoryCopilotSurface(pathname: string): boolean {
  return (
    pathname === "/" ||
    pathname.startsWith("/bugun") ||
    pathname.startsWith("/fabrika") ||
    pathname.startsWith("/planlama") ||
    pathname.startsWith("/isler") ||
    pathname.startsWith("/senaryo-kurtarma")
  );
}
