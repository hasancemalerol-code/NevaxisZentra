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
    `Production planning draft-write verification failed: ${message}`,
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
  "Running production planning draft-write verifier.",
);

// 1. Server-side write files exist
const draftApiContracts = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-api-contracts.ts",
);
const draftApiClient = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-api-client.ts",
);
const draftWriter = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-writer.ts",
);

// 2. Create route exists
const createRoute = readRequiredFile(
  "src/app/(app)/planlama/yeni/page.tsx",
);

// 3. Edit route exists
const editRoute = readRequiredFile(
  "src/app/(app)/planlama/[planId]/duzenle/page.tsx",
);

// 4. Draft form component exists
const draftForm = readRequiredFile(
  "src/features/production-planning/components/production-plan-draft-form.tsx",
);

// 5. API writer is server-only (imports server boundary)
requireText(
  draftApiClient,
  "production-plan-server-boundary",
  "production-plan-draft-api-client.ts",
);

// 6. tenantId not in request body contract
forbidText(
  draftApiContracts,
  "tenantId:",
  "production-plan-draft-api-contracts.ts (request body must not include tenantId)",
);

// 7. expectedVersion is required in update contract
requireText(
  draftApiContracts,
  "expectedVersion",
  "production-plan-draft-api-contracts.ts",
);

// 8. Create route uses POST semantic (no approve/transfer)
forbidText(
  createRoute,
  "approve",
  "create route",
);
forbidText(
  createRoute,
  "transfer",
  "create route",
);

// 9. Edit route checks editable status
requireText(
  editRoute,
  "EDITABLE_STATUSES",
  "edit route",
);

// 10. preview mode write blocked in writer
requireText(
  draftWriter,
  "development-preview",
  "production-plan-draft-writer.ts",
);

// 11. Form tracks dirty state for unsaved-changes warning
requireText(
  draftForm,
  "beforeunload",
  "production-plan-draft-form.tsx",
);

// 12. Form prevents double-submit
requireText(
  draftForm,
  "submitting",
  "production-plan-draft-form.tsx",
);

// 13. Form has "use client"
requireText(
  draftForm,
  '"use client"',
  "production-plan-draft-form.tsx",
);

// 14. Form does NOT directly call backend API
forbidText(
  draftForm,
  "production-plan-draft-api-client",
  "production-plan-draft-form.tsx",
);

// 15. Create route has server action
requireText(
  createRoute,
  '"use server"',
  "create route",
);

// 16. Edit route has server action
requireText(
  editRoute,
  '"use server"',
  "edit route",
);

// 17. No PATCH/DELETE in writer
forbidText(
  draftApiClient,
  "PATCH",
  "production-plan-draft-api-client.ts",
);
forbidText(
  draftApiClient,
  "DELETE",
  "production-plan-draft-api-client.ts",
);

console.log(
  "Production planning draft-write verification passed.",
);

console.log(
  [
    "Verified write permission separation,",
    "POST and PUT write routes,",
    "no PATCH/DELETE,",
    "tenantId absent from request contract,",
    "expectedVersion in update,",
    "server-only API writer,",
    "preview write blocked,",
    "create/edit routes and form present.",
  ].join(" "),
);
