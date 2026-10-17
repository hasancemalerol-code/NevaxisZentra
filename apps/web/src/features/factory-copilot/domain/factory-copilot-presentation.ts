/**
 * Customer wording for what the Copilot reports about itself. The API answers with machine states (routing class,
 * intent code, SUFFICIENT / SOURCE_UNAVAILABLE …); the drawer shows only what a customer can act on: whether the answer
 * came from rules or from a model, and how good the underlying data was.
 */

export type FactoryCopilotSourceKind = "rules" | "model";

export type FactoryCopilotSourceBadge = Readonly<{
  kind: FactoryCopilotSourceKind;
  label: string;
  description: string;
}>;

export function describeFactoryCopilotSource(modelInvoked: boolean): FactoryCopilotSourceBadge {
  return modelInvoked
    ? {
        kind: "model",
        label: "Model destekli yanıt",
        description: "Yanıt, doğrulanmış fabrika verisi üzerinde bir dil modeli tarafından özetlendi.",
      }
    : {
        kind: "rules",
        label: "Kural tabanlı yanıt",
        description: "Yanıt, doğrulanmış fabrika verisinden kurallarla üretildi; dil modeli kullanılmadı.",
      };
}

const CONTEXT_QUALITY_LABELS: Readonly<Record<string, string>> = {
  SUFFICIENT: "Veri yeterli",
  STALE: "Veri güncel değil",
  CONFLICTED: "Veri çelişkili",
  INSUFFICIENT: "Veri yetersiz",
  SOURCE_UNAVAILABLE: "Veri kaynağı kullanılamıyor",
};

/** Customer label of the context quality state, or null when the state is not one the customer needs to see. */
export function describeFactoryCopilotContextQuality(state: string | null | undefined): string | null {
  return state ? (CONTEXT_QUALITY_LABELS[state.trim().toUpperCase()] ?? null) : null;
}

const CONTEXT_QUALITY_FACT = /^Context quality:\s*([A-Z_]+)\s*$/;

/** Key facts and similar lines sometimes carry backend wording; translate the known ones, keep customer-written text. */
export function humanizeFactoryCopilotLine(line: string): string {
  const quality = CONTEXT_QUALITY_FACT.exec(line.trim());
  if (quality) {
    return `Bağlam kalitesi: ${describeFactoryCopilotContextQuality(quality[1]) ?? "belirsiz"}`;
  }

  return line;
}
