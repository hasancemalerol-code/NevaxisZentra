import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { navigationItems } from "@/lib/navigation/primary-navigation";

type RouteParams = Readonly<{
  sections: string[];
}>;

export function generateStaticParams() {
  return navigationItems
    .filter(
      (item) =>
        item.href !== "/"
        && item.href !== "/ayarlar"
        && item.href !== "/enerji"
        && item.href !== "/iot-plc"
        && item.href !== "/operasyonel-riskler"
        && item.href !== "/ticari-zeka/satin-alma-fiyat-analizi"
        && item.href !== "/ticari-zeka/tedarikci-karsilastirmasi"
        && item.href !== "/ticari-zeka/satis-fiyat-analizi"
        && item.href !== "/ticari-zeka/teklif-riskleri"
        && item.href !== "/ticari-zeka/urun-karliligi"
        && item.href !== "/ticari-zeka/musteri-karliligi"
        && item.href !== "/ticari-zeka/malzeme-ve-maliyet-karliligi"
        && item.href !== "/ticari-zeka/karlilik-uyarilari"
        && item.href !== "/ticari-zeka/fiyat-koruma-ve-onay"
        && item.href !== "/ticari-zeka/karlilik-yonetim-merkezi"
        && item.href !== "/operasyon/yonetim-merkezi"
        && item.href !== "/teams"
        && item.href !== "/dijital-is-gucu",
    )
    .map((item) => ({
      sections: item.href
        .split("/")
        .filter(Boolean),
    }));
}

export default async function SectionPlaceholderPage({
  params,
}: Readonly<{
  params: Promise<RouteParams>;
}>) {
  const { sections } = await params;
  const href = `/${sections.join("/")}`;

  const item = navigationItems.find(
    (candidate) => candidate.href === href,
  );

  if (!item) {
    notFound();
  }

  return (
    <PageContent>
      <PageHeader
        description="Bu bölüm, CrevaFactOS ürün navigasyonunda tanımlı route sözleşmesini temsil eder. Modül içeriği sonraki geliştirme paketlerinde uygulanacaktır."
        eyebrow="Uygulama bölümü"
        preview
        title={item.label}
      />

      <Section
        action={
          <Badge tone="neutral">
            Yer tutucu
          </Badge>
        }
        title="İçerik sınırı"
      >
        <EmptyState
          description="Bu sayfa şu anda yalnızca navigasyon, route çözümleme ve uygulama kabuğu doğrulaması için kullanılmaktadır."
          title={`${item.label} içeriği sonraki paketlerde eklenecek`}
        />
      </Section>
    </PageContent>
  );
}