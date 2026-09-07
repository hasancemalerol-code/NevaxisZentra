import Link from "next/link";

import { PageContent, Section } from "@/components/ui/page";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  getAiTaskDeliveryChannelLabel,
  getAiTaskDeliveryStatusLabel,
} from "@/features/ai-task-center/presentation/ai-task-delivery-labels";
import { formatAiTaskShortDateTime } from "@/features/ai-task-center/presentation/ai-task-formatters";
import { readAiTaskDeliveryManagement } from "@/features/ai-task-center/server/ai-task-delivery-queries";

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

  const { items, channels, totalCount } = await readAiTaskDeliveryManagement(query.toString());

  return (
    <PageContent>
      <Section
        title="Teslimatlar"
        description="AI Görev Merkezi teslimat operasyonları — kanonik görev kaydı dışında bağımsız motor değildir."
        action={
          <Link href="/ai-gorevleri" className="text-sm text-primary hover:underline">
            Görev Merkezi
          </Link>
        }
      >
        <div className="mb-4 grid gap-2 sm:grid-cols-3">
          {channels
            .filter((channel) => channel.channelKey !== "development-loopback")
            .map((channel) => (
              <Card key={channel.channelKey} className="p-3 text-sm">
                <div className="font-medium">
                  {getAiTaskDeliveryChannelLabel(channel.channelKey)}
                </div>
                <div className="text-muted-foreground">
                  {channel.available ? "Hazır" : "Kanal yapılandırılmadı"}
                </div>
              </Card>
            ))}
        </div>

        <p className="mb-3 text-sm text-muted-foreground">{totalCount} kayıt</p>
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-muted/40 text-left">
              <tr>
                <th className="px-3 py-2">Kanal</th>
                <th className="px-3 py-2">Durum</th>
                <th className="px-3 py-2">Alıcı</th>
                <th className="px-3 py-2">İstenen</th>
                <th className="px-3 py-2">Görev</th>
              </tr>
            </thead>
            <tbody>
              {items.map((delivery) => (
                <tr key={delivery.id} className="border-t">
                  <td className="px-3 py-2">
                    {getAiTaskDeliveryChannelLabel(delivery.channel)}
                  </td>
                  <td className="px-3 py-2">
                    <Badge tone="neutral">
                      {getAiTaskDeliveryStatusLabel(delivery.status)}
                    </Badge>
                  </td>
                  <td className="px-3 py-2">{delivery.maskedRecipient ?? "—"}</td>
                  <td className="px-3 py-2">
                    {formatAiTaskShortDateTime(delivery.requestedAtUtc)}
                  </td>
                  <td className="px-3 py-2">
                    <Link
                      href={`/ai-gorevleri/${delivery.aiTaskId}`}
                      className="text-primary hover:underline"
                    >
                      Aç
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Section>
    </PageContent>
  );
}
