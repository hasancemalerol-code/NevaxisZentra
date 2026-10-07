/**
 * Client contract for the one-time, localhost-only first-account enrollment of a fresh installation.
 * The enrollment token is read from the per-machine `first-run-token.txt` file by the installing
 * customer and typed here; it is never stored, logged or placed in a URL by this code.
 */

export const FIRST_RUN_STATUS_PATH = "/api/identity/bootstrap/status";
export const FIRST_RUN_ENROLL_PATH = "/api/identity/bootstrap/enroll";
export const FIRST_RUN_TOKEN_FILE_HINT =
  "%LOCALAPPDATA%\\NevaxisZentra\\config\\first-run-token.txt";

export type FirstRunEnrollmentPayload = Readonly<{
  token: string;
  organizationCode: string;
  organizationName: string;
  ownerDisplayName: string;
  ownerEmail: string;
  password: string;
}>;

export type FirstRunEnrollmentOutcome =
  | Readonly<{ status: "enrolled"; tenantKey: string }>
  | Readonly<{ status: "failure"; messages: readonly string[]; unavailable: boolean }>;

const FIELD_ERROR_MESSAGES: Readonly<Record<string, string>> = {
  "organization-code-invalid":
    "Kuruluş kodu 3-40 karakter olmalı; yalnızca küçük harf, rakam ve tire içermelidir.",
  "organization-name-invalid": "Kuruluş adı gereklidir (en fazla 120 karakter).",
  "owner-name-invalid": "Yönetici adı gereklidir (en fazla 120 karakter).",
  "owner-email-invalid": "Geçerli bir e-posta adresi girin.",
  "password-too-short": "Parola en az 12 karakter olmalıdır.",
  "password-too-long": "Parola en fazla 128 karakter olabilir.",
  "password-too-simple":
    "Parola büyük harf, küçük harf, rakam ve sembolden en az üçünü içermelidir.",
  "password-contains-email": "Parola e-posta adresinizi içermemelidir.",
};

export function mapEnrollmentFieldErrors(codes: readonly string[]): readonly string[] {
  return codes.map(
    (code) => FIELD_ERROR_MESSAGES[code] ?? "Girilen bilgiler geçersiz.",
  );
}

export function mapEnrollmentFailure(
  httpStatus: number,
  errorCodes: readonly string[],
): FirstRunEnrollmentOutcome {
  switch (httpStatus) {
    case 400:
      return {
        status: "failure",
        unavailable: false,
        messages: mapEnrollmentFieldErrors(errorCodes),
      };
    case 403:
      return {
        status: "failure",
        unavailable: false,
        messages: [
          "Kurulum anahtarı geçersiz veya süresi dolmuş. Uygulamayı yeniden başlatarak yeni bir anahtar alabilirsiniz.",
        ],
      };
    case 404:
    case 409:
      return {
        status: "failure",
        unavailable: true,
        messages: ["İlk kurulum bu cihazda artık kullanılamıyor."],
      };
    case 429:
      return {
        status: "failure",
        unavailable: false,
        messages: ["Çok fazla deneme yapıldı. Lütfen daha sonra tekrar deneyin."],
      };
    default:
      return {
        status: "failure",
        unavailable: false,
        messages: ["Kurulum tamamlanamadı. Lütfen tekrar deneyin."],
      };
  }
}

export async function fetchFirstRunAvailability(
  fetchImpl: typeof fetch = fetch,
): Promise<boolean> {
  try {
    const response = await fetchImpl(FIRST_RUN_STATUS_PATH, {
      method: "GET",
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) {
      return false;
    }

    const body: unknown = await response.json();
    return (
      typeof body === "object" &&
      body !== null &&
      (body as { available?: unknown }).available === true
    );
  } catch {
    return false;
  }
}

export async function submitFirstRunEnrollment(
  payload: FirstRunEnrollmentPayload,
  fetchImpl: typeof fetch = fetch,
): Promise<FirstRunEnrollmentOutcome> {
  try {
    const response = await fetchImpl(FIRST_RUN_ENROLL_PATH, {
      method: "POST",
      credentials: "same-origin",
      cache: "no-store",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });

    const body: unknown = await response.json().catch(() => null);
    if (response.status === 201 && typeof body === "object" && body !== null) {
      const tenantKey = (body as { tenantKey?: unknown }).tenantKey;
      return {
        status: "enrolled",
        tenantKey: typeof tenantKey === "string" ? tenantKey : payload.organizationCode,
      };
    }

    const errors =
      typeof body === "object" && body !== null && Array.isArray((body as { errors?: unknown }).errors)
        ? ((body as { errors: unknown[] }).errors.filter(
            (item): item is string => typeof item === "string",
          ))
        : [];
    return mapEnrollmentFailure(response.status, errors);
  } catch {
    return mapEnrollmentFailure(0, []);
  }
}
