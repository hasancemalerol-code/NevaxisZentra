"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessAnyGranted } from "../../admin-access/server/require-current-permission";
import { OPERATIONS_COMMAND_CENTER_ROUTE_PATH } from "../domain/operations-command-center-labels";
import { readOperationsCommandCenterWorkspace } from "./operations-command-center-queries";

const MODULE_READ_PERMISSIONS = [
  "production.execution.read",
  "material.read",
  "erp.read",
  "quality.read",
  "maintenance.read",
  "mold.read",
  "commercial-intelligence.read",
  "ai-task.read",
] as const;

export async function refreshOperationsCommandCenterAction() {
  const authorization = await denyUnlessAnyGranted(MODULE_READ_PERMISSIONS);
  if (authorization.status === "failure") {
    return authorization;
  }

  revalidatePath(OPERATIONS_COMMAND_CENTER_ROUTE_PATH);
  const result = await readOperationsCommandCenterWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
  };
}
