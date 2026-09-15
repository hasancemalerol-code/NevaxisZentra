import "./presentation-mode-server-boundary";

import { loadFactoryDecisionCenterWorkspace } from "@/features/factory-decision-center/server/factory-decision-center-reader";
import { readGoldenDemoCustomsFileWorkspace } from "@/features/golden-demo-customs-file/server/golden-demo-customs-file-reader";
import { readGoldenDemoP1Workspace } from "@/features/golden-demo-p1/server/golden-demo-p1-reader";
import { readGoldenDemoP2PresentationStatus } from "@/features/golden-demo-p2/server/golden-demo-p2-reader";

import {
  PRESENTATION_SCENARIOS,
  type PresentationScenarioKey,
} from "../domain/presentation-mode-labels";
import {
  findCriticalOrderRecoveryCase,
  mapCriticalOrderPresentationStatus,
  mapCustomsPresentationStatus,
  mapProductionRecoveryPresentationStatus,
  summarizePresentationStatuses,
} from "../domain/presentation-mode-status";
import { denyUnlessPresentationModeAccess } from "./require-presentation-mode-access";

export type PresentationScenarioState = Readonly<{
  key: PresentationScenarioKey;
  status: ReturnType<typeof mapProductionRecoveryPresentationStatus>;
  resetTargetId?: string;
  caseId?: string;
}>;

export type PresentationModeDashboard = Readonly<{
  scenarios: readonly PresentationScenarioState[];
  summary: ReturnType<typeof summarizePresentationStatuses>;
}>;

export async function loadPresentationModeDashboard(): Promise<
  | { status: "success"; dashboard: PresentationModeDashboard }
  | { status: "failure"; message: string }
> {
  const authorization = await denyUnlessPresentationModeAccess();
  if (authorization.status === "failure") {
    return { status: "failure", message: "Sunum modu erişimi reddedildi." };
  }

  const [productionResult, presentationResult, customsResult, factoryResult] = await Promise.all([
    readGoldenDemoP1Workspace(),
    readGoldenDemoP2PresentationStatus(),
    readGoldenDemoCustomsFileWorkspace(),
    loadFactoryDecisionCenterWorkspace(),
  ]);

  const productionWorkspace =
    productionResult.status === "success" ? productionResult.workspace : null;
  const presentationStatus =
    presentationResult.status === "success" ? presentationResult.presentation : null;
  const customsWorkspace =
    customsResult.status === "success" ? customsResult.workspace : null;
  const criticalCase =
    factoryResult.status === "success"
      ? findCriticalOrderRecoveryCase(factoryResult.workspace.priorityQueue)
      : null;

  const scenarios: PresentationScenarioState[] = PRESENTATION_SCENARIOS.map((scenario) => {
    if (scenario.key === "production-recovery") {
      return {
        key: scenario.key,
        status: mapProductionRecoveryPresentationStatus(productionWorkspace, presentationStatus),
        resetTargetId: productionWorkspace?.event.id,
      };
    }

    if (scenario.key === "customs-file") {
      return {
        key: scenario.key,
        status: mapCustomsPresentationStatus(customsWorkspace),
        resetTargetId: customsWorkspace?.outcomeId,
      };
    }

    return {
      key: scenario.key,
      status: mapCriticalOrderPresentationStatus(criticalCase),
      caseId: criticalCase?.caseId,
    };
  });

  return {
    status: "success",
    dashboard: {
      scenarios,
      summary: summarizePresentationStatuses(scenarios.map((scenario) => scenario.status)),
    },
  };
}
