"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  AI_TASK_CATEGORIES,
  AI_TASK_PRIORITIES,
  type AiTaskCategory,
  type AiTaskPriority,
} from "@/features/ai-task-center/domain/ai-task";
import {
  getAiTaskCategoryLabel,
  getAiTaskPriorityLabel,
} from "@/features/ai-task-center/presentation/ai-task-labels";
import { createMessageTaskDraftAction } from "@/features/teams/server/teams-actions";
import type { RawTeamsMessage } from "@/features/teams/server/teams-api-client";
import { customerMessage } from "@/lib/presentation/customer-message";
import { Notice } from "@/components/ui/notice";

type TeamsMessageTaskDraftFormProps = Readonly<{
  message: RawTeamsMessage;
  channelName: string;
  disclaimer: string;
  onClose: () => void;
}>;

export function TeamsMessageTaskDraftForm({
  message,
  channelName,
  disclaimer,
  onClose,
}: TeamsMessageTaskDraftFormProps) {
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [category, setCategory] = useState<AiTaskCategory>("production-planning");
  const [priority, setPriority] = useState<AiTaskPriority>("medium");
  const [humanNote, setHumanNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  function submitDraft(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;

    setError(null);
    setPending(true);
    void (async () => {
      try {
        const result = await createMessageTaskDraftAction({
          messageId: message.id,
          title: title.trim(),
          category,
          priority,
          humanNote: humanNote.trim() || null,
        });
        if (result.status === "failure") {
          setError(result.message);
          return;
        }
        router.push(`/ai-gorevleri/${result.taskId}`);
        router.refresh();
      } finally {
        setPending(false);
      }
    })();
  }

  return (
    <form
      className="teams-task-draft-form"
      data-testid="teams-message-task-draft-form"
      data-pending={pending ? "true" : "false"}
      onSubmit={submitDraft}
    >
      <div className="teams-task-draft-form__header">
        <strong>Görev taslağı</strong>
        <button type="button" onClick={onClose}>Kapat</button>
      </div>

      <div className="teams-task-draft-form__source" data-testid="teams-message-task-source-preview">
        <p style={{ margin: "0 0 0.35rem", fontSize: "0.8125rem", color: "#6b7280" }}>
          Kaynak mesaj — {channelName}
        </p>
        <p style={{ margin: 0, whiteSpace: "pre-wrap" }}>{message.body}</p>
      </div>

      <label>
        Görev başlığı <span className="zx-required" aria-hidden="true">*</span>
        <input
          data-testid="teams-message-task-title-input"
          disabled={pending}
          onChange={(event) => setTitle(event.target.value)}
          required
          type="text"
          value={title}
        />
      </label>

      <label>
        Kategori
        <select
          data-testid="teams-message-task-category-input"
          disabled={pending}
          onChange={(event) => setCategory(event.target.value as AiTaskCategory)}
          value={category}
        >
          {AI_TASK_CATEGORIES.map((value) => (
            <option key={value} value={value}>
              {getAiTaskCategoryLabel(value)}
            </option>
          ))}
        </select>
      </label>

      <label>
        Öncelik
        <select
          data-testid="teams-message-task-priority-input"
          disabled={pending}
          onChange={(event) => setPriority(event.target.value as AiTaskPriority)}
          value={priority}
        >
          {AI_TASK_PRIORITIES.map((value) => (
            <option key={value} value={value}>
              {getAiTaskPriorityLabel(value)}
            </option>
          ))}
        </select>
      </label>

      <label>
        İsteğe bağlı not
        <textarea
          data-testid="teams-message-task-note-input"
          disabled={pending}
          onChange={(event) => setHumanNote(event.target.value)}
          rows={2}
          value={humanNote}
        />
      </label>

      <p className="teams-task-draft-form__disclaimer">{disclaimer}</p>

      {error ? (
        <Notice tone="danger" alert data-testid="teams-message-task-draft-error">{customerMessage(error)}</Notice>
      ) : null}

      {pending ? (
        <p style={{ margin: 0, color: "#6b7280" }} data-testid="teams-message-task-draft-pending">
          Görev taslağı oluşturuluyor…
        </p>
      ) : null}

      <div>
        <Button
          data-testid="teams-message-task-draft-submit"
          disabled={pending || !title.trim()}
          type="submit"
        >
          Görev taslağını oluştur
        </Button>
      </div>
    </form>
  );
}
