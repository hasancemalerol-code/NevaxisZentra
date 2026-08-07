"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import type { ProductionPlanDraftFieldError } from "../domain/production-plan";

export type ProductionPlanDraftFormValues = Readonly<{
  referenceCode: string;
  title: string;
  notes: string;
  origin: string;
  aiGenerated: boolean;
  confidence: string;
  plannedStartAtUtc: string;
  plannedEndAtUtc: string;
  demandProductId: string;
  demandProductName: string;
  demandQuantity: string;
  demandUnit: string;
}>;

export type ProductionPlanDraftFormProps = Readonly<{
  facilityId: string;
  mode: "create" | "edit";
  productionPlanId?: string;
  expectedVersion?: number;
  previewWriteBlocked?: boolean;
  initialValues?: Partial<ProductionPlanDraftFormValues>;
  onSubmit: (
    values: ProductionPlanDraftFormValues,
    expectedVersion: number | undefined,
  ) => Promise<
    | Readonly<{ status: "success"; redirectTo: string }>
    | Readonly<{
        status: "failure";
        code: string;
        message: string;
        fieldErrors?: readonly ProductionPlanDraftFieldError[];
      }>
  >;
}>;

const EMPTY_FORM_VALUES: ProductionPlanDraftFormValues = {
  referenceCode: "",
  title: "",
  notes: "",
  origin: "manual",
  aiGenerated: false,
  confidence: "",
  plannedStartAtUtc: "",
  plannedEndAtUtc: "",
  demandProductId: "",
  demandProductName: "",
  demandQuantity: "1",
  demandUnit: "pcs",
};

function mergeInitialValues(
  initial: Partial<ProductionPlanDraftFormValues> | undefined,
): ProductionPlanDraftFormValues {
  return {
    ...EMPTY_FORM_VALUES,
    ...initial,
  };
}

function getFieldError(
  fieldErrors: readonly ProductionPlanDraftFieldError[] | undefined,
  fieldName: string,
): string | undefined {
  return fieldErrors?.find((e) => e.field === fieldName)?.message;
}

