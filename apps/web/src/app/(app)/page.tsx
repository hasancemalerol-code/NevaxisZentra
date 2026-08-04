import { Activity, Clock3, ListChecks, RefreshCw, ShieldAlert, Sparkles } from "lucide-react";

import { Badge, StatusBadge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, KpiCard } from "@/components/ui/card";
import { PageContent, PageHeader, Section } from "@/components/ui/page";
import { EmptyState, Skeleton } from "@/components/ui/states";
import { getWebApplicationConfig } from "@/config";

export default function HomePage() {
  const config = getWebApplicationConfig();

  return (
    <PageContent>
      <PageHeader
        eyebrow="AI Görev Merkezi"
        title="Operasyon çalışma alanı"
        description={`${config.applicationName} için masaüstü uygulama kabuğu ve temel tasarım sistemi. Bu ekrandaki değerler yalnızca yerleşim önizlemesidir.`}
        preview
        actions={
          <Button variant="secondary" leadingIcon={<RefreshCw />} disabled title="Gerçek veri bağlantısı UI-02 ve sonraki paketlere ertelendi">
            Veriyi yenile
          </Button>
        }
      />

      <div className="kpi-grid" aria-label="KPI kartı bileşen önizlemeleri">
        <KpiCard label="Açık görev" value="—" detail="Gerçek görev verisi bağlı değil" icon={<ListChecks />} tone="information" preview />
        <KpiCard label="İnceleme bekleyen" value="—" detail="Onay iş akışı eklenmedi" icon={<Clock3 />} tone="warning" preview />
        <KpiCard label="Kritik risk" value="—" detail="Operasyon verisi bağlı değil" icon={<ShieldAlert />} tone="neutral" preview />
        <KpiCard label="Sistem görünümü" value="Hazır" detail="Yalnızca UI kabuğu" icon={<Activity />} tone="success" preview />
      </div>

      <div className="preview-grid">
        <Section
          title="Görev çalışma alanı"
          description="AI Task Center içeriği ve gerçek görev iş akışları sonraki ürün paketlerine ertelenmiştir."
          action={<Badge tone="ai"><Sparkles aria-hidden="true" /> AI alanı</Badge>}
        >
          <EmptyState
            title="Önizleme için görev bulunmuyor"
            description="Bu boş durum, gerçek veri olmadığında kullanılacak erişilebilir sunumu gösterir."
            action={<Button variant="secondary" size="small" disabled>Görev oluşturma daha sonra</Button>}
          />
        </Section>

        <Section title="Sistem durumu" description="Durum ve rozet bileşenlerinin temsili görünümü.">
          <Card className="status-list-card">
            <div className="status-row"><span>Web uygulama kabuğu</span><StatusBadge status="normal" label="Hazır" /></div>
            <div className="status-row"><span>Operasyonel veri</span><StatusBadge status="offline" label="Bağlı değil" /></div>
            <div className="status-row"><span>Dashboard uygulaması</span><StatusBadge status="analysis" label="UI-02" /></div>
          </Card>
        </Section>
      </div>

      <Section title="Yükleme durumu" description="Kart ve içerik alanları için düşük hareketli iskelet önizlemesi.">
        <Card className="skeleton-card" aria-label="Yükleme bileşeni önizlemesi">
          <Skeleton className="skeleton-heading" />
          <Skeleton className="skeleton-line" />
          <Skeleton className="skeleton-line skeleton-line-short" />
        </Card>
      </Section>
    </PageContent>
  );
}
