import { StatePage, StatePageLink } from "@/components/ui/state-page";
import { resolveRequestPathname } from "@/features/saas-subscription/server/require-subscription-access";

const MODULE_RETURN: readonly (readonly [prefix: string, href: string, label: string, module: string])[] = [
  ["/bakim", "/bakim", "Bakım ana sayfasına dön", "Bakım ve Arıza"],
  ["/kalite", "/kalite", "Kalite ana sayfasına dön", "Kalite Kontrol"],
  ["/uretim", "/uretim", "Üretim emirlerine dön", "Üretim"],
  ["/planlama", "/planlama", "Planlara dön", "Üretim Planlama"],
  ["/siparisler", "/siparisler", "Siparişlere dön", "Sipariş Yönetimi"],
  ["/teams", "/teams", "Operasyon iletişimine dön", "Operasyon İletişimi"],
  ["/ihracat", "/ihracat", "İhracat ana sayfasına dön", "İhracat"],
  ["/ai-gorevleri", "/", "Görev Merkezi’ne dön", "Görev Merkezi"],
  ["/erp-entegrasyonlari", "/erp-entegrasyonlari/baglanti", "ERP bağlantısına dön", "ERP Entegrasyonları"],
  ["/ticari-zeka", "/ticari-zeka/karlilik-yonetim-merkezi", "Ticari zekâya dön", "Ticari Zekâ"],
  ["/ayarlar", "/ayarlar", "Ayarlar’a dön", "Ayarlar"],
  ["/operasyon/yonetim-merkezi", "/operasyon/yonetim-merkezi", "Yönetim Merkezi’ne dön", "Operasyon Yönetimi"],
  ["/fabrika-karar-merkezi", "/fabrika-karar-merkezi", "Karar Merkezi’ne dön", "Fabrika Karar Merkezi"],
  ["/kaliplar", "/kaliplar", "Kalıplara dön", "Kalıp ve Takımlar"],
  ["/malzemeler", "/malzemeler", "Malzemelere dön", "Malzeme Yönetimi"],
  ["/vardiyalar", "/vardiyalar", "Vardiyalara dön", "Vardiyalar"],
  ["/satin-alma", "/satin-alma/tedarik-riskleri", "Tedarik risklerine dön", "Satın Alma"],
  ["/stok-ve-depo", "/stok-ve-depo/malzeme-riskleri", "Malzeme risklerine dön", "Stok ve Depo"],
  ["/fabrika", "/fabrika", "Fabrika ekranına dön", "Fabrika"],
  ["/makineler", "/makineler", "Makinelere dön", "Makineler"],
];

/** Missing record or page: standard page header, a calm explanation and ONE primary way back. */
export default async function ApplicationNotFound() {
  const pathname = await resolveRequestPathname();
  const target = MODULE_RETURN.find(([prefix]) => pathname === prefix || pathname.startsWith(`${prefix}/`));

  return (
    <StatePage
      kind="not-found"
      moduleLabel={target ? target[3] : "Zentra"}
      title="Kayıt Bulunamadı"
      description="Aradığınız kayıt mevcut değil, kaldırılmış ya da yetki alanınızın dışında olabilir."
      actions={
        <StatePageLink href={target ? target[1] : "/"} primary>
          {target ? target[2] : "Bugün ekranına dön"}
        </StatePageLink>
      }
    />
  );
}
