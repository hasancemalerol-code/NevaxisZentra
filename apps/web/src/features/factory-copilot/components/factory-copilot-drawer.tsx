"use client";

import { describeFactoryCopilotAiState, describeFactoryCopilotTransportFailure } from "../domain/factory-copilot-failures";
import {
  describeFactoryCopilotContextQuality,
  humanizeFactoryCopilotLine,
} from "../domain/factory-copilot-presentation";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState, useTransition, type FormEvent } from "react";
import { Bot, LoaderCircle, X } from "lucide-react";

import {
  formatFactoryCopilotContextHeader,
  resolveFactoryCopilotContext,
} from "../application/factory-copilot-context";
import type { FactoryCopilotQueryResult } from "../domain/factory-copilot-types";
import {
  loadFactoryCopilotSuggestedQuestionsAction,
  queryFactoryCopilotAction,
} from "../server/factory-copilot-actions";
import { AssistantOriginBadge } from "@/components/ui/assistant-origin";
import { Notice } from "@/components/ui/notice";
import { customerMessage } from "@/lib/presentation/customer-message";

/** AI states that mean "not available by configuration or policy" rather than "something went wrong". */
const AI_STATE_NEUTRAL: ReadonlySet<string> = new Set(["AI_NOT_CONFIGURED", "AI_BUDGET_LIMITED", "AI_POLICY_REJECTED", "INSUFFICIENT_DATA"]);

/** Starter questions that fit the screen the drawer is opened from (used when the server has none). */
function fallbackQuestions(pathname: string): string[] {
  if (pathname.startsWith("/senaryo-kurtarma")) {
    return ["Hangi senaryo en az maliyetli?", "Kurtarma planında risk nerede?", "Neyi onaylamam gerekiyor?"];
  }
  if (pathname.startsWith("/planlama")) return ["Darboğaz nerede?", "Hangi siparişler risk altında?", "Plan hedefin altında mı?"];
  if (pathname.startsWith("/isler")) return ["Hangi işler açık?", "Onay bekleyen ne var?", "Engelli işler neler?"];
  if (pathname.startsWith("/fabrika")) return ["Neden hedefin altındayız?", "Darboğaz nerede?", "Son vardiyada ne değişti?"];
  return ["Bu ekranda önce neye bakmalıyım?", "Açık riskler neler?", "Son vardiyada ne değişti?"];
}

type FactoryCopilotDrawerProps = Readonly<{
  open: boolean;
  onClose: () => void;
  pathname: string;
  searchParams: Readonly<URLSearchParams>;
}>;

