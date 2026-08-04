# CrevaFactOS Web UI Foundation

The Web application is a browser-based, desktop-first factory-operations workspace. UI-01 establishes the shell and reusable visual language only; the locked dashboard data regions and product workflows remain deferred to UI-02 and later packages.

## Structure

- `src/app/(app)/layout.tsx` applies the authenticated-application-shaped shell without implementing authentication.
- `src/app/(app)/page.tsx` is a clearly labelled component preview for AI Task Center.
- `src/app/(app)/[section]/page.tsx` provides minimal placeholders for locked navigation routes.
- `src/components/app-shell` owns the sidebar, top bar, skip link, collapse state, and content viewport.
- `src/components/ui` owns buttons, cards, KPI cards, badges, status treatments, sections, page headers, loading, empty, error, divider, and visually-hidden primitives.
- `src/lib/navigation/primary-navigation.ts` is the single locked navigation contract.
- `src/app/styles.css` contains semantic CSS tokens and component styles.

## Visual contract

The light theme uses a soft blue-grey sidebar, white operational surfaces, navy text, restrained blue interaction states, and limited semantic green, orange, red, information-blue, and AI-purple. Tokens cover surfaces, borders, text, semantic colors, focus rings, shadows, radii, spacing, sidebar widths, top-bar height, and content width. No dark theme, gradients, glassmorphism, chart library, CSS-in-JS layer, or external font is included.

Lucide React `1.27.0` is the only icon library. Imports are named and tree-shakable; icons always accompany an accessible text or ARIA label.

## Responsive and accessibility behavior

- Primary target: 1440px and wider.
- At 1280px the sidebar and content gutters tighten and KPI cards reflow to two columns.
- Around 1024px the navigation becomes compact while preserving accessible link names; secondary top-bar copy is reduced.
- Small-phone/mobile-bottom-navigation behavior is intentionally out of scope.

The shell includes semantic landmarks, `aria-current`, a skip link, native links and buttons, labelled icon controls, visible focus rings, practical hit areas, reduced-motion handling, semantic headings, and tabular KPI numerals. Sidebar preference is stored only in browser `localStorage` after hydration.

## Locked navigation

AI Görev Merkezi, Dashboard / Operasyon Merkezi, Üretim, Planlama, Vardiyalar, Makineler, Kalıplar, Bakım, Kalite, Malzeme Riskleri, Sipariş Operasyonları, CrevaFactOS Teams, Raporlar, Entegrasyonlar, and Ayarlar.

There are no native accounting, finance, receivables/payables, payroll, official stock-ledger, general purchasing ERP, CRM, HR, WMS, banking, or invoicing routes.

## Deferred work

UI-01 performs no API fetching and contains no real factory data, full dashboard, chart, authentication UI, tenant-switcher behavior, CRUD workflow, RabbitMQ publishing, connector, AI agent, Teams messaging, ERP screen, or machine control.
