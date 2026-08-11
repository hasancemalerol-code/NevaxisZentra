import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MOLD-1 sidebar verification failed: ${message}`);
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

console.log("Running MOLD-1 sidebar verifier.");

const primaryNavigation = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const navigationVisibility = readRequired("apps/web/src/lib/navigation/navigation-visibility.ts");
const layout = readRequired("apps/web/src/app/(app)/layout.tsx");
const appShellFrame = readRequired("apps/web/src/components/app-shell/app-shell-frame.tsx");
const sidebar = readRequired("apps/web/src/components/app-shell/sidebar.tsx");
const iconRegistry = readRequired("apps/web/src/lib/navigation/navigation-icon-registry.tsx");
const moldRequestContext = readRequired("apps/web/src/features/molds/server/mold-request-context.ts");

requireText(primaryNavigation, 'label: "Kalıplar"', "Kalıplar sidebar label");
requireText(primaryNavigation, 'href: "/kaliplar"', "Kalıplar sidebar route");
requireText(primaryNavigation, 'iconKey: "molds"', "Kalıplar sidebar icon key");
requireText(navigationVisibility, "resolvePermissionAwareNavigation", "permission-aware navigation");
requireText(layout, "navigation={navigation}", "layout passes filtered navigation");
requireText(appShellFrame, "navigation:", "app shell receives navigation prop");
requireText(sidebar, "navigation.map", "sidebar renders navigation groups");
requireText(sidebar, "isNavigationItemActive", "active route styling");
requireText(sidebar, "data-collapsed", "collapsed sidebar support");
requireText(iconRegistry, "molds:", "molds icon registry entry");
requireText(moldRequestContext, "parseProductionPlanRuntimeConfig", "planning runtime fallback for mold nav");
forbidText(sidebar, "permissionKey", "sidebar must not apply permission filtering");
forbidText(primaryNavigation, "lucide-react", "server navigation must stay serializable");

console.log("MOLD-1 sidebar verification passed.");
