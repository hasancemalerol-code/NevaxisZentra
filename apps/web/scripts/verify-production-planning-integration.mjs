import { spawnSync } from "node:child_process";
import {
  existsSync,
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

const verificationSteps = [
  {
    id: "detail",
    label: "Detail and fallback integration",
    script:
      "verify-production-planning-detail.mjs",
  },
  {
    id: "navigation",
    label: "List and detail navigation",
    script:
      "verify-production-planning-navigation.mjs",
  },
  {
    id: "states",
    label: "Empty and not-found states",
    script:
      "verify-production-planning-states.mjs",
  },
  {
    id: "filter-url",
    label: "Filter URL safety",
    script:
      "verify-production-planning-filter-url.mjs",
  },
];

function fail(message) {
  console.error(
    `Production planning integration verification failed: ${message}`,
  );

  process.exit(1);
}

function runVerificationStep(step) {
  const scriptPath = resolve(
    scriptsDirectory,
    step.script,
  );

  if (!existsSync(scriptPath)) {
    fail(
      `Required verifier is missing: ${step.script}`,
    );
  }

  console.log("");
  console.log(
    `[${step.id}] ${step.label}`,
  );

  const result = spawnSync(
    process.execPath,
    [scriptPath],
    {
      cwd: resolve(
        scriptsDirectory,
        "../../..",
      ),
      encoding: "utf8",
      stdio: "pipe",
    },
  );

  if (result.stdout) {
    process.stdout.write(result.stdout);
  }

  if (result.stderr) {
    process.stderr.write(result.stderr);
  }

  if (result.error) {
    fail(
      `${step.script} could not be started: ${result.error.message}`,
    );
  }

  if (result.status !== 0) {
    fail(
      `${step.script} exited with status ${result.status ?? "unknown"}.`,
    );
  }
}

console.log(
  "Starting production planning integration verification.",
);

for (const step of verificationSteps) {
  runVerificationStep(step);
}

console.log("");
console.log(
  "Production planning integration verification passed.",
);

console.log(
  [
    "Verified detail composition,",
    "centralized fallback labels,",
    "canonical navigation,",
    "empty and not-found states,",
    "and safe URL filter handling.",
  ].join(" "),
);