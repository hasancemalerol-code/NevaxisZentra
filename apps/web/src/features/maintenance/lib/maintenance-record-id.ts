const RECORD_ID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function isMaintenanceRecordId(value: string): boolean {
  return RECORD_ID_PATTERN.test(value.trim());
}

export function looksLikeRawUuid(value: string | undefined): boolean {
  return Boolean(value && RECORD_ID_PATTERN.test(value.trim()));
}
