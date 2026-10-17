// Regression for the supported customer-import path: area -> lines -> stations -> resources (+ materials) committed through
// /api/bulk-data-onboarding with a signed-in session (the same path the shipped web uses), then verified in the database.
//
// Guards the defect where child entities failed silently because their parent was not persisted before the next entity
// type was committed. Fails when counts, parent/child links, or the truthfulness of the job result are wrong.
//
// Required environment (never printed): VERIFY_WEB, VERIFY_FACILITY, VERIFY_ORG, VERIFY_EMAIL, VERIFY_PASSWORD, VERIFY_MFA_SECRET,
// VERIFY_PSQL, VERIFY_DB_PORT, VERIFY_DB_USER, VERIFY_DB_NAME, VERIFY_DB_PASSWORD. Optional: VERIFY_TEMPLATES (defaults to release/customer-import-templates).
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const env = (name) => {
  const value = process.env[name];
  if (!value) throw new Error(`${name} is required`);
  return value;
};
const WEB = env("VERIFY_WEB");
const FACILITY = env("VERIFY_FACILITY");
const here = path.dirname(fileURLToPath(import.meta.url));
const TEMPLATES = process.env.VERIFY_TEMPLATES ?? path.resolve(here, "..", "..", "release", "customer-import-templates");

function totp(secret) {
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  let bits = "";
  for (const c of secret.replace(/=+$/, "").toUpperCase()) bits += alphabet.indexOf(c).toString(2).padStart(5, "0");
  const bytes = [];
  for (let i = 0; i + 8 <= bits.length; i += 8) bytes.push(parseInt(bits.slice(i, i + 8), 2));
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 1000 / 30)));
  const h = crypto.createHmac("sha1", Buffer.from(bytes)).update(counter).digest();
  const o = h[h.length - 1] & 0x0f;
  return String((((h[o] & 0x7f) << 24) | (h[o + 1] << 16) | (h[o + 2] << 8) | h[o + 3]) % 1000000).padStart(6, "0");
}

const jar = new Map();
const absorb = (res) => {
  for (const sc of res.headers.getSetCookie()) {
    const [pair] = sc.split(";");
    const i = pair.indexOf("=");
    jar.set(pair.slice(0, i).trim(), pair.slice(i + 1));
  }
};
const cookie = () => [...jar].map(([k, v]) => `${k}=${v}`).join("; ");
const csrf = () => jar.get([...jar.keys()].find((k) => /csrf/i.test(k)) ?? "");

async function identity(route, body, withCsrf) {
  const headers = { "Content-Type": "application/json", Cookie: cookie() };
  if (withCsrf) headers["X-NevaxisZentra-Csrf"] = csrf();
  const res = await fetch(`${WEB}${route}`, { method: "POST", headers, body: JSON.stringify(body) });
  absorb(res);
  if (!res.ok) throw new Error(`${route} failed (${res.status})`);
}

async function api(method, route, body, form) {
  const headers = { Cookie: cookie(), "X-NevaxisZentra-Csrf": csrf() };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const res = await fetch(`${WEB}${route}${route.includes("?") ? "&" : "?"}facilityId=${FACILITY}`, {
    method,
    headers,
    body: form ?? (body === undefined ? undefined : JSON.stringify(body)),
  });
  const text = await res.text();
  if (!res.ok) throw new Error(`${method} ${route} -> ${res.status} ${text.slice(0, 160)}`);
  return JSON.parse(text);
}

function psql(sql) {
  const result = spawnSync(env("VERIFY_PSQL"), ["-h", "127.0.0.1", "-p", env("VERIFY_DB_PORT"), "-U", env("VERIFY_DB_USER"), "-d", env("VERIFY_DB_NAME"), "-At", "-F", "|", "-c", sql], {
    env: { ...process.env, PGPASSWORD: env("VERIFY_DB_PASSWORD") },
    encoding: "utf8",
  });
  if (result.status !== 0) throw new Error(`psql failed: ${result.stderr.slice(0, 200)}`);
  return result.stdout.trim().split(/\r?\n/).filter(Boolean).map((line) => line.split("|"));
}

