import Link from "next/link";
import { TriangleAlert } from "lucide-react";

import { GoldenDemoCustomsFileWorkspaceView } from "@/features/golden-demo-customs-file/components/golden-demo-customs-file-workspace-view";
import {
  GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE,
  GOLDEN_DEMO_CUSTOMS_FILE_TITLE,
} from "@/features/golden-demo-customs-file/domain/golden-demo-customs-file-labels";
import { readGoldenDemoCustomsFileWorkspace } from "@/features/golden-demo-customs-file/server/golden-demo-customs-file-reader";
import { forbidUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";

export default async function GoldenDemoCustomsFilePage() {
  await forbidUnlessPresentationModeAccess();

  const result = await readGoldenDemoCustomsFileWorkspace();

  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">ALTIN DEMO</p>
            <h1>{GOLDEN_DEMO_CUSTOMS_FILE_TITLE}</h1>
            <p className="commint-executive__subtitle">{GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE}</p>
          </div>
        </header>
        <div role="alert" className="commint-error-banner" data-testid="golden-demo-customs-file-forbidden">
          <TriangleAlert aria-hidden="true" />
          <div>
            <h3>Veri yüklenemedi</h3>
            <p>{result.message}</p>
            <Link href="/operasyon/yonetim-merkezi">Operasyon Yönetim Merkezi</Link>
          </div>
        </div>
      </main>
    );
  }

  return <GoldenDemoCustomsFileWorkspaceView workspace={result.workspace} />;
}
