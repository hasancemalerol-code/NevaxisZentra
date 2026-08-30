"use client";

import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { MessageSquare, Send, UserRound } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type {
  RawTeamsDirectConversationDetail,
  RawTeamsDirectConversationListResponse,
  RawTeamsDirectRecipientListResponse,
} from "@/features/teams/server/teams-api-client";
import {
  createTeamsDirectConversationAction,
  markTeamsDirectConversationReadAction,
  sendTeamsDirectMessageAction,
} from "@/features/teams/server/teams-actions";
import { TEAMS_DIRECT_MESSAGE_DISCLAIMER } from "@/features/teams/domain/teams-constants";

type TeamsDirectMessagesClientProps = Readonly<{
  conversations: RawTeamsDirectConversationListResponse;
  recipients: RawTeamsDirectRecipientListResponse;
  selectedConversation: RawTeamsDirectConversationDetail | null;
  selectedConversationId: string | null;
  canWrite: boolean;
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

export function TeamsDirectMessagesClient({
  conversations,
  recipients,
  selectedConversation,
  selectedConversationId,
  canWrite,
}: TeamsDirectMessagesClientProps) {
  const router = useRouter();
  const [messageBody, setMessageBody] = useState("");
  const [selectedRecipientId, setSelectedRecipientId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const activeConversationId = selectedConversationId ?? selectedConversation?.id ?? null;
  const lastMessageId = selectedConversation?.messages.at(-1)?.id ?? null;

  useEffect(() => {
    if (!activeConversationId || !lastMessageId || !canWrite) {
      return;
    }

    void markTeamsDirectConversationReadAction({
      conversationId: activeConversationId,
      lastReadMessageId: lastMessageId,
    }).then(() => router.refresh());
  }, [activeConversationId, canWrite, lastMessageId, router]);

  const recipientOptions = useMemo(
    () => recipients.items.filter((recipient) =>
      !conversations.items.some((conversation) =>
        conversation.otherParticipantMembershipId === recipient.tenantMembershipId)),
    [conversations.items, recipients.items],
  );

  function selectConversation(conversationId: string) {
    router.push(`/teams/ozel-mesajlar?conversation=${encodeURIComponent(conversationId)}`);
  }

  async function startConversation() {
    if (!selectedRecipientId) {
      setError("Alıcı seçin.");
      return;
    }

    setPending(true);
    setError(null);
    const result = await createTeamsDirectConversationAction({
      recipientTenantMembershipId: selectedRecipientId,
    });
    setPending(false);

    if (result.status === "failure") {
      setError(result.message);
      return;
    }

    router.push(`/teams/ozel-mesajlar?conversation=${encodeURIComponent(result.conversationId)}`);
    router.refresh();
  }

  async function sendMessage(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!activeConversationId || !messageBody.trim()) {
      return;
    }

    setPending(true);
    setError(null);
    const result = await sendTeamsDirectMessageAction({
      conversationId: activeConversationId,
      body: messageBody.trim(),
    });
    setPending(false);

    if (result.status === "failure") {
      setError(result.message);
      return;
    }

    setMessageBody("");
    router.refresh();
  }

  return (
    <div className="teams-workspace" data-testid="teams-direct-messages-page">
      <aside className="teams-workspace-sidebar">
        <div className="teams-workspace-sidebar-header">
          <h2>Konuşmalar</h2>
        </div>
        <ul className="teams-channel-list" data-testid="teams-direct-conversation-list">
          {conversations.items.length === 0 ? (
            <li className="teams-empty-hint">Henüz özel konuşma yok.</li>
          ) : (
            conversations.items.map((conversation) => (
              <li key={conversation.id}>
                <button
                  type="button"
                  className={conversation.id === activeConversationId ? "is-active" : undefined}
                  data-testid={`teams-direct-conversation-${conversation.id}`}
                  onClick={() => selectConversation(conversation.id)}
                >
                  <span className="teams-channel-name">
                    <UserRound aria-hidden size={16} />
                    {conversation.otherParticipantLabel}
                  </span>
                  {conversation.latestMessagePreview ? (
                    <span className="teams-channel-preview">{conversation.latestMessagePreview}</span>
                  ) : null}
                  <span className="teams-channel-meta">
                    {formatWhen(conversation.latestMessageAt ?? conversation.createdAt)}
                    {conversation.unreadCount > 0 ? (
                      <Badge tone="information">{conversation.unreadCount}</Badge>
                    ) : null}
                  </span>
                </button>
              </li>
            ))
          )}
        </ul>

        {canWrite ? (
          <div className="teams-direct-compose" data-testid="teams-direct-start-conversation">
            <label htmlFor="teams-direct-recipient">Yeni konuşma</label>
            <select
              id="teams-direct-recipient"
              value={selectedRecipientId}
              onChange={(event) => setSelectedRecipientId(event.target.value)}
            >
              <option value="">Üye seçin</option>
              {recipientOptions.map((recipient) => (
                <option key={recipient.tenantMembershipId} value={recipient.tenantMembershipId}>
                  {recipient.displayLabel}
                </option>
              ))}
            </select>
            <Button disabled={pending || !selectedRecipientId} onClick={() => void startConversation()} type="button">
              Başlat
            </Button>
          </div>
        ) : null}
      </aside>

      <section className="teams-workspace-main">
        {selectedConversation ? (
          <>
            <header className="teams-workspace-header">
              <div>
                <h2>{selectedConversation.otherParticipantLabel}</h2>
                <p>{selectedConversation.totalMessageCount} mesaj</p>
              </div>
            </header>

            <div className="teams-message-thread" data-testid="teams-direct-message-thread">
              {selectedConversation.messages.length === 0 ? (
                <p className="teams-empty-hint">Henüz mesaj yok. İlk mesajı gönderin.</p>
              ) : (
                selectedConversation.messages.map((message) => (
                  <article
                    key={message.id}
                    className={message.isOwnMessage ? "teams-message is-own" : "teams-message"}
                    data-testid={`teams-direct-message-${message.id}`}
                  >
                    <header>
                      <strong>{message.authorLabel}</strong>
                      <time dateTime={message.createdAt}>{formatWhen(message.createdAt)}</time>
                    </header>
                    <p>{message.body}</p>
                  </article>
                ))
              )}
            </div>

            {canWrite && selectedConversation.canWrite ? (
              <form className="teams-message-form" onSubmit={(event) => void sendMessage(event)}>
                <label className="sr-only" htmlFor="teams-direct-message-body">Mesaj</label>
                <textarea
                  id="teams-direct-message-body"
                  data-testid="teams-direct-message-input"
                  placeholder="Mesajınızı yazın"
                  rows={3}
                  value={messageBody}
                  onChange={(event) => setMessageBody(event.target.value)}
                />
                <Button disabled={pending || !messageBody.trim()} type="submit">
                  <Send aria-hidden size={16} />
                  Gönder
                </Button>
              </form>
            ) : null}
          </>
        ) : (
          <div className="teams-empty-state">
            <MessageSquare aria-hidden size={28} />
            <p>Bir konuşma seçin veya yeni konuşma başlatın.</p>
          </div>
        )}

        {error ? <p className="teams-error" role="alert">{error}</p> : null}
        <p className="teams-disclaimer" data-testid="teams-direct-messages-disclaimer">
          {TEAMS_DIRECT_MESSAGE_DISCLAIMER}
        </p>
      </section>
    </div>
  );
}
