#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB_BASE = process.env.MOLD_BROWSER_BASE_URL ?? "http://localhost:3001";
const TENANT_KEY = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAASIDENT1-CLOSURE";

const MIGRATIONS = [
  {
    id: "20260814055239_AddSaasIdentityAccountAndSessionFoundation",
    marker: "AddSaasIdentityAccountAndSessionFoundation",
    snapshotMarkers: [
      'ToTable("saas_identity_accounts"',
      'ToTable("saas_identity_sessions"',
      'ToTable("saas_identity_invitations"',
      'ToTable("saas_identity_security_audit_events"',
    ],
  },
  {
    id: "20260814095248_FixSaasIdentitySessionResolutionFunction",
    marker: "FixSaasIdentitySessionResolutionFunction",
    snapshotMarkers: ['ToTable("saas_identity_sessions"'],
  },
];

const FUNCTION_NAME = "resolve_saas_identity_session_by_token_hash";
const RLS_TABLES = [
  "saas_identity_sessions",
  "saas_identity_invitations",
  "saas_identity_security_audit_events",
];

const DEV_ACCOUNT_IDS = [
  "bb000002-0001-4000-8000-000000000001",
  "bb000002-0011-4000-8000-000000000011",
  "bb000002-0021-4000-8000-000000000021",
  "bb000002-0031-4000-8000-000000000031",
];

const TENANT_OWNER_EMAIL = "admin@demo.nevaxiszentra.local";
const ACTIVE_USER_EMAIL = "active-user@dev.local";

const UUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;
const INTERNAL_ENUM_PATTERN = /\b(PendingVerification|Locked|mfa_pending|lifecycle_status)\b/;
const OPAQUE_TOKEN_PATTERN = /\b[a-f0-9]{40,}\b/i;

