#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION = "20260908120000_AddAiTaskEmailDeliveryAdapterFoundation";
const DASHBOARD_PATHS = [
  "apps/web/src/app/(app)/dashboard",
  "apps/web/src/components/dashboard",
  "apps/web/src/features/dashboard",
  "apps/web/src/app/command-center.css",
];

function fail(message) {
  console.error(`EMAIL-DELIVERY-ADAPTER-1 verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

const migrationPath = resolve(ROOT, `packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION}.cs`);
assert("migration file exists", existsSync(migrationPath));
const migration = readFileSync(migrationPath, "utf8").replaceAll("\r\n", "\n");
assert("email channel config table", migration.includes("ai_task_email_channel_configurations"));
assert("RLS FORCE", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("email permissions", migration.includes("ai-task.delivery.email.send"));
assert("no raw smtp password in migration", !migration.includes("SMTP__PASSWORD"));

const adapterPath = resolve(ROOT, "packages/NevaxisFactOS.Persistence/AiTasks/Delivery/Email/EmailDeliveryAdapter.cs");
const adapter = readFileSync(adapterPath, "utf8");
assert("canonical channel email", adapter.includes("AiTaskDeliveryChannels.Email"));
assert("smtp provider", readFileSync(resolve(ROOT, "packages/NevaxisFactOS.Persistence/AiTasks/Delivery/Email/EmailDeliveryConstants.cs"), "utf8").includes('Smtp = "smtp"'));
assert("secure link reuse", adapter.includes("secureTaskUrl"));
assert("no duplicate secure token system", !adapter.includes("EmailAccessToken"));

const transportPath = resolve(ROOT, "packages/NevaxisFactOS.Persistence/AiTasks/Delivery/Email/SmtpEmailTransport.cs");
const transport = readFileSync(transportPath, "utf8");
assert("mailkit transport", transport.includes("MailKit") || transport.includes("SmtpClient"));
assert("certificate validation enabled", transport.includes("CheckCertificateRevocation"));

const apiPath = resolve(ROOT, "apps/api/AiTasks/AiTaskDeliveryEndpointRouteBuilderExtensions.cs");
const api = readFileSync(apiPath, "utf8");
assert("email adapter registered", api.includes("EmailDeliveryAdapter"));
assert("env override pattern", api.includes("NEVAXIS_FACTOS__AI_TASK_DELIVERY__EMAIL__"));

const uiPath = resolve(ROOT, "apps/web/src/features/ai-task-center/components/ai-task-delivery-section-client.tsx");
const ui = readFileSync(uiPath, "utf8");
assert("email send action", ui.includes("E-posta ile gönder"));
assert("not configured message", ui.includes("E-posta kanalı henüz yapılandırılmadı"));
assert("recipient masking", ui.includes("email-recipient-masked"));

const labelsPath = resolve(ROOT, "apps/web/src/features/ai-task-center/presentation/ai-task-delivery-labels.ts");
const labels = readFileSync(labelsPath, "utf8");
assert("turkish email readiness labels", labels.includes("Yapılandırılmadı") && labels.includes("Kimlik bilgileri geçersiz"));
assert("email provider status labels", labels.includes("Sunucu tarafından kabul edildi"));

const testsPath = resolve(ROOT, "tests/NevaxisFactOS.Persistence.Tests/AiTaskEmailDeliveryAdapterTests.cs");
assert("email tests file exists", existsSync(testsPath));
const testRun = spawnSync(
  "dotnet",
  [
    "test",
    resolve(ROOT, "tests/NevaxisFactOS.Persistence.Tests/NevaxisFactOS.Persistence.Tests.csproj"),
    "--filter",
    "FullyQualifiedName~Email",
    "--no-restore",
    "-v",
    "q",
  ],
  { cwd: ROOT, encoding: "utf8" },
);
const testOutput = `${testRun.stdout}${testRun.stderr}`;
const match = testOutput.match(/Toplam:\s*(\d+)/) ?? testOutput.match(/Total:\s*(\d+)/);
const testCount = match ? Number(match[1]) : 0;
assert("email test count >= 30", testCount >= 30, `count=${testCount}`);

const whatsappRun = spawnSync(
  "dotnet",
  [
    "test",
    resolve(ROOT, "tests/NevaxisFactOS.Persistence.Tests/NevaxisFactOS.Persistence.Tests.csproj"),
    "--filter",
    "FullyQualifiedName~WhatsApp",
    "--no-restore",
    "-v",
    "q",
  ],
  { cwd: ROOT, encoding: "utf8" },
);
const whatsappOutput = `${whatsappRun.stdout}${whatsappRun.stderr}`;
const whatsappMatch = whatsappOutput.match(/Toplam:\s*(\d+)/) ?? whatsappOutput.match(/Total:\s*(\d+)/);
const whatsappCount = whatsappMatch ? Number(whatsappMatch[1]) : 0;
assert("whatsapp regression count = 26", whatsappCount === 26, `count=${whatsappCount}`);

assert(
  "dashboard protected diff",
  DASHBOARD_PATHS.every((p) => {
    const status = spawnSync("git", ["diff", "--name-only", "--", p], { cwd: ROOT, encoding: "utf8" });
    return !status.stdout.trim();
  }),
);

console.log("EMAIL-DELIVERY-ADAPTER-1 contract verification PASS");
