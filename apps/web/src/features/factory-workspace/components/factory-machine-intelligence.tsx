import { DecisionCard } from "@/components/workspace/decision-card";
import type { FactoryIntelligenceRiskCardApi } from "@/features/factory-intelligence/server/factory-intelligence-api-client";
import type { EdgeMachineDetailBundle } from "@/features/edge/domain/edge-types";

type FactoryMachineIntelligenceProps = Readonly<{
  machineCode: string;
  detail: EdgeMachineDetailBundle | null;
  risk?: FactoryIntelligenceRiskCardApi | null;
}>;

export function FactoryMachineIntelligence({
  machineCode,
  detail,
  risk,
}: FactoryMachineIntelligenceProps) {
  const cycleMs = detail?.cycleProjection?.lastCycleDurationMs;
  const avgCycle = detail?.cycleProjection?.averageRecentCycleDurationMs;
  const cycleDelta =
    cycleMs && avgCycle && avgCycle > 0
      ? `${(((cycleMs - avgCycle) / avgCycle) * 100).toFixed(1)}%`
      : null;

  const signal = detail?.signalProjection?.observedState ?? "—";
  const activeAlarm = detail?.activeAlarms[0];

  if (!risk && !cycleDelta && !activeAlarm) {
    return null;
  }

  return (
    <DecisionCard
      eyebrow="Makine sinyali"
      title={machineCode}
      signal={{
        label: "Ne oldu?",
        value: risk?.observedSummary ?? activeAlarm?.alarmText ?? signal,
      }}
      cause={{
        label: "Neden?",
        value:
          risk?.evidence[0]
          ?? (cycleDelta ? `Çevrim süresi sapması ${cycleDelta}` : activeAlarm?.externalAlarmCode ?? "—"),
      }}
      impact={{
        label: "Etki",
        value: risk?.predictedSummary ?? (activeAlarm ? "Üretim kesintisi riski" : "—"),
      }}
      recommendation={{
        label: "Öneri",
        value: risk?.recommendedActions[0] ?? "Makine detayını ve alarm geçmişini inceleyin.",
      }}
      expectedResult={{
        label: "Beklenen sonuç",
        value: risk?.timeToImpactLabel ?? "Operasyonel stabilite",
      }}
      testId={`factory-machine-intelligence-${machineCode}`}
    />
  );
}