function fail(message) {
  console.error(`SAAS-IDENTITY-1 ui/migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) fail(`Missing ${relativePath}`);
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function requireEnv(name) {
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing required environment variable ${name}`);
  return value;
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function readMigrationConnectionEnv() {
  const appsettingsPath = resolve(ROOT, "apps/api/appsettings.Development.json");
  const appsettings = JSON.parse(readFileSync(appsettingsPath, "utf8"));
  const persistence = appsettings.Persistence ?? {};
  return {
    ...process.env,
    NEVAXIS_FACTOS__MIGRATIONS__HOST: persistence.Host ?? "127.0.0.1",
    NEVAXIS_FACTOS__MIGRATIONS__PORT: String(persistence.Port ?? 5432),
    NEVAXIS_FACTOS__MIGRATIONS__DATABASE: persistence.Database ?? "crevafactos_local",
    NEVAXIS_FACTOS__MIGRATIONS__USERNAME: "crevafactos",
    NEVAXIS_FACTOS__MIGRATIONS__PASSWORD:
      process.env.NEVAXIS_FACTOS__MIGRATIONS__PASSWORD
      ?? process.env.NEVAXIS_FACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE: "Disable",
    NEVAXIS_FACTOS__MIGRATIONS__CONNECTION_TIMEOUT_SECONDS: "15",
    NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "120",
  };
}

function runSecondMigrationApply() {
  const migrationProject = resolve(
    ROOT,
    "packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj",
  );
  return spawnSync(
    "dotnet",
    [
      "tool",
      "run",
      "dotnet-ef",
      "--",
      "database",
      "update",
      "--project",
      migrationProject,
      "--startup-project",
      migrationProject,
      "--context",
      "NevaxisZentraDbContext",
    ],
    {
      cwd: ROOT,
      encoding: "utf8",
      env: readMigrationConnectionEnv(),
    },
  );
}

function normalizeSql(sql) {
  return sql
    .replace(/--.*$/gm, "")
    .replace(/\s+/g, " ")
    .trim()
    .toLowerCase();
}

function extractMigrationFunctionBody(source) {
  const match = source.match(/\$function\$\s*([\s\S]*?)\s*\$function\$/);
  if (!match) fail("Could not extract function body from fix migration source");
  return normalizeSql(match[1]);
}

function extractLiveFunctionBody(definition) {
  const match = definition.match(/\$function\$\s*([\s\S]*?)\s*\$function\$/i)
    ?? definition.match(/as\s+([\s\S]*?)(?:language\s+sql|$)/i);
  if (!match) fail("Could not extract function body from live definition");
  return normalizeSql(match[1]);
}

function base32Decode(encoded) {
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  const normalized = encoded.trim().replace(/=+$/g, "").toUpperCase();
  const output = [];
  let buffer = 0;
  let bitsLeft = 0;

  for (const character of normalized) {
    const value = alphabet.indexOf(character);
    if (value < 0) fail("Invalid Base32 character in MFA secret");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }

  return Buffer.from(output);
}

function computeTotp(secret, counter) {
  const key = base32Decode(secret);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", key).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binary =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  return String(binary % 1_000_000).padStart(6, "0");
}

function generateTotp(secret, utcMs = Date.now()) {
  const counter = Math.floor(utcMs / 1000 / 30);
  return computeTotp(secret, counter);
}

function countDevAccounts() {
  const idList = DEV_ACCOUNT_IDS.map((id) => `'${id}'`).join(", ");
  return Number(db(`SELECT COUNT(*) FROM saas_identity_accounts WHERE id IN (${idList})`));
}

function previewClosureFixtures() {
  const accountCount = Number(db(`
    SELECT COUNT(*) FROM saas_identity_accounts
    WHERE display_name LIKE '${CLOSURE_PREFIX}%'
       OR account_reference LIKE '${CLOSURE_PREFIX.toLowerCase()}%'
  `));
  const sessionCount = Number(db(`
    SELECT COUNT(*) FROM saas_identity_sessions
    WHERE device_label LIKE '${CLOSURE_PREFIX}%'
  `));
  console.log(`Preview ${CLOSURE_PREFIX} fixtures: accounts=${accountCount}, sessions=${sessionCount}`);
  return { accountCount, sessionCount };
}

function cleanupClosureFixtures() {
  db(`
    DELETE FROM saas_identity_sessions
    WHERE device_label LIKE '${CLOSURE_PREFIX}%'
  `);
  db(`
    DELETE FROM saas_identity_accounts
    WHERE display_name LIKE '${CLOSURE_PREFIX}%'
       OR account_reference LIKE '${CLOSURE_PREFIX.toLowerCase()}%'
  `);
  const remaining = previewClosureFixtures();
  assert(`${CLOSURE_PREFIX} fixtures cleaned up`, remaining.accountCount === 0 && remaining.sessionCount === 0);
}

function assertNoSensitivePageText(pageText, label) {
  assert(`${label} has no raw UUID`, !UUID_PATTERN.test(pageText));
  assert(`${label} has no internal enum`, !INTERNAL_ENUM_PATTERN.test(pageText));
  assert(`${label} has no opaque token`, !OPAQUE_TOKEN_PATTERN.test(pageText));
}

function resolveChromiumExecutable() {
  const candidates = [
    process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    process.env.CHROME_PATH,
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
    "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
  ].filter(Boolean);

  for (const candidate of candidates) {
    if (existsSync(candidate)) return candidate;
  }

  return undefined;
}

async function signInThroughForm(page, { organization, email, password }) {
  await page.goto(`${WEB_BASE}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(organization);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();
}

async function fetchMeWithPageCookies(page) {
  return page.evaluate(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/api/identity/me`, {
      method: "GET",
      credentials: "include",
      headers: { Accept: "application/json" },
      cache: "no-store",
    });
    const text = await response.text();
    let json = null;
    try {
      json = text ? JSON.parse(text) : null;
    } catch {
      json = { raw: text };
    }
    return { status: response.status, json, raw: text };
  }, WEB_BASE);
}

