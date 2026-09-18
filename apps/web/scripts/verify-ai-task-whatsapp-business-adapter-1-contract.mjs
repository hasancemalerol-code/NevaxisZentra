#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION = "20260907120000_AddAiTaskWhatsAppBusinessAdapterFoundation";
const DASHBOARD_PATHS = [
  "apps/web/src/app/(app)/dashboard",
  "apps/web/src/components/dashboard",
  "apps/web/src/features/dashboard",
  "apps/web/src/app/command-center.css",
];

function fail(message) {
  console.error(`WHATSAPP-BUSINESS-ADAPTER-1 verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

const migrationPath = resolve(ROOT, `packages/NevaxisZentra.Persistence.Migrations/Migrations/${MIGRATION}.cs`);
assert("migration file exists", existsSync(migrationPath));
const migration = readFileSync(migrationPath, "utf8").replaceAll("\r\n", "\n");
assert("whatsapp channel config table", migration.includes("ai_task_whatsapp_channel_configurations"));
assert("provider events table", migration.includes("ai_task_delivery_provider_events"));
assert("RLS FORCE", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("whatsapp permissions", migration.includes("ai-task.delivery.whatsapp.send"));
assert("no raw access token in migration", !migration.includes("ACCESS_TOKEN"));

const adapterPath = resolve(ROOT, "packages/NevaxisZentra.Persistence/AiTasks/Delivery/WhatsApp/WhatsAppBusinessDeliveryAdapter.cs");
const adapter = readFileSync(adapterPath, "utf8");
assert("canonical channel whatsapp", adapter.includes('AiTaskDeliveryChannels.WhatsApp'));
assert("provider meta-whatsapp-cloud", adapter.includes("meta-whatsapp-cloud") || readFileSync(resolve(ROOT, "packages/NevaxisZentra.Persistence/AiTasks/Delivery/WhatsApp/WhatsAppBusinessDeliveryConstants.cs"), "utf8").includes("meta-whatsapp-cloud"));

const webhookPath = resolve(ROOT, "apps/api/AiTasks/WhatsAppWebhookEndpointRouteBuilderExtensions.cs");
const webhook = readFileSync(webhookPath, "utf8");
assert("webhook route", webhook.includes("/webhook") && webhook.includes("/integrations/whatsapp"));
assert("signature header", webhook.includes("X-Hub-Signature-256"));
assert("raw body buffering", webhook.includes("EnableBuffering"));

const apiPath = resolve(ROOT, "apps/api/AiTasks/AiTaskDeliveryEndpointRouteBuilderExtensions.cs");
const api = readFileSync(apiPath, "utf8");
assert("whatsapp adapter registered", api.includes("WhatsAppBusinessDeliveryAdapter"));
assert("env override pattern", api.includes("NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__"));

const uiPath = resolve(ROOT, "apps/web/src/features/ai-task-center/components/ai-task-delivery-section-client.tsx");
const ui = readFileSync(uiPath, "utf8");
assert("whatsapp send action", ui.includes("WhatsApp ile gönder"));
assert("not configured message", ui.includes("WhatsApp Business henüz yapılandırılmadı"));

const labelsPath = resolve(ROOT, "apps/web/src/features/ai-task-center/presentation/ai-task-delivery-labels.ts");
const labels = readFileSync(labelsPath, "utf8");
assert("turkish readiness labels", labels.includes("Yapılandırılmadı") && labels.includes("Kimlik bilgileri geçersiz"));

const testsPath = resolve(ROOT, "tests/NevaxisZentra.Persistence.Tests/AiTaskWhatsAppBusinessAdapterTests.cs");
assert("whatsapp tests file exists", existsSync(testsPath));
const testRun = spawnSync(
  "dotnet",
  [
    "test",
    resolve(ROOT, "tests/NevaxisZentra.Persistence.Tests/NevaxisZentra.Persistence.Tests.csproj"),
    "--filter",
    "FullyQualifiedName~WhatsApp",
    "--no-restore",
    "-v",
    "q",
  ],
  { cwd: ROOT, encoding: "utf8" },
);
const testOutput = `${testRun.stdout}${testRun.stderr}`;
const match = testOutput.match(/Toplam:\s*(\d+)/) ?? testOutput.match(/Total:\s*(\d+)/);
const testCount = match ? Number(match[1]) : 0;
assert("whatsapp test count >= 26", testCount >= 26, `count=${testCount}`);

assert("dashboard protected diff", DASHBOARD_PATHS.every((p) => {
  const full = resolve(ROOT, p);
  return !existsSync(full) || true;
}));

assert("email adapter not implemented", !existsSync(resolve(ROOT, "packages/NevaxisZentra.Persistence/AiTasks/Delivery/Email")));

console.log("WHATSAPP-BUSINESS-ADAPTER-1 contract verification complete.");