function mapApiResponse(raw: Record<string, unknown>): FactoryCopilotQueryResult {
  const response = raw.response as Record<string, unknown>;
  const provenance = raw.provenance as Record<string, unknown>;
  return {
    intent: String(raw.intent ?? ""),
    routingClass: String(raw.routingClass ?? ""),
    modelInvoked: Boolean(raw.modelInvoked),
    failureState: raw.failureState ? String(raw.failureState) : null,
    response: {
      answer: String(response.answer ?? ""),
      whatUsed: response.whatUsed ? String(response.whatUsed) : null,
      keyFacts: (response.keyFacts as string[] | undefined) ?? [],
      causes: (response.causes as string[] | undefined) ?? [],
      impact: response.impact ? String(response.impact) : null,
      recommendedNextStep: response.recommendedNextStep ? String(response.recommendedNextStep) : null,
      uncertainty: response.uncertainty ? String(response.uncertainty) : null,
      evidence: ((response.evidence as Array<Record<string, unknown>> | undefined) ?? []).map((item) => ({
        kind: String(item.kind ?? ""),
        referenceId: String(item.referenceId ?? ""),
        label: String(item.label ?? ""),
        detail: item.detail ? String(item.detail) : null,
      })),
      relatedActions: ((response.relatedActions as Array<Record<string, unknown>> | undefined) ?? []).map((item) => ({
        label: String(item.label ?? ""),
        href: String(item.href ?? ""),
        linkType: String(item.linkType ?? ""),
      })),
    },
    provenance: {
      contextSnapshotId: provenance.contextSnapshotId ? String(provenance.contextSnapshotId) : null,
      tools: ((provenance.tools as Array<Record<string, unknown>> | undefined) ?? []).map((item) => ({
        toolId: String(item.toolId ?? ""),
        accessClass: String(item.accessClass ?? ""),
        succeeded: Boolean(item.succeeded),
        summary: String(item.summary ?? ""),
        latencyMs: Number(item.latencyMs ?? 0),
      })),
      recordsReferenced: (provenance.recordsReferenced as string[] | undefined) ?? [],
      providerId: provenance.providerId ? String(provenance.providerId) : null,
      modelName: provenance.modelName ? String(provenance.modelName) : null,
      modelInvoked: Boolean(provenance.modelInvoked),
      routingClass: String(provenance.routingClass ?? ""),
      routingReason: String(provenance.routingReason ?? ""),
      contextQualityState: String(provenance.contextQualityState ?? ""),
      totalLatencyMs: Number(provenance.totalLatencyMs ?? 0),
    },
    suggestedQuestions: (raw.suggestedQuestions as string[] | undefined) ?? [],
    contextHeader: {
      entryPoint: String((raw.contextHeader as Record<string, unknown> | undefined)?.entryPoint ?? ""),
      factoryLabel: (raw.contextHeader as Record<string, unknown> | undefined)?.factoryLabel as string | null,
      areaLabel: (raw.contextHeader as Record<string, unknown> | undefined)?.areaLabel as string | null,
      lineLabel: (raw.contextHeader as Record<string, unknown> | undefined)?.lineLabel as string | null,
      resourceLabel: (raw.contextHeader as Record<string, unknown> | undefined)?.resourceLabel as string | null,
      orderLabel: (raw.contextHeader as Record<string, unknown> | undefined)?.orderLabel as string | null,
      workLabel: (raw.contextHeader as Record<string, unknown> | undefined)?.workLabel as string | null,
      scenarioLabel: (raw.contextHeader as Record<string, unknown> | undefined)?.scenarioLabel as string | null,
    },
    queryId: String(raw.queryId ?? ""),
    createdAtUtc: String(raw.createdAtUtc ?? ""),
  };
}

