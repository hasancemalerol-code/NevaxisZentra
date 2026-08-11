import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Navigation serialization verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  return readFileSync(resolve(projectRoot, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running navigation serialization verifier.");

const primaryNavigation = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const navigationTypes = readRequired("apps/web/src/lib/navigation/navigation-types.ts");
const navigationVisibility = readRequired("apps/web/src/lib/navigation/navigation-visibility.ts");
const iconRegistry = readRequired("apps/web/src/lib/navigation/navigation-icon-registry.tsx");
const sidebar = readRequired("apps/web/src/components/app-shell/sidebar.tsx");
const layout = readRequired("apps/web/src/app/(app)/layout.tsx");

forbidText(primaryNavigation, "lucide-react", "primary navigation must be server-safe");
forbidText(primaryNavigation, " icon:", "primary navigation must use iconKey only");
requireText(primaryNavigation, "iconKey:", "primary navigation iconKey");
requireText(navigationTypes, "iconKey: NavigationIconKey", "navigation DTO type");
requireText(navigationVisibility, "resolveMoldNavigationVisibility", "server-side permission filtering");
requireText(navigationVisibility, "iconKey: item.iconKey", "serializable navigation mapping");
forbidText(navigationVisibility, "lucide-react", "navigation visibility must stay server-safe");
requireText(iconRegistry, "navigationIconRegistry", "client icon registry");
requireText(iconRegistry, "molds: Boxes", "Mold navigation icon mapping");
requireText(iconRegistry, "resolveNavigationIcon", "icon resolver");
requireText(sidebar, "resolveNavigationIcon(item.iconKey)", "sidebar icon resolution");
forbidText(sidebar, "const Icon = item.icon", "sidebar must not read React icon components");
requireText(layout, "resolvePermissionAwareNavigation", "server navigation resolution");
forbidText(layout, "\"use client\"", "ApplicationLayout must remain a Server Component");

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

requireText(primaryNavigation, 'href: "/kaliplar"', "Mold route");
requireText(primaryNavigation, 'iconKey: "molds"', "Mold icon key");
requireText(primaryNavigation, 'permissionKey: "mold.read"', "Mold permission key");

console.log("Navigation serialization verification passed.");
