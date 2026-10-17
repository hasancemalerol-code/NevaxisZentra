import { SeverityBadge, type SeverityLevel } from "@/components/ui/severity-badge";

import type { MaintenancePredictiveRiskBand } from "../domain/maintenance-predictive-risk";
import { getMaintenancePredictiveRiskBandLabel } from "../presentation/maintenance-predictive-risk-labels";

const BAND_LEVEL: Record<MaintenancePredictiveRiskBand, SeverityLevel> = {
  critical: "critical",
  high: "high",
  watch: "medium",
  low: "low",
  "insufficient-data": "neutral",
};

/** Risk band indicator shared by every predictive-maintenance screen. */
export function MaintenanceRiskBandBadge({ band }: Readonly<{ band: MaintenancePredictiveRiskBand }>) {
  return <SeverityBadge level={BAND_LEVEL[band] ?? "neutral"}>{getMaintenancePredictiveRiskBandLabel(band)}</SeverityBadge>;
}

/** Display order of the risk bands: most urgent first. */
export const MAINTENANCE_RISK_BAND_ORDER: readonly MaintenancePredictiveRiskBand[] = [
  "critical",
  "high",
  "watch",
  "low",
  "insufficient-data",
];
