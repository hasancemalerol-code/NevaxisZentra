"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  useTransition,
  type ReactNode,
} from "react";

import {
  digitalWorkforceApplyPoliciesAction,
  digitalWorkforceDecideApprovalAction,
  digitalWorkforceGlobalKillAction,
  digitalWorkforceGlobalPauseAction,
  digitalWorkforceGlobalResumeAction,
  digitalWorkforceGlobalSuggestModeAction,
  digitalWorkforcePauseWorkerAction,
  digitalWorkforceResumeHumanControlAction,
  digitalWorkforceResumeWorkerAction,
  digitalWorkforceTakeoverCaseAction,
  digitalWorkforceUpdateAutonomyAction,
} from "../server/digital-workforce-actions";
import { resolveWorkerApiId } from "../domain/digital-workforce-api-ids";
import type {
  DigitalAutonomyMode,
  DigitalWorkerId,
  DigitalWorkerRuntimeStatus,
} from "../domain/digital-workforce-types";
import type {
  DigitalWorkActivityApiSummary,
  DigitalWorkApprovalApiSummary,
  DigitalWorkerApiSummary,
  DigitalWorkforceControlCenterSnapshot,
} from "../server/digital-workforce-api-client";

type ApprovalDecision = "approved" | "rejected" | "modified";

type RuntimeApi = Readonly<{
  snapshot: DigitalWorkforceControlCenterSnapshot | null;
  approvals: readonly DigitalWorkApprovalApiSummary[];
  activity: readonly DigitalWorkActivityApiSummary[];
  apiError: string | null;
  isPending: boolean;
  refresh: () => void;
  globalMode: DigitalAutonomyMode;
  getWorker: (id: DigitalWorkerId) => DigitalWorkerApiSummary | undefined;
  getWorkerMode: (id: DigitalWorkerId) => DigitalAutonomyMode;
  getWorkerStatus: (id: DigitalWorkerId, fallback: DigitalWorkerRuntimeStatus) => DigitalWorkerRuntimeStatus;
  isPaused: (id: DigitalWorkerId) => boolean;
  setWorkerMode: (id: DigitalWorkerId, mode: DigitalAutonomyMode) => Promise<void>;
  setGlobalMode: (mode: DigitalAutonomyMode) => Promise<void>;
  startAll: () => Promise<void>;
  pauseAll: () => Promise<void>;
  suggestModeAll: () => Promise<void>;
  applyPolicies: () => Promise<void>;
  emergencyStop: () => Promise<void>;
  toggleWorkerPause: (id: DigitalWorkerId) => Promise<void>;
  takeOverCase: (caseApiId: string) => Promise<void>;
  resumeHumanControl: (caseApiId: string) => Promise<void>;
  decideApproval: (approvalId: string, decision: ApprovalDecision, concurrencyToken: number) => Promise<void>;
}>;

const RuntimeContext = createContext<RuntimeApi | null>(null);

function mapWorkerStatus(worker: DigitalWorkerApiSummary | undefined, globalKill: boolean, globalPause: boolean): DigitalWorkerRuntimeStatus {
  if (!worker) return "idle";
  if (globalKill || worker.isKilled) return "paused";
  if (globalPause || worker.isPaused) return "paused";
  if (worker.isHumanControlled) return "human-controlled";
  if (worker.pendingApprovals > 0) return "awaiting-approval";
  if (worker.activeCaseCount > 0) return "working";
  if (worker.autonomyMode === "observe") return "observing";
  return "idle";
}

function resolveGlobalMode(workers: readonly DigitalWorkerApiSummary[]): DigitalAutonomyMode {
  if (workers.length === 0) return "observe";
  const first = workers[0]?.autonomyMode as DigitalAutonomyMode;
  return workers.every((worker) => worker.autonomyMode === first) ? first : "draft";
}

