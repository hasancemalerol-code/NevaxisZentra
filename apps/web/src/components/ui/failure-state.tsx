import type { ReactNode } from "react";

import {
  classifyFailureCode,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
} from "@/lib/api/api-failure-classifier";
import { customerMessage } from "@/lib/presentation/customer-message";

import { StatePage, StatePageLink, type StatePageKind } from "./state-page";
import { StateBackButton } from "./state-back-button";
import { StateRetryButton } from "./state-retry-button";

type FailureLike = Readonly<{ code?: unknown; message?: string | null }>;

type FailureStateOptions = Readonly<{
  /** Title for a plain load failure, e.g. "Üretim hatları yüklenemedi". */
  failedTitle: string;
  /** Noun phrase for a missing record, e.g. "üretim hattı". */
  entityLabel?: string;
  backHref?: string;
  backLabel?: string;
  /** Section inside a page that already has a header. */
  inline?: boolean;
  /** Module name for the standard page header on full-page states. */
  moduleLabel?: string;
}>;

/**
 * The one mapping from a data-layer failure (refusal / failure code) to a customer state.
 * Denied never says "try later", unavailable never says "no permission", a missing record is not red and is not a denial,
 * and raw backend wording is never shown.
 */
export function failureStateContent(failure: FailureLike, options: FailureStateOptions): {
  kind: StatePageKind;
  title: string;
  description: string;
} {
  const kind = classifyFailureCode(typeof failure.code === "string" ? failure.code : null);
  const entity = options.entityLabel ?? "kayıt";
  switch (kind) {
    case "permission-denied":
      return {
        kind: "forbidden",
        title: "Erişim Yetkiniz Yok",
        description: "Hesabınızın rolü bu içeriği görüntülemeye izin vermiyor. Erişim gerekiyorsa yöneticinizle iletişime geçin.",
      };
    case "subscription-required":
      return { kind: "entitlement", title: "Bu modül aboneliğinizde etkin değil", description: SUBSCRIPTION_REQUIRED_MESSAGE };
    case "unauthenticated":
      return { kind: "error", title: "Oturumunuz sona ermiş", description: SESSION_REQUIRED_MESSAGE };
    case "not-found":
      return {
        kind: "not-found",
        title: "Kayıt Bulunamadı",
        description: `İstenen ${entity} mevcut değil, kaldırılmış ya da yetki alanınızın dışında olabilir.`,
      };
    case "validation":
    case "conflict":
      return { kind: "error", title: options.failedTitle, description: customerMessage(failure.message) };
    default:
      return {
        kind: "unavailable",
        title: options.failedTitle,
        description: "Bağlantı ve servis durumunu kontrol edip yeniden deneyin; sorun sürerse yöneticinize başvurun.",
      };
  }
}

export function FailureStatePage({ code, message, ...options }: FailureLike & FailureStateOptions): ReactNode {
  const content = failureStateContent({ code, message }, options);
  const retryable = content.kind === "unavailable" || content.kind === "error";
  return (
    <StatePage
      kind={content.kind}
      title={content.title}
      description={content.description}
      inline={options.inline}
      moduleLabel={options.moduleLabel}
      actions={
        options.backHref || retryable || content.kind === "not-found" ? (
          <>
            {retryable ? <StateRetryButton /> : null}
            {options.backHref ? (
              <StatePageLink href={options.backHref} primary={!retryable}>
                {options.backLabel ?? "Geri dön"}
              </StatePageLink>
            ) : content.kind === "not-found" ? <StateBackButton /> : null}
          </>
        ) : undefined
      }
    />
  );
}

/** Access gate result (`denyUnlessGranted`): a real denial and a failed check are different states with different wording. */
export function AuthorizationGateState({ code, inline = true, backHref = "/dashboard" }: Readonly<{ code: string; inline?: boolean; backHref?: string }>): ReactNode {
  const denied = code === "authorization-denied";
  return (
    <StatePage
      kind={denied ? "forbidden" : "unavailable"}
      title={denied ? "Erişim Yetkiniz Yok" : "Yetki doğrulanamadı"}
      description={
        denied
          ? "Hesabınızın rolü bu içeriği görüntülemeye izin vermiyor. Erişim gerekiyorsa yöneticinizle iletişime geçin."
          : "Yetki doğrulaması şu anda yapılamıyor. Lütfen biraz sonra yeniden deneyin."
      }
      inline={inline}
      actions={
        <>
          {denied ? null : <StateRetryButton />}
          <StatePageLink href={backHref} primary={denied}>Ana sayfaya dön</StatePageLink>
        </>
      }
    />
  );
}
