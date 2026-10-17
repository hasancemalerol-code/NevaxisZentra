"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  TEAMS_HANDOVER_DISCLAIMER,
  TEAMS_HANDOVER_ITEM_KINDS,
  TEAMS_HANDOVER_SHIFT_TYPES,
} from "@/features/teams/domain/teams-constants";
import {
  createTeamsHandoverAction,
  updateTeamsHandoverDraftAction,
} from "@/features/teams/server/teams-actions";
import type { TeamsHandoverItemWriteBody } from "@/features/teams/server/teams-api-client";
import { customerMessage } from "@/lib/presentation/customer-message";
import { Notice } from "@/components/ui/notice";

type ChannelOption = Readonly<{
  id: string;
  name: string;
}>;

type HandoverItemDraft = Readonly<{
  itemKind: string;
  title: string;
  body: string;
  linkKind: "none" | "message" | "task";
  messageId: string;
  aiTaskId: string;
}>;

type TeamsHandoverFormProps = Readonly<{
  mode: "create" | "edit";
  handoverId?: string | null;
  channels: readonly ChannelOption[];
  lockedChannelId?: string | null;
  initialShiftDate?: string;
  initialShiftType?: string;
  initialSummary?: string;
  initialSafetyNote?: string;
  initialItems?: readonly HandoverItemDraft[];
}>;

