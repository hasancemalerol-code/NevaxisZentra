import "./maintenance-server-boundary";

import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import { readProductionResourceWorkspace } from "../../production-resources/server/production-resources-api-client";

import type { MaintenanceAssetOption } from "../domain/maintenance-asset-option";

/**
 * Known machines/resources offered as suggestions in the maintenance create forms.
 * Best effort: the forms stay usable with free text when the resource list cannot be read.
 */
export async function readMaintenanceAssetOptions(): Promise<readonly MaintenanceAssetOption[]> {
  try {
    const configResult = parseProductionExecutionRuntimeConfig();
    if (configResult.status !== "success") {
      return [];
    }

    const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
    if (contextResult.status === "failure") {
      return [];
    }

    const workspace = await readProductionResourceWorkspace(contextResult.context);
    if (workspace.status !== "success") {
      return [];
    }

    return dedupeMaintenanceAssetOptions(
      workspace.value.resources.map((resource) => ({
        reference: resource.machineRegistryCode ?? resource.code,
        name: resource.name,
      })),
    );
  } catch {
    return [];
  }
}

export function dedupeMaintenanceAssetOptions(
  options: readonly MaintenanceAssetOption[],
): readonly MaintenanceAssetOption[] {
  const seen = new Set<string>();
  const result: MaintenanceAssetOption[] = [];
  for (const option of options) {
    const reference = option.reference.trim();
    if (!reference || seen.has(reference)) {
      continue;
    }
    seen.add(reference);
    result.push({ reference, name: option.name?.trim() || undefined });
  }
  return result.sort((a, b) => a.reference.localeCompare(b.reference, "tr"));
}
