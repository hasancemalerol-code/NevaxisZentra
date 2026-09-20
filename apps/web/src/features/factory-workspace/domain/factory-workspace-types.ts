import type { OperationalStatusVariant } from "@/components/workspace/operational-status";
import type { EdgeMachineDetailBundle, MachineRegistryReadModel } from "@/features/edge/domain/edge-types";
import type { FactoryIntelligenceRiskCardApi } from "@/features/factory-intelligence/server/factory-intelligence-api-client";
import type { MixedProductionLineDetailApi, MixedProductionPlaybackApi } from "@/features/mixed-production/server/mixed-production-api-client";
import type { ProductionResourceRowApi, ProductionResourceWorkspaceApi } from "@/features/production-resources/server/production-resources-api-client";
import type { ThroughputOptimizerWorkspaceApi } from "@/features/throughput-optimizer/server/throughput-optimizer-api-client";
import type { OptimizationOpportunityApi } from "@/features/continuous-optimization/server/continuous-optimization-api-client";
import type {
  ScenarioComparisonRowApi,
  ThroughputIntelligenceOverviewApi,
} from "@/features/throughput-intelligence/server/throughput-intelligence-api-client";
import type { RoleLandingProfile } from "@/lib/navigation/role-landing";

export type FactoryHierarchyKind = "factory" | "area" | "line" | "station" | "resource";

export type FactoryHierarchyNode = Readonly<{
  id: string;
  kind: FactoryHierarchyKind;
  label: string;
  code: string;
  state?: string;
  stateTone?: OperationalStatusVariant;
  areaCode?: string;
  lineId?: string;
  stationId?: string;
  resourceId?: string;
  children?: readonly FactoryHierarchyNode[];
}>;

export type FactoryWorkspaceSelection = Readonly<{
  areaCode?: string;
  lineId?: string;
  stationId?: string;
  resourceId?: string;
  search?: string;
}>;

export type FactoryWorkspaceShiftContext = Readonly<{
  shiftLabel: string;
  openItems: number;
  supervisorLabel?: string;
}>;

export type FactoryWorkspacePayload = Readonly<{
  facilityLabel: string;
  roleProfile: RoleLandingProfile;
  hierarchy: FactoryHierarchyNode;
  resources: ProductionResourceWorkspaceApi;
  selection: FactoryWorkspaceSelection;
  risks: readonly FactoryIntelligenceRiskCardApi[];
  shift: FactoryWorkspaceShiftContext | null;
  activeOrderCount: number | null;
  lineDetail: MixedProductionLineDetailApi | null;
  playback: MixedProductionPlaybackApi | null;
  throughput:
    | Readonly<{
        overview: ThroughputIntelligenceOverviewApi;
        comparison: readonly ScenarioComparisonRowApi[];
      }>
    | null;
  optimizer: ThroughputOptimizerWorkspaceApi | null;
  continuousOptimization: readonly OptimizationOpportunityApi[];
  selectedResource: ProductionResourceRowApi | null;
  machineRegistry: MachineRegistryReadModel | null;
  machineDetail: EdgeMachineDetailBundle | null;
  canReadMixedProduction: boolean;
  canReadEdge: boolean;
  canReadFactoryIntelligence: boolean;
}>;
