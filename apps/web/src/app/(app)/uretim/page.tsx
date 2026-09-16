import Link from "next/link";
import {
  BarChart3,
  CheckCircle2,
  Factory,
  Package,
  Pause,
  Play,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { ProductionOrderCard } from "@/features/production-execution/components/production-order-card";
import { readProductionOrders } from "@/features/production-execution/server/production-order-reader";
import type { ProductionOrderSummary } from "@/features/production-execution/domain/production-order";

function countByStatus(
  orders: ProductionOrderSummary[],
  status: string,
): number {
  return orders.filter((o) => o.status === status).length;
}

export default async function ProductionExecutionPage() {
  const result = await readProductionOrders();

  if (result.status === "failure") {
    return (
      <main className="fact-workspace production-execution-page">
        <header className="production-execution-header">
          <p>ÜRETİM YÖNETİMİ</p>
          <h1>Üretim Emirleri</h1>
        </header>

        <div role="alert" className="state-panel error-state">
          <div className="state-icon">
            <ShieldCheck aria-hidden="true" />
          </div>
          <div>
            <h3>Üretim emirleri yüklenemedi</h3>
            <p>
              Veriler alınırken bir hata oluştu. Lütfen sayfayı yenileyin veya
              sistem yöneticinize başvurun.
            </p>
          </div>
        </div>
      </main>
    );
  }

  const orders = result.orders;

  const totalCount = orders.length;
  const inProgressCount = countByStatus(orders, "in-progress");
  const readyCount = countByStatus(orders, "ready");
  const pausedCount = countByStatus(orders, "paused");
  const completedCount = countByStatus(orders, "completed");

  return (
    <main className="fact-workspace production-execution-page">
      <header className="production-execution-header">
        <p>ÜRETİM YÖNETİMİ</p>

        <h1>Üretim Emirleri</h1>

        <span>
          Planlamadan üretime aktarılan emirleri izleyin, yürütün ve
          operasyonel ilerlemeyi yönetin.
        </span>

        <Link href="/uretim/hatlar" className="production-execution-header__link">
          <Factory aria-hidden="true" />
          Üretim Hatları
        </Link>

        <Link href="/uretim/raporlama" className="production-execution-header__link">
          <BarChart3 aria-hidden="true" />
          Operasyonel Raporlama
        </Link>
      </header>

      <section className="production-execution-kpis" aria-label="Özet istatistikler">
        <Card className="production-execution-kpis__item--total">
          <Factory aria-hidden="true" />

          <div>
            <span>Toplam Emir</span>
            <strong>{totalCount}</strong>
            <small>Aktif üretim çalışma alanı</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--in-progress">
          <Play aria-hidden="true" />

          <div>
            <span>Üretimde</span>
            <strong>{inProgressCount}</strong>
            <small>Aktif yürütme altında</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--ready">
          <Package aria-hidden="true" />

          <div>
            <span>Hazır</span>
            <strong>{readyCount}</strong>
            <small>Başlatılmayı bekliyor</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--paused">
          <Pause aria-hidden="true" />

          <div>
            <span>Duraklatıldı</span>
            <strong>{pausedCount}</strong>
            <small>Devam etmeyi bekliyor</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--completed">
          <CheckCircle2 aria-hidden="true" />

          <div>
            <span>Tamamlandı</span>
            <strong>{completedCount}</strong>
            <small>Bu tesiste tamamlanan</small>
          </div>
        </Card>
      </section>

      <section className="production-execution-safety" aria-label="Operasyon bilgisi">
        <div>
          <ShieldCheck aria-hidden="true" />

          <div>
            <p>SHOP-FLOOR EXECUTION</p>

            <h2>
              Üretim emirleri kontrollü operasyon adımlarıyla yürütülür
            </h2>

            <span>
              Başlatma, duraklatma, üretim miktarı, fire ve tamamlama kayıtları
              yetkili kullanıcı işlemleriyle ve denetlenebilir geçmişle
              yönetilir.
            </span>
          </div>
        </div>

        <Badge tone="information">
          İzlenebilir Üretim
        </Badge>
      </section>

      <section className="production-execution-workspace" aria-label="Üretim emirleri listesi">
        <div className="production-execution-workspace__heading">
          <div>
            <p>ÜRETİM ÇALIŞMA ALANI</p>
            <h2>Aktif üretim emirleri</h2>
            <span>
              Üretim durumunu, planlanan miktarı, gerçekleşen üretimi ve
              operasyonel ilerlemeyi takip edin.
            </span>
          </div>
        </div>

        {orders.length === 0 ? (
          <div className="production-execution-empty" role="status" aria-label="Boş liste">
            <div className="production-execution-empty__icon">
              <Factory aria-hidden="true" />
            </div>

            <div>
              <p>ÜRETİM EMRİ YOK</p>
              <h2>Henüz üretime aktarılmış bir emir bulunmuyor</h2>
            </div>

            <p className="production-execution-empty__desc">
              Planlama modülünde hazırlanan üretim handoff kayıtları üretime
              aktarıldığında burada görüntülenir.
            </p>
          </div>
        ) : (
          <div className="production-execution-workspace__list">
            {orders.map((order) => (
              <ProductionOrderCard key={order.id} order={order} />
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