async function runBrowserVerification({
  activeUserPassword,
  tenantOwnerPassword,
  mfaSharedSecret,
}) {
  const ready = await fetch(`${WEB_BASE}/giris`, { redirect: "manual" });
  assert("web server reachable", ready.status === 200 || ready.status === 307 || ready.status === 308, `status=${ready.status}`);

  const { chromium } = await import("playwright");
  const executablePath = resolveChromiumExecutable();
  const browser = await chromium.launch({
    headless: true,
    ...(executablePath ? { executablePath } : {}),
  });

  const consoleErrors = [];

  try {
    const activeContext = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
    const activePage = await activeContext.newPage();
    activePage.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    activePage.on("pageerror", (err) => consoleErrors.push(String(err)));

    await signInThroughForm(activePage, {
      organization: TENANT_KEY,
      email: ACTIVE_USER_EMAIL,
      password: activeUserPassword,
    });
    await activePage.waitForURL("**/dashboard", { timeout: 30000 });
    assert("active user reaches dashboard", activePage.url().includes("/dashboard"));
    assert(
      "active user dashboard shell visible",
      await activePage.locator("h1").filter({ hasText: "AI Görev Merkezi" }).first().isVisible(),
    );

    const activeMe = await fetchMeWithPageCookies(activePage);
    assert("active user /me authenticated", activeMe.status === 200, `status=${activeMe.status}`);

    const activeOverflow = await activePage.evaluate(() => ({
      doc: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
      body: document.body.scrollWidth > document.body.clientWidth + 1,
    }));
    assert("active user no horizontal overflow", !activeOverflow.doc && !activeOverflow.body);

    const activeText = await activePage.locator("body").innerText();
    assertNoSensitivePageText(activeText, "active user dashboard");

    await activePage.getByRole("button", { name: "Oturumu kapat" }).click();
    await activePage.waitForURL("**/giris", { timeout: 30000 });
    assert("active user signed out to /giris", activePage.url().includes("/giris"));
    await activeContext.close();

    const adminContext = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
    const adminPage = await adminContext.newPage();
    adminPage.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    adminPage.on("pageerror", (err) => consoleErrors.push(String(err)));

    await signInThroughForm(adminPage, {
      organization: TENANT_KEY,
      email: TENANT_OWNER_EMAIL,
      password: tenantOwnerPassword,
    });
    await adminPage.waitForURL("**/mfa-dogrula", { timeout: 30000 });

    const pendingMe = await fetchMeWithPageCookies(adminPage);
    assert("admin password-only /me returns 403", pendingMe.status === 403, `status=${pendingMe.status}`);

    const mfaTextBefore = await adminPage.locator("body").innerText();
    assertNoSensitivePageText(mfaTextBefore, "admin MFA page");

    await adminPage.locator("#mfa-totp").fill(generateTotp(mfaSharedSecret));
    await adminPage.getByRole("button", { name: "Doğrula" }).click();
    await adminPage.waitForURL("**/dashboard", { timeout: 30000 });

    const adminMe = await fetchMeWithPageCookies(adminPage);
    assert("admin MFA-complete /me authenticated", adminMe.status === 200, `status=${adminMe.status}`);
    assert(
      "admin /me returns accountReference not UUID",
      adminMe.json?.accountReference && !UUID_PATTERN.test(adminMe.json.accountReference),
      adminMe.raw?.slice(0, 200),
    );

    const adminOverflow = await adminPage.evaluate(() => ({
      doc: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
      body: document.body.scrollWidth > document.body.clientWidth + 1,
    }));
    assert("admin no horizontal overflow", !adminOverflow.doc && !adminOverflow.body);

    const adminText = await adminPage.locator("body").innerText();
    assertNoSensitivePageText(adminText, "admin dashboard");

    await adminContext.close();

    const hydrationLike = consoleErrors.some((line) =>
      /hydration|serialization|Text content does not match|Only plain objects/i.test(line),
    );
    assert("no hydration/serialization/console errors", !hydrationLike, consoleErrors.join(" | "));
  } finally {
    await browser.close();
  }
}

console.log("Running SAAS-IDENTITY-1 ui/migration verifier.");

const tenantOwnerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const activeUserPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

const snapshot = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const endpoints = readRequired("apps/api/SaasIdentity/SaasIdentityEndpointRouteBuilderExtensions.cs");
const authExtensions = readRequired("apps/api/Authentication/AuthenticationServiceCollectionExtensions.cs");
const authConstants = readRequired("apps/web/src/features/saas-identity/lib/saas-identity-constants.ts");
const signInPage = readRequired("apps/web/src/app/(auth)/giris/page.tsx");
const mfaPage = readRequired("apps/web/src/app/(auth)/mfa-dogrula/page.tsx");
const forgotPage = readRequired("apps/web/src/app/(auth)/sifremi-unuttum/page.tsx");
const resetPage = readRequired("apps/web/src/app/(auth)/sifre-sifirla/page.tsx");
const invitationPage = readRequired("apps/web/src/app/(auth)/davet/kabul/page.tsx");

