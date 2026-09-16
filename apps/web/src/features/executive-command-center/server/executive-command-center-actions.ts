"use server";

import { revalidatePath } from "next/cache";

import { readExecutiveCommandCenterFilters } from "../application/executive-command-center-filter";
import { EXECUTIVE_COMMAND_CENTER_ROUTE_PATH } from "../domain/executive-command-center-labels";
import { readExecutiveCommandCenterWorkspace } from "./executive-command-center-queries";

export async function refreshExecutiveCommandCenterAction(searchParamsRecord?: Record<string, string>) {
  const params = new URLSearchParams(searchParamsRecord ?? {});
  const filters = readExecutiveCommandCenterFilters(params);

  revalidatePath(EXECUTIVE_COMMAND_CENTER_ROUTE_PATH);
  const result = await readExecutiveCommandCenterWorkspace(filters);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return { status: "success" as const, workspace: result.workspace };
}