export function FactoryCopilotDrawer({
  open,
  onClose,
  pathname,
  searchParams,
}: FactoryCopilotDrawerProps) {
  const copilotContext = useMemo(
    () => resolveFactoryCopilotContext(pathname, searchParams),
    [pathname, searchParams],
  );
  const [queryDraft, setQueryDraft] = useState("");
  const [suggestions, setSuggestions] = useState<string[]>([]);
  const [result, setResult] = useState<FactoryCopilotQueryResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [errorCategory, setErrorCategory] = useState<string | null>(null);
  const [asked, setAsked] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();
  const [suggestionsLoading, setSuggestionsLoading] = useState(false);
  const openedAtPath = useRef(pathname);

  // The panel belongs to the screen it was opened on: navigating elsewhere closes it.
  useEffect(() => {
    if (open && openedAtPath.current !== pathname) {
      setResult(null);
      setAsked(null);
      setError(null);
      setQueryDraft("");
      onClose();
    }
    if (!open) {
      openedAtPath.current = pathname;
    }
  }, [open, pathname, onClose]);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    setSuggestionsLoading(true);
    void (async () => {
      let next: string[];
      try {
        const loaded = await loadFactoryCopilotSuggestedQuestionsAction(copilotContext);
        next = "questions" in loaded ? [...new Set(loaded.questions)] : fallbackQuestions(pathname);
      } catch {
        next = fallbackQuestions(pathname);
      }
      if (!cancelled) {
        setSuggestions(next.length > 0 ? next : fallbackQuestions(pathname));
        setSuggestionsLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [open, copilotContext, pathname]);

  // Escape closes the panel, like any dialog.
  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  const submitQuery = useCallback(
    (queryText: string) => {
      const trimmed = queryText.trim();
      if (!trimmed) return;
      setError(null);
      setErrorCategory(null);
      setAsked(trimmed);
      startTransition(async () => {
        let response: Awaited<ReturnType<typeof queryFactoryCopilotAction>>;
        try {
          response = await queryFactoryCopilotAction({
            queryText: trimmed,
            context: copilotContext,
          });
        } catch {
          // The action call itself failed (for example the session cookie is gone and the sign-in redirect came back
          // instead of an action response): classify it instead of letting it crash the page.
          const failure = await describeFactoryCopilotTransportFailure(window.location.href);
          setError(failure.message);
          setQueryDraft((current) => current || trimmed);
          return;
        }
        if ("status" in response && response.status === "failure") {
          setError(response.message);
          setErrorCategory(response.category ?? null);
          setQueryDraft((current) => current || trimmed);
          return;
        }
        const queryResult = response as FactoryCopilotQueryResult;
        setResult(mapApiResponse(queryResult as unknown as Record<string, unknown>));
        setSuggestions([...new Set(queryResult.suggestedQuestions)]);
      });
    },
    [copilotContext],
  );

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submitQuery(queryDraft);
    setQueryDraft("");
  }

  if (!open) {
    return null;
  }

  const qualityLabel = result ? describeFactoryCopilotContextQuality(result.provenance.contextQualityState) : null;

  const header = result?.contextHeader ?? {
    entryPoint: copilotContext.entryPoint,
    areaLabel: copilotContext.areaCode ?? null,
    lineLabel: copilotContext.lineCode ?? copilotContext.lineId ?? null,
    resourceLabel: copilotContext.resourceCode ?? copilotContext.resourceId ?? null,
    orderLabel: copilotContext.orderReference ?? null,
    workLabel: copilotContext.digitalWorkCaseId ?? null,
    scenarioLabel: copilotContext.scenarioRunId ?? null,
  };

  return (
    <>
      <button
        type="button"
        className="factory-copilot-drawer__backdrop"
        aria-label="Copilot panelini kapat"
        onClick={onClose}
      />
      <aside className="factory-copilot-drawer" data-testid="factory-copilot-drawer" role="dialog" aria-modal="true" aria-label="Zentra Copilot">
        <header className="factory-copilot-drawer__header">
          <div className="factory-copilot-drawer__title-row">
            <strong>Zentra Copilot</strong>
            <button type="button" aria-label="Kapat" onClick={onClose}>
              <X size={16} aria-hidden="true" />
            </button>
          </div>
          <p className="factory-copilot-drawer__context">{formatFactoryCopilotContextHeader(header)}</p>
          {result ? (
            <div className="factory-copilot-drawer__meta" data-testid="factory-copilot-source">
              <AssistantOriginBadge origin={result.modelInvoked ? "model" : "rules"} />
              {qualityLabel ? <span className="factory-copilot-drawer__badge">{qualityLabel}</span> : null}
            </div>
          ) : null}
        </header>

        <div className="factory-copilot-drawer__body">
          <div className="factory-copilot-drawer__suggestions" aria-busy={suggestionsLoading}>
            {suggestions.map((question) => (
              <button
                key={question}
                type="button"
                className="factory-copilot-drawer__suggestion"
                onClick={() => submitQuery(question)}
              >
                {question}
              </button>
            ))}
          </div>

          {isPending ? (
            <p className="factory-copilot-drawer__empty" role="status" aria-live="polite">
              <LoaderCircle size={16} className="zx-spin" aria-hidden="true" /> Yanıt hazırlanıyor…
            </p>
          ) : null}

          {asked ? (
            <p className="factory-copilot-drawer__question" data-testid="factory-copilot-question">
              <span className="visually-hidden">Sorunuz: </span>
              {asked}
            </p>
          ) : null}

          {error ? (
            <Notice tone={errorCategory === "insufficient-context" ? "neutral" : "danger"} alert={errorCategory !== "insufficient-context"} data-testid="factory-copilot-error">
              {customerMessage(error)}
            </Notice>
          ) : null}
          {result ? (
            <article className="factory-copilot-drawer__answer">
              {describeFactoryCopilotAiState(result.failureState) ? (
                <Notice
                  tone={AI_STATE_NEUTRAL.has(result.failureState ?? "") ? "neutral" : "warning"}
                  data-testid="factory-copilot-ai-state"
                >
                  {describeFactoryCopilotAiState(result.failureState)}
                </Notice>
              ) : null}
              {result.response.answer ? <p>{result.response.answer}</p> : null}
              {result.response.keyFacts.length > 0 ? (
                <>
                  <h3 className="factory-copilot-drawer__section-title">Temel gerçekler</h3>
                  <ul className="factory-copilot-drawer__facts">
                    {result.response.keyFacts.map((fact) => (
                      <li key={fact}>{humanizeFactoryCopilotLine(fact)}</li>
                    ))}
                  </ul>
                </>
              ) : null}
              {result.response.causes.length > 0 ? (
                <>
                  <h3 className="factory-copilot-drawer__section-title">Nedenler</h3>
                  <ul className="factory-copilot-drawer__causes">
                    {result.response.causes.map((cause) => (
                      <li key={cause}>{cause}</li>
                    ))}
                  </ul>
                </>
              ) : null}
              {result.response.evidence.length > 0 ? (
                <>
                  <h3 className="factory-copilot-drawer__section-title">Kanıt</h3>
                  <div className="factory-copilot-drawer__evidence">
                    {result.response.evidence.map((item) => (
                      <span key={`${item.kind}-${item.referenceId}`} className="factory-copilot-drawer__evidence-chip">
                        {item.label}
                        {item.detail ? `: ${item.detail}` : ""}
                      </span>
                    ))}
                  </div>
                </>
              ) : null}
              {result.response.recommendedNextStep ? (
                <>
                  <h3 className="factory-copilot-drawer__section-title">Sonraki adım</h3>
                  <p>{result.response.recommendedNextStep}</p>
                </>
              ) : null}
              {result.response.uncertainty ? (
                <>
                  <h3 className="factory-copilot-drawer__section-title">Belirsizlik</h3>
                  <p>{result.response.uncertainty}</p>
                </>
              ) : null}
              {result.response.relatedActions.length > 0 ? (
                <>
                  <h3 className="factory-copilot-drawer__section-title">İlgili aksiyonlar</h3>
                  <div className="factory-copilot-drawer__actions">
                    {result.response.relatedActions.map((action) => (
                      <Link key={action.href} href={action.href} className="factory-copilot-drawer__action-link">
                        {action.label}
                      </Link>
                    ))}
                  </div>
                </>
              ) : null}
            </article>
          ) : error || isPending || asked ? null : (
            <p className="factory-copilot-drawer__empty">
              Operasyonel sorunuzu yazın veya önerilen sorulardan birini seçin.
            </p>
          )}
        </div>

        <footer className="factory-copilot-drawer__footer">
          <form className="factory-copilot-drawer__form" onSubmit={onSubmit}>
            <input
              className="factory-copilot-drawer__input"
              value={queryDraft}
              onChange={(event) => setQueryDraft(event.target.value)}
              placeholder="Bu bağlamda ne sorabilirim?"
              aria-label="Copilot sorusu"
            />
            <button className="factory-copilot-drawer__submit" type="submit" disabled={isPending || !queryDraft.trim()}>
              Sor
            </button>
          </form>
        </footer>
      </aside>
    </>
  );
}

export function FactoryCopilotToggle({ onOpen }: Readonly<{ onOpen: () => void }>) {
  return (
    <button type="button" className="factory-copilot-toggle" aria-haspopup="dialog" onClick={onOpen} data-testid="factory-copilot-toggle">
      <Bot size={16} aria-hidden="true" />
      Zentra Copilot
    </button>
  );
}
