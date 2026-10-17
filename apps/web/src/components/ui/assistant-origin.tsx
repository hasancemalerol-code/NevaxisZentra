import { Bot, Cpu } from "lucide-react";

export type AssistantOrigin = "rules" | "model" | "unavailable";

const COPY: Record<AssistantOrigin, { label: string; hint: string }> = {
  rules: {
    label: "Kural tabanlı asistan",
    hint: "Yanıtlar doğrulanmış veriden kurallarla üretilir; yapay zekâ modeli kullanılmaz.",
  },
  model: {
    label: "Model destekli asistan",
    hint: "Yanıtlar doğrulanmış veri üzerinde bir dil modeli tarafından hazırlanır ve insan onayına tabidir.",
  },
  unavailable: {
    label: "Asistan etkin değil",
    hint: "Bu kurulumda asistan yapılandırılmamış. Yöneticinizle iletişime geçin.",
  },
};

/**
 * The one way every assistant surface says where its answers come from. Rules-based and model-backed assistants are
 * visually distinct, and no provider, template or prompt identifier is ever shown to a customer.
 */
export function AssistantOriginBadge({ origin }: Readonly<{ origin: AssistantOrigin }>) {
  const copy = COPY[origin];
  return (
    <span
      className={`zx-sev ${origin === "model" ? "zx-sev--ai" : "zx-sev--neutral"}`}
      title={copy.hint}
      data-assistant-origin={origin}
    >
      {origin === "model" ? <Cpu aria-hidden="true" /> : <Bot aria-hidden="true" />}
      {copy.label}
    </span>
  );
}

export function describeAssistantOrigin(origin: AssistantOrigin): string {
  return COPY[origin].hint;
}
