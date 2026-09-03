"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import type { RawShiftHandoverDetail } from "../server/shift-handover-api-client";
import {
  createFollowUpTaskAction,
  shiftHandoverLifecycleAction,
} from "../server/shift-handover-actions";

type Props = Readonly<{
  detail: RawShiftHandoverDetail;
}>;

export function ShiftHandoverLifecycleActions({ detail }: Props) {
  const router = useRouter();

  async function run(action: "ready" | "handover" | "acknowledge" | "complete") {
    const result = await shiftHandoverLifecycleAction(detail.id, action, detail.version);
    if (result.status === "failure") {
      window.alert(result.message);
      return;
    }
    router.refresh();
  }

  return (
    <div className="shift-handover-actions">
      {detail.capabilities.canManage && detail.status === "draft" ? (
        <Button onClick={() => run("ready")} type="button">Haz─▒rla</Button>
      ) : null}
      {detail.capabilities.canManage && detail.status === "ready-for-handover" ? (
        <Button onClick={() => run("handover")} type="button">Devret</Button>
      ) : null}
      {detail.capabilities.canAcknowledge && detail.status === "handed-over" ? (
        <Button onClick={() => run("acknowledge")} type="button">Teslim Al / Onayla</Button>
      ) : null}
      {detail.capabilities.canManage && detail.status === "acknowledged" ? (
        <Button onClick={() => run("complete")} type="button">Tamamla</Button>
      ) : null}
    </div>
  );
}

export function ShiftHandoverFollowUpTasks({ detail }: Props) {
  const router = useRouter();
  const followUpItems = detail.items.filter((item) => item.requiresFollowUp);

  if (followUpItems.length === 0) {
    return <p>Takip gerektiren madde yok.</p>;
  }

  return (
    <div className="shift-handover-items">
      {followUpItems.map((item) => (
        <div className="shift-handover-item" key={item.id}>
          <div className="shift-handover-item-header">
            <strong>{item.title}</strong>
            {item.linkedAiTaskId ? (
              <Link href={`/?task=${item.linkedAiTaskId}`}>AI G├Ârev Merkezi</Link>
            ) : detail.capabilities.canCreateTask ? (
              <Button
                onClick={async () => {
                  const result = await createFollowUpTaskAction(detail.id, item.id);
                  if (result.status === "failure") {
                    window.alert(result.message);
                    return;
                  }
                  router.refresh();
                }}
                type="button"
                variant="secondary"
              >
                Takip G├Ârevi Olu┼ştur
              </Button>
            ) : null}
          </div>
        </div>
      ))}
    </div>
  );
}
