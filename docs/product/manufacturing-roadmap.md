# Nevaxis Zentra — Manufacturing Roadmap (Current)

> **Status:** CURRENT ROADMAP  
> **Effective:** 2026-09-16  
> **Baseline:** `52dd07a` and prior PASS architecture preserved

## Historical vs Current

Historical closure reports (Golden Demo P1–P3, ULTRA-MEGA packages, Enterprise Production Readiness) remain immutable evidence. They do **not** define the current MP1/MP2/MP3 roadmap.

This document is the **canonical current roadmap** for manufacturing platform phases.

## Product Direction

Nevaxis Zentra is **not** an injection-only factory OS.

```
GENERIC MANUFACTURING CORE
        ↓
CAPABILITY LAYERS
        ↓
INJECTION MANUFACTURING (specialized)
        ↓
OTHER MANUFACTURING TYPES (future)
```

**Primary optimization objective:** MAXIMUM VERIFIED GOOD THROUGHPUT

Guardrails: Quality, Scrap, Safety, Maintenance Risk, Policy

Philosophy: *More output is not success unless good output is verified.*

## Architecture Hierarchy

```
Factory (= Facility)
  → Area
    → Line
      → Station
        → Operation
          → Resource
```

**Resource types:** Machine, Human, Robot, Tool, Conveyor, Buffer/WIP, Quality Station

## Current Phases

### MP1 — Mixed Production & Connected Operations Ultra

**Status:** IN IMPLEMENTATION

**Purpose:** Model automated, semi-automated, and human-operated production stations in one canonical production flow.

**Scope:**
- Generic manufacturing hierarchy
- Product routing / production flow
- Human-operated stations
- Buffer/WIP
- Canonical production events
- Production timeline
- Line state projection
- Line playback foundation
- Operator interaction foundation
- Digital work instruction foundation
- Universal connector resource mapping
- Injection compatibility (no regression)

**PASS gate:** MIX-LINE-01 deterministic mixed line (Injection → Buffer → Assembly → Welding → Test → Packaging)

---

### MP2 — Throughput Intelligence & Simulation Ultra

**Status:** PLANNED (not started)

**Purpose:** Determine why the production system cannot generate more GOOD output and simulate interventions.

**Scope:**
- Dynamic constraint engine (current / emerging / secondary)
- Throughput loss tree
- Skill model and skill-aware line balancing
- Scenario engine / what-if simulator
- Maximum verified good throughput with guardrails

**PASS gate:** Quantify gap, identify constraint, simulate interventions, show constraint movement

---

### MP3 — Autonomous Throughput Digital Workforce Ultra

**Status:** PLANNED (not started)

**Purpose:** Controlled action and VERIFIED production improvement via Throughput Optimizer Digital Worker.

**Scope:**
- OBSERVE → DIAGNOSE → SIMULATE → RECOMMEND → POLICY/APPROVAL → EXECUTE → MEASURE → VERIFY → LEARN
- Reuses DigitalWorkCase (no new case architecture)
- Verified Improvement Engine
- Factory Learning from verified interventions

**PASS gate:** Golden demo on MIX-LINE-01 with actual/simulated GOOD throughput increase and VERIFIED closure

## Replaced Definitions

| Old concept | Status |
|-------------|--------|
| OLD MP1 | **REPLACED** by MP1 Mixed Production & Connected Operations Ultra |
| OLD MP2 | **REPLACED** by MP2 Throughput Intelligence & Simulation Ultra |
| OLD MP3 | **REPLACED** by MP3 Autonomous Throughput Digital Workforce Ultra |

No duplicate MP4/MP5 packages. No parallel roadmap definitions.

## Customer Release Note

Physical Clean-PC Customer Release Acceptance remains **PENDING** and is independent of this manufacturing roadmap.
