#!/usr/bin/env node
import {
  applyDevelopmentSaasIdentitySecrets,
  requireDevelopmentSaasIdentitySecret,
  resolveRepositoryRoot,
} from "../shared/load-development-saas-identity-secrets.mjs";

const API = (process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000").replace(/\/$/, "");
const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001").replace(/\/$/, "");

function fail(message) {
  console.error(`CREDENTIAL-READINESS probe failed: ${message}`);
  process.exit(1);
}

const root = resolveRepositoryRoot();
applyDevelopmentSaasIdentitySecrets(root);
const password = requireDevelopmentSaasIdentitySecret(
  "DevelopmentSaasIdentity__TenantOwnerPassword",
  root,
);

const body = {
  organizationContext: "dev-pilot-tenant",
  email: "tenant-owner@dev.local",
  password,
  deviceLabel: "credential-readiness-probe",
};

for (const [label, base] of [["API", API], ["WEB", WEB]]) {
  const response = await fetch(`${base}/api/identity/sign-in`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });
  const text = await response.text();
  if (response.status !== 200) {
    fail(`${label} sign-in expected 200, got ${response.status}: ${text.slice(0, 160)}`);
  }
  if (!text.includes('"requiresMfa":true')) {
    fail(`${label} sign-in did not require MFA: ${text.slice(0, 160)}`);
  }
}

console.log("PASS credential-readiness sign-in probe (API + WEB, MFA required)");
