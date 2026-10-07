#!/usr/bin/env node
// Release gate: every permission key the web app asks for (denyUnlessGranted / hasGrantedPermission / permissionKey)
// must exist in the target database's permission_definitions, and every table the web/API need must be readable
// by the runtime role. A key that no definition row backs can never be granted to any role, so the page that
// requires it fails for every customer ("Authorization denied" on a fully privileged owner).
//
// Usage: node scripts/release/verify-permission-catalog.mjs <psql.exe> <host> <port> <database> <user>
//        (password from PGPASSWORD; never printed)
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const [psql, host, port, database, user] = process.argv.slice(2);
if (!psql || !host || !port || !database || !user) {
  console.error("usage: verify-permission-catalog.mjs <psql> <host> <port> <database> <user>");
  process.exit(2);
}

const repositoryRoot = path.resolve(path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1")), "..", "..");

function walk(directory, files = []) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      if (entry.name !== "node_modules" && entry.name !== ".next") walk(full, files);
    } else if (/\.(ts|tsx)$/.test(entry.name)) files.push(full);
  }
  return files;
}

const used = new Map();
const add = (key, file) => {
  if (!/^[a-z0-9-]+(\.[a-z0-9-]+)+$/.test(key)) return;
  if (!used.has(key)) used.set(key, new Set());
  used.get(key).add(path.relative(repositoryRoot, file));
};
for (const file of walk(path.join(repositoryRoot, "apps", "web", "src"))) {
  const source = fs.readFileSync(file, "utf8");
  for (const match of source.matchAll(/(?:denyUnlessGranted|forbidUnlessGranted|hasGrantedPermission\([^,()]+,|permissionKey:|requiredPermission:)\s*\(?\s*"([^"]+)"/g)) add(match[1], file);
  for (const match of source.matchAll(/denyUnlessAnyGranted\(\s*\[([^\]]*)\]/g)) for (const key of match[1].matchAll(/"([^"]+)"/g)) add(key[1], file);
}

const run = (sql) =>
  execFileSync(psql, ["-w", "-h", host, "-p", port, "-U", user, "-d", database, "-tA", "-f", "-"], {
    env: { ...process.env, PGCONNECT_TIMEOUT: "10" },
    encoding: "utf8",
    input: sql,
    maxBuffer: 64 * 1024 * 1024,
  })
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

const defined = new Set(run("select key from permission_definitions where status = 'Active';"));
const missing = [...used.keys()].filter((key) => !defined.has(key)).sort();

console.log(`permission keys referenced by the web app: ${used.size}; active definitions in database: ${defined.size}`);
if (missing.length > 0) {
  console.error("MISSING permission definitions:");
  for (const key of missing) console.error(`  ${key}  <- ${[...used.get(key)].slice(0, 2).join(", ")}`);
  process.exit(1);
}

// Every table the EF model maps must exist and be selectable by the connecting (runtime) role.
const snapshot = fs.readFileSync(
  path.join(repositoryRoot, "packages", "NevaxisZentra.Persistence.Migrations", "Migrations", "NevaxisFactOSDbContextModelSnapshot.cs"),
  "utf8",
);
const modelTables = [...new Set([...snapshot.matchAll(/ToTable\("([a-z0-9_]+)"/g)].map((match) => match[1]))].sort();
const tableProbe = modelTables
  .map((table) => `select '${table}', coalesce(has_table_privilege(current_user, to_regclass('public.${table}'), 'SELECT'), false)`)
  .join(" union all ");
// identity_subjects: migration 20260930100000_HardenIdentitySubjectsRuntimeAccess deliberately removes direct runtime
// access (the API resolves subjects through security-definer functions).
const DELIBERATELY_UNREADABLE = new Set(["identity_subjects"]);
const unreadable = run(tableProbe)
  .map((line) => line.split("|"))
  .filter(([table, readable]) => readable !== "t" && !DELIBERATELY_UNREADABLE.has(table))
  .map(([table]) => table);
console.log(`model tables: ${modelTables.length}; not readable by ${user}: ${unreadable.length}`);
if (unreadable.length > 0) {
  console.error("Tables missing or without SELECT for the runtime role:");
  for (const table of unreadable) console.error(`  ${table}`);
  process.exit(1);
}

console.log("PERMISSION_CATALOG_VERIFY: PASS");
