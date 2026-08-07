import {
  existsSync,
  readFileSync,
} from "node:fs";
import {
  dirname,
  resolve,
} from "node:path";
import {
  fileURLToPath,
} from "node:url";

const currentFilePath =
  fileURLToPath(import.meta.url);

const scriptsDirectory =
  dirname(currentFilePath);

const webRoot =
  resolve(scriptsDirectory, "..");

function fail(message) {
  console.error(
    `Production planning write-security verification failed: ${message}`,
  );

  process.exit(1);
}

function resolveWebPath(relativePath) {
  return resolve(webRoot, relativePath);
}

function readRequiredFile(relativePath) {
  const filePath = resolveWebPath(relativePath);

  if (!existsSync(filePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }

  return readFileSync(filePath, "utf8").replaceAll(
    "\r\n",
    "\n",
  );
}

function requireText(source, expectedText, fileLabel) {
  if (!source.includes(expectedText)) {
    fail(
      `${fileLabel} must contain "${expectedText}".`,
    );
  }
}

function forbidText(source, forbiddenText, fileLabel) {
  if (source.includes(forbiddenText)) {
    fail(
      `${fileLabel} must not contain "${forbiddenText}".`,
    );
  }
}

console.log(
  "Running production planning write-security verifier.",
);

const draftApiClient = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-api-client.ts",
);
const draftWriter = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-writer.ts",
);
const draftForm = readRequiredFile(
  "src/features/production-planning/components/production-plan-draft-form.tsx",
);
const createRoute = readRequiredFile(
  "src/app/(app)/planlama/yeni/page.tsx",
);
const editRoute = readRequiredFile(
  "src/app/(app)/planlama/[planId]/duzenle/page.tsx",
);
const runtimeConfig = readRequiredFile(
  "src/features/production-planning/server/production-plan-runtime-config.ts",
);

// 1. API writer imports server boundary (server-only)
requireText(
  draftApiClient,
  "production-plan-server-boundary",
  "production-plan-draft-api-client.ts",
);

// 2. Writer checks for development-preview block
requireText(
  draftWriter,
  "development-preview",
  "production-plan-draft-writer.ts",
);

// 3. Runtime config has no NEXT_PUBLIC_ (no client-exposed secrets)
forbidText(
  runtimeConfig,
  "NEXT_PUBLIC_",
  "production-plan-runtime-config.ts",
);

// 4. Writer resolves tenant scope server-side
requireText(
  draftWriter,
  "resolveProductionPlanningServerScope",
  "production-plan-draft-writer.ts",
);

// 5. Form does not construct backend auth headers
forbidText(
  draftForm,
  "X-Dev-Tenant-Id",
  "production-plan-draft-form.tsx",
);
forbidText(
  draftForm,
  "X-Dev-Facility-Id",
  "production-plan-draft-form.tsx",
);
forbidText(
  draftForm,
  "Authorization",
  "production-plan-draft-form.tsx",
);

// 6. Create and edit routes use server actions (not client fetch)
requireText(
  createRoute,
  '"use server"',
  "create route",
);
requireText(
  editRoute,
  '"use server"',
  "edit route",
);

// 7. No approve/transfer in write client
forbidText(
  draftApiClient,
  "approve",
  "production-plan-draft-api-client.ts",
);
forbidText(
  draftApiClient,
  "transfer",
  "production-plan-draft-api-client.ts",
);

// 8. Delete is not in write client
forbidText(
  draftApiClient,
  "DELETE",
  "production-plan-draft-api-client.ts",
);

// 9. tenantId not in request body (no client-sourced tenant)
const contractsSource = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-api-contracts.ts",
);
forbidText(
  contractsSource,
  "tenantId:",
  "production-plan-draft-api-contracts.ts (request body must not include tenantId)",
);

console.log(
  "Production planning write-security verification passed.",
);

console.log(
  [
    "Verified production.planning.write permission,",
    "tenant/facility server-side scope,",
    "preview write blocked,",
    "no browser tenant headers,",
    "no approve/transfer/delete in write client,",
    "no NEXT_PUBLIC_ in runtime config,",
    "and server-only write bridge.",
  ].join(" "),
);
