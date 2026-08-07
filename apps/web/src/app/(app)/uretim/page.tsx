import {
  CheckCircle2,
  Factory,
  Pause,
  Play,
} from "lucide-react";

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
    throw new Error(
      [
        "Üretim emirleri listesi yüklenemedi.",
        result.code,
        result.message,
      ].join(" "),
    );
  }

  const orders = result.orders;

  const inProgressCount = countByStatus(orders, "in-progress");
  const readyCount = countByStatus(orders, "ready");
  const pausedCount = countByStatus(orders, "paused");
  const completedCount = countByStatus(orders, "completed");

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>Üretim Yönetimi</p>

        <h1>Üretim Emirleri</h1>

        <span>
          Planlama onaylı üretim emirlerini izleyin, başlatın,
          duraklatın ve tamamlayın.
        </span>
      </header>

      <section className="production-execution-kpis">
        <Card>
          <Play aria-hidden="true" />

          <div>
            <span>Üretimde</span>
            <strong>{inProgressCount}</strong>
            <small>Aktif üretim emri</small>
          </div>
        </Card>

        <Card>
          <Factory aria-hidden="true" />

          <div>
            <span>Hazır</span>
            <strong>{readyCount}</strong>
            <small>Başlatılmayı bekliyor</small>
          </div>
        </Card>

        <Card>
          <Pause aria-hidden="true" />

          <div>
            <span>Duraklatıldı</span>
            <strong>{pausedCount}</strong>
            <small>Devam etmeyi bekliyor</small>
          </div>
        </Card>

        <Card>
          <CheckCircle2 aria-hidden="true" />

          <div>
            <span>Tamamlandı</span>
            <strong>{completedCount}</strong>
            <small>Bu tesiste tamamlanan</small>
          </div>
        </Card>
      </section>

      {orders.length === 0 ? (
        <section className="production-execution-empty">
          <Factory aria-hidden="true" />

          <div>
            <h2>Üretim emri bulunamadı</h2>

            <p>
              Bu tesiste henüz tüketilmiş bir üretim planlama el değişimi
              bulunmuyor. Planlama onaylı bir plan için el değişimi
              hazırlandıktan sonra üretim emirleri burada görünür.
            </p>
          </div>
        </section>
      ) : (
        <section className="production-execution-list">
          {orders.map((order) => (
            <ProductionOrderCard key={order.id} order={order} />
          ))}
        </section>
      )}
    </main>
  );
}
