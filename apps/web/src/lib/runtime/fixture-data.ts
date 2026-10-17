import { deferredSurfacesEnabled } from "./deferred-surfaces";

/**
 * Seeded pilot, demo, fixture and simulator data may exist only in explicitly isolated environments (Development, Demo,
 * Presentation, controlled pilot/test). A normal Production tenant fails closed: no fixture names, simulator gateways,
 * synthetic orders or demo labels are ever shown, and a premium empty state is shown instead.
 */
export function fixtureDataVisible(env: Readonly<Record<string, string | undefined>> = process.env): boolean {
  return deferredSurfacesEnabled(env);
}

/** Provenance values that identify sample, simulated or development-pilot records. */
const FIXTURE_PROVENANCE = /^(simulat|sample|fixture|demo|seed|development[-_ ]?pilot|golden[-_ ]?demo|pilot[-_ ]?data)/i;

export function isFixtureProvenance(value: string | null | undefined): boolean {
  return FIXTURE_PROVENANCE.test((value ?? "").trim());
}

/** Keeps fixture rows only in isolated environments. `isFixture` decides per row. */
export function withoutFixtures<T>(rows: readonly T[], isFixture: (row: T) => boolean, env?: Readonly<Record<string, string | undefined>>): T[] {
  return fixtureDataVisible(env) ? [...rows] : rows.filter((row) => !isFixture(row));
}

const FIXTURE_KEYS = ["provider", "sourceProvider", "providerKey", "vendorKey", "dataOrigin", "evidenceSource", "sourceKind"] as const;

/** Seed naming conventions: test transports, test companies, simulator codes, development-prefixed names. */
const FIXTURE_TEXT = /\btest[- ]transport\b|-TEST-COMPANY-|^SIM-|^Development\b/i;

function isFixtureRecord(value: unknown): boolean {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const record = value as Record<string, unknown>;
  if (record.isPilotData === true || record.hasDevelopmentPilotData === true || record.isSimulatorFixture === true) return true;
  if (FIXTURE_KEYS.some((key) => typeof record[key] === "string" && isFixtureProvenance(record[key] as string))) return true;
  return Object.values(record).some((entry) => typeof entry === "string" && FIXTURE_TEXT.test(entry));
}

/**
 * Removes fixture/simulator records from an API payload outside isolated environments: arrays lose their fixture rows
 * (also one level below the root object), and a single record that is itself a fixture becomes `null` (treat as not found).
 */
export function stripFixtureRecords<T>(value: T, env?: Readonly<Record<string, string | undefined>>): T | null {
  if (fixtureDataVisible(env)) return value;
  if (Array.isArray(value)) return value.filter((item) => !isFixtureRecord(item)) as unknown as T;
  if (typeof value !== "object" || value === null) return value;
  if (isFixtureRecord(value)) return null;
  const out: Record<string, unknown> = { ...(value as Record<string, unknown>) };
  for (const [key, entry] of Object.entries(out)) {
    if (Array.isArray(entry)) out[key] = entry.filter((item) => !isFixtureRecord(item));
  }
  return out as T;
}
