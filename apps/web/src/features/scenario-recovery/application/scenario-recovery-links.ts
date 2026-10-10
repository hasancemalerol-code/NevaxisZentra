export function buildScenarioRecoveryHref(input: {
  lineId?: string;
  runId?: string;
  entryPoint?: string;
  orderReference?: string;
  constraintStationCode?: string;
}): string {
  const params = new URLSearchParams();
  if (input.runId) params.set("runId", input.runId);
  if (input.lineId) params.set("lineId", input.lineId);
  if (input.entryPoint) params.set("entryPoint", input.entryPoint);
  if (input.orderReference) params.set("orderReference", input.orderReference);
  if (input.constraintStationCode) params.set("constraintStationCode", input.constraintStationCode);
  const query = params.toString();
  return query ? `/senaryo-kurtarma?${query}` : "/senaryo-kurtarma";
}

export function buildWorksCaseHref(caseId: string, queue?: string): string {
  const params = new URLSearchParams();
  params.set("is", caseId);
  if (queue) {
    params.set("kuyruk", queue);
  }
  return `/isler?${params.toString()}`;
}

export type ScenarioRecoveryCreateInput = Readonly<{
  lineId: string;
  entryPoint?: string;
  orderReference?: string;
  constraintStationCode?: string;
}>;

/** The explicit "create scenario" command body. Built only for the user-triggered action, never while viewing the page. */
export function buildCreateScenarioRunRequest(input: ScenarioRecoveryCreateInput) {
  return {
    lineId: input.lineId,
    category: "PRODUCTION_RECOVERY",
    entryPoint: input.entryPoint ?? "DIRECT",
    problem: {
      summary: input.orderReference
        ? `Order ${input.orderReference} delivery risk with operational constraint`
        : "Operational recovery scenario",
      orderReference: input.orderReference,
      constraintStationCode: input.constraintStationCode,
      contributingFactors: ["throughput-gap", "constraint-detected"],
    },
    objectives: [
      { objectiveType: "PROTECT_OTIF", label: "Protect delivery commitment", priority: 3, isHardConstraint: true },
      { objectiveType: "MAXIMIZE_VERIFIED_GOOD_THROUGHPUT", label: "Increase good throughput", priority: 2, isHardConstraint: false },
    ],
    sourceEntityType: input.entryPoint?.toLowerCase() ?? "direct",
    sourceEntityId: input.lineId,
  };
}
