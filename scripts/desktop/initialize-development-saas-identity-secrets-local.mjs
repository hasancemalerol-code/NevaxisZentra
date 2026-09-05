#!/usr/bin/env node
/**
 * One-time local bootstrap: writes infrastructure/.env.local when Development Pilot
 * secrets are missing. Uses only existing local env files — never agent transcripts.
 */
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { resolveRepositoryRoot } from "../shared/load-development-saas-identity-secrets.mjs";

const REQUIRED = [
  "DevelopmentSaasIdentity__Enabled",
  "DevelopmentSaasIdentity__TenantOwnerPassword",
  "DevelopmentSaasIdentity__MfaSharedSecret",
  "DevelopmentSaasIdentity__ActiveUserPassword",
  "DevelopmentSaasIdentity__DeactivatedUserPassword",
  "DevelopmentSaasIdentity__NoMembershipUserPassword",
];

function parseEnvFile(filePath) {
  const values = new Map();
  if (!existsSync(filePath)) {
    return values;
  }
  for (const line of readFileSync(filePath, "utf8").split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) continue;
    const i = trimmed.indexOf("=");
    if (i <= 0) continue;
    values.set(trimmed.slice(0, i).trim(), trimmed.slice(i + 1).trim());
  }
  return values;
}

const root = resolveRepositoryRoot();
const localPath = join(root, "infrastructure", ".env.local");
const infraPath = join(root, "infrastructure", ".env");
const existingLocal = parseEnvFile(localPath);
const infra = parseEnvFile(infraPath);

const missing = REQUIRED.filter((key) => !existingLocal.has(key) && !infra.has(key) && !process.env[key]);
if (missing.length === REQUIRED.length) {
  console.error(
    "Cannot initialize infrastructure/.env.local: no DevelopmentSaasIdentity secrets found in env or infrastructure/.env.",
  );
  process.exit(1);
}

const lines = [];
if (existsSync(localPath)) {
  lines.push(...readFileSync(localPath, "utf8").split(/\r?\n/));
} else {
  lines.push(
    "# Local-only Development Pilot SaaS identity secrets.",
    "# Gitignored — do not commit.",
    "",
  );
}

const presentKeys = new Set(
  lines
    .map((line) => line.trim())
    .filter((line) => line && !line.startsWith("#") && line.includes("="))
    .map((line) => line.slice(0, line.indexOf("=")).trim()),
);

let changed = false;
for (const key of REQUIRED) {
  if (presentKeys.has(key)) {
    continue;
  }
  const value = process.env[key] ?? infra.get(key);
  if (!value) {
    continue;
  }
  lines.push(`${key}=${value}`);
  presentKeys.add(key);
  changed = true;
}

if (!changed) {
  console.log("infrastructure/.env.local already contains Development Pilot secrets.");
  process.exit(0);
}

writeFileSync(localPath, `${lines.filter((line, index, all) => line.length > 0 || index < all.length - 1).join("\n")}\n`, "utf8");
console.log("infrastructure/.env.local updated with missing Development Pilot secrets.");
