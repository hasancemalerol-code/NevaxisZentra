"use client";

import { useMemo, useState, useTransition } from "react";
import { Link2, Send, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import {
  getAiTaskDeliveryChannelLabel,
  getAiTaskDeliveryStatusLabel,
  getAiTaskWhatsAppConsentLabel,
  getAiTaskWhatsAppProviderStatusLabel,
  getAiTaskWhatsAppReadinessLabel,
  getAiTaskEmailProviderStatusLabel,
  getAiTaskEmailReadinessLabel,
  maskEmailForDisplay,
  AI_TASK_SECURE_ACCESS_TTL_OPTIONS,
} from "@/features/ai-task-center/presentation/ai-task-delivery-labels";
import { formatAiTaskShortDateTime } from "@/features/ai-task-center/presentation/ai-task-formatters";
import type {
  RawAiTaskDelivery,
  RawAiTaskDeliveryChannel,
  RawAiTaskSecureAccessGrant,
} from "@/features/ai-task-center/server/ai-task-delivery-api-client";
import {
  enqueueAiTaskDeliveryAction,
  issueAiTaskSecureAccessAction,
  revokeAiTaskSecureAccessAction,
  rotateAiTaskSecureAccessAction,
} from "@/features/ai-task-center/server/ai-task-delivery-actions";

type Props = Readonly<{
  taskId: string;
  deliveries: readonly RawAiTaskDelivery[];
  grants: readonly RawAiTaskSecureAccessGrant[];
  channels: readonly RawAiTaskDeliveryChannel[];
  canManage: boolean;
}>;

export function AiTaskDeliverySectionClient({
  taskId,
  deliveries,
  grants,
  channels,
  canManage,
}: Props) {
  const [issuedLink, setIssuedLink] = useState<string | null>(null);
  const [ttlPolicy, setTtlPolicy] = useState("24h");
  const [whatsappRecipient, setWhatsappRecipient] = useState("");
  const [whatsappConsentKnown, setWhatsappConsentKnown] = useState(false);
  const [emailRecipient, setEmailRecipient] = useState("");
  const [isPending, startTransition] = useTransition();

  const activeGrant = useMemo(
    () => grants.find((grant) => grant.isActive) ?? null,
    [grants],
  );

  const foundationChannels = channels.filter(
    (channel) => channel.channelKey !== "development-loopback",
  );

  const whatsappChannel = foundationChannels.find((channel) => channel.channelKey === "whatsapp");
  const emailChannel = foundationChannels.find((channel) => channel.channelKey === "email");
  const maskedEmailPreview = emailRecipient.includes("@")
    ? maskEmailForDisplay(emailRecipient.trim())
    : null;

  const loopbackChannel = channels.find((channel) => channel.channelKey === "development-loopback");
  const secureLinkState = issuedLink ?? activeGrant ? "Hazır" : "Önce güvenli bağlantı oluşturun";

  const readinessLabel = (channel: RawAiTaskDeliveryChannel) =>
    channel.channelKey === "whatsapp"
      ? getAiTaskWhatsAppReadinessLabel(channel)
      : channel.channelKey === "email"
        ? getAiTaskEmailReadinessLabel(channel)
        : channel.available
          ? "Hazır"
          : "Yapılandırılmadı";

  return (
    <section className="zx-panel zx-delivery" data-testid="ai-task-delivery-section" aria-labelledby="ai-task-delivery-heading">
      <header className="zx-panel__header">
        <div>
          <h2 id="ai-task-delivery-heading" className="zx-panel__title">Teslimat ve Güvenli Paylaşım</h2>
          <p className="zx-panel__subtitle">Kanal teslimatları ve güvenli bağlantı yönetimi.</p>
        </div>
        {canManage ? (
          <Badge tone="neutral">
            <ShieldCheck aria-hidden="true" />
            Yönetim
          </Badge>
        ) : null}
      </header>

      {canManage ? (
        <div className="zx-subpanel">
          <div className="zx-inline-form">
            <label className="zx-field">
              <span>Bağlantı süresi</span>
              <select value={ttlPolicy} onChange={(event) => setTtlPolicy(event.target.value)}>
                {AI_TASK_SECURE_ACCESS_TTL_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <Button
              type="button"
              disabled={isPending}
              leadingIcon={<Link2 />}
              onClick={() =>
                startTransition(async () => {
                  const result = await issueAiTaskSecureAccessAction(taskId, ttlPolicy);
                  if (result?.accessToken) {
                    setIssuedLink(`/gorev#access=${encodeURIComponent(result.accessToken)}`);
                  }
                })
              }
            >
              Güvenli bağlantı oluştur
            </Button>
            {activeGrant ? (
              <>
                <Button
                  type="button"
                  variant="secondary"
                  disabled={isPending}
                  onClick={() =>
                    startTransition(async () => {
                      await revokeAiTaskSecureAccessAction(activeGrant.id);
                      setIssuedLink(null);
                    })
                  }
                >
                  Bağlantıyı İptal Et
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  disabled={isPending}
                  onClick={() =>
                    startTransition(async () => {
                      const result = await rotateAiTaskSecureAccessAction(
                        activeGrant.id,
                        ttlPolicy,
                      );
                      if (result?.accessToken) {
                        setIssuedLink(`/gorev#access=${encodeURIComponent(result.accessToken)}`);
                      }
                    })
                  }
                >
                  Yeni Bağlantı Oluştur
                </Button>
              </>
            ) : null}
          </div>

          {issuedLink ? (
            <div className="zx-notice zx-notice--success">
              <ShieldCheck aria-hidden="true" />
              <div className="zx-notice__body">
                <span className="zx-notice__title">Güvenli Bağlantı (tek sefer gösterilir)</span>
                <p className="zx-code-box">{issuedLink}</p>
              </div>
            </div>
          ) : null}

          {activeGrant ? (
            <p className="zx-hint">Aktif bağlantı süresi: {formatAiTaskShortDateTime(activeGrant.expiresAtUtc)}</p>
          ) : null}
        </div>
      ) : null}

      <div className="zx-block">
        <h3 className="zx-block__title">Kanal hazırlığı</h3>
        <ul className="zx-channel-grid" aria-label="Kanal hazırlık durumu">
          {foundationChannels.map((channel) => (
            <li key={channel.channelKey} className="zx-channel">
              <span className="zx-channel__name">{getAiTaskDeliveryChannelLabel(channel.channelKey)}</span>
              <Badge tone={channel.available ? "success" : "neutral"}>{readinessLabel(channel)}</Badge>
            </li>
          ))}
        </ul>
      </div>

      {canManage ? (
        <div className="zx-subpanel">
          <h3 className="zx-subpanel__title">WhatsApp ile gönder</h3>
          {!whatsappChannel?.available ? (
            <p className="zx-hint">WhatsApp Business henüz yapılandırılmadı.</p>
          ) : (
            <div className="zx-form-stack">
              <label className="zx-field">
                <span>Alıcı telefon (E.164)</span>
                <input
                  placeholder="+90555..."
                  value={whatsappRecipient}
                  onChange={(event) => setWhatsappRecipient(event.target.value)}
                />
              </label>
              <label className="zx-check">
                <input
                  type="checkbox"
                  checked={whatsappConsentKnown}
                  onChange={(event) => setWhatsappConsentKnown(event.target.checked)}
                />
                <span>
                  Onay durumu: {getAiTaskWhatsAppConsentLabel(whatsappConsentKnown ? "known" : "unknown")}
                </span>
              </label>
              <p className="zx-hint">Güvenli bağlantı: {secureLinkState}</p>
              <div>
                <Button
                  type="button"
                  disabled={isPending || !issuedLink || !whatsappRecipient || !whatsappConsentKnown}
                  leadingIcon={<Send />}
                  onClick={() =>
                    startTransition(async () => {
                      await enqueueAiTaskDeliveryAction(taskId, {
                        channel: "whatsapp",
                        recipientKind: "external",
                        idempotencyKey: crypto.randomUUID(),
                        maskedRecipient: whatsappRecipient,
                        recipientE164: whatsappRecipient,
                        consentStatus: whatsappConsentKnown ? "known" : "unknown",
                        secureAccessGrantId: activeGrant?.id,
                        secureTaskUrl: issuedLink
                          ? new URL(issuedLink, window.location.origin).toString()
                          : undefined,
                      });
                    })
                  }
                >
                  WhatsApp ile gönder
                </Button>
              </div>
            </div>
          )}
        </div>
      ) : null}

      {canManage ? (
        <div className="zx-subpanel" data-testid="email-send-section">
          <h3 className="zx-subpanel__title">E-posta ile gönder</h3>
          {!emailChannel?.available && emailChannel?.reasonCode !== "degraded" ? (
            <p className="zx-hint">E-posta kanalı henüz yapılandırılmadı.</p>
          ) : (
            <div className="zx-form-stack">
              <label className="zx-field">
                <span>Alıcı e-posta</span>
                <input
                  placeholder="ornek@firma.com"
                  value={emailRecipient}
                  onChange={(event) => setEmailRecipient(event.target.value)}
                  data-testid="email-recipient-input"
                />
              </label>
              {maskedEmailPreview ? (
                <p className="zx-hint" data-testid="email-recipient-masked">
                  Maskelenmiş alıcı: {maskedEmailPreview}
                </p>
              ) : null}
              <p className="zx-hint">Güvenli bağlantı: {secureLinkState}</p>
              <div>
                <Button
                  type="button"
                  disabled={isPending || !issuedLink || !emailRecipient.includes("@")}
                  data-testid="email-send-button"
                  leadingIcon={<Send />}
                  onClick={() =>
                    startTransition(async () => {
                      await enqueueAiTaskDeliveryAction(taskId, {
                        channel: "email",
                        recipientKind: "external",
                        idempotencyKey: crypto.randomUUID(),
                        maskedRecipient: maskedEmailPreview ?? emailRecipient,
                        recipientEmail: emailRecipient.trim(),
                        recipientEligibility: "eligible",
                        secureAccessGrantId: activeGrant?.id,
                        secureTaskUrl: issuedLink
                          ? new URL(issuedLink, window.location.origin).toString()
                          : undefined,
                      });
                    })
                  }
                >
                  E-posta ile gönder
                </Button>
              </div>
            </div>
          )}
        </div>
      ) : null}

      <div className="zx-block">
        <div className="zx-block__head">
          <h3 className="zx-block__title">Teslimat geçmişi</h3>
          {canManage && loopbackChannel?.available ? (
            <Button
              type="button"
              variant="secondary"
              size="small"
              disabled={isPending}
              leadingIcon={<Send />}
              onClick={() =>
                startTransition(async () => {
                  await enqueueAiTaskDeliveryAction(taskId, {
                    channel: "development-loopback",
                    recipientKind: "secure-link",
                    idempotencyKey: crypto.randomUUID(),
                  });
                })
              }
            >
              Test teslimatı
            </Button>
          ) : null}
        </div>
        {deliveries.length === 0 ? (
          <p className="zx-hint">Henüz teslimat kaydı yok.</p>
        ) : (
          <DataTable label="Teslimat geçmişi" minWidth={620}>
            <thead>
              <tr>
                <th scope="col">Kanal</th>
                <th scope="col">Durum</th>
                <th scope="col">Sağlayıcı durumu</th>
                <th scope="col">İstenen</th>
                <th scope="col">Gönderildi</th>
              </tr>
            </thead>
            <tbody>
              {deliveries.map((delivery) => (
                <tr key={delivery.id}>
                  <td>{getAiTaskDeliveryChannelLabel(delivery.channel)}</td>
                  <td>{getAiTaskDeliveryStatusLabel(delivery.status)}</td>
                  <td>
                    {delivery.channel === "whatsapp"
                      ? getAiTaskWhatsAppProviderStatusLabel(delivery.providerStatus)
                      : delivery.channel === "email"
                        ? getAiTaskEmailProviderStatusLabel(delivery.providerStatus)
                        : "—"}
                  </td>
                  <td>{formatAiTaskShortDateTime(delivery.requestedAtUtc)}</td>
                  <td>{delivery.sentAtUtc ? formatAiTaskShortDateTime(delivery.sentAtUtc) : "—"}</td>
                </tr>
              ))}
            </tbody>
          </DataTable>
        )}
      </div>
    </section>
  );
}
