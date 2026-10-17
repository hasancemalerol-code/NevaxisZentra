"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Plus, RefreshCw } from "lucide-react";

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
      {canManage && counts.playbooks > 0 ? (
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
        variant="secondary"
        onClick={() => setView("active")}
      >
        {counts.active > 0 ? `Çalışan Akışlar (${counts.active})` : "Çalışan Akışlar"}
      </Button>

      <Button
        aria-current={activeView === "history" ? "page" : undefined}
        type="button"
        variant="secondary"
        onClick={() => setView("history")}
      >
        {counts.history > 0 ? `Geçmiş (${counts.history})` : "Geçmiş"}
      </Button>

      <Button leadingIcon={<RefreshCw />} type="button" variant="secondary" onClick={refresh}>
        Veriyi yenile
      </Button>
    </div>
  );
}
