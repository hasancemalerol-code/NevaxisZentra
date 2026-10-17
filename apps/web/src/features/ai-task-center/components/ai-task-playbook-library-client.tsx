"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import {
  BookOpen,
  History,
  Pencil,
  Play,
  Power,
  PowerOff,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/states";

import type {
  AiTaskPlaybookRunSummary,
  AiTaskPlaybookSummary,
} from "../domain/ai-task-playbook";
import {
  buildAiTaskPlaybookLibraryHref,
  buildAiTaskPlaybookRunDetailHref,
  parseAiTaskPlaybookLibraryView,
} from "../presentation/ai-task-playbook-library-navigation";
import { formatAiTaskShortDateTime } from "../presentation/ai-task-formatters";
import {
  formatAiTaskPlaybookProgress,
  getAiTaskPlaybookCategoryLabel,
  getAiTaskPlaybookRunStatusLabel,
  getAiTaskPlaybookRunStatusTone,
  getAiTaskPlaybookTriggerSourceLabel,
} from "../presentation/ai-task-playbook-labels";
import {
  formatAiTaskPlaybookLastRunLabel,
  formatAiTaskPlaybookScopeLabel,
  formatAiTaskPlaybookStepCountLabel,
  formatAiTaskPlaybookUpdatedAtLabel,
  formatAiTaskPlaybookVersionLabel,
  localizeAiTaskPlaybookDisplayName,
} from "../presentation/ai-task-playbook-presentation";
import {
  disableAiTaskPlaybookAction,
  enableAiTaskPlaybookAction,
  startAiTaskPlaybookRunAction,
} from "../server/ai-task-playbook-actions";
import type { AiTaskPlaybookCapabilities } from "../server/ai-task-playbook-queries";
import { customerMessage } from "@/lib/presentation/customer-message";

type AiTaskPlaybookLibraryClientProps = Readonly<{
  playbooks: readonly AiTaskPlaybookSummary[];
  activeRuns: readonly AiTaskPlaybookRunSummary[];
  historyRuns: readonly AiTaskPlaybookRunSummary[];
  capabilities: AiTaskPlaybookCapabilities;
}>;

function RunCard({ run }: Readonly<{ run: AiTaskPlaybookRunSummary }>) {
  const runDetailHref = buildAiTaskPlaybookRunDetailHref(run.id);

  return (
    <article className="ai-task-playbook-run-card" data-testid={`ai-task-playbook-run-${run.id}`}>
      <div className="ai-task-playbook-run-card__header">
        <div>
          <h3 className="ai-task-playbook-run-card__name">
            {localizeAiTaskPlaybookDisplayName(run.playbookName)}
          </h3>
          <p className="ai-task-playbook-run-card__meta">
            {getAiTaskPlaybookTriggerSourceLabel(run.triggerSource)}
            {" · "}
            {formatAiTaskShortDateTime(run.startedAtUtc)}
          </p>
        </div>
        <Badge tone={getAiTaskPlaybookRunStatusTone(run.status)}>
          {getAiTaskPlaybookRunStatusLabel(run.status)}
        </Badge>
      </div>

      <div className="ai-task-playbook-run-card__metrics">
        <div className="ai-task-playbook-run-card__metric">
          <span>Zorunlu</span>
          <strong>{formatAiTaskPlaybookProgress(run.requiredStepsCompleted, run.requiredStepsTotal)}</strong>
        </div>
        <div className="ai-task-playbook-run-card__metric">
          <span>İsteğe bağlı</span>
          <strong>{formatAiTaskPlaybookProgress(run.optionalStepsCompleted, run.optionalStepsTotal)}</strong>
        </div>
        <div className="ai-task-playbook-run-card__metric">
          <span>Bloke</span>
          <strong>{run.blockedStepsCount}</strong>
        </div>
        <div className="ai-task-playbook-run-card__metric">
          <span>Onay</span>
          <strong>{run.approvalsPending}</strong>
        </div>
      </div>

      {run.atRisk ? (
        <p className="ai-task-playbook-run-card__risk">Risk altında</p>
      ) : null}

      <div className="ai-task-playbook-run-card__actions">
        <Link
          className="button button-secondary button-medium ai-task-playbook-run-card__detail-link"
          data-testid={`ai-task-playbook-run-detail-link-${run.id}`}
          href={runDetailHref}
        >
          Detayı gör
        </Link>
      </div>
    </article>
  );
}

