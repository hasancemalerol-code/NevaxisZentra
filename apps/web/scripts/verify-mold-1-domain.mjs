import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MOLD-1 domain verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(projectRoot, relativePath);
  if (!existsSync(absolutePath)) fail(`Missing ${relativePath}`);
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

console.log("Running MOLD-1 domain verifier.");

const stateMachine = readRequired("packages/NevaxisFactOS.Persistence/Mold/MoldLifecycleStateMachine.cs");
const normalizer = readRequired("packages/NevaxisFactOS.Persistence/Mold/MoldCodeNormalizer.cs");
const validator = readRequired("packages/NevaxisFactOS.Persistence/Mold/MoldTechnicalValidator.cs");
const repo = readRequired("packages/NevaxisFactOS.Persistence/Mold/MoldRepository.cs");
const labels = readRequired("apps/web/src/features/molds/presentation/mold-labels.ts");

requireText(stateMachine, "MoldLifecycleState.Scrapped", "state machine");
requireText(stateMachine, "CanTransition", "state machine");
requireText(normalizer, "ToUpperInvariant", "code normalizer");
requireText(validator, "Cavity count must be positive", "technical validator");
requireText(validator, "cannot be negative", "technical validator");
requireText(repo, "IdempotencyKey", "repository idempotency");
requireText(repo, "ExpectedVersion", "optimistic concurrency");
requireText(labels, "İşletmeye Ait", "Turkish ownership label");
requireText(labels, "Taslak", "Turkish lifecycle label");

console.log("MOLD-1 domain verification passed.");