const csv = (name) => {
  const [header, ...rows] = fs.readFileSync(path.join(TEMPLATES, name), "utf8").trim().split(/\r?\n/);
  const keys = header.split(",");
  return rows.map((row) => Object.fromEntries(row.split(",").map((value, index) => [keys[index], value])));
};

const checks = [];
const check = (name, ok, detail = "") => {
  checks.push({ name, ok });
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}${detail ? ` (${detail})` : ""}`);
};

await identity("/api/identity/sign-in", { organizationContext: env("VERIFY_ORG"), email: env("VERIFY_EMAIL"), password: env("VERIFY_PASSWORD") });
await identity("/api/identity/mfa/verify", { totpCode: totp(env("VERIFY_MFA_SECRET")) }, true);

const areas = csv("areas.csv");
const lines = csv("lines.csv");
const stations = csv("stations.csv");
const resources = csv("resources.csv");
const materials = csv("materials.csv");
const expectedRows = areas.length + lines.length + stations.length + resources.length + materials.length;

const job = await api("POST", "/api/bulk-data-onboarding/jobs", {
  name: "Regresyon: hiyerarşi aktarımı",
  sourceProviderKind: "csv-file",
  sourceSystem: "CUSTOMER-FILE-IMPORT",
  idempotencyKey: `bulk-regression-${Date.now()}`,
  mappingProfileId: null,
});
const form = new FormData();
for (const file of ["areas.csv", "lines.csv", "stations.csv", "resources.csv", "materials.csv"]) {
  form.append("files", new Blob([fs.readFileSync(path.join(TEMPLATES, file))], { type: "text/csv" }), file);
}
await api("POST", `/api/bulk-data-onboarding/jobs/${job.id}/files`, undefined, form);
for (const step of ["discover", "mapping/auto", "stage-validate", "dry-run"]) {
  await api("POST", `/api/bulk-data-onboarding/jobs/${job.id}/${step}`);
}
const committed = await api("POST", `/api/bulk-data-onboarding/jobs/${job.id}/commit`);
const workspace = await api("GET", `/api/bulk-data-onboarding/jobs/${job.id}`);

check("every source row is accounted for (imported + updated + failed = source rows)", committed.importedRows + committed.updatedRows + committed.failedRows === expectedRows,
  `${committed.importedRows}+${committed.updatedRows}+${committed.failedRows} of ${expectedRows}`);
check("validated rows never exceed source rows (re-runs report existing rows as updated, not as new)", workspace.job.validRows <= expectedRows, String(workspace.job.validRows));
check("no failed rows", committed.failedRows === 0, String(committed.failedRows));
check("job status is completed (never completed while rows failed)", committed.status === "completed", committed.status);

const where = `facility_id='${FACILITY}'`;
const count = (table) => Number(psql(`select count(*) from ${table} where ${where}`)[0][0]);
check("areas persisted", count("mixed_production_areas") >= areas.length);
check("lines persisted", count("mixed_production_lines") >= lines.length, String(count("mixed_production_lines")));
check("stations persisted", count("mixed_production_stations") >= stations.length, String(count("mixed_production_stations")));
check("resources persisted", count("mixed_production_resources") >= resources.length, String(count("mixed_production_resources")));
check("materials persisted", count("materials") >= materials.length);

const lineParent = new Map(psql(`select l.code, a.code from mixed_production_lines l join mixed_production_areas a on a.id=l.area_id where l.${where}`));
check("every line is linked to its area", lines.every((l) => lineParent.get(l.line_code) === l.area_code));
const stationParent = new Map(psql(`select s.code, l.code from mixed_production_stations s join mixed_production_lines l on l.id=s.line_id where s.${where}`));
check("every station is linked to its line", stations.every((s) => stationParent.get(s.station_code) === s.line_code));
const resourceParent = new Map(psql(`select r.code, s.code from mixed_production_resources r join mixed_production_stations s on s.id=r.station_id where r.${where}`));
check("every resource is linked to its station", resources.every((r) => resourceParent.get(r.resource_code) === r.station_code));

const failed = checks.filter((c) => !c.ok);
console.log(failed.length === 0 ? "\nBulk onboarding regression PASSED." : `\nBulk onboarding regression FAILED (${failed.length}).`);
process.exit(failed.length === 0 ? 0 : 1);
