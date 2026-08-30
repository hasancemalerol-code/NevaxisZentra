"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { Hash, MessageSquare, Send, Users } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type {
  RawTeamsChannelDetail,
  RawTeamsChannelListResponse,
} from "@/features/teams/server/teams-api-client";
import {
  markTeamsChannelReadAction,
  sendTeamsMessageAction,
} from "@/features/teams/server/teams-actions";
import { TeamsMessageTaskDraftForm } from "@/features/teams/components/teams-message-task-draft-form";
import { isHandoverChannelType, TEAMS_MESSAGE_TASK_DRAFT_DISCLAIMER } from "@/features/teams/domain/teams-constants";

type TeamsWorkspaceClientProps = Readonly<{
  channels: RawTeamsChannelListResponse;
  selectedChannel: RawTeamsChannelDetail | null;
  selectedChannelId: string | null;
  selectedMessageId: string | null;
  canWrite: boolean;
  canCreateTask: boolean;
  canCreateApproval: boolean;
  canCreateHandover: boolean;
}>;

const DATE_TIME = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "short",
  timeStyle: "short",
});

function formatWhen(value: string | null | undefined): string {
  if (!value) return "—";
  const parsed = Date.parse(value);
  return Number.isNaN(parsed) ? "—" : DATE_TIME.format(parsed);
}

