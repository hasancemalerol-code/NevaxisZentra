# Nevaxis FactOS — Golden Demo Backup Recording Plan

Record **after** sales-ready freeze passes, using the **real localhost Web + API** flow. No mocked UI, no fabricated outcomes.

## Recording setup

- Resolution: 1920×1080 (or presenter laptop native)
- Browser: Chrome/Edge, 100% zoom
- Route: `http://localhost:3000/golden-demo/uretim-kurtarma`
- Pre-roll: reset Golden Demo, confirm starting state
- Audio: optional presenter voice or silent with on-screen captions
- Label each clip with date and commit SHA

## Clips (short, 15–90 seconds each)

| Clip ID | Content | Start state |
|---------|---------|-------------|
| `01-start-state` | Reset confirmation + KPI header | Post-reset active |
| `02-target-risk` | Production target at risk KPIs | Step 01 |
| `03-machine-intelligence` | Machine metrics panel | Step 02 |
| `04-cycle-loss` | Cycle loss decomposition | Step 03 |
| `05-production-recovery` | Recovery opportunity +X KPI | Step 04 |
| `06-digital-factory-team` | Agent cards + one evidence drawer | Step 05 |
| `07-approval-intervention` | Approve → start → complete intervention | Steps 08–09 |
| `08-validation` | Validation run panel | Step 10 |
| `09-before-after` | Before/after table | Step 11 |
| `10-production-recovered` | PRODUCTION RECOVERED + hero KPI | Step 13–14 |
| `11-energy` | Energy impact section | Step 15 |
| `12-business-impact` | Estimated business impact (secondary) | Step 16 |

## Post-production

- Store clips in internal sales asset folder (not in repo unless policy allows)
- Match presenter card and runbook click order
- If live demo fails Tuesday, play clips **in sequence** — do not edit to imply outcomes not supported by runtime

## Truthfulness rules

- Show actual verification state (VERIFIED only for success hero)
- Do not overlay fake +X numbers
- Do not claim live PLC unless connected
- Include classification labels visible in UI (ÖLÇÜLMÜŞ / HESAPLANMIŞ / TAHMİNİ)
