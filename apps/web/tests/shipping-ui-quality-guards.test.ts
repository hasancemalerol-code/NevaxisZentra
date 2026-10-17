import assert from "node:assert/strict";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";

// Static guards for the shipping-UI premium bar (RC3 Package 4). They cannot replace the browser sweep, but they make
// the regressions that the sweep found impossible to reintroduce silently: unstyled classes, nested landmarks,
// unnamed tables, raw failure text, failing text contrast and missing state boundaries.
const srcRoot = path.resolve(__dirname, "..", "src");

function walk(directory: string, found: string[] = []): string[] {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(full, found);
    else found.push(full);
  }
  return found;
}

const files = walk(srcRoot);
const tsxFiles = files.filter((file) => file.endsWith(".tsx"));
const cssFiles = files.filter((file) => file.endsWith(".css"));
const rel = (file: string) => path.relative(srcRoot, file).split(path.sep).join("/");
const read = (file: string) => readFileSync(file, "utf8");

// ── 1. every statically written class has a stylesheet rule ───────────────────────────────────────────────────────

// Hook / modifier classes that intentionally carry no rule of their own (selectors target their parent, they mark a
// variant that only changes children, or they are the state-panel marker of the shared primitive).
const HOOK_CLASSES = new Set([
  "account-copy--placeholder",
  "apps-analytics-chart__legend-line--actual",
  "dashboard-panel--digital-workforce",
  "dashboard-panel--market-rates",
  "edge-ops-table--alarms",
  "edge-reliability-table--dead-letter",
  "empty-state",
  "erp-workspace-page--readonly",
  "export-kpis__item--active",
  "factory-showcase--error",
  "factory-showcase-bar",
  "factory-showcase-distribution__ring",
  "maintenance-ai-link-row",
  "maintenance-evaluate-control",
  "shell-popover--search",
  "shell-popover--workspace",
  "status-badge",
]);

test("every statically written className token has a CSS rule (no unstyled screens)", () => {
  const css = cssFiles.map(read).join("\n");
  const defined = new Set([...css.matchAll(/\.(-?[_a-zA-Z][\w-]*)/g)].map((match) => match[1]));
  const missing: string[] = [];

  for (const file of tsxFiles) {
    const used = new Set<string>();
    for (const match of read(file).matchAll(/className=(?:"([^"]+)"|\{`([^`]+)`\}|\{"([^"]+)"\})/g)) {
      const text = (match[1] ?? match[2] ?? match[3]).replace(/\$\{[^}]*\}/g, " ");
      for (const token of text.split(/\s+/)) {
        if (token && /^[a-z][\w-]*$/i.test(token)) used.add(token);
      }
    }

    for (const token of used) {
      // dynamic prefixes (`badge-${tone}` leaves `badge-`) are validated by their concrete values elsewhere
      if (/(--|-|_)$/.test(token) || HOOK_CLASSES.has(token) || defined.has(token)) continue;
      missing.push(`${rel(file)}: ${token}`);
    }
  }

  assert.deepEqual(missing, [], "class names without any CSS rule render as raw browser defaults");
});

