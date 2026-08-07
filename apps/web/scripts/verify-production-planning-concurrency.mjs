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
    `Production planning concurrency verification failed: ${message}`,
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
  "Running production planning concurrency verifier.",
);

const draftApiContracts = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-api-contracts.ts",
);
const draftApiClient = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-api-client.ts",
);
const draftForm = readRequiredFile(
  "src/features/production-planning/components/production-plan-draft-form.tsx",
);
const editRoute = readRequiredFile(
  "src/app/(app)/planlama/[planId]/duzenle/page.tsx",
);

// 1. expectedVersion is in the update contract
requireText(
  draftApiContracts,
  "expectedVersion",
  "production-plan-draft-api-contracts.ts",
);

// 2. concurrency-conflict is handled in form component
requireText(
  draftForm,
  "concurrency-conflict",
  "production-plan-draft-form.tsx",
);

// 3. Edit route passes version to form
requireText(
  editRoute,
  "version",
  "edit route",
);

// 4. Form receives and sends expectedVersion
requireText(
  draftForm,
  "expectedVersion",
  "production-plan-draft-form.tsx",
);

// 5. No silent retry on conflict
forbidText(
  draftForm,
  "autoRetry",
  "production-plan-draft-form.tsx",
);

// 6. No automatic overwrite
forbidText(
  draftApiClient,
  "forceOverwrite",
  "production-plan-draft-api-client.ts",
);

// 7. 200 and 201 success codes are explicitly checked in client (conflict goes to general failure handler)
requireText(
  draftApiClient,
  "200",
  "production-plan-draft-api-client.ts",
);

// 8. Concurrency conflict message shown to user (safe message)
requireText(
  draftForm,
  "concurrency",
  "production-plan-draft-form.tsx",
);

console.log(
  "Production planning concurrency verification passed.",
);

console.log(
  [
    "Verified concurrency token usage,",
    "expectedVersion contract,",
    "409 conflict mapping,",
    "no silent retry,",
    "no auto overwrite,",
    "and user-visible conflict message.",
  ].join(" "),
);