for (const migrationInfo of MIGRATIONS) {
  const migration = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${migrationInfo.id}.cs`);
  const designer = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${migrationInfo.id}.Designer.cs`);

  assert(`${migrationInfo.id} migration source present`, migration.includes(migrationInfo.marker));
  assert(`${migrationInfo.id} designer present`, designer.includes(migrationInfo.marker));
  for (const marker of migrationInfo.snapshotMarkers) {
    assert(`${migrationInfo.id} snapshot includes ${marker}`, snapshot.includes(marker));
  }

  const historyCount = Number(db(`
    SELECT COUNT(*) FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '${migrationInfo.id}'
  `));
  assert(`${migrationInfo.id} history row exactly one`, historyCount === 1, `count=${historyCount}`);
}

const fixMigration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260814095248_FixSaasIdentitySessionResolutionFunction.cs",
);
const migrationFunctionBody = extractMigrationFunctionBody(fixMigration);
const liveFunctionDefinition = db(`SELECT pg_get_functiondef('${FUNCTION_NAME}(text)'::regprocedure)`);
const liveFunctionBody = extractLiveFunctionBody(liveFunctionDefinition);

assert(
  "resolve_saas_identity_session_by_token_hash source/live body agreement",
  migrationFunctionBody === liveFunctionBody,
  "migration and live function bodies differ",
);

assert("function is SECURITY DEFINER", fixMigration.includes("SECURITY DEFINER"));
assert("function sets search_path", fixMigration.includes("SET search_path = pg_catalog, public"));
forbidText(migrationFunctionBody, "execute ", "migration function body");
forbidText(migrationFunctionBody, "format(", "migration function body");
forbidText(migrationFunctionBody, "quote_ident(", "migration function body");

const securityDefiner = db(`
  SELECT prosecdef::text
  FROM pg_proc
  WHERE proname = '${FUNCTION_NAME}'
`);
assert("live function SECURITY DEFINER", securityDefiner === "t" || securityDefiner === "true", securityDefiner);

const searchPathConfig = db(`
  SELECT COALESCE(array_to_string(proconfig, ','), '')
  FROM pg_proc
  WHERE proname = '${FUNCTION_NAME}'
`);
assert(
  "live function search_path = pg_catalog, public",
  searchPathConfig.includes("search_path=pg_catalog, public"),
  searchPathConfig,
);

forbidText(liveFunctionBody, "execute ", "live function body");
forbidText(liveFunctionBody, "format(", "live function body");
forbidText(liveFunctionBody, "quote_ident(", "live function body");

assert("fix migration revokes PUBLIC execute", fixMigration.includes("REVOKE ALL ON FUNCTION resolve_saas_identity_session_by_token_hash(text) FROM PUBLIC"));
assert(
  "fix migration grants runtime execute",
  fixMigration.includes("GRANT EXECUTE ON FUNCTION resolve_saas_identity_session_by_token_hash(text) TO crevafactos_runtime"),
);

const functionOwner = db(`
  SELECT r.rolname
  FROM pg_proc p
  JOIN pg_roles r ON r.oid = p.proowner
  WHERE p.proname = '${FUNCTION_NAME}'
`);
assert("function owner is NevaxisZentra", functionOwner === "crevafactos", functionOwner);
assert("function owner is not crevafactos_runtime", functionOwner !== "crevafactos_runtime");

const publicExecute = Number(db(`
  SELECT COUNT(*)
  FROM information_schema.routine_privileges
  WHERE routine_name = '${FUNCTION_NAME}'
    AND grantee = 'PUBLIC'
    AND privilege_type = 'EXECUTE'
`));
assert("EXECUTE revoked from PUBLIC", publicExecute === 0, `count=${publicExecute}`);

