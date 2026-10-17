import { displayDataName } from "./display-name";

/**
 * Opaque identifiers (GUIDs, technical references) are for machines. When a display name is missing the UI must fall
 * back to a plain word instead of printing the identifier as if it were a name.
 */
const OPAQUE_ID = /^[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}$/i;

export function isOpaqueId(value: string | null | undefined): boolean {
  return typeof value === "string" && OPAQUE_ID.test(value.trim());
}

/** `value` when it reads like a name, otherwise `fallback`. */
export function displayLabel(value: string | null | undefined, fallback: string): string {
  const text = value?.trim();
  return text && !isOpaqueId(text) ? text : fallback;
}

const OPAQUE_ACTOR = /^(?:[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}|(?:mem|usr|user|sub|acc|tenant)[_-][\w-]{4,}|[0-9a-f]{24,})$/i;

/** True for member, account and subject identifiers that must never be shown as a person. */
export function isOpaqueActorId(value: string | null | undefined): boolean {
  return typeof value === "string" && OPAQUE_ACTOR.test(value.trim());
}

/** The label for a creator / actor / owner: the readable name when there is one, otherwise a plain role word. */
export function displayActor(value: string | null | undefined, fallback = "Yetkili kullanıcı"): string {
  const text = value?.trim();
  if (!text) return "—";
  return isOpaqueActorId(text) ? fallback : text;
}

/**
 * A labeler for one screen: readable names pass through, and every distinct opaque member label gets the same stable
 * "Ekip üyesi N" label wherever it appears, so two different people never look identical and no raw id is shown.
 */
export function createActorLabeler(prefix = "Ekip üyesi"): (value: string | null | undefined) => string {
  const seen = new Map<string, number>();
  return (value) => {
    const text = value?.trim();
    if (!text) return "—";
    if (!isOpaqueActorId(text)) return displayDataName(text);
    if (!seen.has(text)) seen.set(text, seen.size + 1);
    return `${prefix} ${seen.get(text)}`;
  };
}
