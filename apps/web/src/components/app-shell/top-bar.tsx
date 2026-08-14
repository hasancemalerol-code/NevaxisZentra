"use client";

import { Building2, ChevronDown, Search } from "lucide-react";
import { useCallback, useEffect, useId, useState } from "react";

import { AccountMenu } from "@/features/saas-identity/components/account-menu";

import { GlobalSearchPalette } from "./global-search-palette";
import { NotificationsControl } from "./notifications-control";
import type { ShellSearchItem } from "./shell-types";
import { useShellPopover } from "./use-shell-popover";
import { WorkspaceContextPanel } from "./workspace-context-panel";

type TopBarProps = Readonly<{
  accountDisplayName?: string | null;
  emailMasked?: string | null;
  organizationLabel?: string | null;
  searchItems: readonly ShellSearchItem[];
  settingsHref?: string | null;
}>;

export function TopBar({
  accountDisplayName = null,
  emailMasked = null,
  organizationLabel = null,
  searchItems,
  settingsHref = null,
}: TopBarProps) {
  const workspacePanelId = useId();
  const [workspaceOpen, setWorkspaceOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);

  const closeWorkspace = useCallback(() => {
    setWorkspaceOpen(false);
  }, []);

  const closeSearch = useCallback(() => {
    setSearchOpen(false);
  }, []);

  const workspaceRef = useShellPopover(workspaceOpen, closeWorkspace);
  const searchRef = useShellPopover(searchOpen, closeSearch);

  useEffect(() => {
    function handleShortcut(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setWorkspaceOpen(false);
        setSearchOpen(true);
      }
    }

    document.addEventListener("keydown", handleShortcut);
    return () => {
      document.removeEventListener("keydown", handleShortcut);
    };
  }, []);

  const workspaceButtonLabel = organizationLabel?.trim() || "Mevcut kuruluş";

  return (
    <header className="top-bar">
      <div
        className="workspace-context shell-anchor"
        ref={workspaceRef}
      >
        <span className="workspace-label">Çalışma alanı</span>
        <button
          aria-controls={workspacePanelId}
          aria-expanded={workspaceOpen}
          aria-haspopup="dialog"
          className="workspace-selector"
          onClick={() => {
            setSearchOpen(false);
            setWorkspaceOpen((current) => !current);
          }}
          type="button"
        >
          <Building2 aria-hidden="true" />
          <span>{workspaceButtonLabel}</span>
          <ChevronDown aria-hidden="true" />
        </button>

        {workspaceOpen ? (
          <WorkspaceContextPanel
            organizationLabel={organizationLabel}
            panelId={workspacePanelId}
          />
        ) : null}
      </div>

      <div className="top-bar-actions" aria-label="Uygulama araçları">
        <div className="shell-anchor shell-anchor--search" ref={searchRef}>
          <button
            aria-expanded={searchOpen}
            aria-haspopup="dialog"
            className="search-placeholder"
            onClick={() => {
              setWorkspaceOpen(false);
              setSearchOpen((current) => !current);
            }}
            type="button"
          >
            <Search aria-hidden="true" />
            <span>Modül veya sayfa ara</span>
            <kbd>⌘ K</kbd>
          </button>

          {searchOpen ? (
            <GlobalSearchPalette
              items={searchItems}
              onClose={closeSearch}
            />
          ) : null}
        </div>

        <NotificationsControl />

        <span className="top-bar-divider" aria-hidden="true" />

        {accountDisplayName ? (
          <AccountMenu
            displayName={accountDisplayName}
            emailMasked={emailMasked}
            organizationLabel={organizationLabel}
            settingsHref={settingsHref}
          />
        ) : (
          <span className="account-copy account-copy--placeholder">
            <strong>Hesap</strong>
            <small>Oturum bağlamı</small>
          </span>
        )}
      </div>
    </header>
  );
}
