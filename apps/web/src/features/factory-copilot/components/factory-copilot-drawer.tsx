"use client";

import { describeFactoryCopilotAiState, describeFactoryCopilotTransportFailure } from "../domain/factory-copilot-failures";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, useTransition, type FormEvent } from "react";
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
  const [isPending, startTransition] = useTransition();

  useEffect(() => {
    if (!open) return;
    startTransition(async () => {
      let loaded: Awaited<ReturnType<typeof loadFactoryCopilotSuggestedQuestionsAction>>;
      try {
        loaded = await loadFactoryCopilotSuggestedQuestionsAction(copilotContext);
      } catch {
        setSuggestions(["Neden hedefin altındayız?", "Darboğaz nerede?", "Son vardiyada ne değişti?"]);
        return;
      }
      if ("status" in loaded && loaded.status === "failure") {
        setSuggestions([
          "Neden hedefin altındayız?",
          "Darboğaz nerede?",
          "Son vardiyada ne değişti?",
          "Hangi işler açık?",
          "Ne simüle edebiliriz?",
        ]);
        return;
      }
      if ("questions" in loaded) {
        setSuggestions([...loaded.questions]);
      }
    });
  }, [open, copilotContext]);

  const submitQuery = useCallback(
    (queryText: string) => {
      const trimmed = queryText.trim();
      if (!trimmed) return;
      setError(null);
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
          return;
        }
        if ("status" in response && response.status === "failure") {
          setError(response.message);
          return;
        }
        const queryResult = response as FactoryCopilotQueryResult;
        setResult(mapApiResponse(queryResult as unknown as Record<string, unknown>));
        setSuggestions([...queryResult.suggestedQuestions]);
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
      <aside className="factory-copilot-drawer" data-testid="factory-copilot-drawer" aria-label="Zentra Copilot">
        <header className="factory-copilot-drawer__header">
          <div className="factory-copilot-drawer__title-row">
            <strong>Zentra Copilot</strong>
            <button type="button" aria-label="Kapat" onClick={onClose}>
              <X size={16} />
            </button>
          </div>
          <p className="factory-copilot-drawer__context">{formatFactoryCopilotContextHeader(header)}</p>
          {result ? (
            <div className="factory-copilot-drawer__meta">
              <span className="factory-copilot-drawer__badge">{result.intent}</span>
              <span className="factory-copilot-drawer__badge">{result.routingClass}</span>
              <span className="factory-copilot-drawer__badge">
                {result.modelInvoked ? "Model" : "No model"}
              </span>
              <span className="factory-copilot-drawer__badge">{result.provenance.contextQualityState}</span>
            </div>
          ) : null}
        </header>

        <div className="factory-copilot-drawer__body">
          <div className="factory-copilot-drawer__suggestions">
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

          {error ? <p className="factory-copilot-drawer__feedback">{error}</p> : null}
          {isPending ? (
            <p className="factory-copilot-drawer__empty">
              <LoaderCircle size={16} /> Yanıt hazırlanıyor…
            </p>
          ) : null}

          {result ? (
            <article className="factory-copilot-drawer__answer">
              {describeFactoryCopilotAiState(result.failureState) ? (
                <p className="factory-copilot-drawer__feedback" role="status" data-testid="factory-copilot-ai-state">
                  {describeFactoryCopilotAiState(result.failureState)}
                </p>
              ) : null}
              <p>{result.response.answer}</p>
              {result.response.keyFacts.length > 0 ? (
                <>
                  <h3 className="factory-copilot-drawer__section-title">Temel gerçekler</h3>
                  <ul className="factory-copilot-drawer__facts">
                    {result.response.keyFacts.map((fact) => (
                      <li key={fact}>{fact}</li>
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
          ) : (
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
            <button className="factory-copilot-drawer__submit" type="submit" disabled={isPending}>
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
    <button type="button" className="factory-copilot-toggle" onClick={onOpen} data-testid="factory-copilot-toggle">
      <Bot size={16} />
      Zentra Copilot
    </button>
  );
}