function todayIsoDate(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

function emptyItem(overrides?: Partial<HandoverItemDraft>): HandoverItemDraft {
  return {
    itemKind: "attention",
    title: "",
    body: "",
    linkKind: "none",
    messageId: "",
    aiTaskId: "",
    ...overrides,
  };
}

function toWriteItems(items: readonly HandoverItemDraft[]): TeamsHandoverItemWriteBody[] {
  return items.map((item) => ({
    itemKind: item.itemKind,
    title: item.title.trim(),
    body: item.body.trim(),
    messageId: item.linkKind === "message" && item.messageId.trim() ? item.messageId.trim() : null,
    aiTaskId: item.linkKind === "task" && item.aiTaskId.trim() ? item.aiTaskId.trim() : null,
  }));
}

export function TeamsHandoverForm({
  mode,
  handoverId = null,
  channels,
  lockedChannelId = null,
  initialShiftDate,
  initialShiftType = "morning",
  initialSummary = "",
  initialSafetyNote = "",
  initialItems,
}: TeamsHandoverFormProps) {
  const router = useRouter();
  const [channelId, setChannelId] = useState(lockedChannelId ?? channels[0]?.id ?? "");
  const [shiftDate, setShiftDate] = useState(initialShiftDate ?? todayIsoDate());
  const [shiftType, setShiftType] = useState(initialShiftType);
  const [summary, setSummary] = useState(initialSummary);
  const [safetyNote, setSafetyNote] = useState(initialSafetyNote);
  const [items, setItems] = useState<HandoverItemDraft[]>(
    initialItems && initialItems.length > 0 ? [...initialItems] : [emptyItem()],
  );
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  function updateItem(index: number, patch: Partial<HandoverItemDraft>) {
    setItems((current) => current.map((item, itemIndex) => (
      itemIndex === index ? { ...item, ...patch } : item
    )));
  }

  function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    setError(null);
    setPending(true);
    void (async () => {
      try {
        const payload = {
          channelId,
          shiftDate,
          shiftType,
          summary: summary.trim(),
          safetyNote: safetyNote.trim(),
          items: toWriteItems(items),
        };
        const result = mode === "edit" && handoverId
          ? await updateTeamsHandoverDraftAction({ handoverId, ...payload })
          : await createTeamsHandoverAction(payload);
        if (result.status === "failure") {
          setError(result.message);
          return;
        }
        router.push(`/teams/devretme/${result.handoverId}`);
        router.refresh();
      } catch (caught) {
        setError(caught instanceof Error ? caught.message : "Vardiya devri oluşturulamadı.");
      } finally {
        setPending(false);
      }
    })();
  }

  return (
    <form
      className="teams-task-draft-form"
      data-testid={mode === "edit" ? "teams-handover-edit-form" : "teams-handover-create-form"}
      data-pending={pending ? "true" : "false"}
      onSubmit={submit}
    >
      <label>
        Kanal <span className="zx-required" aria-hidden="true">*</span>
        <select
          data-testid="teams-handover-channel-input"
          disabled={pending || Boolean(lockedChannelId)}
          onChange={(event) => setChannelId(event.target.value)}
          required
          value={channelId}
        >
          {channels.map((channel) => (
            <option key={channel.id} value={channel.id}>
              {channel.name}
            </option>
          ))}
        </select>
      </label>

      <label>
        Vardiya tarihi <span className="zx-required" aria-hidden="true">*</span>
        <input
          data-testid="teams-handover-shift-date-input"
          disabled={pending}
          onChange={(event) => setShiftDate(event.target.value)}
          required
          type="date"
          value={shiftDate}
        />
      </label>

      <label>
        Vardiya türü <span className="zx-required" aria-hidden="true">*</span>
        <select
          data-testid="teams-handover-shift-type-input"
          disabled={pending}
          onChange={(event) => setShiftType(event.target.value)}
          required
          value={shiftType}
        >
          {Object.entries(TEAMS_HANDOVER_SHIFT_TYPES).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </label>

      <label>
        Özet <span className="zx-required" aria-hidden="true">*</span>
        <textarea
          data-testid="teams-handover-summary-input"
          disabled={pending}
          onChange={(event) => setSummary(event.target.value)}
          required
          rows={4}
          value={summary}
        />
      </label>

      <label>
        Güvenlik / dikkat notu
        <textarea
          data-testid="teams-handover-safety-note-input"
          disabled={pending}
          onChange={(event) => setSafetyNote(event.target.value)}
          rows={3}
          value={safetyNote}
        />
      </label>

      <fieldset>
        <legend>Devir maddeleri</legend>
        {items.map((item, index) => (
          <div
            className="teams-handover-item-editor"
            data-testid={`teams-handover-item-${index}`}
            key={`item-${index}`}
          >
            <label>
              Madde türü
              <select
                data-testid={`teams-handover-item-kind-${index}`}
                disabled={pending}
                onChange={(event) => updateItem(index, { itemKind: event.target.value })}
                value={item.itemKind}
              >
                {Object.entries(TEAMS_HANDOVER_ITEM_KINDS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Başlık
              <input
                data-testid={`teams-handover-item-title-${index}`}
                disabled={pending}
                onChange={(event) => updateItem(index, { title: event.target.value })}
                required
                type="text"
                value={item.title}
              />
            </label>
            <label>
              Açıklama
              <textarea
                data-testid={`teams-handover-item-body-${index}`}
                disabled={pending}
                onChange={(event) => updateItem(index, { body: event.target.value })}
                required
                rows={3}
                value={item.body}
              />
            </label>
            <label>
              Güvenli bağlantı
              <select
                data-testid={`teams-handover-item-link-kind-${index}`}
                disabled={pending}
                onChange={(event) => updateItem(index, {
                  linkKind: event.target.value as HandoverItemDraft["linkKind"],
                  messageId: event.target.value === "message" ? item.messageId : "",
                  aiTaskId: event.target.value === "task" ? item.aiTaskId : "",
                })}
                value={item.linkKind}
              >
                <option value="none">Yok</option>
                <option value="message">Mevcut Teams mesajı</option>
                <option value="task">Mevcut AI görevi</option>
              </select>
            </label>
            {item.linkKind === "message" ? (
              <label>
                Mesaj kimliği
                <input
                  data-testid={`teams-handover-item-message-${index}`}
                  disabled={pending}
                  onChange={(event) => updateItem(index, { messageId: event.target.value })}
                  type="text"
                  value={item.messageId}
                />
              </label>
            ) : null}
            {item.linkKind === "task" ? (
              <label>
                Görev kimliği
                <input
                  data-testid={`teams-handover-item-task-${index}`}
                  disabled={pending}
                  onChange={(event) => updateItem(index, { aiTaskId: event.target.value })}
                  type="text"
                  value={item.aiTaskId}
                />
              </label>
            ) : null}
            {items.length > 1 ? (
              <Button
                data-testid={`teams-handover-item-remove-${index}`}
                disabled={pending}
                onClick={() => setItems((current) => current.filter((_, itemIndex) => itemIndex !== index))}
                type="button"
              >
                Maddeyi kaldır
              </Button>
            ) : null}
          </div>
        ))}
        {items.length < 20 ? (
          <Button
            data-testid="teams-handover-item-add"
            disabled={pending}
            onClick={() => setItems((current) => [...current, emptyItem()])}
            type="button"
          >
            Madde ekle
          </Button>
        ) : null}
      </fieldset>

      <p className="teams-task-draft-form__disclaimer">{TEAMS_HANDOVER_DISCLAIMER}</p>

      {error ? (
        <Notice tone="danger" alert data-testid="teams-handover-form-error">{customerMessage(error)}</Notice>
      ) : null}

      <div>
        <Button
          data-testid="teams-handover-form-submit"
          disabled={pending || !channelId || !shiftDate || !summary.trim() || items.some((item) => !item.title.trim() || !item.body.trim())}
          type="submit"
        >
          {mode === "edit" ? "Taslağı kaydet" : "Vardiya devri oluştur"}
        </Button>
        <Button type="button" variant="secondary" onClick={() => router.push("/teams/devretme")}>
          Vazgeç
        </Button>
      </div>
    </form>
  );
}
