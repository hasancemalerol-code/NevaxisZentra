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
    lineId,
    lineCode: lineId ? undefined : entryPoint === "FABRIKA" ? "MIX-LINE-01" : undefined,
    resourceId,
    digitalWorkCaseId: workCaseId,
    scenarioRunId,
    orderReference,
    searchQuery: readParam(searchParams, "q") ?? readParam(searchParams, "ara"),
  };
}

export function formatFactoryCopilotContextHeader(
  header: Readonly<{
    entryPoint: string;
    factoryLabel?: string | null;
    lineLabel?: string | null;
    resourceLabel?: string | null;
    orderLabel?: string | null;
    workLabel?: string | null;
    scenarioLabel?: string | null;
  }>,
): string {
  const parts = [
    header.factoryLabel ? `Fabrika: ${header.factoryLabel}` : null,
    header.lineLabel ? `Hat: ${header.lineLabel}` : null,
    header.resourceLabel ? `Kaynak: ${header.resourceLabel}` : null,
    header.orderLabel ? `Sipariş: ${header.orderLabel}` : null,
    header.workLabel ? `İş: ${header.workLabel}` : null,
    header.scenarioLabel ? `Senaryo: ${header.scenarioLabel}` : null,
  ].filter(Boolean);

  return parts.length > 0 ? parts.join(" · ") : header.entryPoint;
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
