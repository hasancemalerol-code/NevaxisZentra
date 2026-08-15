"use server";

import type { DashboardShowcaseViewModel } from "../domain/dashboard-showcase-types";
import { readDashboardShowcaseWorkspace } from "./dashboard-showcase-queries";

export async function refreshDashboardShowcaseAction(): Promise<
  | Readonly<{ status: "success"; viewModel: DashboardShowcaseViewModel }>
  | Readonly<{ status: "failure"; message: string }>
> {
  return readDashboardShowcaseWorkspace();
}
