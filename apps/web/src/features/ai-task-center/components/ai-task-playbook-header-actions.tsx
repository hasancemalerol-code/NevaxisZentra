"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { ArrowLeft, Plus, RefreshCw } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  buildAiTaskPlaybookLibraryHref,
  parseAiTaskPlaybookLibraryView,
  type AiTaskPlaybookLibraryView,
} from "../presentation/ai-task-playbook-library-navigation";

type AiTaskPlaybookHeaderActionsProps = Readonly<{
  canManage: boolean;
  counts: Readonly<{
    playbooks: number;
    active: number;
    history: number;
  }>;
}>;

export function AiTaskPlaybookHeaderActions({
  canManage,
  counts,
}: AiTaskPlaybookHeaderActionsProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const activeView = parseAiTaskPlaybookLibraryView(searchParams.get("gorunum"));

  function setView(view: AiTaskPlaybookLibraryView) {
    const extras: Record<string, string> = {};
    const playbookFilter = searchParams.get("playbook");
    if (view === "history" && playbookFilter) {
      extras.playbook = playbookFilter;
    }
    router.push(buildAiTaskPlaybookLibraryHref(view, extras), { scroll: false });
  }

  function refresh() {
    router.refresh();
  }

  return (
    <div className="ai-task-playbook-page-actions">
      {canManage ? (
        <Link
          className="button button-primary button-medium ai-task-playbook-page-actions__primary"
          href="/ai-gorevleri/playbooklar/yeni"
        >
          <span className="button-icon" aria-hidden="true">
            <Plus />
          </span>
          <span>Yeni Playbook</span>
        </Link>
      ) : null}

      <Button
        aria-current={activeView === "active" ? "page" : undefined}
        type="button"
        variant={activeView === "active" ? "secondary" : "quiet"}
        onClick={() => setView("active")}
      >
        {counts.active > 0 ? `Çalışan Akışlar (${counts.active})` : "Çalışan Akışlar"}
      </Button>

      <Button
        aria-current={activeView === "history" ? "page" : undefined}
        type="button"
        variant={activeView === "history" ? "secondary" : "quiet"}
        onClick={() => setView("history")}
      >
        {counts.history > 0 ? `Geçmiş (${counts.history})` : "Geçmiş"}
      </Button>

      <Button leadingIcon={<RefreshCw />} type="button" variant="secondary" onClick={refresh}>
        Veriyi yenile
      </Button>

      <Link className="ai-task-playbook-back-link" href="/ai-gorevleri">
        <ArrowLeft aria-hidden="true" />
        <span>Görev listesine dön</span>
      </Link>
    </div>
  );
}