test("no utility-framework class names are used (the app ships no utility CSS)", () => {
  const offenders: string[] = [];
  const utility = /className="[^"]*(?:^|\s)(?:p[xytblr]?-\d|m[xytblr]?-\d|gap-\d|flex(?:-col)?|grid-cols-\d|text-(?:xs|sm|base|lg)|font-(?:medium|semibold|bold)|rounded-(?:md|lg|xl)|bg-\[|text-white)(?:\s|")/;
  for (const file of tsxFiles) {
    if (utility.test(read(file))) offenders.push(rel(file));
  }
  assert.deepEqual(offenders, []);
});

// ── 2. landmarks ──────────────────────────────────────────────────────────────────────────────────────────────────

const MAIN_OWNERS = new Set([
  "components/app-shell/app-shell-frame.tsx",
  "features/saas-subscription/components/subscription-recovery-shell.tsx",
  "features/ai-task-center/components/public-secure-task-view.tsx",
  "features/dashboard-showcase/components/factory-showcase-shell.tsx",
  "app/(auth)/layout.tsx",
]);

test("only the shells own the main landmark (pages and views render inside it)", () => {
  const offenders = tsxFiles.filter((file) => /<main[\s>]/.test(read(file)) && !MAIN_OWNERS.has(rel(file))).map(rel);
  assert.deepEqual(offenders, [], "a nested <main> breaks the landmark outline for assistive technology");
});

// ── 3. data tables have accessible names ──────────────────────────────────────────────────────────────────────────

test("every table carries an accessible name", () => {
  const unnamed: string[] = [];
  for (const file of tsxFiles) {
    const text = read(file);
    for (const match of text.matchAll(/<table(?=[\s>])[^>]*>/g)) {
      if (!/aria-label|aria-labelledby/.test(match[0]) && !text.slice(match.index, match.index! + 400).includes("<caption")) {
        unnamed.push(`${rel(file)}: ${match[0].slice(0, 60)}`);
      }
    }
  }
  assert.deepEqual(unnamed, []);
});

// ── 4. no raw failure text reaches a customer ─────────────────────────────────────────────────────────────────────

test("failure messages are rendered through customerMessage, never raw", () => {
  const raw = /(?:>|^[ \t]+)\{(?:result\.message|state\.message|panelState\.message|authorization\.message|actionError|errorMessage|apiError)\}(?=[ \t]*(?:<|$))/m;
  const offenders = tsxFiles.filter((file) => raw.test(read(file))).map(rel);
  assert.deepEqual(offenders, []);
});

test("error boundaries never render exception text", () => {
  for (const boundary of ["app/(app)/error.tsx", "app/global-error.tsx"]) {
    const text = read(path.join(srcRoot, boundary));
    assert.doesNotMatch(text, /\{error\.message\}|error\.stack|\{String\(error\)\}/, boundary);
    assert.match(text, /error\.digest/, `${boundary} shows only the opaque support reference`);
  }
});

test("customer message helper keeps Turkish copy and replaces backend wording", async () => {
  const { customerMessage, GENERIC_UNAVAILABLE_MESSAGE } = await import("../src/lib/presentation/customer-message");
  assert.equal(customerMessage("Oturumunuz sona ermiş olabilir."), "Oturumunuz sona ermiş olabilir.");
  assert.equal(customerMessage("Maintenance record was not found."), GENERIC_UNAVAILABLE_MESSAGE);
  assert.equal(customerMessage("workspace payload is invalid."), GENERIC_UNAVAILABLE_MESSAGE);
  assert.equal(customerMessage("API base URL is not configured."), GENERIC_UNAVAILABLE_MESSAGE);
  assert.equal(customerMessage("Authorization denied."), GENERIC_UNAVAILABLE_MESSAGE);
  assert.equal(customerMessage(undefined, "Bu işlem yapılamadı."), "Bu işlem yapılamadı.");
});

test("opaque identifiers are never shown as names", async () => {
  const { displayLabel } = await import("../src/lib/presentation/display-label");
  assert.equal(displayLabel("bb000001-0005-4000-8000-000000000005", "Fabrika"), "Fabrika");
  assert.equal(displayLabel("Sweep Fabrika", "Fabrika"), "Sweep Fabrika");
  assert.equal(displayLabel(undefined, "Fabrika"), "Fabrika");
});

// ── 5. state boundaries exist ─────────────────────────────────────────────────────────────────────────────────────

test("every route group has not-found, access-denied, error and loading boundaries", () => {
  for (const boundary of [
    "app/not-found.tsx",
    "app/forbidden.tsx",
    "app/global-error.tsx",
    "app/(app)/not-found.tsx",
    "app/(app)/forbidden.tsx",
    "app/(app)/error.tsx",
    "app/(app)/loading.tsx",
  ]) {
    assert.ok(existsSync(path.join(srcRoot, boundary)), `${boundary} must exist`);
  }
});

// ── 6. text contrast ──────────────────────────────────────────────────────────────────────────────────────────────

function luminance(hex: string): number {
  const value = hex.replace("#", "");
  const channels = [0, 2, 4].map((index) => parseInt(value.slice(index, index + 2), 16) / 255);
  const [r, g, b] = channels.map((c) => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4));
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrast(foreground: string, background: string): number {
  const [light, dark] = [luminance(foreground), luminance(background)].sort((a, b) => b - a);
  return (light + 0.05) / (dark + 0.05);
}

test("muted and accent text tokens keep WCAG AA contrast on every shell surface", () => {
  const surfaces = ["#ffffff", "#f8fafc", "#f4f6f9", "#eaf0f6", "#e8edf3", "#e5ecfd", "#f4f7fb"];
  const tokens = readFileSync(path.join(srcRoot, "design-system", "zentra-premium.css"), "utf8");
  for (const token of ["--zx-text-muted", "--zx-text", "--zx-accent-strong", "--zx-critical", "--zx-high", "--zx-low", "--zx-info", "--zx-ai"]) {
    const value = new RegExp(`${token}:\\s*(#[0-9a-fA-F]{6})`).exec(tokens)?.[1];
    assert.ok(value, `${token} must be a solid colour`);
    for (const surface of surfaces) {
      assert.ok(contrast(value, surface) >= 4.5, `${token} ${value} on ${surface} is ${contrast(value, surface).toFixed(2)}:1`);
    }
  }
  const stylesheet = readFileSync(path.join(srcRoot, "app", "styles.css"), "utf8");
  const muted = /--color-text-muted:\s*(#[0-9a-fA-F]{6})/.exec(stylesheet)?.[1];
  assert.ok(muted && contrast(muted, "#e8edf3") >= 4.5, `--color-text-muted ${muted} fails AA on the sidebar surface`);
});

test("known low-contrast slate greys are not used for text", () => {
  const banned = ["#738196", "#738198", "#66758b", "#8395aa", "#718096", "#8491a4", "#7b899c", "#758398", "#6c7b90", "#94a3b8", "#6b7a8f", "#6b7a90"];
  const dark = ["command-center.css", "dashboard-premium.css", "dashboard.css", "dashboard-layout-fix.css", "auth.css", "factory-showcase.css"];
  const offenders: string[] = [];
  for (const file of cssFiles) {
    if (dark.some((name) => file.endsWith(name))) continue;
    for (const match of read(file).matchAll(/(?:^|[;{\s])color\s*:\s*(#[0-9a-fA-F]{6})\b/g)) {
      if (banned.includes(match[1].toLowerCase())) offenders.push(`${rel(file)}: ${match[1]}`);
    }
  }
  assert.deepEqual(offenders, []);
});

// ── 7. one page-title scale and no raw provider labels ───────────────────────────────────────────────────────────

test("the shared page-title scale is defined and applied to every page h1", () => {
  const premium = readFileSync(path.join(srcRoot, "design-system", "zentra-premium.css"), "utf8");
  assert.match(premium, /--zx-page-title-size:/);
  assert.match(premium, /\.content-viewport h1:not\(/);
});

test("assistant surfaces never print provider, model or prompt identifiers", () => {
  const offenders: string[] = [];
  for (const file of tsxFiles) {
    const text = read(file);
    if (/\{[\w.?]*(?:modelName|providerIdentifier|providerId|promptVersion)\}/.test(text)) offenders.push(rel(file));
  }
  assert.deepEqual(offenders, []);
});

test("shipping-route page headings use exactly one h1 source per screen kind", () => {
  // the Task Center header embedded in the Today screen must not introduce a second h1
  const list = read(path.join(srcRoot, "features", "ai-task-center", "components", "ai-task-list-page-content.tsx"));
  assert.match(list, /headingLevel=\{embedded \? 2 : 1\}/);
  const today = read(path.join(srcRoot, "features", "bugun", "components", "bugun-page-content.tsx"));
  // Today merges into the Task Center header: it supplies the title and renders no second header of its own
  assert.match(today, /AiTaskListPageContent[\s\S]{0,600}title="Bugün neye dikkat etmeliyim\?"/);
  assert.doesNotMatch(today, /WorkspaceHeader|StatusStrip/);
});

// ── 8. the shipping-route matrix cannot silently shrink or regress ────────────────────────────────────────────────

test("every shipping page has a passing premium-bar record (>= 8/10) and the inventory cannot shrink", () => {
  const matrix = JSON.parse(readFileSync(path.resolve(__dirname, "fixtures", "shipping-ui-matrix.json"), "utf8")) as ReadonlyArray<{
    route: string;
    score: number;
    result: string;
    desktop: string;
    responsive: string;
    accessibility: string;
  }>;
  assert.ok(matrix.length >= 131, `the shipping matrix lists ${matrix.length} routes; it must not drop below 131`);

  const pageRoutes: string[] = [];
  const appRoot = path.join(srcRoot, "app", "(app)");
  for (const file of walk(appRoot)) {
    if (path.basename(file) !== "page.tsx") continue;
    const route = "/" + path.relative(appRoot, path.dirname(file)).split(path.sep).join("/");
    pageRoutes.push(route === "/" ? "/" : route);
  }
  const excluded = (route: string) => /^\/(golden-demo|sunum-modu|internal|dijital-is-gucu)(\/|$)/.test(route) || route.includes("[...");
  const expected = pageRoutes.filter((route) => !excluded(route)).sort();
  assert.deepEqual(matrix.map((row) => row.route).sort(), expected, "add a record for every new shipping page (and drop removed ones)");

  for (const row of matrix) {
    assert.ok(row.score >= 8, `${row.route} scores ${row.score}/10`);
    assert.equal(row.result, "PASS", row.route);
    assert.equal(row.desktop, "PASS", row.route);
    assert.equal(row.responsive, "PASS", row.route);
    assert.equal(row.accessibility, "PASS", row.route);
  }
});

// ── 9. deferred and demo-only surfaces stay out of Production ────────────────────────────────────────────────────

test("deferred surfaces are off by default in Production and on only in an explicit demo/development context", async () => {
  const { deferredSurfacesEnabled, isSurfaceHrefVisible, isDeferredSurfaceHref } = await import("../src/lib/runtime/deferred-surfaces");
  assert.equal(deferredSurfacesEnabled({ NODE_ENV: "production" }), false);
  assert.equal(deferredSurfacesEnabled({ NODE_ENV: "production", NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL: "PRODUCTION" }), false);
  assert.equal(deferredSurfacesEnabled({}), false);
  assert.equal(deferredSurfacesEnabled({ NODE_ENV: "production", NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL: "demo" }), true);
  assert.equal(deferredSurfacesEnabled({ NODE_ENV: "production", NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL: "PRESENTATION" }), true);
  assert.equal(deferredSurfacesEnabled({ NODE_ENV: "development" }), true);
  assert.equal(deferredSurfacesEnabled({ NODE_ENV: "production", NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES: "1" }), true);
  assert.equal(isDeferredSurfaceHref("/dijital-is-gucu"), true);
  assert.equal(isDeferredSurfaceHref("/dijital-is-gucu/onaylar?x=1"), true);
  assert.equal(isDeferredSurfaceHref("/dijital-is-gucum"), false);
  assert.equal(isSurfaceHrefVisible("/dijital-is-gucu/sonuclar", { NODE_ENV: "production" }), false);
  assert.equal(isSurfaceHrefVisible("/uretim", { NODE_ENV: "production" }), true);
});

test("Digital Workforce navigation is filtered, its routes fail closed and no page links to it unconditionally", () => {
  const visibility = read(path.join(srcRoot, "lib", "navigation", "navigation-visibility.ts"));
  assert.match(visibility, /isSurfaceHrefVisible\(item\.href\)/, "every navigation surface (primary, contextual, apps, search) is filtered");

  const offenders: string[] = [];
  for (const file of tsxFiles.concat(files.filter((f) => f.endsWith(".ts")))) {
    const key = rel(file);
    if (key.startsWith("features/digital-workforce/") || key.startsWith("app/(app)/dijital-is-gucu/") || key.startsWith("lib/navigation/") || key.startsWith("lib/runtime/")) continue;
    if (/href=["`{][^>]*\/dijital-is-gucu/.test(read(file)) && !/deferredSurfacesEnabled|available/.test(read(file))) offenders.push(key);
  }
  assert.deepEqual(offenders, [], "links to the deferred module must be behind deferredSurfacesEnabled()");
});

test("the Golden Demo entry card is rendered only in an allowed demo context", () => {
  const page = read(path.join(srcRoot, "app", "(app)", "operasyon", "yonetim-merkezi", "page.tsx"));
  assert.match(page, /deferredSurfacesEnabled\(\) \? \(\s*<section className="commint-panel" data-testid="golden-demo-p1-link-section">/);
  assert.doesNotMatch(page.replace(/deferredSurfacesEnabled\(\) \? \([\s\S]*?<\/section>/, ""), /ALTIN DEMO/);
});

test("no table keeps the generic accessible name", () => {
  const generic = tsxFiles.filter((file) => /aria-label="Veri tablosu"/.test(read(file))).map(rel);
  assert.deepEqual(generic, []);
});

test("route pages report failures through the shared failure state, never an ad-hoc panel", () => {
  const AD_HOC = /className="(?:state-panel error-state|commint-error-banner|commint-partial-error|quality-error-panel|maintenance-error-panel|energy-error-banner|saas-admin-alert)"/;
  const offenders = tsxFiles
    .filter((file) => /[\/]app[\/]\(app\)[\/].*page\.tsx$/.test(file))
    .filter((file) => AD_HOC.test(read(file)))
    // The dashboard "preview removed" notice and the (deferred) demo pages are not failure panels.
    .filter((file) => !/[\/](dashboard|golden-demo|sunum-modu)[\/]/.test(file))
    .map(rel);
  assert.deepEqual(offenders, []);
});

test("no customer screen prints the internal rule-set version", () => {
  const offenders = tsxFiles.filter((file) => /Kural seti/.test(read(file))).map(rel);
  assert.deepEqual(offenders, []);
});

test("the copilot launcher is single: the toggle yields to a page header action", () => {
  const shell = read(path.join(srcRoot, "features", "factory-copilot", "components", "factory-copilot-shell.tsx"));
  assert.match(shell, /headerActions === 0 \?/);
});

test("scrollable table wrappers are keyboard-focusable", () => {
  const scrollable = new Set<string>();
  const cssFiles: string[] = [];
  const walk = (dir: string) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) walk(full);
      else if (entry.name.endsWith(".css")) cssFiles.push(full);
    }
  };
  walk(srcRoot);
  for (const file of cssFiles) {
    const css = read(file);
    for (const match of css.matchAll(/\.([a-z0-9_-]*(?:table-wrap|table-scroll))\s*\{([^}]*)\}/gi)) {
      if (/overflow(?:-x)?\s*:\s*(?:auto|scroll)/.test(match[2])) scrollable.add(match[1]);
    }
  }
  const offenders: string[] = [];
  for (const file of tsxFiles) {
    const source = read(file);
    for (const cls of scrollable) {
      for (const tag of source.matchAll(new RegExp(`<div[^>]*className="[^"]*\\b${cls}\\b[^"]*"[^>]*>`, "g"))) {
        if (!/tabIndex=\{0\}/.test(tag[0])) offenders.push(`${rel(file)}: ${cls}`);
      }
    }
  }
  assert.deepEqual(offenders, []);
});

test("edge screens show customer-safe copy and the failure message is never raw", () => {
  const edgeDir = path.join(srcRoot, "features", "edge", "components");
  const files = tsxFiles.filter((file) => file.startsWith(edgeDir));
  const offenders: string[] = [];
  for (const file of files) {
    const code = read(file).replace(/\/\*[\s\S]*?\*\//g, "").replace(/^\s*\/\/.*$/gm, "");
    for (const m of code.matchAll(/>([^<>{}]*)</g)) {
      if (/\b(Edge|Gateway|Dead Letter|Simülatör|API Bağlantısı|Transport|Replay)\b/.test(m[1])) offenders.push(`${rel(file)}: ${m[1].trim()}`);
    }
  }
  assert.deepEqual(offenders, []);
  const view = read(path.join(edgeDir, "edge-workspace-view.tsx"));
  assert.match(view, /customerMessage\(loadError\)/);
  assert.match(view, /<h1>Makineler<\/h1>/);
});


test("edge screens: no English in string literals, accessible scroll regions and tables, no sub-12px text, no replan wording", () => {
  const edgeDir = path.join(srcRoot, "features", "edge", "components");
  const files = tsxFiles.filter((file) => file.startsWith(edgeDir));
  const offenders: string[] = [];
  for (const file of files) {
    const code = read(file).replace(/\/\*[\s\S]*?\*\//g, "").replace(/^\s*\/\/.*$/gm, "");
    for (const m of code.matchAll(/(?:label|title|aria-label|placeholder)(?:=|:\s*)"([^"]+)"/g)) {
      if (/\b(Edge|Gateway|Dead Letter|Setup|Transport|Replay|Backlog|Retry)\b/.test(m[1])) offenders.push(`${rel(file)}: ${m[1]}`);
    }
    for (const m of code.matchAll(/<table\b[^>]*>/g)) {
      if (!/aria-label=/.test(m[0])) offenders.push(`${rel(file)}: table without aria-label`);
    }
    for (const m of code.matchAll(/<div[^>]*(?:table-wrap|table-scroll)[^>]*>/g)) {
      if (!/role="region"/.test(m[0]) || !/aria-label=/.test(m[0])) offenders.push(`${rel(file)}: scroll wrapper without role/aria-label`);
    }
    if (/fontSize:\s*(?:9|10|11)\b/.test(code)) offenders.push(`${rel(file)}: font size below 12px`);
  }
  assert.deepEqual(offenders, []);
  for (const rel2 of [["adaptive-production-planning", "domain", "adaptive-planning-labels.ts"], ["factory-network", "domain", "factory-network-labels.ts"]]) {
    const source = read(path.join(srcRoot, "features", ...rel2)).replace(/case "[^"]*":/g, "");
    assert.doesNotMatch(source, /"[^"\n]*[Rr]eplan[^"\n]*"/, rel2.join("/"));
  }
});


test("customer-facing JSX text never uses internal English product jargon", () => {
  const banned = /(Human-in-the-Loop|Related Context|Pickup deadline|Export readiness|\bDisposition\b|\bSetup\b|\bBacklog\b|\bGateway\b|\bBloker\b|Eskale|[Mm]ateryalize|Omnichannel|Kaynak grid|\bSync\b|BLOCKED \/ ESCALATED|Quality exposure)/;
  const offenders: string[] = [];
  for (const file of tsxFiles) {
    const code = read(file).replace(/\/\*[\s\S]*?\*\//g, "").replace(/^\s*\/\/.*$/gm, "");
    for (const m of code.matchAll(/>([^<>{}]*)</g)) {
      if (banned.test(m[1])) offenders.push(`${rel(file)}: ${m[1].trim().slice(0, 60)}`);
    }
  }
  assert.deepEqual(offenders, []);
});

test("simulation workflow labels keep their meaning and are not renamed to sample data", async () => {
  const network = read(path.join(srcRoot, "features", "factory-network", "domain", "factory-network-labels.ts"));
  const adaptive = read(path.join(srcRoot, "features", "adaptive-production-planning", "domain", "adaptive-planning-labels.ts"));
  for (const source of [network, adaptive]) {
    assert.match(source, /"Simülasyon"/);
    assert.doesNotMatch(source, /return "Örnek veri"/);
  }
});

test("sample-data banners only render when sample data is actually present", () => {
  const dir = path.join(srcRoot, "features", "commercial-intelligence", "components");
  for (const file of readdirSync(dir)) {
    const source = read(path.join(dir, file));
    for (const m of source.matchAll(/\{([^{}?]*)\?\s*\(\s*<p[^>]*className="commint-provenance-strip"/g)) {
      assert.match(m[1], /hasDevelopmentPilotData/, `${file}: provenance strip must be gated on hasDevelopmentPilotData`);
    }
  }
  const labels = read(path.join(srcRoot, "features", "maintenance", "presentation", "maintenance-predictive-risk-labels.ts"));
  assert.doesNotMatch(labels, /Örnek veriye dayalı/);
});

test("every stylesheet has balanced braces (a stray brace breaks the production build)", () => {
  const offenders: string[] = [];
  for (const file of cssFiles) {
    const code = read(file).replace(/\/\*[\s\S]*?\*\//g, "").replace(/"(?:[^"\\n]|\.)*"|'(?:[^'\\n]|\.)*'/g, '""');
    let depth = 0;
    for (const ch of code) {
      if (ch === "{") depth++;
      else if (ch === "}") depth--;
      if (depth < 0) break;
    }
    if (depth !== 0) offenders.push(rel(file));
  }
  assert.deepEqual(offenders, []);
});

test("no source file contains stray control characters (a stripped CSS escape such as the arrow glyph renders as a missing-glyph box)", () => {
  const offenders: string[] = [];
  for (const file of files.filter((item) => /\.(css|tsx?)$/.test(item))) {
    for (const ch of read(file)) {
      const code = ch.charCodeAt(0);
      if (code < 32 && code !== 9 && code !== 10 && code !== 13) {
        offenders.push(rel(file));
        break;
      }
    }
  }
  assert.deepEqual(offenders, []);
});