export function ProductionPlanDraftForm({
  facilityId,
  mode,
  productionPlanId,
  expectedVersion,
  previewWriteBlocked,
  initialValues,
  onSubmit,
}: ProductionPlanDraftFormProps) {
  const router = useRouter();

  const [values, setValues] = useState<ProductionPlanDraftFormValues>(
    mergeInitialValues(initialValues),
  );

  const [submitting, setSubmitting] = useState(false);
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [concurrencyError, setConcurrencyError] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<
    readonly ProductionPlanDraftFieldError[]
  >([]);

  const isDirty = useRef(false);
  const submitted = useRef(false);

  useEffect(() => {
    isDirty.current = true;
  }, [values]);

  useEffect(() => {
    function handleBeforeUnload(event: BeforeUnloadEvent) {
      if (isDirty.current && !submitted.current) {
        event.preventDefault();
      }
    }

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => {
      window.removeEventListener("beforeunload", handleBeforeUnload);
    };
  }, []);

  const updateField = useCallback(
    (field: keyof ProductionPlanDraftFormValues) =>
      (
        event: React.ChangeEvent<
          HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >,
      ) => {
        const value =
          event.target.type === "checkbox"
            ? String((event.target as HTMLInputElement).checked)
            : event.target.value;

        setValues((prev) => ({
          ...prev,
          [field]:
            event.target.type === "checkbox"
              ? (event.target as HTMLInputElement).checked
              : value,
        }));
      },
    [],
  );

  const handleSubmit = useCallback(
    async (event: React.FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (submitting) {
        return;
      }

      setSubmitting(true);
      setGlobalError(null);
      setConcurrencyError(false);
      setFieldErrors([]);

      try {
        const result = await onSubmit(values, expectedVersion);

        if (result.status === "success") {
          submitted.current = true;
          isDirty.current = false;
          router.push(result.redirectTo);
          return;
        }

        if (result.code === "concurrency-conflict") {
          setConcurrencyError(true);
          return;
        }

        if (result.fieldErrors && result.fieldErrors.length > 0) {
          setFieldErrors(result.fieldErrors);
          setGlobalError(
            "Lütfen formdaki hataları düzeltin.",
          );
          return;
        }

        setGlobalError(
          result.message ||
            "Bir hata oluştu. Lütfen tekrar deneyin.",
        );
      } finally {
        setSubmitting(false);
      }
    },
    [submitting, onSubmit, values, expectedVersion, router],
  );

  if (previewWriteBlocked) {
    return (
      <div className="production-plan-form-blocked">
        <p>Geliştirme önizleme modunda yazma işlemleri devre dışı bırakılmıştır.</p>
        <p>Gerçek plan oluşturmak için development-api modunu kullanın.</p>
      </div>
    );
  }

  return (
    <form
      className="production-plan-draft-form"
      onSubmit={handleSubmit}
      noValidate
    >
      {globalError ? (
        <div className="production-plan-draft-form__global-error" role="alert">
          {globalError}
        </div>
      ) : null}

      {concurrencyError ? (
        <div className="production-plan-draft-form__concurrency-error" role="alert">
          Bu plan başka bir kullanıcı tarafından değiştirildi. Lütfen sayfayı yenileyip tekrar deneyin.
        </div>
      ) : null}

      <input type="hidden" name="facilityId" value={facilityId} readOnly />
      {mode === "edit" && productionPlanId ? (
        <input
          type="hidden"
          name="productionPlanId"
          value={productionPlanId}
          readOnly
        />
      ) : null}

      <section className="production-plan-draft-form__section">
        <h2>Temel Bilgiler</h2>

        <div className="production-plan-draft-form__field">
          <label htmlFor="referenceCode">
            Referans Kodu <span aria-hidden="true">*</span>
          </label>
          <input
            id="referenceCode"
            name="referenceCode"
            type="text"
            required
            maxLength={100}
            value={values.referenceCode}
            onChange={updateField("referenceCode")}
            disabled={submitting}
            aria-describedby={
              getFieldError(fieldErrors, "referenceCode")
                ? "referenceCode-error"
                : undefined
            }
          />
          {getFieldError(fieldErrors, "referenceCode") ? (
            <span
              id="referenceCode-error"
              className="production-plan-draft-form__field-error"
              role="alert"
            >
              {getFieldError(fieldErrors, "referenceCode")}
            </span>
          ) : null}
        </div>

        <div className="production-plan-draft-form__field">
          <label htmlFor="title">
            Plan Başlığı <span aria-hidden="true">*</span>
          </label>
          <input
            id="title"
            name="title"
            type="text"
            required
            maxLength={300}
            value={values.title}
            onChange={updateField("title")}
            disabled={submitting}
            aria-describedby={
              getFieldError(fieldErrors, "title")
                ? "title-error"
                : undefined
            }
          />
          {getFieldError(fieldErrors, "title") ? (
            <span
              id="title-error"
              className="production-plan-draft-form__field-error"
              role="alert"
            >
              {getFieldError(fieldErrors, "title")}
            </span>
          ) : null}
        </div>

        <div className="production-plan-draft-form__field">
          <label htmlFor="notes">Notlar</label>
          <textarea
            id="notes"
            name="notes"
            maxLength={4000}
            value={values.notes}
            onChange={updateField("notes")}
            disabled={submitting}
            rows={3}
          />
        </div>

        <div className="production-plan-draft-form__field">
          <label htmlFor="origin">Kaynak</label>
          <select
            id="origin"
            name="origin"
            value={values.origin}
            onChange={updateField("origin")}
            disabled={submitting}
          >
            <option value="manual">Manuel</option>
            <option value="approved-order">Onaylı Sipariş</option>
            <option value="erp-production-request">ERP Üretim Talebi</option>
            <option value="ai-recommendation">AI Önerisi</option>
          </select>
        </div>
      </section>

      <section className="production-plan-draft-form__section">
        <h2>Zaman Penceresi</h2>

        <div className="production-plan-draft-form__field">
          <label htmlFor="plannedStartAtUtc">
            Planlanan Başlangıç (UTC) <span aria-hidden="true">*</span>
          </label>
          <input
            id="plannedStartAtUtc"
            name="plannedStartAtUtc"
            type="datetime-local"
            required
            value={values.plannedStartAtUtc}
            onChange={updateField("plannedStartAtUtc")}
            disabled={submitting}
            aria-describedby={
              getFieldError(fieldErrors, "plannedStartAtUtc")
                ? "plannedStartAtUtc-error"
                : undefined
            }
          />
          {getFieldError(fieldErrors, "plannedStartAtUtc") ? (
            <span
              id="plannedStartAtUtc-error"
              className="production-plan-draft-form__field-error"
              role="alert"
            >
              {getFieldError(fieldErrors, "plannedStartAtUtc")}
            </span>
          ) : null}
        </div>

        <div className="production-plan-draft-form__field">
          <label htmlFor="plannedEndAtUtc">
            Planlanan Bitiş (UTC) <span aria-hidden="true">*</span>
          </label>
          <input
            id="plannedEndAtUtc"
            name="plannedEndAtUtc"
            type="datetime-local"
            required
            value={values.plannedEndAtUtc}
            onChange={updateField("plannedEndAtUtc")}
            disabled={submitting}
            aria-describedby={
              getFieldError(fieldErrors, "plannedEndAtUtc")
                ? "plannedEndAtUtc-error"
                : undefined
            }
          />
          {getFieldError(fieldErrors, "plannedEndAtUtc") ? (
            <span
              id="plannedEndAtUtc-error"
              className="production-plan-draft-form__field-error"
              role="alert"
            >
              {getFieldError(fieldErrors, "plannedEndAtUtc")}
            </span>
          ) : null}
        </div>
      </section>

      <section className="production-plan-draft-form__section">
        <h2>Talepler</h2>
        <p className="production-plan-draft-form__section-hint">
          En az bir talep gereklidir. Şu an tek talep girişi desteklenmektedir.
        </p>

        {getFieldError(fieldErrors, "demands") ? (
          <span
            className="production-plan-draft-form__field-error"
            role="alert"
          >
            {getFieldError(fieldErrors, "demands")}
          </span>
        ) : null}

        <div className="production-plan-draft-form__field">
          <label htmlFor="demandProductId">
            Ürün / Parça ID <span aria-hidden="true">*</span>
          </label>
          <input
            id="demandProductId"
            name="demandProductId"
            type="text"
            required
            value={values.demandProductId}
            onChange={updateField("demandProductId")}
            disabled={submitting}
          />
        </div>

        <div className="production-plan-draft-form__field">
          <label htmlFor="demandProductName">
            Ürün Adı <span aria-hidden="true">*</span>
          </label>
          <input
            id="demandProductName"
            name="demandProductName"
            type="text"
            required
            value={values.demandProductName}
            onChange={updateField("demandProductName")}
            disabled={submitting}
          />
        </div>

        <div className="production-plan-draft-form__field">
          <label htmlFor="demandQuantity">
            Miktar <span aria-hidden="true">*</span>
          </label>
          <input
            id="demandQuantity"
            name="demandQuantity"
            type="number"
            required
            min="0.001"
            step="any"
            value={values.demandQuantity}
            onChange={updateField("demandQuantity")}
            disabled={submitting}
          />
        </div>

        <div className="production-plan-draft-form__field">
          <label htmlFor="demandUnit">Birim</label>
          <select
            id="demandUnit"
            name="demandUnit"
            value={values.demandUnit}
            onChange={updateField("demandUnit")}
            disabled={submitting}
          >
            <option value="pcs">Adet</option>
            <option value="kg">kg</option>
            <option value="g">g</option>
            <option value="t">Ton</option>
            <option value="l">Litre</option>
            <option value="m">m</option>
            <option value="m2">m²</option>
            <option value="m3">m³</option>
            <option value="box">Kutu</option>
            <option value="pallet">Palet</option>
          </select>
        </div>
      </section>

      <section className="production-plan-draft-form__section">
        <h2>Kaynaklar</h2>
        <p className="production-plan-draft-form__section-hint">
          Makine, kalıp ve malzeme atamaları isteğe bağlıdır.
          Gerçek kaynak kataloğu entegrasyonu bu sürümde henüz mevcut değildir.
        </p>
      </section>

      <section className="production-plan-draft-form__section">
        <h2>Riskler</h2>
        <p className="production-plan-draft-form__section-hint">
          Bu sürümde risk girişi basit form üzerinden desteklenmemektedir.
          Riskler API üzerinden eklenebilir.
        </p>
      </section>

      <div className="production-plan-draft-form__actions">
        <button
          type="submit"
          className="button button-primary button-medium"
          disabled={submitting || concurrencyError}
          aria-busy={submitting}
        >
          {submitting
            ? "Kaydediliyor…"
            : mode === "create"
              ? "Taslak Oluştur"
              : "Değişiklikleri Kaydet"}
        </button>
      </div>
    </form>
  );
}
