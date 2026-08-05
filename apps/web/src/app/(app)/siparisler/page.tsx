import {
  Bot,
  CheckCircle2,
  CircleAlert,
  FileText,
  Mail,
  MessageCircle,
  PackageCheck,
  RefreshCw,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { OrderDraftWorkspace } from "@/features/orders/components/order-draft-workspace";
import { ORDER_DRAFT_PREVIEW_DATA } from "@/features/orders/data/order-draft-preview-data";

const intakeSources = [
  {
    id: "email",
    label: "E-posta",
    description:
      "Sipariş mesajları ve ekli PDF, Excel veya görseller analiz edilir.",
    icon: Mail,
    status: "Bağlantı bekliyor",
  },
  {
    id: "whatsapp",
    label: "WhatsApp",
    description:
      "Müşteri mesajları ortak sipariş havuzuna aktarılır.",
    icon: MessageCircle,
    status: "Bağlantı bekliyor",
  },
  {
    id: "erp",
    label: "ERP",
    description:
      "Müşteri, ürün, fiyat, stok ve termin verileri doğrulanır.",
    icon: RefreshCw,
    status: "Adaptör bekliyor",
  },
] as const;

const workflowSteps = [
  {
    id: "collect",
    label: "Veri toplama",
    description:
      "Mesajlar, belgeler ve bağlı sistem kayıtları tek sipariş bağlamında birleştirilir.",
  },
  {
    id: "match",
    label: "Müşteri ve ürün eşleştirme",
    description:
      "Müşteri, ürün kodu, varyant, miktar ve teslimat bilgileri belirlenir.",
  },
  {
    id: "validate",
    label: "Risk ve eksik bilgi kontrolü",
    description:
      "Eksik, çelişkili veya düşük güvenli alanlar yetkili kullanıcıya gösterilir.",
  },
  {
    id: "approve",
    label: "Yetkili onayı",
    description:
      "Sipariş kullanıcı onayı olmadan kesinleştirilmez veya ERP'ye aktarılmaz.",
  },
] as const;

export default function OrdersPage() {
  return (
    <PageContent>
      <PageHeader
        description="E-posta, WhatsApp, ERP ve bağlı satış kanallarından gelen talepleri AI destekli sipariş taslaklarına dönüştürün."
        eyebrow="Omnichannel Sipariş Yönetimi"
        preview
        title="Siparişler"
      />

      <Section
        action={
          <Badge tone="ai">
            <Bot aria-hidden="true" />
            Human-in-the-Loop
          </Badge>
        }
        description="AI kaynak verileri toplar ve sipariş taslağını hazırlar. Son işlem yetkili kullanıcı onayına bağlıdır."
        title="AI sipariş toplama merkezi"
      >
        <div className="orders-intake-sources">
          {intakeSources.map((source) => {
            const Icon = source.icon;

            return (
              <Card
                className="orders-intake-source-card"
                key={source.id}
              >
                <div className="orders-intake-source-card__icon">
                  <Icon aria-hidden="true" />
                </div>

                <div>
                  <h2>{source.label}</h2>
                  <p>{source.description}</p>
                </div>

                <Badge tone="neutral">
                  {source.status}
                </Badge>
              </Card>
            );
          })}
        </div>
      </Section>

      <Section
        action={
          <Badge tone="information">
            <FileText aria-hidden="true" />
            {ORDER_DRAFT_PREVIEW_DATA.length} aktif taslak
          </Badge>
        }
        description="Farklı kanallardan toplanan sipariş taleplerini, AI güven puanlarını, açık sorunları ve onay durumlarını inceleyin."
        title="Sipariş taslakları"
      >
        <OrderDraftWorkspace
          orderDrafts={ORDER_DRAFT_PREVIEW_DATA}
        />
      </Section>

      <Section
        action={
          <Badge tone="information">
            <FileText aria-hidden="true" />
            Sipariş taslağı
          </Badge>
        }
        description="Her sipariş, kaynak mesajları ve AI çıkarımlarıyla birlikte izlenebilir biçimde hazırlanır."
        title="Sipariş oluşturma akışı"
      >
        <div className="orders-workflow">
          {workflowSteps.map((step, index) => (
            <Card
              className="orders-workflow-card"
              key={step.id}
            >
              <span className="orders-workflow-card__number">
                {index + 1}
              </span>

              <div>
                <h2>{step.label}</h2>
                <p>{step.description}</p>
              </div>
            </Card>
          ))}
        </div>
      </Section>

      <Section
        action={
          <Badge tone="success">
            <CheckCircle2 aria-hidden="true" />
            Kontrollü aktarım
          </Badge>
        }
        description="AI açıkça belirtilmeyen ürün, miktar, fiyat veya teslim şartını uyduramaz."
        title="Güvenlik sınırları"
      >
        <Card className="orders-safety-card">
          <CircleAlert aria-hidden="true" />

          <div>
            <h2>Yetkili onayı zorunludur</h2>
            <p>
              Düşük güvenli eşleşmeler otomatik kesinleştirilmez.
              Onaylanan sipariş sonradan değişirse yeniden kullanıcı
              onayı gerekir.
            </p>
          </div>

          <PackageCheck aria-hidden="true" />
        </Card>
      </Section>
    </PageContent>
  );
}