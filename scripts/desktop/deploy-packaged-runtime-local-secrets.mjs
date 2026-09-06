#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { applyDevelopmentSaasIdentitySecrets, resolveRepositoryRoot } from "../shared/load-development-saas-identity-secrets.mjs";

const repositoryRoot = resolveRepositoryRoot(process.argv[1] ?? process.cwd());
applyDevelopmentSaasIdentitySecrets(repositoryRoot);

const targetDir = join(process.env.LOCALAPPDATA ?? "", "CrevaFactOS", "config");
mkdirSync(targetDir, { recursive: true });
const targetPath = join(targetDir, "runtime.env.local");

const keys = [
  "DevelopmentSaasIdentity__TenantOwnerPassword",
  "DevelopmentSaasIdentity__MfaSharedSecret",
  "DevelopmentSaasIdentity__Enabled",
  "DevelopmentSaasIdentity__ActiveUserPassword",
  "DevelopmentSaasIdentity__DeactivatedUserPassword",
  "DevelopmentSaasIdentity__NoMembershipUserPassword",
  "CREVAFACTOS__PERSISTENCE__PASSWORD",
  "CREVAFACTOS__MIGRATIONS__PASSWORD",
];

const lines = [];
for (const key of keys) {
  const value = process.env[key];
  if (value) {
    lines.push(`${key}=${value}`);
  }
}

if (!lines.some((line) => line.startsWith("DevelopmentSaasIdentity__TenantOwnerPassword="))) {
  console.error("Missing required Development Pilot password secret.");
  process.exit(1);
}

writeFileSync(
  targetPath,
  "# Machine-local packaged runtime secrets. DO NOT COMMIT.\n" + lines.join("\n") + "\n",
  "utf8",
);
console.log("PACKAGED_RUNTIME_LOCAL_SECRETS: READY");
