import { notFound } from "next/navigation";

import { DigitalWorkforceWorkerDetailView } from "@/features/digital-workforce/components/digital-workforce-worker-detail-view";
import { getDigitalWorker } from "@/features/digital-workforce/data/digital-workforce-demo-catalog";
import { resolveWorkerApiId } from "@/features/digital-workforce/domain/digital-workforce-api-ids";
import type { DigitalWorkerId } from "@/features/digital-workforce/domain/digital-workforce-types";
import { readDigitalWorkforceWorkerDetail } from "@/features/digital-workforce/server/digital-workforce-reader";

type PageProps = Readonly<{
  params: Promise<{ workerId: string }>;
  searchParams: Promise<{ case?: string }>;
}>;

export default async function DigitalWorkforceWorkerPage({ params, searchParams }: PageProps) {
  const { workerId } = await params;
  const worker = getDigitalWorker(workerId);
  if (!worker) {
    notFound();
  }

  const apiDetail = await readDigitalWorkforceWorkerDetail(resolveWorkerApiId(workerId as DigitalWorkerId));
  const query = await searchParams;
  return (
    <DigitalWorkforceWorkerDetailView
      apiDetail={apiDetail.status === "success" ? apiDetail.detail : null}
      apiError={apiDetail.status === "failure" ? apiDetail.message : null}
      focusCaseId={query.case ?? null}
      worker={worker}
    />
  );
}
