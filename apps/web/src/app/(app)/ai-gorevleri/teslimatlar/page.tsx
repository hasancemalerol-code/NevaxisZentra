import Link from "next/link";
import { Send } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { DataTable } from "@/components/ui/data-table";
import { FailureStatePage } from "@/components/ui/failure-state";
import { EmptyPanel } from "@/components/ui/empty-panel";
import { PageFrame, PageHeader } from "@/components/ui/page";
import { Panel } from "@/components/ui/panel";
import {
  getAiTaskDeliveryChannelLabel,
  getAiTaskDeliveryStatusLabel,
  getAiTaskWhatsAppConsentLabel,
  getAiTaskWhatsAppProviderStatusLabel,
  getAiTaskWhatsAppReadinessLabel,
  getAiTaskEmailProviderStatusLabel,
  getAiTaskEmailReadinessLabel,
} from "@/features/ai-task-center/presentation/ai-task-delivery-labels";
import { formatAiTaskShortDateTime } from "@/features/ai-task-center/presentation/ai-task-formatters";
import { readAiTaskDeliveryManagement } from "@/features/ai-task-center/server/ai-task-delivery-queries";
import "@/features/ai-task-center/components/ai-task-center-round3.css";

type PageProps = Readonly<{
  searchParams: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default async function AiTaskDeliveriesPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (typeof value === "string") {
      query.set(key, value);
    }
  }

  const management = await readAiTaskDeliveryManagement(query.toString());
  if (management.status === "failure") {
    return (
      <PageFrame data-page="ai-task-deliveries">
        <PageHeader
          eyebrow="GÖREV MERKEZİ"
          title="Teslimatlar"
          description="E-posta ve mesaj teslimat kayıtları ile kanal hazırlık durumu."
        />
        <FailureStatePage
          code={management.code}
          failedTitle="Teslimat bilgisi şu anda alınamıyor"
          backHref="/ai-gorevleri"
          backLabel="Görev Merkezi’ne dön"
          inline
        />
      </PageFrame>
    );
  }

  const { items, channels, totalCount } = management;
  const visibleChannels = channels.filter((channel) => channel.channelKey !== "development-loopback");

  return (
    <PageFrame data-page="ai-task-deliveries">
      <PageHeader
        eyebrow="GÖREV MERKEZİ"
        title="Teslimatlar"
        description="E-posta ve mesaj teslimat kayıtları ile kanal hazırlık durumu."
      />

      <Panel headingId="delivery-channels-heading" title="Teslimat kanalları" subtitle="Kanal hazırlığı yönetici yapılandırmasına bağlıdır.">
        <ul className="zx-channel-grid ai-task-delivery-channels" aria-label="Teslimat kanalı hazırlık durumu">
          {visibleChannels.map((channel) => {
            const readiness =
              channel.channelKey === "whatsapp"
                ? getAiTaskWhatsAppReadinessLabel(channel)
                : channel.channelKey === "email"
                  ? getAiTaskEmailReadinessLabel(channel)
                  : channel.available
                    ? "Hazır"
                    : "Yapılandırılmadı";
            return (
              <li key={channel.channelKey} className="zx-channel">
                <span className="zx-channel__name">{getAiTaskDeliveryChannelLabel(channel.channelKey)}</span>
                <Badge tone={channel.available ? "success" : "neutral"}>{readiness}</Badge>
              </li>
            );
          })}
        </ul>
      </Panel>

      <Panel
        flush
        headingId="delivery-records-heading"
        title="Teslimat kayıtları"
        subtitle={`${totalCount} kayıt`}
      >
        {items.length === 0 ? (
          <div className="zx-panel__body">
            <EmptyPanel
              icon={<Send />}
              title="Henüz teslimat kaydı yok"
              description="Bir görev için e-posta veya mesaj teslimatı istendiğinde kayıtlar burada listelenir."
            />
          </div>
        ) : (
          <DataTable label="Teslimat kayıtları" minWidth={900}>
            <thead>
              <tr>
                <th scope="col">Kanal</th>
                <th scope="col">Durum</th>
                <th scope="col">Sağlayıcı durumu</th>
                <th scope="col">Alıcı</th>
                <th scope="col">Onay</th>
                <th scope="col" className="zx-num">Deneme</th>
                <th scope="col">Hata</th>
                <th scope="col">İstenen</th>
                <th scope="col">Görev</th>
              </tr>
            </thead>
            <tbody>
              {items.map((delivery) => (
                <tr key={delivery.id}>
                  <td>{getAiTaskDeliveryChannelLabel(delivery.channel)}</td>
                  <td>
                    <Badge tone="neutral">{getAiTaskDeliveryStatusLabel(delivery.status)}</Badge>
                  </td>
                  <td>
                    {delivery.channel === "whatsapp"
                      ? getAiTaskWhatsAppProviderStatusLabel(delivery.providerStatus)
                      : delivery.channel === "email"
                        ? getAiTaskEmailProviderStatusLabel(delivery.providerStatus)
                        : "—"}
                  </td>
                  <td>{delivery.maskedRecipient ?? "—"}</td>
                  <td>
                    {delivery.channel === "whatsapp" ? getAiTaskWhatsAppConsentLabel(delivery.consentStatus) : "—"}
                  </td>
                  <td className="zx-num">{delivery.attemptCount}</td>
                  <td>{delivery.lastErrorCategory ? "Teslimat hatası" : "—"}</td>
                  <td>{formatAiTaskShortDateTime(delivery.requestedAtUtc)}</td>
                  <td>
                    <Link href={`/ai-gorevleri/${delivery.aiTaskId}`} className="zx-table-link">
                      Aç
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </DataTable>
        )}
      </Panel>
    </PageFrame>
  );
}
