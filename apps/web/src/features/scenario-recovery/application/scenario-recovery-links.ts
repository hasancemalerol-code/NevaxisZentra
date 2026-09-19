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
