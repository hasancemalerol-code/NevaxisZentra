#!/usr/bin/env node
/**
 * Canonical Development Pilot SaaS identity secret loader.
 * Order: process env → infrastructure/.env.local → infrastructure/.env
 * Does NOT read agent transcripts or other ephemeral sources.
 */
import { existsSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import os from "node:os";

const REQUIRED_KEYS = [
  "DevelopmentSaasIdentity__TenantOwnerPassword",
  "DevelopmentSaasIdentity__MfaSharedSecret",
];

const OPTIONAL_KEYS = [
  "DevelopmentSaasIdentity__Enabled",
  "DevelopmentSaasIdentity__ActiveUserPassword",
  "DevelopmentSaasIdentity__DeactivatedUserPassword",
  "DevelopmentSaasIdentity__NoMembershipUserPassword",
];

export function resolveRepositoryRoot(startDir = dirname(fileURLToPath(import.meta.url))) {
  let current = resolve(startDir);
  for (let depth = 0; depth < 8; depth += 1) {
    if (existsSync(join(current, "CrevaFactOS.sln"))) {
      return current;
    }
    const parent = dirname(current);
    if (parent === current) {
      break;
    }
    current = parent;
  }
  throw new Error("CrevaFactOS repository root not found.");
}

function parseEnvFile(filePath) {
  const values = new Map();
  if (!existsSync(filePath)) {
    return values;
  }

  for (const line of readFileSync(filePath, "utf8").split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) {
      continue;
    }

    const separator = trimmed.indexOf("=");
    if (separator <= 0) {
      continue;
    }

    const key = trimmed.slice(0, separator).trim();
    const value = trimmed.slice(separator + 1).trim().replace(/^"|"$/g, "");
    if (key) {
      values.set(key, value);
    }
  }

  return values;
}

export function getPackagedMachineSecretsPath() {
  const localAppData = process.env.LOCALAPPDATA || join(os.homedir(), "AppData", "Local");
  return join(localAppData, "CrevaFactOS", "config", "runtime.env.local");
}

export function loadDevelopmentSaasIdentitySecrets(repositoryRoot = resolveRepositoryRoot()) {
  const layers = [
    parseEnvFile(join(repositoryRoot, "infrastructure", ".env")),
    parseEnvFile(join(repositoryRoot, "infrastructure", ".env.local")),
    parseEnvFile(getPackagedMachineSecretsPath()),
  ];

  const resolved = new Map();
  for (const key of [...REQUIRED_KEYS, ...OPTIONAL_KEYS]) {
    if (process.env[key]) {
      resolved.set(key, process.env[key]);
    }
  }

  for (const layer of layers) {
    for (const [key, value] of layer) {
      if (!REQUIRED_KEYS.includes(key) && !OPTIONAL_KEYS.includes(key)) {
        continue;
      }
      if (!resolved.has(key) && value) {
        resolved.set(key, value);
      }
    }
  }

  for (const layer of layers) {
    for (const [key, value] of layer) {
      if (REQUIRED_KEYS.includes(key) || OPTIONAL_KEYS.includes(key)) {
        if (value) {
          resolved.set(key, value);
        }
      }
    }
  }

  return resolved;
}

export function applyDevelopmentSaasIdentitySecrets(repositoryRoot = resolveRepositoryRoot()) {
  const secrets = loadDevelopmentSaasIdentitySecrets(repositoryRoot);
  for (const [key, value] of secrets) {
    if (!process.env[key]) {
      process.env[key] = value;
    }
  }
  return secrets;
}

export function requireDevelopmentSaasIdentitySecret(name, repositoryRoot = resolveRepositoryRoot()) {
  applyDevelopmentSaasIdentitySecrets(repositoryRoot);
  const value = process.env[name] ?? loadDevelopmentSaasIdentitySecrets(repositoryRoot).get(name);
  if (!value) {
    throw new Error(
      `Missing required Development Pilot secret ${name}. ` +
        "Set it in infrastructure/.env.local (preferred) or infrastructure/.env.",
    );
  }
  return value;
}

export function describeDevelopmentSaasIdentitySecretSources(repositoryRoot = resolveRepositoryRoot()) {
  const infra = join(repositoryRoot, "infrastructure", ".env");
  const local = join(repositoryRoot, "infrastructure", ".env.local");
  const sources = {};

  for (const key of REQUIRED_KEYS) {
    if (process.env[key]) {
      sources[key] = "process-env";
    } else if (parseEnvFile(getPackagedMachineSecretsPath()).has(key)) {
      sources[key] = "%LOCALAPPDATA%/CrevaFactOS/config/runtime.env.local";
    } else if (parseEnvFile(local).has(key)) {
      sources[key] = "infrastructure/.env.local";
    } else if (parseEnvFile(infra).has(key)) {
      sources[key] = "infrastructure/.env";
    } else {
      sources[key] = "missing";
    }
  }

  return sources;
}

if (import.meta.url === `file://${process.argv[1]?.replace(/\\/g, "/")}`) {
  const root = resolveRepositoryRoot();
  applyDevelopmentSaasIdentitySecrets(root);
  const sources = describeDevelopmentSaasIdentitySecretSources(root);
  for (const key of REQUIRED_KEYS) {
    const present = Boolean(process.env[key]);
    console.log(`${key} source=${sources[key]} present=${present}`);
  }
  const missing = REQUIRED_KEYS.filter((key) => !process.env[key]);
  if (missing.length > 0) {
    process.exit(1);
  }
}
