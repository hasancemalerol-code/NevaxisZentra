"use client";

import { useMemo, useState, useTransition } from "react";
import { Link2, Send, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  getAiTaskDeliveryChannelLabel,
  getAiTaskDeliveryStatusLabel,
  getAiTaskWhatsAppConsentLabel,
  getAiTaskWhatsAppProviderStatusLabel,
  getAiTaskWhatsAppReadinessLabel,
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
  const [isPending, startTransition] = useTransition();

  const activeGrant = useMemo(
    () => grants.find((grant) => grant.isActive) ?? null,
    [grants],
  );

  const foundationChannels = channels.filter(
    (channel) => channel.channelKey !== "development-loopback",
  );

  const whatsappChannel = foundationChannels.find((channel) => channel.channelKey === "whatsapp");

  return (
    <Card className="border-border/70 bg-card/80 p-5 shadow-sm" data-testid="ai-task-delivery-section">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-base font-semibold tracking-tight">
            Teslimat ve Güvenli Paylaşım
          </h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Kanal teslimatları ve güvenli bağlantı yönetimi.
          </p>
        </div>
        {canManage ? (
          <Badge tone="neutral" className="gap-1">
            <ShieldCheck className="size-3.5" />
            Yönetim
          </Badge>
        ) : null}
      </div>

      {canManage ? (
        <div className="mt-5 grid gap-4 rounded-lg border border-border/60 bg-muted/20 p-4">
          <div className="flex flex-wrap items-end gap-3">
            <label className="grid gap-1 text-sm">
              <span className="font-medium">Bağlantı Süresi</span>
              <select
                className="rounded-md border border-input bg-background px-3 py-2"
                value={ttlPolicy}
                onChange={(event) => setTtlPolicy(event.target.value)}
              >
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
              onClick={() =>
                startTransition(async () => {
                  const result = await issueAiTaskSecureAccessAction(taskId, ttlPolicy);
                  if (result?.accessToken) {
                    setIssuedLink(`/gorev#access=${encodeURIComponent(result.accessToken)}`);
                  }
                })
              }
            >
              <Link2 className="mr-2 size-4" />
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
            <div className="rounded-md border border-emerald-500/30 bg-emerald-500/5 p-3 text-sm">
              <p className="font-medium text-emerald-700">Güvenli Bağlantı (tek sefer gösterilir)</p>
              <p className="mt-2 break-all font-mono text-xs">{issuedLink}</p>
            </div>
          ) : null}

          {activeGrant ? (
            <p className="text-sm text-muted-foreground">
              Aktif bağlantı süresi: {formatAiTaskShortDateTime(activeGrant.expiresAtUtc)}
            </p>
          ) : null}
        </div>
      ) : null}

      <div className="mt-5">
        <h3 className="text-sm font-medium">Kanal Hazırlığı</h3>
        <div className="mt-2 grid gap-2 sm:grid-cols-2">
          {foundationChannels.map((channel) => (
            <div
              key={channel.channelKey}
              className="flex items-center justify-between rounded-md border border-border/60 px-3 py-2 text-sm"
            >
              <span>{getAiTaskDeliveryChannelLabel(channel.channelKey)}</span>
              <span className="text-muted-foreground">
                {channel.channelKey === "whatsapp"
                  ? getAiTaskWhatsAppReadinessLabel(channel)
                  : channel.available
                    ? "Hazır"
                    : "Henüz yapılandırılmadı"}
              </span>
            </div>
          ))}
        </div>
      </div>

      {canManage ? (
        <div className="mt-5 rounded-lg border border-border/60 bg-muted/10 p-4">
          <h3 className="text-sm font-medium">WhatsApp ile gönder</h3>
          {!whatsappChannel?.available ? (
            <p className="mt-2 text-sm text-muted-foreground">
              WhatsApp Business henüz yapılandırılmadı.
            </p>
          ) : (
            <div className="mt-3 grid gap-3">
              <label className="grid gap-1 text-sm">
                <span className="font-medium">Alıcı telefon (E.164)</span>
                <input
                  className="rounded-md border border-input bg-background px-3 py-2"
                  placeholder="+90555..."
                  value={whatsappRecipient}
                  onChange={(event) => setWhatsappRecipient(event.target.value)}
                />
              </label>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={whatsappConsentKnown}
                  onChange={(event) => setWhatsappConsentKnown(event.target.checked)}
                />
                <span>
                  Onay durumu: {getAiTaskWhatsAppConsentLabel(whatsappConsentKnown ? "known" : "unknown")}
                </span>
              </label>
              <p className="text-sm text-muted-foreground">
                Güvenli bağlantı: {issuedLink ?? activeGrant ? "Hazır" : "Önce güvenli bağlantı oluşturun"}
              </p>
              <Button
                type="button"
                disabled={isPending || !issuedLink || !whatsappRecipient || !whatsappConsentKnown}
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
                <Send className="mr-2 size-3.5" />
                WhatsApp ile gönder
              </Button>
            </div>
          )}
        </div>
      ) : null}

      <div className="mt-5">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-medium">Teslimat Geçmişi</h3>
          {canManage ? (
            <Button
              type="button"
              variant="secondary"
              disabled={isPending}
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
              <Send className="mr-2 size-3.5" />
              Test Teslimat
            </Button>
          ) : null}
        </div>
        {deliveries.length === 0 ? (
          <p className="mt-2 text-sm text-muted-foreground">Henüz teslimat kaydı yok.</p>
        ) : (
          <div className="mt-2 overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-muted-foreground">
                  <th className="py-2 pr-3">Kanal</th>
                  <th className="py-2 pr-3">Durum</th>
                  <th className="py-2 pr-3">Sağlayıcı Durumu</th>
                  <th className="py-2 pr-3">İstenen</th>
                  <th className="py-2 pr-3">Gönderildi</th>
                </tr>
              </thead>
              <tbody>
                {deliveries.map((delivery) => (
                  <tr key={delivery.id} className="border-b border-border/50">
                    <td className="py-2 pr-3">
                      {getAiTaskDeliveryChannelLabel(delivery.channel)}
                    </td>
                    <td className="py-2 pr-3">
                      {getAiTaskDeliveryStatusLabel(delivery.status)}
                    </td>
                    <td className="py-2 pr-3">
                      {delivery.channel === "whatsapp"
                        ? getAiTaskWhatsAppProviderStatusLabel(delivery.providerStatus)
                        : "—"}
                    </td>
                    <td className="py-2 pr-3">
                      {formatAiTaskShortDateTime(delivery.requestedAtUtc)}
                    </td>
                    <td className="py-2 pr-3">
                      {delivery.sentAtUtc
                        ? formatAiTaskShortDateTime(delivery.sentAtUtc)
                        : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Card>
  );
}
