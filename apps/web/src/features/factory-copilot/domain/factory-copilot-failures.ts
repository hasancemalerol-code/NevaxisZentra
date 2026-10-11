import {
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";

/** What the customer should do about a failed Copilot request. Each category is a different next step. */
export type FactoryCopilotFailureCategory =
  | "authentication"
  | "authorization"
  | "subscription"
  | "invalid-input"
  | "insufficient-context"
  | "service-unavailable"
  | "unexpected";

export type FactoryCopilotFailureDescription = Readonly<{
  category: FactoryCopilotFailureCategory;
  message: string;
}>;

export function describeFactoryCopilotRequestFailure(
  failure: ClassifiedApiFailure,
  backendMessage?: string | null,
): FactoryCopilotFailureDescription {
  if (failure.failureCode?.includes("insufficient-context")) {
    return {
      category: "insufficient-context",
      message: "Copilot için hat veya üretim bağlamı bulunamadı. Bir hat seçip tekrar deneyin.",
    };
  }

  switch (failure.kind) {
    case "unauthenticated":
      return { category: "authentication", message: SESSION_REQUIRED_MESSAGE };
    case "permission-denied":
      return { category: "authorization", message: "Bu işlem için yetkiniz yok." };
    case "subscription-required":
      return { category: "subscription", message: SUBSCRIPTION_REQUIRED_MESSAGE };
    case "validation":
      return {
        category: "invalid-input",
        message: backendMessage?.trim() ? backendMessage : "Soru geçersiz. Kısaltıp tekrar deneyin.",
      };
    case "source-unavailable":
      return { category: "service-unavailable", message: "Copilot servisine şu an ulaşılamıyor. Biraz sonra tekrar deneyin." };
    default:
      return { category: "unexpected", message: "Copilot beklenmeyen bir hatayla karşılaştı." };
  }
}

/** Customer-facing label of the AI state the API reports in `failureState` (null for non-AI states). */
export function describeFactoryCopilotAiState(state: string | null | undefined): string | null {
  switch (state) {
    case "AI_NOT_CONFIGURED":
      return "AI servisi bu kurulumda yapılandırılmamış (yönetici işlemi gerekir).";
    case "AI_AUTHENTICATION_FAILED":
      return "AI sağlayıcı kimlik doğrulaması reddedildi (yönetici işlemi gerekir).";
    case "AI_RATE_LIMITED":
      return "AI sağlayıcı istek sınırına ulaştı; kısa süre sonra tekrar deneyin.";
    case "AI_TIMEOUT":
      return "AI sağlayıcı zamanında yanıt vermedi; tekrar deneyin.";
    case "AI_PROVIDER_UNAVAILABLE":
      return "AI sağlayıcıya geçici olarak ulaşılamıyor; tekrar deneyin.";
    case "AI_INVALID_OUTPUT":
    case "AI_UNGROUNDED_RESPONSE":
      return "AI yanıtı doğrulanamadığı için gösterilmedi.";
    case "AI_BUDGET_LIMITED":
    case "AI_POLICY_REJECTED":
      return "AI kullanımı politika veya bütçe nedeniyle kapalı.";
    case "INSUFFICIENT_DATA":
      return "Bu soru için yeterli üretim verisi yok; tahmin yürütülmedi.";
    case "prompt-injection-detected":
      return "Güvenlik politikası gereği istek işlenmedi.";
    default:
      return null;
  }
}

/**
 * A server-action call that throws (instead of returning a failure) never reached the Copilot code: the transport
 * failed. The most common cause is a session cookie that is gone, where the sign-in middleware answers the action
 * request with a redirect to the login page. A manual-redirect probe tells that apart from an unreachable service.
 */
export async function describeFactoryCopilotTransportFailure(
  probeUrl: string,
  probe: (url: string, init: RequestInit) => Promise<Response> = (url, init) => fetch(url, init),
): Promise<FactoryCopilotFailureDescription> {
  try {
    const response = await probe(probeUrl, { method: "HEAD", redirect: "manual", cache: "no-store" });
    if (response.type === "opaqueredirect" || (response.status >= 300 && response.status < 400) || response.status === 401) {
      return { category: "authentication", message: SESSION_REQUIRED_MESSAGE };
    }
  } catch {
    // The probe itself failed: the service cannot be reached from this browser.
  }

  return {
    category: "service-unavailable",
    message: "Copilot servisine şu an ulaşılamıyor. Biraz sonra tekrar deneyin.",
  };
}
