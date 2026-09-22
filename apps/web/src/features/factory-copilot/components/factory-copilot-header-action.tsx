"use client";

import { Bot } from "lucide-react";

import { useFactoryCopilotShell } from "./factory-copilot-shell-context";

type FactoryCopilotHeaderActionProps = Readonly<{
  label?: string;
}>;

export function FactoryCopilotHeaderAction({ label = "Zentra Copilot" }: FactoryCopilotHeaderActionProps) {
  const copilot = useFactoryCopilotShell();

  if (!copilot?.canAccess) {
    return null;
  }

  return (
    <button
      type="button"
      className="factory-copilot-header-action"
      data-testid="factory-copilot-header-action"
      onClick={copilot.open}
    >
      <Bot aria-hidden="true" size={16} />
      {label}
    </button>
  );
}
