#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260913135602_AddFactoryNetworkOptimizationFoundation.cs"),
  "utf8",
);

const tables = [
  "factory_networks",
  "factory_network_facilities",
  "facility_capability_profiles",
  "network_capacity_snapshots",
  "network_scenarios",
  "network_allocations",
  "network_allocation_items",
  "factory_network_policies",
  "network_activity_events",
];

for (const table of tables) {
  if (!migration.includes(`name: "${table}"`)) {
    throw new Error(`${table} table missing`);
  }
}

const permissions = [
  "000000000f64",
  "factory-network.read",
  "000000000f65",
  "factory-network.simulate",
  "000000000f66",
  "factory-network.manage",
  "000000000f67",
  "factory-network.approve",
  "000000000f68",
  "factory-network.execute",
  "000000000f69",
  "factory-network.policy.manage",
];

for (const permission of permissions) {
  if (!migration.includes(permission)) {
    throw new Error(`permission ${permission} missing`);
  }
}

if (!migration.includes("ENABLE ROW LEVEL SECURITY")) {
  throw new Error("RLS missing");
}

if (!migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE factory_networks TO crevafactos_runtime")) {
  throw new Error("runtime grants missing");
}

if (!migration.includes("INSERT INTO tenant_role_permissions")) {
  throw new Error("tenant_role_permissions inserts missing");
}

const plantBNetworkSeedMarkers = [
  "bb000001-0005-4000-8000-000000000008",
  "plant-b-gebze",
  "bb000001-0008-4000-8000-000000000810",
  "Nevaxis Pilot Fabrika Ağı",
  "bb000001-0008-4000-8000-000000000815",
  "bb000001-0008-4000-8000-000000000817",
];

for (const marker of plantBNetworkSeedMarkers) {
  if (!migration.includes(marker)) {
    throw new Error(`Plant B network seed marker ${marker} missing`);
  }
}

console.log("FACTORY_NETWORK_UM8_MIGRATION_VERIFIER = PASS");
