import {
  denyUnlessGranted,
  forbidUnlessGranted,
} from "@/features/admin-access/server/require-current-permission";

export const PRESENTATION_MODE_ACCESS_PERMISSION = "presentation-mode.access";

export async function denyUnlessPresentationModeAccess() {
  return denyUnlessGranted(PRESENTATION_MODE_ACCESS_PERMISSION);
}

export async function forbidUnlessPresentationModeAccess() {
  return forbidUnlessGranted(PRESENTATION_MODE_ACCESS_PERMISSION);
}
