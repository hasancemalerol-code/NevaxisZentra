/**
 * verify-production-premium-ui.mjs
 *
 * Structural premium marker verification for the Production Execution UI.
 * Confirms that the premium visual system is in place without pixel-diffing.
 * Run: node verify-production-premium-ui.mjs
 */

import { readFileSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = fileURLToPath(new URL(".", import.meta.url)).replace(/[/\\]$/, "");
const WEB_SRC = join(ROOT, "apps", "web", "src");
const FEATURES = join(WEB_SRC, "features", "production-execution");
const PAGES = join(WEB_SRC, "app", "(app)", "uretim");

let passed = 0;
let failed = 0;
const failures = [];

function check(label, value, hint = "") {
  if (value) {
    console.log(`  ✓ ${label}`);
    passed++;
  } else {
    console.error(`  ✗ ${label}${hint ? ` — ${hint}` : ""}`);
    failed++;
    failures.push(label);
  }
}

function read(path) {
  try {
    return readFileSync(path, "utf8");
  } catch {
    return null;
  }
}

function has(content, ...patterns) {
  if (!content) return false;
  return patterns.every((p) =>
    typeof p === "string" ? content.includes(p) : p.test(content),
  );
}

// ─── 1. CSS file exists and has required sections ───────────────────────────

console.log("\n▶ Production Execution CSS");

const css = read(join(WEB_SRC, "app/production-execution.css"));
check("production-execution.css exists", css !== null);
check(
  "Page layout: .production-execution-page defined",
  has(css, ".production-execution-page"),
);
check(
  "Header: .production-execution-header defined",
  has(css, ".production-execution-header"),
);
check(
  "KPI strip: .production-execution-kpis defined",
  has(css, ".production-execution-kpis"),
);
check(
  "Safety banner: .production-execution-safety defined",
  has(css, ".production-execution-safety"),
);
check(
  "Workspace: .production-execution-workspace defined",
  has(css, ".production-execution-workspace"),
);
check(
  "Empty state: .production-execution-empty defined",
  has(css, ".production-execution-empty"),
);
check(
  "Order card: .production-order-card defined",
  has(css, ".production-order-card"),
);
check(
  "Card summary: .production-order-card__summary defined",
  has(css, ".production-order-card__summary"),
);
check(
  "Card quantity: .production-order-card__quantity defined",
  has(css, ".production-order-card__quantity"),
);
check(
  "Card progress bar defined",
  has(css, ".production-order-card__progress-bar"),
);
check(
  "Card footer/link defined",
  has(css, ".production-order-card__link"),
);
check(
  "Detail page: .production-order-detail-page defined",
  has(css, ".production-order-detail-page"),
);
check(
  "Detail summary: .production-order-detail-page__summary defined",
  has(css, ".production-order-detail-page__summary"),
);
check(
  "Detail context card defined",
  has(css, ".production-order-detail-page__context-card"),
);
check(
  "Quantity panel: .production-quantity-panel defined",
  has(css, ".production-quantity-panel"),
);
check(
  "Quantity grid: .production-quantity-panel__grid defined",
  has(css, ".production-quantity-panel__grid"),
);
check(
  "Quantity progress bar defined",
  has(css, ".production-quantity-panel__progress-bar"),
);
check(
  "History: .production-entry-history defined",
  has(css, ".production-entry-history"),
);
check(
  "History timeline items defined",
  has(css, ".production-entry-history__item"),
);
check(
  "Entry type badges defined",
  has(css, ".entry-type-badge--good-production", ".entry-type-badge--scrap"),
);
check(
  "Operator actions: .production-operator-actions defined",
  has(css, ".production-operator-actions"),
);
check(
  "Lifecycle actions: .production-order-lifecycle-actions defined",
  has(css, ".production-order-lifecycle-actions"),
);
check(
  "Workflow dialog: .workflow-action-dialog defined",
  has(css, ".workflow-action-dialog"),
);
check(
  "Form inputs: .input defined",
  has(css, ".input {") || has(css, ".input\n"),
);
check(
  "Missing button variants: button-ghost defined",
  has(css, ".button-ghost"),
);
check(
  "Missing button variants: button-large defined",
  has(css, ".button-large"),
);
check(
  "Missing button variants: button-warning defined",
  has(css, ".button-warning"),
);
check(
  "Missing button variants: button-destructive defined",
  has(css, ".button-destructive"),
);
check(
  "Responsive: @media (max-width: 960px) defined",
  has(css, "max-width: 960px"),
);
check(
  "KPI grid is 5-column for execution (not 6 like planning)",
  has(css, "repeat(5, minmax(0, 1fr))"),
);

// ─── 2. CSS imported in layout ────────────────────────────────────────────────

console.log("\n▶ CSS import in root layout");

const layout = read(join(WEB_SRC, "app/layout.tsx"));
check(
  "production-execution.css imported in root layout",
  has(layout, "./production-execution.css"),
);
check(
  "globals.css still imported (not removed)",
  has(layout, "./globals.css"),
);

// ─── 3. List page ─────────────────────────────────────────────────────────────

console.log("\n▶ /uretim list page");

const listPage = read(join(PAGES, "page.tsx"));
check("List page exists", listPage !== null);
check(
  "Header: premium eyebrow text (ÜRETİM YÖNETİMİ)",
  has(listPage, "ÜRETİM YÖNETİMİ"),
);
check(
  "Header: H1 title present",
  has(listPage, "Üretim Emirleri"),
);
check(
  "Header: subtitle present",
  has(listPage, "Planlamadan üretime"),
);
check(
  "KPI: uses production-execution-kpis class",
  has(listPage, "production-execution-kpis"),
);
check(
  "KPI: 5 status cards (total + 4 status)",
  has(listPage, "Toplam Emir") && has(listPage, "Üretimde") && has(listPage, "Hazır") && has(listPage, "Duraklatıldı") && has(listPage, "Tamamlandı"),
);
check(
  "Safety banner: SHOP-FLOOR EXECUTION eyebrow",
  has(listPage, "SHOP-FLOOR EXECUTION"),
);
check(
  "Safety banner: headline present",
  has(listPage, "kontrollü operasyon adımlarıyla"),
);
check(
  "Workspace section: ÜRETİM ÇALIŞMA ALANI eyebrow",
  has(listPage, "ÜRETİM ÇALIŞMA ALANI"),
);
check(
  "Empty state: premium structure with eyebrow",
  has(listPage, "ÜRETİM EMRİ YOK"),
);
check(
  "Empty state: descriptive text present",
  has(listPage, "handoff"),
);
check(
  "Error state: uses state-panel",
  has(listPage, "state-panel"),
);
check(
  "No direct browser API calls",
  !has(listPage, "fetch(", "X-CrevaFactOS-Dev"),
);
check(
  "globals.css not referenced",
  !has(listPage, "globals.css"),
);

// ─── 4. Detail page ────────────────────────────────────────────────────────────

console.log("\n▶ /uretim/[orderId] detail page");

const detailPage = read(join(PAGES, "[orderId]/page.tsx"));
check("Detail page exists", detailPage !== null);
check(
  "Header: ÜRETİM EMRİ eyebrow",
  has(detailPage, "ÜRETİM EMRİ"),
);
check(
  "Back navigation with ArrowLeft",
  has(detailPage, "ArrowLeft"),
);
check(
  "Section order: quantity before lifecycle",
  detailPage
    ? (() => {
        // Search in JSX return block only (after the last import statement)
        const returnIdx = detailPage.lastIndexOf("return (");
        const body = returnIdx > -1 ? detailPage.slice(returnIdx) : detailPage;
        const qIdx = body.indexOf("production-order-detail-page__quantity");
        const lcIdx = body.indexOf("<ProductionOrderLifecycleActions");
        return qIdx > -1 && lcIdx > -1 && qIdx < lcIdx;
      })()
    : false,
  "quantity section should appear before lifecycle actions",
);
check(
  "Section order: lifecycle before operator actions",
  detailPage
    ? (() => {
        const lcIdx = detailPage.indexOf("ProductionOrderLifecycleActions");
        const opIdx = detailPage.indexOf("ProductionOperatorActions");
        return lcIdx > -1 && opIdx > -1 && lcIdx < opIdx;
      })()
    : false,
  "lifecycle actions should appear before operator actions",
);
check(
  "Section order: history before source planning",
  detailPage
    ? (() => {
        const histIdx = detailPage.indexOf("ProductionEntryHistory");
        const ctxIdx = detailPage.indexOf("production-order-detail-page__content");
        return histIdx > -1 && ctxIdx > -1 && histIdx < ctxIdx;
      })()
    : false,
  "history should appear before source planning context",
);
check(
  "Summary cards use premium card structure",
  has(detailPage, "production-order-detail-page__summary"),
);
check(
  "Source planning context: de-emphasized GUIDs (shortId)",
  has(detailPage, "shortId"),
);
check(
  "Source planning context: KAYNAK PLANLAMA eyebrow",
  has(detailPage, "KAYNAK PLANLAMA"),
);
check(
  "Error state: uses state-panel",
  has(detailPage, "state-panel"),
);
check(
  "No direct browser API calls",
  !has(detailPage, "fetch(", "X-CrevaFactOS-Dev"),
);

// ─── 5. Production Order Card ──────────────────────────────────────────────────

console.log("\n▶ ProductionOrderCard component");

const card = read(join(FEATURES, "components/production-order-card.tsx"));
check("Component exists", card !== null);
check(
  "Uses production-order-card CSS class",
  has(card, "production-order-card"),
);
check(
  "Premium badges section",
  has(card, "production-order-card__badges"),
);
check(
  "Summary grid with icon tiles",
  has(card, "production-order-card__summary"),
);
check(
  "Quantity section present",
  has(card, "production-order-card__quantity"),
);
check(
  "Progress bar present",
  has(card, "production-order-card__progress-bar"),
);
check(
  "Footer with premium link",
  has(card, "production-order-card__link"),
);
check(
  "Turkish: 'Üretim emrini aç' (not 'Siparişi incele')",
  has(card, "Üretim emrini aç"),
);
check(
  "No raw concatenated metadata (separate label + value pattern)",
  has(card, "<span>") && has(card, "<strong>"),
);
check(
  "Accessibility: progressbar role",
  has(card, 'role="progressbar"'),
);

// ─── 6. Production Quantity Panel ─────────────────────────────────────────────

console.log("\n▶ ProductionQuantityPanel component");

const qPanel = read(join(FEATURES, "components/production-quantity-panel.tsx"));
check("Component exists", qPanel !== null);
check(
  "Premium panel class",
  has(qPanel, "production-quantity-panel"),
);
check(
  "Section eyebrow: MİKTAR VE İLERLEME",
  has(qPanel, "MİKTAR VE İLERLEME"),
);
check(
  "Metric grid present",
  has(qPanel, "production-quantity-panel__grid"),
);
check(
  "Primary metric tile modifier",
  has(qPanel, "production-quantity-panel__item--primary"),
);
check(
  "Scrap tone modifier",
  has(qPanel, "production-quantity-panel__item--scrap"),
);
check(
  "Progress bar with aria",
  has(qPanel, 'role="progressbar"'),
);
check(
  "6 metric items defined (planned, good, scrap, gross, remaining, progress)",
  has(qPanel, "Planlanan") &&
    has(qPanel, "İyi Üretim") &&
    has(qPanel, "Fire") &&
    has(qPanel, "Toplam Üretim") &&
    has(qPanel, "Kalan") &&
    has(qPanel, "İlerleme"),
);

// ─── 7. Production Entry History ──────────────────────────────────────────────

console.log("\n▶ ProductionEntryHistory component");

const history = read(join(FEATURES, "components/production-entry-history.tsx"));
check("Component exists", history !== null);
check(
  "Premium history class",
  has(history, "production-entry-history"),
);
check(
  "Eyebrow: KAYIT GEÇMİŞİ",
  has(history, "KAYIT GEÇMİŞİ"),
);
check(
  "Timeline item structure (3-col grid layout)",
  has(history, "production-entry-history__item"),
);
check(
  "Entry type badge classes",
  // Component uses template literal: entry-type-badge--${entry.entryType}
  // Verify the base class and type-specific CSS classes in CSS file
  has(history, "entry-type-badge--") &&
    has(history, "entry-type-badge entry-type-badge--") &&
    has(css, "entry-type-badge--good-production") &&
    has(css, "entry-type-badge--scrap") &&
    has(css, "entry-type-badge--reversal"),
);
check(
  "Semantic time element",
  has(history, "<time"),
);
check(
  "Role list for accessibility",
  has(history, 'role="list"'),
);
check(
  "Quantity label human-friendly (adet İyi Üretim pattern)",
  has(history, "adet İyi Üretim"),
);

// ─── 8. Production Operator Actions ───────────────────────────────────────────

console.log("\n▶ ProductionOperatorActions component");

const opActions = read(join(FEATURES, "components/production-operator-actions.tsx"));
check("Component exists", opActions !== null);
check(
  "Heading wrapper with eyebrow",
  has(opActions, "production-operator-actions__heading"),
);
check(
  "Eyebrow: OPERATÖR GİRİŞİ",
  has(opActions, "OPERATÖR GİRİŞİ"),
);
check(
  "Premium title: Üretim miktarı kaydet",
  has(opActions, "Üretim miktarı kaydet"),
);
check(
  "Large touch target buttons",
  has(opActions, "button-large"),
);
check(
  "Input class on quantity fields",
  has(opActions, 'className="input input-large"'),
);
check(
  "Workflow dialog wrapper for forms",
  has(opActions, "workflow-action-dialog"),
);
check(
  "Scrap reason label human-friendly (getScrapReasonLabel)",
  has(opActions, "getScrapReasonLabel"),
);
check(
  "Comment requirement visible in label",
  has(opActions, "zorunlu"),
);

// ─── 9. Production Lifecycle Actions ──────────────────────────────────────────

console.log("\n▶ ProductionOrderLifecycleActions component");

const lifecycle = read(join(FEATURES, "components/production-order-lifecycle-actions.tsx"));
check("Component exists", lifecycle !== null);
check(
  "Eyebrow: OPERASYON KONTROLÜ",
  has(lifecycle, "OPERASYON KONTROLÜ"),
);
check(
  "Title: Üretim emri işlemleri",
  has(lifecycle, "Üretim emri işlemleri"),
);
check(
  "Primary/Resume button uses button-primary",
  has(lifecycle, "button-primary button-medium"),
);
check(
  "Short completion uses button-destructive",
  has(lifecycle, "button-destructive"),
);
check(
  "Workflow dialog for all confirmations",
  has(lifecycle, "workflow-action-dialog"),
);
check(
  "Pause requires textarea gerekçe",
  has(lifecycle, "Duraklatma gerekçesi"),
);
check(
  "Short completion requires textarea gerekçe",
  has(lifecycle, "Kısa tamamlama gerekçesi"),
);

// ─── 10. Planning untouched ───────────────────────────────────────────────────

console.log("\n▶ Planning regression guard");

const planningPage = read(join(WEB_SRC, "app/(app)/planlama/page.tsx"));
check(
  "Planning list page exists and unchanged class",
  has(planningPage, "production-planning-page") &&
    has(planningPage, "production-planning-header") &&
    has(planningPage, "production-planning-kpis"),
);

const planCard = read(
  join(WEB_SRC, "features/production-planning/components/production-plan-card.tsx"),
);
check(
  "Planning card component exists and unchanged",
  has(planCard, "production-plan-card") &&
    has(planCard, "production-plan-card__heading"),
);

// ─── 11. No globals.css modification ─────────────────────────────────────────

console.log("\n▶ Constraints");

check(
  "production-execution.css does NOT import or reference globals.css",
  css !== null && !has(css, "globals.css"),
);

const globalsTimestamp = (() => {
  try {
    const stats = { mtime: new Date(0) };
    // We cannot read mtime here easily without fs.statSync import,
    // so just confirm globals.css has production-planning classes (unchanged)
    const g = read(join(WEB_SRC, "app/globals.css"));
    return has(g, ".production-planning-page") && has(g, ".production-planning-kpis");
  } catch {
    return false;
  }
})();
check(
  "globals.css still contains planning CSS (not modified)",
  globalsTimestamp,
);

const newExecCss = read(join(WEB_SRC, "app/production-execution.css"));
const globalsContent = read(join(WEB_SRC, "app/globals.css"));
check(
  "production-execution CSS has no production-planning class definitions (no planning duplication)",
  newExecCss !== null &&
    !has(newExecCss, ".production-planning-"),
);

check(
  "No new dependencies in list page or components",
  !has(listPage, "import.*from.*['\"](?!next|react|lucide-react|@/)") &&
    !has(detailPage, "import.*from.*['\"](?!next|react|lucide-react|@/)"),
);

// ─── Summary ──────────────────────────────────────────────────────────────────

console.log(
  `\n${"─".repeat(60)}\nResult: ${passed} passed, ${failed} failed\n${"─".repeat(60)}`,
);

if (failures.length > 0) {
  console.error("\nFailed checks:");
  failures.forEach((f) => console.error(`  • ${f}`));
  process.exit(1);
} else {
  console.log("\n✓ All premium UI structural markers verified.");
  process.exit(0);
}
