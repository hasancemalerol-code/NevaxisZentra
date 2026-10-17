"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Download, Upload } from "lucide-react";
import { useState } from "react";

import { BULK_DATA_ONBOARDING_ROUTE } from "../domain/bulk-data-onboarding-labels";
import type { BulkImportJobWorkspaceApi } from "../server/bulk-data-onboarding-api-client";
import { customerMessage } from "@/lib/presentation/customer-message";

type BulkDataOnboardingWorkflowClientProps = Readonly<{
  facilityId: string;
  initialWorkspace: BulkImportJobWorkspaceApi | null;
}>;

export function BulkDataOnboardingWorkflowClient({
  facilityId,
  initialWorkspace,
}: BulkDataOnboardingWorkflowClientProps) {
  const router = useRouter();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [selectedFiles, setSelectedFiles] = useState<FileList | null>(null);
  const [providerKind, setProviderKind] = useState<"csv-file" | "xlsx-file">("csv-file");

  const jobId = workspace?.job.id;

  async function apiCall(path: string, init?: RequestInit) {
    const separator = path.includes("?") ? "&" : "?";
    const response = await fetch(`${path}${separator}facilityId=${facilityId}`, {
      ...init,
      credentials: "include",
    });
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { message?: string } | null;
      throw new Error(body?.message ?? `İşlem başarısız (${response.status}).`);
    }

    return response.json();
  }

  async function refreshWorkspace(id: string) {
    const next = (await apiCall(`/api/bulk-data-onboarding/jobs/${id}`)) as BulkImportJobWorkspaceApi;
    setWorkspace(next);
    router.replace(`${BULK_DATA_ONBOARDING_ROUTE}?jobId=${id}`);
  }

  async function createJob() {
    setBusy(true);
    setMessage(null);
    try {
      const job = (await apiCall("/api/bulk-data-onboarding/jobs", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: `Müşteri Dosya Aktarımı ${new Date().toLocaleString("tr-TR")}`,
          sourceProviderKind: providerKind,
          sourceSystem: "CUSTOMER-FILE-IMPORT",
          idempotencyKey: `customer-import-${Date.now()}`,
          mappingProfileId: null,
        }),
      })) as { id: string };

      if (selectedFiles && selectedFiles.length > 0) {
        const form = new FormData();
        Array.from(selectedFiles).forEach((file) => form.append("files", file));
        await apiCall(`/api/bulk-data-onboarding/jobs/${job.id}/files`, {
          method: "POST",
          body: form,
        });
      }

      await refreshWorkspace(job.id);
      setMessage("İçe aktarma işi oluşturuldu.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "İş oluşturulamadı.");
    } finally {
      setBusy(false);
    }
  }

  async function runStep(path: string, successMessage: string) {
    if (!jobId) return;
    setBusy(true);
    setMessage(null);
    try {
      const result = (await apiCall(`/api/bulk-data-onboarding/jobs/${jobId}/${path}`, { method: "POST" })) as { failedRows?: number } | null;
      await refreshWorkspace(jobId);
      const failedRows = path === "commit" ? result?.failedRows ?? 0 : 0;
      setMessage(failedRows > 0 ? `İçe aktarma kısmen tamamlandı: ${failedRows} kayıt aktarılamadı.` : successMessage);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Adım tamamlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="commint-panel bulk-onboarding-actions" data-testid="bulk-onboarding-actions">
      <h2>Dosyadan İçe Aktarma</h2>
      <p className="bulk-onboarding-actions__hint">
        CSV/XLSX dosyalarını yükleyin, keşfedin, doğrulayın, ön deneme çalıştırın ve onayınızla kaynakları oluşturun.
      </p>

      {!jobId ? (
        <div className="bulk-onboarding-actions__create">
          <label>
            Dosya türü
            <select
              value={providerKind}
              onChange={(event) => setProviderKind(event.target.value as "csv-file" | "xlsx-file")}
            >
              <option value="csv-file">CSV</option>
              <option value="xlsx-file">XLSX</option>
            </select>
          </label>
          <label className="bulk-onboarding-actions__dropzone">
            <span className="bulk-onboarding-actions__drop-label">Dosyalar</span>
            <span className="bulk-onboarding-actions__drop-box">
              <Upload aria-hidden="true" size={20} />
              {selectedFiles && selectedFiles.length > 0
                ? Array.from(selectedFiles).map((file) => file.name).join(", ")
                : "Dosya seçin veya sürükleyin"}
            </span>
            <input
              type="file"
              multiple
              className="bulk-onboarding-actions__file"
              accept={providerKind === "csv-file" ? ".csv,.txt" : ".xlsx"}
              onChange={(event) => setSelectedFiles(event.target.files)}
            />
          </label>
          <button type="button" className="button button-primary button-medium" disabled={busy} onClick={() => void createJob()}>
            Yeni İçe Aktarma Başlat
          </button>
        </div>
      ) : (
        <div className="bulk-onboarding-actions__steps">
          <button type="button" className="button button-secondary button-medium" disabled={busy} onClick={() => void runStep("discover", "Kaynak keşfi tamamlandı.")}>
            Keşif
          </button>
          <button type="button" className="button button-secondary button-medium" disabled={busy} onClick={() => void runStep("mapping/auto", "Otomatik eşleme uygulandı.")}>
            Eşleme
          </button>
          <button type="button" className="button button-secondary button-medium" disabled={busy} onClick={() => void runStep("stage-validate", "Doğrulama tamamlandı.")}>
            Doğrula
          </button>
          <button type="button" className="button button-secondary button-medium" disabled={busy} onClick={() => void runStep("dry-run", "Deneme çalıştırması tamamlandı.")}>
            Deneme çalıştırması
          </button>
          <button type="button" className="button button-primary button-medium" disabled={busy} onClick={() => void runStep("commit", "Kontrollü içe aktarma tamamlandı.")}>
            İçe aktar
          </button>
        </div>
      )}

      <div className="bulk-onboarding-actions__templates">
        <span>Şablonları indir:</span>
        {["areas.csv", "lines.csv", "stations.csv", "resources.csv", "materials.csv"].map((template) => (
          <a key={template} className="bulk-onboarding-actions__template" href={`/api/bulk-data-onboarding/templates/${template}`} download>
            <Download aria-hidden="true" size={14} />
            {template}
          </a>
        ))}
      </div>

      <p className="bulk-onboarding-actions__links">
        İçe aktarma sonrası{" "}
        <Link href="/fabrika">Fabrika</Link> ve{" "}
        <Link href="/uretim/kaynaklar">Üretim Kaynakları</Link> ekranlarından kaynakları doğrulayın.
      </p>

      {message ? <p data-testid="bulk-onboarding-action-message">{customerMessage(message)}</p> : null}
    </section>
  );
}
