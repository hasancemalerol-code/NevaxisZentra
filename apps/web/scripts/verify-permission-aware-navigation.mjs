import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Permission-aware navigation verification failed: ${message}`);
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

console.log("Running permission-aware navigation verifier.");

const primaryNavigation = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const navigationVisibility = readRequired("apps/web/src/lib/navigation/navigation-visibility.ts");
const layout = readRequired("apps/web/src/app/(app)/layout.tsx");
const moldReader = readRequired("apps/web/src/features/molds/server/mold-reader.ts");
const sidebar = readRequired("apps/web/src/components/app-shell/sidebar.tsx");

requireText(primaryNavigation, 'label: "Kalıplar"', "Kalıplar label");
requireText(primaryNavigation, 'href: "/kaliplar"', "Kalıplar route");
requireText(primaryNavigation, 'permissionKey: "mold.read"', "Kalıplar permission key");
requireText(navigationVisibility, "resolvePermissionAwareNavigation", "navigation resolver");
requireText(navigationVisibility, 'item.permissionKey === "mold.read"', "server-side mold filter");
requireText(navigationVisibility, "resolveMoldNavigationVisibility", "mold visibility probe");
requireText(moldReader, "resolveMoldNavigationVisibility", "mold reader visibility");
requireText(moldReader, "probeMoldReadPermission", "mold read probe");
requireText(layout, "resolvePermissionAwareNavigation", "layout uses permission-aware navigation");
forbidText(sidebar, "permissionKey", "sidebar must not re-filter by permission");
forbidText(navigationVisibility, '"use client"', "navigation visibility stays server-side");

console.log("Permission-aware navigation verification passed.");