const runtimeExecute = Number(db(`
  SELECT COUNT(*)
  FROM information_schema.routine_privileges
  WHERE routine_name = '${FUNCTION_NAME}'
    AND grantee = 'crevafactos_runtime'
    AND privilege_type = 'EXECUTE'
`));
assert("EXECUTE granted to crevafactos_runtime", runtimeExecute === 1, `count=${runtimeExecute}`);

for (const tableName of RLS_TABLES) {
  const rlsState = db(`
    SELECT relrowsecurity, relforcerowsecurity
    FROM pg_class
    WHERE relname = '${tableName}'
  `);
  assert(`${tableName} RLS enabled`, rlsState === "t|t", rlsState);
}

const nonRestrictiveIdentityFks = Number(db(`
  SELECT COUNT(*)
  FROM pg_constraint c
  JOIN pg_class t ON t.oid = c.conrelid
  WHERE c.contype = 'f'
    AND t.relname LIKE 'saas_identity_%'
    AND c.confdeltype <> 'r'
`));
assert("identity foreign keys are restrictive", nonRestrictiveIdentityFks === 0, `count=${nonRestrictiveIdentityFks}`);

assert("login route wired in API", endpoints.includes('MapPost("/sign-in"'));
assert("forgot-password route wired in API", endpoints.includes('MapPost("/password/forgot"'));
assert("reset-password route wired in API", endpoints.includes('MapPost("/password/reset"'));
assert("invitation route wired in API", endpoints.includes('MapPost("/invitations/accept"'));
assert("MFA verify route wired in API", endpoints.includes('MapPost("/mfa/verify"'));
assert("me route wired in API", endpoints.includes('MapGet("/me"'));

assert("login page wired", signInPage.includes("SignInForm"));
assert("MFA page wired", mfaPage.includes("MfaVerifyForm"));
assert("forgot-password page wired", forgotPage.includes("ForgotPasswordForm"));
assert("reset-password page wired", resetPage.includes("ResetPasswordForm"));
assert("invitation page wired", invitationPage.includes("InvitationAcceptForm"));
assert("auth route constants include login", authConstants.includes('"/giris"'));
assert("auth route constants include forgot", authConstants.includes('"/sifremi-unuttum"'));
assert("auth route constants include reset", authConstants.includes('"/sifre-sifirla"'));
assert("auth route constants include invitation", authConstants.includes('"/davet/kabul"'));
assert("auth route constants include MFA", authConstants.includes('"/mfa-dogrula"'));

assert(
  "production cannot use Development header auth",
  authExtensions.includes("Development authentication is available only in the Development environment."),
);
assert(
  "Development auth gated by environment",
  authExtensions.includes("if (!environment.IsDevelopment())"),
);

previewClosureFixtures();

const devAccountsBefore = countDevAccounts();
if (devAccountsBefore !== DEV_ACCOUNT_IDS.length) {
  fail(
    `dev identity fixtures not bootstrapped — expected ${DEV_ACCOUNT_IDS.length} deterministic accounts, found ${devAccountsBefore}`,
  );
}

const historyBefore = MIGRATIONS.map((migrationInfo) => ({
  id: migrationInfo.id,
  count: Number(db(`
    SELECT COUNT(*) FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '${migrationInfo.id}'
  `)),
}));

const applyResult = runSecondMigrationApply();
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("second migration apply succeeds", applyResult.status === 0, applyOutput.slice(-500));
assert(
  "second migration apply is no-op",
  /No migrations were applied|already up to date|Done\./i.test(applyOutput),
  applyOutput.slice(-500),
);

for (const entry of historyBefore) {
  const historyAfter = Number(db(`
    SELECT COUNT(*) FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '${entry.id}'
  `));
  assert(
    `${entry.id} history not duplicated`,
    historyAfter === entry.count,
    `${entry.count}→${historyAfter}`,
  );
}

const devAccountsAfter = countDevAccounts();
assert(
  "restart did not duplicate deterministic dev accounts",
  devAccountsAfter === devAccountsBefore,
  `${devAccountsBefore}→${devAccountsAfter}`,
);

await runBrowserVerification({
  activeUserPassword,
  tenantOwnerPassword,
  mfaSharedSecret,
});

cleanupClosureFixtures();

console.log("SAAS-IDENTITY-1 ui/migration verification passed.");