export function TeamsWorkspaceClient({
  channels,
  selectedChannel,
  selectedChannelId,
  selectedMessageId,
  canWrite,
  canCreateTask,
  canCreateApproval,
  canCreateHandover,
}: TeamsWorkspaceClientProps) {
  const router = useRouter();
  const [messageBody, setMessageBody] = useState("");
  const [selectedMentions, setSelectedMentions] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [showTaskDraftForm, setShowTaskDraftForm] = useState(false);

  const activeChannelId = selectedChannelId ?? selectedChannel?.id ?? null;
  const activeMessage = selectedChannel?.messages.find((message) => message.id === selectedMessageId) ?? null;

  const mentionCandidates = useMemo(
    () => selectedChannel?.members ?? [],
    [selectedChannel?.members],
  );

  function selectChannel(channelId: string) {
    router.push(`/teams?channel=${encodeURIComponent(channelId)}`);
  }

  function selectMessage(channelId: string, messageId: string) {
    router.push(
      `/teams?channel=${encodeURIComponent(channelId)}&message=${encodeURIComponent(messageId)}`,
    );
  }

  function toggleMention(membershipId: string) {
    setSelectedMentions((current) =>
      current.includes(membershipId)
        ? current.filter((id) => id !== membershipId)
        : [...current, membershipId],
    );
  }

  function submitMessage() {
    if (!selectedChannel || !messageBody.trim() || pending) return;
    setError(null);
    setPending(true);
    void (async () => {
      try {
        const result = await sendTeamsMessageAction({
          channelId: selectedChannel.id,
          body: messageBody.trim(),
          mentionedMembershipIds: selectedMentions,
        });
        if (result.status === "failure") {
          setError(result.message);
          return;
        }
        setMessageBody("");
        setSelectedMentions([]);
        const lastMessageId = selectedChannel.messages.at(-1)?.id ?? null;
        await markTeamsChannelReadAction({
          channelId: selectedChannel.id,
          lastReadMessageId: lastMessageId,
        });
        router.push(`/teams?channel=${encodeURIComponent(selectedChannel.id)}`);
        router.refresh();
      } finally {
        setPending(false);
      }
    })();
  }

  function handleComposerKeyDown(event: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      submitMessage();
    }
  }

  return (
    <div className="teams-workspace" data-testid="teams-workspace">
      <aside className="teams-rail" data-testid="teams-channel-rail">
        <div className="teams-rail__header">
          <strong>Kanallar</strong>
          <p style={{ margin: "0.35rem 0 0", color: "#6b7280", fontSize: "0.875rem" }}>
            {channels.items.length} erişilebilir kanal
          </p>
        </div>
        <div className="teams-rail__list">
          {channels.items.length === 0 ? (
            <div className="teams-empty">Erişilebilir kanal yok.</div>
          ) : (
            channels.items.map((channel) => (
              <button
                key={channel.id}
                type="button"
                className="teams-rail__item"
                data-active={channel.id === activeChannelId}
                data-testid={`teams-channel-${channel.id}`}
                onClick={() => selectChannel(channel.id)}
              >
                <div className="teams-rail__item-title">
                  <span>
                    <Hash aria-hidden="true" style={{ display: "inline", width: 14, height: 14 }} />
                    {" "}
                    {channel.name}
                  </span>
                  {channel.unreadCount > 0 ? (
                    <Badge tone="information">{channel.unreadCount}</Badge>
                  ) : null}
                </div>
                {channel.latestMessage ? (
                  <div className="teams-rail__preview">
                    {channel.latestMessage.authorLabel}
                    {": "}
                    {channel.latestMessage.bodyPreview}
                  </div>
                ) : (
                  <div className="teams-rail__preview">Henüz mesaj yok</div>
                )}
              </button>
            ))
          )}
        </div>
      </aside>

      <section className="teams-panel" data-testid="teams-selected-channel">
        {!selectedChannel || (activeChannelId !== null && selectedChannel.id !== activeChannelId) ? (
          activeChannelId ? (
            <div className="teams-empty" data-testid="teams-selected-channel-loading">
              <MessageSquare aria-hidden="true" />
              <p>Kanal yükleniyor…</p>
            </div>
          ) : (
            <div className="teams-empty" data-testid="teams-selected-channel-empty">
              <MessageSquare aria-hidden="true" />
              <p>Bir kanal seçin veya erişilebilir kanal bekleyin.</p>
            </div>
          )
        ) : (
          <>
            <div
              className="teams-panel__header"
              data-testid="teams-selected-channel-ready"
              data-channel-id={selectedChannel.id}
            >
              <h2 style={{ margin: 0 }}>{selectedChannel.name}</h2>
              <div className="teams-members">
                <Users aria-hidden="true" size={14} />
                <span>{selectedChannel.members.length} üye</span>
                {selectedChannel.members.map((member) => (
                  <Badge key={member.tenantMembershipId} tone="neutral">
                    {member.displayLabel}
                  </Badge>
                ))}
              </div>
              <div style={{ marginTop: "0.75rem", display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
                {selectedChannel.taskContext?.canOpen && selectedChannel.taskContext.deepLinkPath ? (
                  <Link href={selectedChannel.taskContext.deepLinkPath} className="teams-context-chip">
                    Görevi aç
                  </Link>
                ) : null}
                {canCreateApproval && selectedChannel.aiTaskId ? (
                  <Link
                    href={`/teams/onaylar/yeni?channel=${encodeURIComponent(selectedChannel.id)}&task=${encodeURIComponent(selectedChannel.aiTaskId)}`}
                    className="teams-context-chip"
                    data-testid="teams-channel-request-approval"
                  >
                    Görevden onay iste
                  </Link>
                ) : null}
                <Link
                  href={`/teams/is-kutum`}
                  className="teams-context-chip"
                  data-testid="teams-channel-work-inbox-link"
                >
                  İş kutum
                </Link>
                <Link
                  href={`/teams/onaylar`}
                  className="teams-context-chip"
                  data-testid="teams-channel-approvals-link"
                >
                  Onay istekleri
                </Link>
                {isHandoverChannelType(selectedChannel.channelType) ? (
                  <>
                    <Link
                      href={`/teams/devretme`}
                      className="teams-context-chip"
                      data-testid="teams-channel-handovers-link"
                    >
                      Vardiya devri
                    </Link>
                    {canCreateHandover ? (
                      <Link
                        href={`/teams/devretme/yeni?channel=${encodeURIComponent(selectedChannel.id)}`}
                        className="teams-context-chip"
                        data-testid="teams-channel-create-handover"
                      >
                        Vardiya devri oluştur
                      </Link>
                    ) : null}
                  </>
                ) : null}
              </div>
              {selectedChannel.lastReadAt ? (
                <p style={{ margin: "0.5rem 0 0", color: "#6b7280", fontSize: "0.8125rem" }}>
                  Okundu: {formatWhen(selectedChannel.lastReadAt)}
                </p>
              ) : null}
            </div>

            <div className="teams-messages" data-testid="teams-message-timeline">
              {selectedChannel.messages.length === 0 ? (
                <div className="teams-empty">Bu kanalda henüz mesaj yok.</div>
              ) : (
                selectedChannel.messages.map((message) => (
                  <article
                    key={message.id}
                    className="teams-message"
                    data-selected={message.id === selectedMessageId ? "true" : "false"}
                    data-testid={`teams-message-${message.id}`}
                  >
                    <div className="teams-message__meta">
                      <strong>{message.authorLabel}</strong>
                      <span>{formatWhen(message.createdAt)}</span>
                    </div>
                    <button
                      type="button"
                      className="teams-message__select"
                      data-testid={`teams-message-select-${message.id}`}
                      onClick={() => selectMessage(selectedChannel.id, message.id)}
                    >
                      <p style={{ margin: 0, whiteSpace: "pre-wrap", textAlign: "left" }}>{message.body}</p>
                    </button>
                    {message.linkedTask ? (
                      <div
                        className="teams-message__linked-task"
                        data-testid={`teams-message-linked-task-${message.id}`}
                      >
                        {message.linkedTask.canOpenTask ? (
                          <Link href={message.linkedTask.taskDeepLinkPath} className="teams-context-chip">
                            Bağlı görevi aç
                          </Link>
                        ) : (
                          <span>Bağlı görev: {message.linkedTask.taskReference}</span>
                        )}
                        <span>{message.linkedTask.taskReference}</span>
                        <span>{message.linkedTask.taskStatus}</span>
                      </div>
                    ) : null}
                    {message.id === selectedMessageId ? (
                      <div className="teams-message__actions">
                        {!message.linkedTask ? (
                          canCreateTask ? (
                            <Button
                              type="button"
                              data-testid="teams-message-convert-to-task"
                              onClick={() => setShowTaskDraftForm(true)}
                            >
                              Göreve dönüştür
                            </Button>
                          ) : (
                            <p
                              style={{ margin: 0, color: "#6b7280", fontSize: "0.875rem" }}
                              data-testid="teams-message-task-create-denied"
                            >
                              Görev taslağı oluşturmak için ai-task.create yetkisi gerekir.
                            </p>
                          )
                        ) : null}
                        {canCreateApproval ? (
                          <Link
                            href={`/teams/onaylar/yeni?channel=${encodeURIComponent(selectedChannel.id)}&message=${encodeURIComponent(message.id)}`}
                            className="teams-context-chip"
                            data-testid="teams-message-request-approval"
                          >
                            Onay iste
                          </Link>
                        ) : (
                          <p
                            style={{ margin: 0, color: "#6b7280", fontSize: "0.875rem" }}
                            data-testid="teams-message-approval-create-denied"
                          >
                            Onay isteği için teams.approvals.create yetkisi gerekir.
                          </p>
                        )}
                        {canCreateHandover && isHandoverChannelType(selectedChannel.channelType) ? (
                          <Link
                            href={`/teams/devretme/yeni?channel=${encodeURIComponent(selectedChannel.id)}&message=${encodeURIComponent(message.id)}`}
                            className="teams-context-chip"
                            data-testid="teams-message-create-handover"
                          >
                            Devir maddesine ekle
                          </Link>
                        ) : null}
                      </div>
                    ) : null}
                    {message.sourceContext?.canOpen && message.sourceContext.deepLinkPath ? (
                      <div style={{ marginTop: "0.5rem" }}>
                        <Link href={message.sourceContext.deepLinkPath} className="teams-context-chip">
                          {message.sourceContext.label ?? "Bağlamı aç"}
                        </Link>
                      </div>
                    ) : null}
                    {message.mentions.length > 0 ? (
                      <div className="teams-message__mentions">
                        {message.mentions.map((mention) => (
                          <Badge key={mention.tenantMembershipId} tone="information">
                            @{mention.displayLabel}
                          </Badge>
                        ))}
                      </div>
                    ) : null}
                  </article>
                ))
              )}
            </div>

            {showTaskDraftForm && activeMessage ? (
              <TeamsMessageTaskDraftForm
                channelName={selectedChannel.name}
                disclaimer={TEAMS_MESSAGE_TASK_DRAFT_DISCLAIMER}
                message={activeMessage}
                onClose={() => setShowTaskDraftForm(false)}
              />
            ) : null}

            {canWrite ? (
              <form
                className="teams-composer"
                data-testid="teams-message-composer"
                data-pending={pending ? "true" : "false"}
                onSubmit={(event) => {
                  event.preventDefault();
                  submitMessage();
                }}
              >
                {mentionCandidates.length > 0 ? (
                  <div className="teams-members">
                    <span style={{ fontSize: "0.875rem" }}>Etiketle:</span>
                    {mentionCandidates.map((member) => (
                      <button
                        key={member.tenantMembershipId}
                        type="button"
                        onClick={() => toggleMention(member.tenantMembershipId)}
                      >
                        <Badge tone={selectedMentions.includes(member.tenantMembershipId) ? "information" : "neutral"}>
                          @{member.displayLabel}
                        </Badge>
                      </button>
                    ))}
                  </div>
                ) : null}
                <textarea
                  value={messageBody}
                  onChange={(event) => setMessageBody(event.target.value)}
                  onKeyDown={handleComposerKeyDown}
                  placeholder="Operasyon mesajınızı yazın…"
                  disabled={pending}
                  data-testid="teams-composer-input"
                />
                {error ? (
                  <p role="alert" style={{ color: "#b91c1c", margin: 0 }} data-testid="teams-composer-error">
                    {error}
                  </p>
                ) : null}
                {pending ? (
                  <p style={{ margin: 0, color: "#6b7280", fontSize: "0.875rem" }} data-testid="teams-composer-pending">
                    Mesaj gönderiliyor…
                  </p>
                ) : null}
                <div>
                  <Button
                    type="submit"
                    disabled={pending || !messageBody.trim()}
                    data-testid="teams-send-message"
                  >
                    <Send aria-hidden="true" />
                    Gönder
                  </Button>
                </div>
              </form>
            ) : (
              <div className="teams-composer teams-composer--readonly" data-testid="teams-read-only-notice">
                <p style={{ margin: 0, color: "#6b7280" }}>
                  Bu kanalda yalnızca okuma yetkiniz var.
                </p>
              </div>
            )}
          </>
        )}
      </section>

      <p className="teams-disclaimer">{channels.provenanceDisclaimer}</p>
    </div>
  );
}
