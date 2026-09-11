import type { DigitalWorkerId } from "./digital-workforce-types";

export const DIGITAL_WORKER_API_IDS: Record<DigitalWorkerId, string> = {
  "dw-fact-001": "bb000001-00d1-4000-8000-000000000001",
  "dw-fact-002": "bb000001-00d2-4000-8000-000000000002",
  "dw-fact-003": "bb000001-00d3-4000-8000-000000000003",
  "dw-fact-004": "bb000001-00d4-4000-8000-000000000004",
  "dw-fact-005": "bb000001-00d5-4000-8000-000000000005",
  "dw-fact-006": "bb000001-00d6-4000-8000-000000000006",
  "dw-fact-007": "bb000001-00d7-4000-8000-000000000007",
};

export function resolveWorkerApiId(workerId: DigitalWorkerId): string {
  return DIGITAL_WORKER_API_IDS[workerId];
}

export function resolveWorkerUiId(apiId: string): DigitalWorkerId | null {
  const entry = Object.entries(DIGITAL_WORKER_API_IDS).find(([, id]) => id === apiId);
  return entry ? (entry[0] as DigitalWorkerId) : null;
}
