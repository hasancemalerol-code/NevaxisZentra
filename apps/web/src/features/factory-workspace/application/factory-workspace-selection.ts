import type { FactoryWorkspaceSelection } from "../domain/factory-workspace-types";

export function readFactoryWorkspaceSelection(
  searchParams: Readonly<Record<string, string | string[] | undefined>> | undefined,
): FactoryWorkspaceSelection {
  const read = (key: string): string | undefined => {
    const value = searchParams?.[key];
    if (Array.isArray(value)) {
      return value[0];
    }
    return typeof value === "string" && value.trim() ? value.trim() : undefined;
  };

  return {
    areaCode: read("alan"),
    lineId: read("hat"),
    stationId: read("istasyon"),
    resourceId: read("kaynak"),
    search: read("q"),
  };
}

export function buildFactoryWorkspaceHref(
  basePath: string,
  selection: FactoryWorkspaceSelection,
): string {
  const params = new URLSearchParams();
  if (selection.areaCode) params.set("alan", selection.areaCode);
  if (selection.lineId) params.set("hat", selection.lineId);
  if (selection.stationId) params.set("istasyon", selection.stationId);
  if (selection.resourceId) params.set("kaynak", selection.resourceId);
  if (selection.search) params.set("q", selection.search);
  const suffix = params.size > 0 ? `?${params.toString()}` : "";
  return `${basePath}${suffix}`;
}

export function resolveActiveSelectionKind(
  selection: FactoryWorkspaceSelection,
): "factory" | "area" | "line" | "station" | "resource" {
  if (selection.resourceId) return "resource";
  if (selection.stationId) return "station";
  if (selection.lineId) return "line";
  if (selection.areaCode) return "area";
  return "factory";
}
