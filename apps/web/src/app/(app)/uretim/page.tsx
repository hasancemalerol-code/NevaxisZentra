import Link from "next/link";
import {
  BarChart3,
  CheckCircle2,
  Factory,
  Package,
  Pause,
  Play,
} from "lucide-react";

import { Card } from "@/components/ui/card";
import { ProductionOrderCard } from "@/features/production-execution/components/production-order-card";
import { readProductionOrders } from "@/features/production-execution/server/production-order-reader";
import type { ProductionOrderSummary } from "@/features/production-execution/domain/production-order";
import { FailureStatePage } from "@/components/ui/failure-state";

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
      <div className="fact-workspace production-execution-page">
        <header className="production-execution-header">
          <p>ÜRETİM</p>
          <h1>Üretim Emirleri</h1>
        </header>

        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Üretim emirleri yüklenemedi" inline />
      </div>
    );
  }

  const orders = result.orders;

  const totalCount = orders.length;
  const inProgressCount = countByStatus(orders, "in-progress");
  const readyCount = countByStatus(orders, "ready");
  const pausedCount = countByStatus(orders, "paused");
  const completedCount = countByStatus(orders, "completed");

  return (
    <div className="fact-workspace production-execution-page">
      <header className="production-execution-header">
        <div>
          <p>ÜRETİM</p>
          <h1>Üretim Emirleri</h1>
          <span>
            Planlamadan üretime aktarılan emirleri izleyin, yürütün ve
            operasyonel ilerlemeyi yönetin.
          </span>
        </div>

        <div className="production-execution-header__actions">
          <Link href="/uretim/hatlar" className="production-execution-header__link">
            <Factory aria-hidden="true" />
            Üretim Hatları
          </Link>

          <Link href="/uretim/raporlama" className="production-execution-header__link">
            <BarChart3 aria-hidden="true" />
            Operasyonel Raporlama
          </Link>
        </div>
      </header>

      <section className="production-execution-kpis" aria-label="Özet istatistikler">
        <Card className="production-execution-kpis__item--total">
          <Factory aria-hidden="true" />

          <div>
            <span>Toplam Emir</span>
            <strong>{totalCount}</strong>
            <small>Tüm emirler</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--in-progress">
          <Play aria-hidden="true" />

          <div>
            <span>Üretimde</span>
            <strong>{inProgressCount}</strong>
            <small>Aktif yürütmede</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--ready">
          <Package aria-hidden="true" />

          <div>
            <span>Hazır</span>
            <strong>{readyCount}</strong>
            <small>Başlatma bekliyor</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--paused">
          <Pause aria-hidden="true" />

          <div>
            <span>Duraklatıldı</span>
            <strong>{pausedCount}</strong>
            <small>Devam bekliyor</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--completed">
          <CheckCircle2 aria-hidden="true" />

          <div>
            <span>Tamamlandı</span>
            <strong>{completedCount}</strong>
            <small>Tamamlanan emirler</small>
          </div>
        </Card>
      </section>

      <section className="production-execution-workspace" aria-label="Üretim emirleri listesi">
        <div className="production-execution-workspace__heading">
          <div>
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
              Planlama modülünde hazırlanan üretim devir kayıtları üretime
              aktarıldığında burada görüntülenir.
            </p>

            <Link href="/planlama" className="button button-primary button-medium">
              Plana git
            </Link>
          </div>
        ) : (
          <div className="production-execution-workspace__list">
            {orders.map((order) => (
              <ProductionOrderCard key={order.id} order={order} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
