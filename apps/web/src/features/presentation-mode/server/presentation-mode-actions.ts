"use server";

import "./presentation-mode-server-boundary";

import { revalidatePath } from "next/cache";

import { factoryDecisionResetAction } from "@/features/factory-decision-center/server/factory-decision-center-actions";
import { goldenDemoCustomsResetAction } from "@/features/golden-demo-customs-file/server/golden-demo-customs-file-actions";
import { goldenDemoResetPilotEventAction } from "@/features/golden-demo-p2/server/golden-demo-p2-actions";

import { PRESENTATION_MODE_ROUTE } from "../domain/presentation-mode-labels";
import type { PresentationScenarioKey } from "../domain/presentation-mode-labels";
import { denyUnlessPresentationModeAccess } from "./require-presentation-mode-access";

type ActionResult =
  | Readonly<{ status: "success" }>
  | Readonly<{ status: "failure"; message: string }>;

function revalidatePresentationSurfaces() {
  revalidatePath(PRESENTATION_MODE_ROUTE);
  revalidatePath("/golden-demo/uretim-kurtarma");
  revalidatePath("/golden-demo/gumruk-dosyasi");
  revalidatePath("/fabrika-karar-merkezi");
}

export async function presentationModeResetScenarioAction(
  scenarioKey: PresentationScenarioKey,
  targetId?: string,
): Promise<ActionResult> {
  const authorization = await denyUnlessPresentationModeAccess();
  if (authorization.status === "failure") {
    return { status: "failure", message: "Sunum modu sıfırlama yetkiniz yok." };
  }

  if (scenarioKey === "production-recovery") {
    if (!targetId) {
      return { status: "failure", message: "Üretim kurtarma senaryosu bulunamadı." };
    }

    const result = await goldenDemoResetPilotEventAction(targetId);
    if (result.status === "failure") {
      return result;
    }

    revalidatePresentationSurfaces();
    return { status: "success" };
  }

  if (scenarioKey === "customs-file") {
    if (!targetId) {
      return { status: "failure", message: "Gümrük dosyası senaryosu bulunamadı." };
    }

    const result = await goldenDemoCustomsResetAction(targetId);
    if (result.status === "failure") {
      return result;
    }

    revalidatePresentationSurfaces();
    return { status: "success" };
  }

  const result = await factoryDecisionResetAction();
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePresentationSurfaces();
  return { status: "success" };
}
