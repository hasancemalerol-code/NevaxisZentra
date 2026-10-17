/**
 * Customer-facing wording for failure messages.
 *
 * Server clients compose `message` strings for logs, tests and developers (for example "workspace payload is invalid."
 * or "API base URL is not configured."). Those must never reach a customer screen. A message is shown only when it is
 * written for customers: Turkish copy. Anything else is replaced by calm, actionable Turkish wording.
 */

export const GENERIC_UNAVAILABLE_MESSAGE =
  "Bu bilgi şu anda alınamadı. Lütfen birkaç dakika sonra yeniden deneyin; sorun sürerse yöneticinize başvurun.";

export const GENERIC_ACTION_FAILED_MESSAGE =
  "İşlem tamamlanamadı. Bilgileri kontrol edip yeniden deneyin; sorun sürerse yöneticinize başvurun.";

const TURKISH_LETTER = /[ığüşöçİĞÜŞÖÇ]/;
const TURKISH_FUNCTION_WORD = /(^|[\s,.;:(])(ve|bir|için|değil|yok|ile|bu|şu|lütfen|zorunludur|gerekli|gereklidir|bulunamadı|yüklenemedi|kullanılamıyor|başarısız|oturum|yetki|hata)([\s,.;:)]|$)/i;
const TECHNICAL_TOKEN = /(\bundefined\b|\bnull\b|\bNaN\b|\[object|\bexception\b|stack ?trace|\bECONN|fetch failed|Npgsql|System\.|\bat \w+\.\w+ \(|[a-z]+[A-Z][a-zA-Z]+ is |payload is|API base URL)/;

/** True when `message` is already customer-facing Turkish copy. */
export function isCustomerMessage(message: string | null | undefined): message is string {
  if (typeof message !== "string") {
    return false;
  }

  const text = message.trim();
  if (text.length === 0 || TECHNICAL_TOKEN.test(text)) {
    return false;
  }

  return TURKISH_LETTER.test(text) || TURKISH_FUNCTION_WORD.test(text);
}

/** The message to show a customer: the original when it is customer-facing Turkish, otherwise `fallback`. */
export function customerMessage(
  message: string | null | undefined,
  fallback: string = GENERIC_UNAVAILABLE_MESSAGE,
): string {
  return isCustomerMessage(message) ? message.trim() : fallback;
}