export function DigitalWorkforceRuntimeProvider({
  children,
  initialSnapshot = null,
  initialApprovals = [],
  initialActivity = [],
  apiError = null,
}: Readonly<{
  children: ReactNode;
  initialSnapshot?: DigitalWorkforceControlCenterSnapshot | null;
  initialApprovals?: readonly DigitalWorkApprovalApiSummary[];
  initialActivity?: readonly DigitalWorkActivityApiSummary[];
  apiError?: string | null;
}>) {
  const [snapshot, setSnapshot] = useState(initialSnapshot);
  const [approvals, setApprovals] = useState(initialApprovals);
  const [activity, setActivity] = useState(initialActivity);
  const [error, setError] = useState(apiError);
  const [isPending, startTransition] = useTransition();

  const refresh = useCallback(() => {
    startTransition(() => {
      window.location.reload();
    });
  }, []);

  const getWorker = useCallback(
    (id: DigitalWorkerId) => snapshot?.workers.find((w) => w.id === resolveWorkerApiId(id)),
    [snapshot],
  );

  const globalMode = useMemo(
    () => resolveGlobalMode(snapshot?.workers ?? []),
    [snapshot],
  );

  const api = useMemo<RuntimeApi>(() => ({
    snapshot,
    approvals,
    activity,
    apiError: error,
    isPending,
    refresh,
    globalMode,
    getWorker,
    getWorkerMode(id) {
      const worker = getWorker(id);
      return (worker?.autonomyMode ?? "observe") as DigitalAutonomyMode;
    },
    getWorkerStatus(id, fallback) {
      const worker = getWorker(id);
      if (!worker) return fallback;
      return mapWorkerStatus(worker, snapshot?.globalKillActive ?? false, snapshot?.globalPauseActive ?? false);
    },
    isPaused(id) {
      const worker = getWorker(id);
      return (snapshot?.globalPauseActive ?? false) || (snapshot?.globalKillActive ?? false) || worker?.isPaused === true;
    },
    async setWorkerMode(id, mode) {
      const worker = getWorker(id);
      if (!worker) return;
      const result = await digitalWorkforceUpdateAutonomyAction(worker.id, mode, worker.concurrencyToken);
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setSnapshot((current) => current ? {
        ...current,
        workers: current.workers.map((w) => w.id === worker.id ? { ...w, autonomyMode: mode, concurrencyToken: w.concurrencyToken + 1 } : w),
      } : current);
      refresh();
    },
    async setGlobalMode(mode) {
      for (const worker of snapshot?.workers ?? []) {
        await digitalWorkforceUpdateAutonomyAction(worker.id, mode, worker.concurrencyToken);
      }
      refresh();
    },
    async startAll() {
      const result = await digitalWorkforceGlobalResumeAction();
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
    async pauseAll() {
      const result = await digitalWorkforceGlobalPauseAction();
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
    async suggestModeAll() {
      const result = await digitalWorkforceGlobalSuggestModeAction();
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
    async applyPolicies() {
      const result = await digitalWorkforceApplyPoliciesAction();
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
    async emergencyStop() {
      const result = await digitalWorkforceGlobalKillAction();
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
    async toggleWorkerPause(id) {
      const worker = getWorker(id);
      if (!worker) return;
      const result = worker.isPaused
        ? await digitalWorkforceResumeWorkerAction(worker.id, worker.concurrencyToken)
        : await digitalWorkforcePauseWorkerAction(worker.id, worker.concurrencyToken);
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
    async takeOverCase(caseApiId) {
      const result = await digitalWorkforceTakeoverCaseAction(caseApiId);
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
    async resumeHumanControl(caseApiId) {
      const result = await digitalWorkforceResumeHumanControlAction(caseApiId);
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
    async decideApproval(approvalId, decision, concurrencyToken) {
      const result = await digitalWorkforceDecideApprovalAction(approvalId, decision, concurrencyToken);
      if (result.status === "failure") setError(result.message);
      else refresh();
    },
  }), [snapshot, approvals, activity, error, isPending, refresh, getWorker, globalMode]);

  return <RuntimeContext.Provider value={api}>{children}</RuntimeContext.Provider>;
}

export function useDigitalWorkforceRuntime(): RuntimeApi {
  const value = useContext(RuntimeContext);
  if (!value) {
    throw new Error("Digital workforce runtime is missing.");
  }
  return value;
}
