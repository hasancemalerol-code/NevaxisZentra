"use client";

import {
  useMemo,
  useState,
} from "react";
import {
  CheckCircle2,
  CircleAlert,
  Clock3,
  Database,
  Play,
  RefreshCw,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import { createOrderTransferCommand } from "../application/order-transfer-command";
import type { OrderTransferExecutionResult } from "../application/order-transfer-executor";
import { evaluateOrderTransferRetry } from "../application/order-transfer-retry-policy";
import {
  createOrderTransferPreviewService,
  executeOrderTransferPreview,
} from "../application/order-transfer-preview-service";
import type { OrderTransferPreviewScenario } from "../application/order-transfer-preview-executor";
import type { OrderTransferAuditRecord } from "../application/order-transfer-audit-record";
import type { OrderDraft } from "../domain/order-draft";
import { formatOrderDateTime } from "../presentation/order-draft-formatters";
import {
  getOrderTransferExecutionResultDescription,
  getOrderTransferExecutionResultLabel,
} from "../presentation/order-transfer-execution-labels";
import { getOrderTargetSystemLabel } from "../presentation/order-target-system-labels";
import { OrderTransferAuditPreviewCard } from "./order-transfer-audit-preview-card";

type OrderTransferExecutionPreviewPanelProps =
  Readonly<{
    orderDraft: OrderDraft;
    currentUserId?: string;
  }>;

type ExecutionTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger";

function getExecutionTone(
  result: OrderTransferExecutionResult | null,
): ExecutionTone {
  if (!result) {
    return "neutral";
  }

  switch (result.status) {
    case "completed":
      return "success";

    case "partially-completed":
      return "warning";

    case "failed":
      return "danger";
  }
}

function getExecutionIcon(
  result: OrderTransferExecutionResult | null,
) {
  if (!result) {
    return Database;
  }

  switch (result.status) {
    case "completed":
      return CheckCircle2;

    case "partially-completed":
      return Clock3;

    case "failed":
      return CircleAlert;
  }
}

export function OrderTransferExecutionPreviewPanel({
  orderDraft,
  currentUserId = "preview-user",
}: OrderTransferExecutionPreviewPanelProps) {
  const [scenario, setScenario] =
    useState<OrderTransferPreviewScenario>(
      "success",
    );

  const [result, setResult] =
    useState<OrderTransferExecutionResult | null>(
      null,
    );

  const [auditRecord, setAuditRecord] =
    useState<OrderTransferAuditRecord | null>(
      null,
    );

  const [isExecuting, setIsExecuting] =
    useState(false);

  const commandResult = useMemo(
    () =>
      createOrderTransferCommand({
        orderDraft,
        requestedByUserId: currentUserId,
        requestedAt: orderDraft.updatedAt,
      }),
    [currentUserId, orderDraft],
  );

  const tone = getExecutionTone(result);
  const ResultIcon = getExecutionIcon(result);

  const retryDecision =
    result?.status === "failed"
      ? evaluateOrderTransferRetry({
          failure: result,
          completedAttemptCount: 0,
        })
      : null;

  function clearExecutionResult() {
    setResult(null);
    setAuditRecord(null);
  }

  function selectScenario(
    nextScenario: OrderTransferPreviewScenario,
  ) {
    setScenario(nextScenario);
    clearExecutionResult();
  }

  async function handleExecute() {
    if (!commandResult.success) {
      return;
    }

    setIsExecuting(true);
    clearExecutionResult();

    try {
      const previewService =
        createOrderTransferPreviewService({
          executorOptions: {
            scenario,
          },
        });

      const serviceResult =
        await executeOrderTransferPreview(
          previewService,
          {
            command: commandResult.command,
            currentOrderDraft: orderDraft,
          },
        );

      setResult(serviceResult.execution);
      setAuditRecord(serviceResult.auditRecord);
    } finally {
      setIsExecuting(false);
    }
  }

  return (
    <Card className="order-transfer-execution-preview">
      <div className="order-transfer-execution-preview__heading">
        <div>
          <p>Bağlı sistem simülasyonu</p>
          <h2>Aktarım yürütme önizlemesi</h2>
        </div>

        <Badge tone={tone}>
          <ResultIcon aria-hidden="true" />

          {result
            ? getOrderTransferExecutionResultLabel(
                result,
              )
            : "Henüz çalıştırılmadı"}
        </Badge>
      </div>

      <div className="order-transfer-execution-preview__notice">
        <ShieldCheck aria-hidden="true" />

        <p>
          Bu panel gerçek ERP veya MES sistemine veri
          göndermez. Aktarım yürütmesi ve audit kaydı,
          kontrollü önizleme servisi üzerinden birlikte
          oluşturulur.
        </p>
      </div>

      <fieldset className="order-transfer-execution-preview__scenarios">
        <legend>Önizleme senaryosu</legend>

        <label>
          <input
            checked={scenario === "success"}
            name="order-transfer-preview-scenario"
            onChange={() => {
              selectScenario("success");
            }}
            type="radio"
          />

          <span>
            <CheckCircle2 aria-hidden="true" />
            Başarılı aktarım
          </span>
        </label>

        <label>
          <input
            checked={scenario === "partial"}
            name="order-transfer-preview-scenario"
            onChange={() => {
              selectScenario("partial");
            }}
            type="radio"
          />

          <span>
            <Clock3 aria-hidden="true" />
            Kısmi aktarım
          </span>
        </label>

        <label>
          <input
            checked={scenario === "failure"}
            name="order-transfer-preview-scenario"
            onChange={() => {
              selectScenario("failure");
            }}
            type="radio"
          />

          <span>
            <CircleAlert aria-hidden="true" />
            Başarısız aktarım
          </span>
        </label>
      </fieldset>

      {!commandResult.success ? (
        <div className="order-transfer-execution-preview__blocked">
          <CircleAlert aria-hidden="true" />

          <div>
            <strong>
              Önizleme yürütmesi başlatılamıyor
            </strong>

            <p>
              Sipariş aktarım komutu hazırlık
              kontrollerinden geçmedi. Engelleyici
              sorunlar, onay ve hedef sistem koşulları
              tamamlanmalıdır.
            </p>
          </div>
        </div>
      ) : null}

      {result ? (
        <div
          className="order-transfer-execution-preview__result"
          data-status={result.status}
        >
          <ResultIcon aria-hidden="true" />

          <div>
            <strong>
              {getOrderTransferExecutionResultLabel(
                result,
              )}
            </strong>

            <p>
              {getOrderTransferExecutionResultDescription(
                result,
              )}
            </p>
          </div>
        </div>
      ) : null}

      {result?.status === "completed" ? (
        <dl className="order-transfer-execution-preview__details">
          <div>
            <dt>Hedef sistem</dt>

            <dd>
              {getOrderTargetSystemLabel(
                result.audit.targetSystem,
              )}
            </dd>
          </div>

          <div>
            <dt>Harici sipariş numarası</dt>
            <dd>{result.externalOrderId}</dd>
          </div>

          <div>
            <dt>Başlangıç</dt>

            <dd>
              {formatOrderDateTime(
                result.audit.startedAt,
              )}
            </dd>
          </div>

          <div>
            <dt>Tamamlanma</dt>

            <dd>
              {formatOrderDateTime(
                result.audit.finishedAt,
              )}
            </dd>
          </div>
        </dl>
      ) : null}

      {result?.status ===
      "partially-completed" ? (
        <div className="order-transfer-execution-preview__steps">
          <div>
            <strong>Tamamlanan adımlar</strong>

            <ul>
              {result.completedSteps.map(
                (step) => (
                  <li key={step}>
                    <CheckCircle2 aria-hidden="true" />
                    {step}
                  </li>
                ),
              )}
            </ul>
          </div>

          <div>
            <strong>Bekleyen adımlar</strong>

            <ul>
              {result.pendingSteps.map(
                (step) => (
                  <li key={step}>
                    <Clock3 aria-hidden="true" />
                    {step}
                  </li>
                ),
              )}
            </ul>
          </div>
        </div>
      ) : null}

      {result?.status === "failed" ? (
        <div className="order-transfer-execution-preview__failure-meta">
          <span>
            Hata kodu:{" "}
            <strong>{result.code}</strong>
          </span>

          <span>
            Yeniden denenebilir:{" "}
            <strong>
              {retryDecision?.retryAllowed
                ? "Evet"
                : "Hayır"}
            </strong>
          </span>

          {retryDecision?.retryAllowed ? (
            <>
              <span>
                Sonraki deneme:{" "}
                <strong>
                  {retryDecision.nextAttemptNumber}
                </strong>
              </span>

              <span>
                Bekleme süresi:{" "}
                <strong>
                  {retryDecision.delayMilliseconds /
                    1000}{" "}
                  saniye
                </strong>
              </span>
            </>
          ) : (
            <span>
              İşlem:{" "}
              <strong>
                {retryDecision?.reason ===
                "manual-review-required"
                  ? "Manuel inceleme gerekiyor"
                  : retryDecision?.reason ===
                      "maximum-attempts-reached"
                    ? "Maksimum deneme sayısına ulaşıldı"
                    : "Otomatik tekrar uygulanamaz"}
              </strong>
            </span>
          )}
        </div>
      ) : null}

      {auditRecord ? (
        <OrderTransferAuditPreviewCard
          auditRecord={auditRecord}
        />
      ) : null}

      <div className="order-transfer-execution-preview__actions">
        <Button
          disabled={!result}
          leadingIcon={<RefreshCw />}
          onClick={clearExecutionResult}
          variant="secondary"
        >
          Sonucu temizle
        </Button>

        <Button
          disabled={
            !commandResult.success ||
            isExecuting
          }
          leadingIcon={
            isExecuting ? (
              <RefreshCw />
            ) : (
              <Play />
            )
          }
          onClick={handleExecute}
          variant="primary"
        >
          {isExecuting
            ? "Çalıştırılıyor"
            : "Önizlemeyi çalıştır"}
        </Button>
      </div>
    </Card>
  );
}