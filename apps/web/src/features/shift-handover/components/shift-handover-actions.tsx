"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { EmptyPanel } from "@/components/ui/empty-panel";
import { customerMessage } from "@/lib/presentation/customer-message";
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
  const [error, setError] = useState<string | null>(null);

  async function run(action: "ready" | "handover" | "acknowledge" | "complete") {
    setError(null);
    const result = await shiftHandoverLifecycleAction(detail.id, action, detail.version);
    if (result.status === "failure") {
      setError(customerMessage(result.message));
      return;
    }
    router.refresh();
  }

  const hasAction =
    (detail.capabilities.canManage && ["draft", "ready-for-handover", "acknowledged"].includes(detail.status))
    || (detail.capabilities.canAcknowledge && detail.status === "handed-over");

  return (
    <div className="shift-handover-actions-block">
      <div className="shift-handover-actions">
        {detail.capabilities.canManage && detail.status === "draft" ? (
          <Button onClick={() => run("ready")} type="button">Devre Hazırla</Button>
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
        {hasAction ? null : <p className="shift-handover-muted">Bu kayıt için uygun bir işlem yok.</p>}
      </div>
      {error ? <p className="shift-handover-error" role="alert">{error}</p> : null}
    </div>
  );
}

export function ShiftHandoverFollowUpTasks({ detail }: Props) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const followUpItems = detail.items.filter((item) => item.requiresFollowUp);

  if (followUpItems.length === 0) {
    return <EmptyPanel compact title="Takip gerektiren kalem yok" description="Takip gerektiren bir kalem işaretlendiğinde burada görev oluşturabilirsiniz." />;
  }

  return (
    <div className="shift-handover-items">
      {followUpItems.map((item) => (
        <div className="shift-handover-item" key={item.id}>
          <div className="shift-handover-item-header">
            <strong>{item.title}</strong>
            {item.linkedAiTaskId ? (
              <Link href={`/?task=${item.linkedAiTaskId}`}>Yapay Zekâ Görev Merkezi</Link>
            ) : detail.capabilities.canCreateTask ? (
              <Button
                onClick={async () => {
                  setError(null);
                  const result = await createFollowUpTaskAction(detail.id, item.id);
                  if (result.status === "failure") {
                    setError(customerMessage(result.message));
                    return;
                  }
                  router.refresh();
                }}
                type="button"
                variant="secondary"
              >
                Takip Görevi Oluştur
              </Button>
            ) : null}
          </div>
        </div>
      ))}
      {error ? <p className="shift-handover-error" role="alert">{error}</p> : null}
    </div>
  );
}
