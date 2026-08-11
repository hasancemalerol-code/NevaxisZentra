import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Navigation icon-registry verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  return readFileSync(resolve(projectRoot, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

console.log("Running navigation icon-registry verifier.");

const navigationTypes = readRequired("apps/web/src/lib/navigation/navigation-types.ts");
const iconRegistry = readRequired("apps/web/src/lib/navigation/navigation-icon-registry.tsx");
const primaryNavigation = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const sidebar = readRequired("apps/web/src/components/app-shell/sidebar.tsx");

requireText(navigationTypes, '"molds"', "navigation icon key type");
requireText(iconRegistry, "molds: Boxes", "molds icon mapping");
requireText(iconRegistry, "resolveNavigationIcon", "icon resolver");
requireText(primaryNavigation, 'iconKey: "molds"', "Kalıplar icon key");
requireText(sidebar, "resolveNavigationIcon(item.iconKey)", "sidebar resolves icon keys");

const iconKeyMatches = primaryNavigation.match(/iconKey: "[^"]+"/g) ?? [];
for (const iconKeyLine of iconKeyMatches) {
  const iconKey = iconKeyLine.match(/iconKey: "([^"]+)"/)?.[1];
  if (!iconKey) {
    fail(`Unable to parse icon key from ${iconKeyLine}.`);
  }

  if (!iconRegistry.includes(`${iconKey}:`) && !iconRegistry.includes(`"${iconKey}":`)) {
    fail(`iconKey "${iconKey}" is missing from navigationIconRegistry.`);
  }
}

console.log("Navigation icon-registry verification passed.");
