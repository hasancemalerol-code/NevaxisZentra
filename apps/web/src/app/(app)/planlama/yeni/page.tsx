import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { parseProductionPlanRuntimeConfig } from "@/features/production-planning/server/production-plan-runtime-config";
import { resolveProductionPlanningServerScope } from "@/features/production-planning/server/production-plan-scope";
import { ProductionPlanDraftForm } from "@/features/production-planning/components/production-plan-draft-form";
import { writeCreateProductionPlanDraft } from "@/features/production-planning/server/production-plan-draft-writer";
import type { ProductionPlanDraftFormValues } from "@/features/production-planning/components/production-plan-draft-form";

export default async function ProductionPlanCreatePage() {
  const configResult = parseProductionPlanRuntimeConfig();

  if (configResult.status === "failure") {
    throw new Error(
      [
        "Production planning configuration could not be resolved.",
        configResult.code,
        configResult.message,
      ].join(" "),
    );
  }

  const config = configResult.config;
  const previewWriteBlocked = config.mode === "development-preview";

  const scopeResult = resolveProductionPlanningServerScope(config);

  const facilityId =
    scopeResult.status === "success"
      ? scopeResult.scope.facilityId
      : config.facilityId ?? "";

  async function handleCreate(
    values: ProductionPlanDraftFormValues,
    _expectedVersion: number | undefined,
  ) {
    "use server";

    const quantity = parseFloat(values.demandQuantity);

    if (Number.isNaN(quantity) || quantity <= 0) {
      return {
        status: "failure" as const,
        code: "validation-failed",
        message: "Miktar geçersiz.",
        fieldErrors: [
          {
            field: "demands[0].requiredQuantity",
            code: "invalid-value",
            message: "Miktar sıfırdan büyük olmalıdır.",
          },
        ],
      };
    }

    const startUtc = values.plannedStartAtUtc
      ? `${values.plannedStartAtUtc}:00Z`
      : "";
    const endUtc = values.plannedEndAtUtc
      ? `${values.plannedEndAtUtc}:00Z`
      : "";

    const result = await writeCreateProductionPlanDraft({
      facilityId,
      referenceCode: values.referenceCode,
      title: values.title,
      notes: values.notes || undefined,
      origin: values.origin,
      aiGenerated: values.aiGenerated,
      confidence: values.confidence || undefined,
      plannedStartAtUtc: startUtc,
      plannedEndAtUtc: endUtc,
      demands: [
        {
          id: crypto.randomUUID(),
          productId: values.demandProductId,
          productName: values.demandProductName,
          requiredQuantity: quantity,
          unit: values.demandUnit,
          priority: "normal",
        },
      ],
      risks: [],
      resources: {},
      sourceReferences: [],
    });

    if (result.status === "success") {
      return {
        status: "success" as const,
        redirectTo: `/planlama/${result.value.id}`,
      };
    }

    return {
      status: "failure" as const,
      code: result.code,
      message: result.message,
      fieldErrors: result.fieldErrors,
    };
  }

  return (
    <main className="production-plan-create-page">
      <div className="production-plan-create-page__back">
        <Link href="/planlama">
          <ArrowLeft aria-hidden="true" />
          Planlama çalışma alanına dön
        </Link>
      </div>

      <header className="production-plan-create-page__header">
        <p>Üretim Planlama</p>
        <h1>Yeni Plan Taslağı</h1>
        <span>
          Yeni üretim planı taslağı oluşturun. Taslak kaydedilene kadar herhangi bir üretime etkisi olmaz.
        </span>
      </header>

      <ProductionPlanDraftForm
        facilityId={facilityId}
        mode="create"
        previewWriteBlocked={previewWriteBlocked}
        onSubmit={handleCreate}
      />
    </main>
  );
}
