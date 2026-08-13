import "./shift-risk-server-boundary";

import type { ShiftRiskCapabilities } from "../domain/shift-risk-types";
import { probeShiftRiskPermissions } from "./shift-risk-api";
import { resolveShiftWorkforceRequestContext } from "../../shift-workforce/server/shift-workforce-request-context";

export type ShiftRiskPermissionsResult =
  | Readonly<{ status: "success"; capabilities: ShiftRiskCapabilities }>
  | Readonly<{ status: "failure"; message: string; code?: string }>;

export async function readShiftRiskPermissions(): Promise<ShiftRiskPermissionsResult> {
  const contextResult = resolveShiftWorkforceRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      message: contextResult.message,
      code: contextResult.code,
    };
  }

  const capabilities = await probeShiftRiskPermissions(contextResult.context);
  return { status: "success", capabilities };
}
