"use client";

import { useMemo, useState } from "react";
import { customerMessage } from "@/lib/presentation/customer-message";

type PreviewState = Readonly<{
  fileKind: string;
  detectedHeaders: string[];
  previewRows: ReadonlyArray<Record<string, string>>;
  schemaFingerprint: string;
  sheetNames?: string[];
}>;

type DryRunState = Readonly<{
  totalRows: number;
  validRows: number;
  invalidRows: number;
  duplicateRows: number;
  newProjections: number;
  updates: number;
  unchanged: number;
  quarantineCandidates: number;
}>;

type IntegrationFileImportWorkbenchProps = Readonly<{
  connectorId: string;
  connectorName: string;
  facilityId: string;
  apiBaseUrl: string;
}>;

const defaultMappings = [
  { sourceField: "code", targetField: "code", required: true },
  { sourceField: "name", targetField: "name", required: true },
];

export function IntegrationFileImportWorkbench({
  connectorId,
  connectorName,
  facilityId,
  apiBaseUrl,
}: IntegrationFileImportWorkbenchProps) {
  const [file, setFile] = useState<File | null>(null);
  const [sheetName, setSheetName] = useState<string>("");
  const [preview, setPreview] = useState<PreviewState | null>(null);
  const [dryRun, setDryRun] = useState<DryRunState | null>(null);
  const [importResult, setImportResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [mappingVersionId, setMappingVersionId] = useState<string | null>(null);

  const mappingJson = useMemo(
    () =>
      JSON.stringify({
        targetCanonicalEntityType: "material",
        sourceEntityType: "materials",
        sourceKeyField: "code",
        fieldMappings: defaultMappings.map((mapping) => ({
          sourceField: mapping.sourceField,
          targetField: mapping.targetField,
          required: mapping.required,
          targetType: "string",
        })),
        defaults: {
          materialcategory: "other",
          polymerfamily: "unknown",
          physicalform: "granule",
          unitofmeasure: "kg",
          sourcetype: "imported",
        },
      }),
    [],
  );

  async function runPreview() {
    if (!file) {
      return;
    }

    setBusy(true);
    setError(null);
    setDryRun(null);
    setImportResult(null);

    try {
      const form = new FormData();
      form.append("file", file);
      const response = await fetch(
        `${apiBaseUrl}/api/integration/connectors/${connectorId}/file-imports/preview?facilityId=${facilityId}`,
        { method: "POST", body: form, credentials: "include" },
      );
      if (!response.ok) {
        throw new Error("Önizleme başarısız.");
      }

      const payload = (await response.json()) as PreviewState;
      setPreview(payload);
      if (payload.sheetNames?.length && !sheetName) {
        setSheetName(payload.sheetNames[0] ?? "");
      }
    } catch (previewError) {
      setError(previewError instanceof Error ? previewError.message : "Önizleme hatası.");
    } finally {
      setBusy(false);
    }
  }

  async function createAndActivateMapping() {
    if (!preview) {
      return;
    }

    setBusy(true);
    setError(null);

    try {
      const createResponse = await fetch(
        `${apiBaseUrl}/api/integration/connectors/${connectorId}/mappings`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            facilityId,
            sourceHeaders: preview.detectedHeaders,
            mappingJson,
            targetCanonicalEntityType: "material",
          }),
        },
      );
      if (!createResponse.ok) {
        throw new Error("Eşleme oluşturulamadı.");
      }

      const created = (await createResponse.json()) as { id: string };
      const validateResponse = await fetch(
        `${apiBaseUrl}/api/integration/connectors/${connectorId}/mappings/${created.id}/validate`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            facilityId,
            expectedVersion: 1,
            sourceHeaders: preview.detectedHeaders,
          }),
        },
      );
      if (!validateResponse.ok) {
        throw new Error("Eşleme doğrulanamadı.");
      }

      const activateResponse = await fetch(
        `${apiBaseUrl}/api/integration/connectors/${connectorId}/mappings/${created.id}/activate`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ facilityId, expectedVersion: 2 }),
        },
      );
      if (!activateResponse.ok) {
        throw new Error("Eşleme etkinleştirilemedi.");
      }

      setMappingVersionId(created.id);
    } catch (mappingError) {
      setError(mappingError instanceof Error ? mappingError.message : "Eşleme hatası.");
    } finally {
      setBusy(false);
    }
  }

  async function runDryRun() {
    if (!file) {
      return;
    }

    setBusy(true);
    setError(null);

    try {
      const form = new FormData();
      form.append("file", file);
      const query = new URLSearchParams({ facilityId });
      if (mappingVersionId) {
        query.set("mappingVersionId", mappingVersionId);
      }
      if (sheetName) {
        query.set("sheetName", sheetName);
      }

      const response = await fetch(
        `${apiBaseUrl}/api/integration/connectors/${connectorId}/file-imports/dry-run?${query}`,
        { method: "POST", body: form, credentials: "include" },
      );
      if (!response.ok) {
        throw new Error("Deneme çalıştırması başarısız.");
      }

      setDryRun((await response.json()) as DryRunState);
    } catch (dryRunError) {
      setError(dryRunError instanceof Error ? dryRunError.message : "Deneme çalıştırması hatası.");
    } finally {
      setBusy(false);
    }
  }

  async function confirmImport() {
    if (!file) {
      return;
    }

    setBusy(true);
    setError(null);

    try {
      const form = new FormData();
      form.append("file", file);
      const query = new URLSearchParams({
        facilityId,
        importIdempotencyKey: crypto.randomUUID(),
      });
      if (mappingVersionId) {
        query.set("mappingVersionId", mappingVersionId);
      }
      if (sheetName) {
        query.set("sheetName", sheetName);
      }

      const response = await fetch(
        `${apiBaseUrl}/api/integration/connectors/${connectorId}/file-imports/import?${query}`,
        { method: "POST", body: form, credentials: "include" },
      );
      if (!response.ok) {
        throw new Error("İçe aktarma başarısız.");
      }

      const payload = (await response.json()) as { syncRunId: string };
      setImportResult(`İçe aktarma tamamlandı. Çalışma kimliği: ${payload.syncRunId}`);
    } catch (importError) {
      setError(importError instanceof Error ? importError.message : "İçe aktarma hatası.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="state-panel">
      <header>
        <p>DOSYA AKTARIMI</p>
        <h2>{connectorName}</h2>
      </header>

      <div className="form-grid">
        <label>
          CSV / XLSX dosyası
          <input
            type="file"
            accept=".csv,.txt,.xlsx"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          />
        </label>

        {preview?.sheetNames && preview.sheetNames.length > 1 ? (
          <label>
            Sayfa
            <select value={sheetName} onChange={(event) => setSheetName(event.target.value)}>
              {preview.sheetNames.map((name) => (
                <option key={name} value={name}>{name}</option>
              ))}
            </select>
          </label>
        ) : null}
      </div>

      <div className="button-row">
        <button type="button" disabled={!file || busy} onClick={runPreview}>Önizle</button>
        <button type="button" disabled={!preview || busy} onClick={createAndActivateMapping}>Eşlemeyi Etkinleştir</button>
        <button type="button" disabled={!preview || busy} onClick={runDryRun}>Deneme Çalıştır</button>
        <button type="button" disabled={!dryRun || busy} onClick={confirmImport}>İçe Aktar</button>
      </div>

      {error ? <div role="alert" className="state-panel error-state">{customerMessage(error)}</div> : null}
      {preview ? (
        <div>
          <h3>Önizleme ({preview.fileKind})</h3>
          <p>Sütunlar: {preview.detectedHeaders.join(", ")}</p>
          <p>Şema parmak izi: {preview.schemaFingerprint}</p>
        </div>
      ) : null}
      {dryRun ? (
        <div>
          <h3>Deneme sonucu</h3>
          <ul>
            <li>Toplam: {dryRun.totalRows}</li>
            <li>Geçerli: {dryRun.validRows}</li>
            <li>Geçersiz: {dryRun.invalidRows}</li>
            <li>Yinelenen: {dryRun.duplicateRows}</li>
            <li>Yeni: {dryRun.newProjections}</li>
            <li>Güncelleme: {dryRun.updates}</li>
            <li>Değişmeyen: {dryRun.unchanged}</li>
            <li>Karantina: {dryRun.quarantineCandidates}</li>
          </ul>
        </div>
      ) : null}
      {importResult ? <p>{importResult}</p> : null}
    </section>
  );
}
