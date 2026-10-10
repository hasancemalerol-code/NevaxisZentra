/**
 * One classification of a failed API response for every server-side API client.
 *
 * The backend failure code (`failureCode` / `code`) is authoritative; the HTTP status is only the fallback when the
 * body carries no structured code. This stops a licence/entitlement refusal (HTTP 403 with
 * `saas-subscription.*`), an expired session, a missing record or a validation error from all being shown to the
 * customer as "permission denied".
 */
export type ApiFailureKind =
  | "unauthenticated"
  | "permission-denied"
  | "subscription-required"
  | "not-found"
  | "validation"
  | "conflict"
  | "source-unavailable";

export type ClassifiedApiFailure = Readonly<{
  kind: ApiFailureKind;
  /** The backend failure code when the body carried one. */
  failureCode: string | null;
  /** True when `kind` was decided from a recognised backend code rather than from the HTTP status alone. */
  structured: boolean;
}>;

export const SUBSCRIPTION_REQUIRED_MESSAGE =
  "Bu modül aboneliğinize dahil değil veya aboneliğiniz etkin değil. Yöneticinizle iletişime geçin.";
export const SESSION_REQUIRED_MESSAGE = "Oturumunuz sona ermiş olabilir. Lütfen yeniden giriş yapın.";

function readFailureCode(body: unknown): string | null {
  if (typeof body !== "object" || body === null) {
    return null;
  }

  const record = body as Record<string, unknown>;
  for (const key of ["failureCode", "code", "errorCode"]) {
    const value = record[key];
    if (typeof value === "string" && value.trim().length > 0) {
      return value.trim().toLowerCase();
    }
  }

  return null;
}

function kindFromCode(code: string): ApiFailureKind | null {
  if (code.startsWith("saas-subscription.") || code.includes("entitlement") || code.includes("subscription")) {
    return "subscription-required";
  }

  if (code.includes("not-found") || code.includes("not_found")) {
    return "not-found";
  }

  if (/(^|[.-])(unauthenticated|session-expired|session-required|mfa-required|authentication-required)($|[.-])/.test(code)) {
    return "unauthenticated";
  }

  if (/(^|[.-])(validation|invalid-request|invalid-scope|facility-missing|invalid-input)($|[.-])/.test(code)) {
    return "validation";
  }

  if (/(^|[.-])(conflict|concurrency|idempotency)($|[.-])/.test(code)) {
    return "conflict";
  }

  if (/(permission-denied|facility-access-denied|forbidden|access-denied|not-authorized|authorization-denied)/.test(code)) {
    return "permission-denied";
  }

  if (/(unavailable|source-unavailable|timeout)/.test(code)) {
    return "source-unavailable";
  }

  return null;
}

function kindFromStatus(status: number): ApiFailureKind {
  if (status === 401) return "unauthenticated";
  if (status === 402 || status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 400 || status === 422) return "validation";
  if (status === 409 || status === 412) return "conflict";
  return "source-unavailable";
}

export function classifyApiFailure(status: number, body: unknown): ClassifiedApiFailure {
  const failureCode = readFailureCode(body);
  const fromCode = failureCode ? kindFromCode(failureCode) : null;
  if (fromCode) {
    return { kind: fromCode, failureCode, structured: true };
  }

  return { kind: kindFromStatus(status), failureCode, structured: false };
}

/** Reads (a clone of) a failed response body defensively and classifies it. Never throws. */
export async function classifyFailedResponse(response: Response): Promise<ClassifiedApiFailure> {
  let body: unknown = null;
  try {
    body = JSON.parse(await response.clone().text());
  } catch {
    body = null;
  }

  return classifyApiFailure(response.status, body);
}

export type ForbiddenFailure = Readonly<{
  status: "failure";
  code: "permission-denied" | "subscription-required" | "unauthenticated" | "source-unavailable";
  message: string;
  retryable: false;
}>;

/**
 * The failure to report for an HTTP 403 (or other refusal): the caller's own denial wording only when the backend
 * really denied the permission; a licence, session or availability cause is reported as such (a refusal that carries any other code stays a denial).
 */
export function failureForRefusal(failure: ClassifiedApiFailure, deniedMessage: string): ForbiddenFailure {
  switch (failure.kind) {
    case "subscription-required":
      return { status: "failure", code: "subscription-required", message: SUBSCRIPTION_REQUIRED_MESSAGE, retryable: false };
    case "unauthenticated":
      return { status: "failure", code: "unauthenticated", message: SESSION_REQUIRED_MESSAGE, retryable: false };
    case "source-unavailable":
      return { status: "failure", code: "source-unavailable", message: "Servis şu anda kullanılamıyor.", retryable: false };
    default:
      return { status: "failure", code: "permission-denied", message: deniedMessage, retryable: false };
  }
}

/** The failure code for an HTTP 403 given the parsed body: licence and session causes are not "permission denied". */
export function refusalCode(
  status: number,
  payload: unknown,
): "permission-denied" | "subscription-required" | "unauthenticated" {
  const kind = classifyApiFailure(status, payload).kind;
  return kind === "subscription-required" || kind === "unauthenticated" ? kind : "permission-denied";
}

/** True when a reader/client failure code states that the requested record does not exist. */
export function isNotFoundFailureCode(code: unknown): boolean {
  if (typeof code !== "string" || code.length === 0) {
    return false;
  }

  const normalized = code.toLowerCase();
  return normalized.includes("not-found") || normalized.includes("not_found") || normalized === "http-404";
}

export const RECORD_NOT_FOUND_HEADING = "Kayıt bulunamadı";
