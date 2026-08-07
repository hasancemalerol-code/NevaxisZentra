import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";

import { readProductionPlanningDetail } from "@/features/production-planning/server/production-plan-reader";
import { parseProductionPlanRuntimeConfig } from "@/features/production-planning/server/production-plan-runtime-config";
import { resolveProductionPlanningServerScope } from "@/features/production-planning/server/production-plan-scope";
import { ProductionPlanDraftForm } from "@/features/production-planning/components/production-plan-draft-form";
import { writeUpdateProductionPlanDraft } from "@/features/production-planning/server/production-plan-draft-writer";
import type { ProductionPlanDraftFormValues } from "@/features/production-planning/components/production-plan-draft-form";

type ProductionPlanEditPageProps = Readonly<{
  params: Promise<{ planId: string }>;
}>;

const EDITABLE_STATUSES = new Set(["draft", "needs-review"]);

export default async function ProductionPlanEditPage({
  params,
}: ProductionPlanEditPageProps) {
  const { planId } = await params;

  const detailResult = await readProductionPlanningDetail(planId);

  if (detailResult.status === "failure") {
    throw new Error(
      [
        "Production plan could not be loaded for editing.",
        detailResult.code,
        detailResult.message,
      ].join(" "),
    );
  }

  if (detailResult.status === "not-found") {
    notFound();
  }

  const plan = detailResult.productionPlan;

  if (!EDITABLE_STATUSES.has(plan.status)) {
    return (
      <main className="production-plan-edit-page">
        <div className="production-plan-edit-page__back">
          <Link href={`/planlama/${planId}`}>
            <ArrowLeft aria-hidden="true" />
            Plana geri dön
          </Link>
        </div>

        <div className="production-plan-edit-page__not-editable">
          <h1>Plan Düzenlenemiyor</h1>
          <p>
            Bu plan şu anda <strong>{plan.status}</strong> durumunda olduğundan düzenlenemez.
            Yalnızca <em>Taslak</em> veya <em>İnceleme Gerekiyor</em> durumundaki planlar düzenlenebilir.
          </p>
          <Link href={`/planlama/${planId}`}>Plana dön</Link>
        </div>
      </main>
    );
  }

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

  const expectedVersion = plan.version;

  const primaryDemand = plan.demands[0];

  const initialValues: Partial<ProductionPlanDraftFormValues> = {
    referenceCode: plan.referenceCode,
    title: plan.title,
    notes: plan.notes ?? "",
    origin: plan.origin,
    aiGenerated: plan.aiGenerated,
    confidence: plan.confidence,
    plannedStartAtUtc: plan.timeWindow.plannedStartAt
      ? plan.timeWindow.plannedStartAt.slice(0, 16)
      : "",
    plannedEndAtUtc: plan.timeWindow.plannedEndAt
      ? plan.timeWindow.plannedEndAt.slice(0, 16)
      : "",
    demandProductId: primaryDemand?.productId ?? "",
    demandProductName: primaryDemand?.productName ?? "",
    demandQuantity: primaryDemand?.requiredQuantity != null
      ? String(primaryDemand.requiredQuantity)
      : "1",
    demandUnit: primaryDemand?.unit ?? "pcs",
  };

  async function handleUpdate(
    values: ProductionPlanDraftFormValues,
    version: number | undefined,
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

    const result = await writeUpdateProductionPlanDraft(planId, {
      facilityId,
      expectedVersion: version ?? expectedVersion,
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
          id: primaryDemand?.id ?? crypto.randomUUID(),
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
        redirectTo: `/planlama/${planId}`,
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
    <main className="production-plan-edit-page">
      <div className="production-plan-edit-page__back">
        <Link href={`/planlama/${planId}`}>
          <ArrowLeft aria-hidden="true" />
          Plana geri dön
        </Link>
      </div>

      <header className="production-plan-edit-page__header">
        <p>Üretim Planı</p>
        <h1>{plan.title} — Düzenle</h1>
        <span>
          Planı düzenleyip kaydedin. Kayıt sırasında eşzamanlılık kontrolü yapılır.
        </span>
      </header>

      <ProductionPlanDraftForm
        facilityId={facilityId}
        mode="edit"
        productionPlanId={planId}
        expectedVersion={expectedVersion}
        previewWriteBlocked={previewWriteBlocked}
        initialValues={initialValues}
        onSubmit={handleUpdate}
      />
    </main>
  );
}