export function AiTaskPlaybookLibraryClient({
  playbooks,
  activeRuns,
  historyRuns,
  capabilities,
}: AiTaskPlaybookLibraryClientProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [historyPlaybookFilterId, setHistoryPlaybookFilterId] = useState<string | null>(
    () => searchParams.get("playbook") || null,
  );
  const [actionError, setActionError] = useState<string | null>(null);
  const activeView = parseAiTaskPlaybookLibraryView(searchParams.get("gorunum"));

  function refresh() {
    startTransition(() => {
      router.refresh();
    });
  }

  function openHistoryForPlaybook(playbookId: string) {
    setHistoryPlaybookFilterId(playbookId);
    router.push(buildAiTaskPlaybookLibraryHref("history", { playbook: playbookId }), { scroll: false });
  }

  async function handleEnable(playbook: AiTaskPlaybookSummary) {
    setActionError(null);
    const result = await enableAiTaskPlaybookAction(playbook.id, playbook.version);
    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    refresh();
  }

  async function handleDisable(playbook: AiTaskPlaybookSummary) {
    setActionError(null);
    const result = await disableAiTaskPlaybookAction(playbook.id, playbook.version);
    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    refresh();
  }

  async function handleStartRun(playbook: AiTaskPlaybookSummary) {
    setActionError(null);
    const result = await startAiTaskPlaybookRunAction(playbook.id, {
      triggerSource: "manual",
    });
    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    router.push(`/ai-gorevleri/playbooklar/calisma/${result.runId}`);
  }

  const filteredHistoryRuns = useMemo(() => {
    if (!historyPlaybookFilterId) return historyRuns;
    return historyRuns.filter((run) => run.playbookId === historyPlaybookFilterId);
  }, [historyPlaybookFilterId, historyRuns]);

  const historyFilterPlaybook = historyPlaybookFilterId
    ? playbooks.find((playbook) => playbook.id === historyPlaybookFilterId)
    : null;

  return (
    <div className="ai-task-playbook-library" data-testid="ai-task-playbook-library">
      {actionError ? (
        <p className="ai-task-playbook-library__error" role="alert">{customerMessage(actionError)}</p>
      ) : null}

      {activeView === "playbooks" ? (
        <Card className="ai-task-playbook-module">
          {playbooks.length === 0 ? (
            <EmptyState
              action={
                capabilities.canManage ? (
                  <Link
                    className="button button-primary button-medium"
                    href="/ai-gorevleri/playbooklar/yeni"
                  >
                    Yeni playbook oluştur
                  </Link>
                ) : undefined
              }
              description="İlk operasyon akışınızı oluşturarak başlayın."
              title="Henüz playbook yok"
            />
          ) : (
            <div className="ai-task-playbook-library-row-list">
              {playbooks.map((playbook) => {
                const displayName = localizeAiTaskPlaybookDisplayName(
                  playbook.name,
                  playbook.playbookKey,
                );
                const lastRunLabel = formatAiTaskPlaybookLastRunLabel(playbook.lastRunAtUtc);
                const lastRunMuted = !playbook.lastRunAtUtc;

                return (
                  <article
                    key={playbook.id}
                    className={`ai-task-playbook-library-row${playbook.enabled ? " ai-task-playbook-library-row--enabled" : ""}`}
                    data-testid={`ai-task-playbook-card-${playbook.id}`}
                  >
                    <div className="ai-task-playbook-library-row__main">
                      <div className="ai-task-playbook-library-row__heading">
                        <h3 className="ai-task-playbook-library-row__name">{displayName}</h3>
                        <div className="ai-task-playbook-library-row__badges">
                          <Badge tone={playbook.enabled ? "success" : "neutral"}>
                            {playbook.enabled ? "Etkin" : "Devre Dışı"}
                          </Badge>
                          <Badge tone="neutral">{formatAiTaskPlaybookVersionLabel(playbook.version)}</Badge>
                          <Badge tone="neutral">{formatAiTaskPlaybookStepCountLabel(playbook.stepCount)}</Badge>
                        </div>
                      </div>

                      <div className="ai-task-playbook-library-row__meta">
                        <div className="ai-task-playbook-library-row__meta-item">
                          <span className="ai-task-playbook-library-row__meta-label">Kategori</span>
                          <span>{getAiTaskPlaybookCategoryLabel(playbook.category)}</span>
                        </div>
                        <div className="ai-task-playbook-library-row__meta-item">
                          <span className="ai-task-playbook-library-row__meta-label">Kapsam</span>
                          <span>{formatAiTaskPlaybookScopeLabel(playbook.facilityId)}</span>
                        </div>
                        <div className="ai-task-playbook-library-row__meta-item">
                          <span className="ai-task-playbook-library-row__meta-label">Sürüm</span>
                          <span>{formatAiTaskPlaybookVersionLabel(playbook.version)}</span>
                        </div>
                        <div className="ai-task-playbook-library-row__meta-item">
                          <span className="ai-task-playbook-library-row__meta-label">Adım</span>
                          <span>{playbook.stepCount}</span>
                        </div>
                        <div className="ai-task-playbook-library-row__meta-item">
                          <span className="ai-task-playbook-library-row__meta-label">Durum</span>
                          <span>{playbook.enabled ? "Etkin" : "Devre Dışı"}</span>
                        </div>
                        <div className="ai-task-playbook-library-row__meta-item">
                          <span className="ai-task-playbook-library-row__meta-label">Son çalıştırma</span>
                          <span className={lastRunMuted ? "ai-task-playbook-library-row__muted" : undefined}>
                            {lastRunLabel}
                          </span>
                        </div>
                        <div className="ai-task-playbook-library-row__meta-item">
                          <span className="ai-task-playbook-library-row__meta-label">Son güncelleme</span>
                          <span>{formatAiTaskPlaybookUpdatedAtLabel(playbook.updatedAtUtc)}</span>
                        </div>
                      </div>
                    </div>

                    <div
                      className="ai-task-playbook-library-row__actions"
                      data-testid={`ai-task-playbook-card-actions-${playbook.id}`}
                    >
                      {capabilities.canRun && playbook.enabled ? (
                        <Button
                          aria-label="Çalıştır"
                          disabled={isPending}
                          leadingIcon={<Play />}
                          type="button"
                          onClick={() => handleStartRun(playbook)}
                        >
                          Çalıştır
                        </Button>
                      ) : null}

                      {capabilities.canManage ? (
                        <Button
                          aria-label="Düzenle"
                          disabled={isPending}
                          leadingIcon={<Pencil />}
                          type="button"
                          variant="secondary"
                          onClick={() => router.push(`/ai-gorevleri/playbooklar/${playbook.id}`)}
                        >
                          Düzenle
                        </Button>
                      ) : null}

                      <Button
                        aria-label="Geçmiş"
                        disabled={isPending}
                        leadingIcon={<History />}
                        type="button"
                        variant="quiet"
                        onClick={() => openHistoryForPlaybook(playbook.id)}
                      >
                        Geçmiş
                      </Button>

                      {capabilities.canManage ? (
                        playbook.enabled ? (
                          <Button
                            aria-label="Devre Dışı Bırak"
                            disabled={isPending}
                            leadingIcon={<PowerOff />}
                            type="button"
                            variant="quiet"
                            onClick={() => handleDisable(playbook)}
                          >
                            Devre Dışı Bırak
                          </Button>
                        ) : (
                          <Button
                            aria-label="Etkinleştir"
                            disabled={isPending}
                            leadingIcon={<Power />}
                            type="button"
                            variant="quiet"
                            onClick={() => handleEnable(playbook)}
                          >
                            Etkinleştir
                          </Button>
                        )
                      ) : null}
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </Card>
      ) : null}

      {activeView === "active" ? (
        <Card className="ai-task-playbook-module">
          <div className="ai-task-playbook-section__header">
            <div>
              <p className="ai-task-playbook-module__eyebrow">Canlı</p>
              <h2>Çalışan Akışlar</h2>
              <p>Devam eden, bekleyen veya bloke playbook çalışmaları.</p>
            </div>
          </div>

          {activeRuns.length === 0 ? (
            <EmptyState
              description="Şu anda aktif playbook çalışması bulunmuyor."
              title="Aktif akış yok"
            />
          ) : (
            <div className="ai-task-playbook-run-list">
              {activeRuns.map((run) => (
                <RunCard key={run.id} run={run} />
              ))}
            </div>
          )}
        </Card>
      ) : null}

      {activeView === "history" ? (
        <Card className="ai-task-playbook-module">
          <div className="ai-task-playbook-section__header">
            <div>
              <p className="ai-task-playbook-module__eyebrow">Arşiv</p>
              <h2>Geçmiş</h2>
              <p>
                {historyFilterPlaybook
                  ? `${localizeAiTaskPlaybookDisplayName(historyFilterPlaybook.name, historyFilterPlaybook.playbookKey)} için tamamlanan çalışmalar.`
                  : "Tamamlanan, iptal edilen veya başarısız çalışmalar."}
              </p>
            </div>
            {historyPlaybookFilterId ? (
              <Button
                type="button"
                variant="quiet"
                onClick={() => {
                  setHistoryPlaybookFilterId(null);
                  router.push(buildAiTaskPlaybookLibraryHref("history"), { scroll: false });
                }}
              >
                Tüm geçmiş
              </Button>
            ) : null}
          </div>

          {filteredHistoryRuns.length === 0 ? (
            <EmptyState
              description="Henüz tamamlanmış playbook çalışması yok."
              title="Geçmiş boş"
            />
          ) : (
            <div className="ai-task-playbook-run-list">
              {filteredHistoryRuns.map((run) => (
                <RunCard key={run.id} run={run} />
              ))}
            </div>
          )}
        </Card>
      ) : null}

      <div className="ai-task-playbook-library__hint">
        <BookOpen aria-hidden="true" />
        <span>Playbooklar operasyonel süreçleri adım adım yönetir; kritik adımlar insan onayı gerektirebilir.</span>
      </div>
    </div>
  );
}
