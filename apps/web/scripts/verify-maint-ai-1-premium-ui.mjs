#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function read(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`Missing ${rel}`);
  return readFileSync(abs, "utf8");
}

function assert(label, ok, detail = "") {
  if (!ok) {
    console.error(`✗ ${label}${detail ? `: ${detail}` : ""}`);
    process.exit(1);
  }
  console.log(`✓ ${label}`);
}

const view = read("apps/web/src/features/maintenance/components/maintenance-condition-view.tsx");
const section = read("apps/web/src/features/maintenance/components/maintenance-ai-assistant-section.tsx");
const labels = read("apps/web/src/features/maintenance/presentation/maintenance-ai-agent-labels.ts");
const css = read("apps/web/src/features/maintenance/components/maintenance-workspace.css");
const page = read("apps/web/src/app/(app)/bakim/durum-zekasi/page.tsx");

assert("primary page identity Kestirici Bakım", view.includes("<h1>Kestirici Bakım</h1>"));
assert("AI Bakım Asistanı section", section.includes("AI Bakım Asistanı"));
assert("no new sidebar item for AI", !read("apps/web/src/components/app-shell/sidebar.tsx").includes("AI Bakım Asistanı"));
assert("durum-zekasi route", page.includes("durum-zekasi") || existsSync(resolve(ROOT, "apps/web/src/app/(app)/bakim/durum-zekasi/page.tsx")));
assert("safety copy present", labels.includes("AI çıktısı karar desteğidir"));
assert("local provider disclaimer", labels.includes("gerçek model doğrulaması değildir"));
assert("Turkish confidence labels", labels.includes("Düşük") && labels.includes("Orta") && labels.includes("Yüksek"));
assert("generate action", section.includes("AI Taslağı Hazırla"));
assert("edit action", section.includes("Taslağı Düzenle"));
assert("approve action", section.includes("Onayla ve Bakım Talebi Oluştur"));
assert("reject action", section.includes("Reddet"));
assert("regenerate action", section.includes("Güncel Verilerle Yeniden Hazırla"));
assert("summary UI", section.includes("draft.validatedOutput.summary") || section.includes("Özet"));
assert("missing information UI", section.includes("missingInformation") || section.includes("Eksik Bilgi"));
assert("evidence UI", section.includes("evidenceReferences") || section.includes("Kanıt"));
assert("stale warning UI", section.includes("stale"));
assert("maintenance-ai-section css", css.includes(".maintenance-ai-section"));
assert("no raw UUID render", !section.includes("{draft.id}") && !section.includes("draftId}"));
assert("no raw enum render", section.includes("getMaintenanceAiDraftStatusLabel"));
assert("no AI Task Center page", !existsSync(resolve(ROOT, "apps/web/src/app/(app)/ai-task-center/page.tsx")));

console.log("MAINT-AI-1 premium UI verifier PASS");
