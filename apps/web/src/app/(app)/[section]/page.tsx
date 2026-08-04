import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import { PageContent, PageHeader, Section } from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { navigationItems } from "@/lib/navigation/primary-navigation";

export function generateStaticParams() {
  return navigationItems
    .filter((item) => item.href !== "/")
    .map((item) => ({ section: item.href.slice(1) }));
}

export default async function SectionPlaceholderPage({ params }: Readonly<{ params: Promise<{ section: string }> }>) {
  const { section } = await params;
  const item = navigationItems.find((candidate) => candidate.href === `/${section}`);
  if (!item) {
    notFound();
  }

  return (
    <PageContent>
      <PageHeader
        eyebrow="Uygulama bölümü"
        title={item.label}
        description="Bu rota, kilitli ürün navigasyonunu ifade eden erişilebilir bir UI-01 yer tutucusudur. Ürün içeriği ve gerçek veri henüz uygulanmadı."
        preview
      />
      <Section title="İçerik sınırı" action={<Badge tone="neutral">Yer tutucu</Badge>}>
        <EmptyState
          title={`${item.label} içeriği daha sonra eklenecek`}
          description="Bu paket yalnızca masaüstü uygulama kabuğunu ve tasarım sistemi temelini oluşturur."
        />
      </Section>
    </PageContent>
  );
}
