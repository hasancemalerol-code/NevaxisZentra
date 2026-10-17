

import { ErpReconciliationView } from "@/features/erp/components/erp-reconciliation-view";
import { readErpReconciliation } from "@/features/erp/server/erp-reconciliation-reader";

import { ErpFailurePage } from "@/features/erp/components/erp-failure-page";

export default async function ErpEslestirmePage() {
  const result = await readErpReconciliation();

  if (result.status === "failure") {
    return (
      <ErpFailurePage
        title="Eşleştirme"
        description="Harici ERP müşteri ve ürün kayıtlarının Zentra operasyonel referanslarıyla eşleştirilmesi."
        listHref="/erp-entegrasyonlari/eslestirme"
        listLabel="Eşleştirmeye Dön"
        failedTitle="Eşleştirme verisi yüklenemedi"
        code={"code" in result ? result.code : undefined}
        message={result.message}
      />
    );
  }

  return (
    <ErpReconciliationView customers={result.customers} products={result.products} />
  );
}
