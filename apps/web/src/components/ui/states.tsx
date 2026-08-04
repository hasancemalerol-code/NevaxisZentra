import { CircleAlert, Inbox } from "lucide-react";
import type { ReactNode } from "react";

import { Button } from "./button";

export function Skeleton({ className = "", label = "İçerik yükleniyor" }: Readonly<{ className?: string; label?: string }>) {
  return <span className={`skeleton ${className}`.trim()} role="status" aria-label={label} />;
}

type StateProps = Readonly<{
  title: string;
  description: string;
  action?: ReactNode;
}>;

export function EmptyState({ title, description, action }: StateProps) {
  return (
    <div className="state-panel empty-state">
      <span className="state-icon" aria-hidden="true"><Inbox /></span>
      <div><h3>{title}</h3><p>{description}</p></div>
      {action ? <div>{action}</div> : null}
    </div>
  );
}

export function ErrorState({ title, description, onRetry }: StateProps & Readonly<{ onRetry?: () => void }>) {
  return (
    <div className="state-panel error-state" role="alert">
      <span className="state-icon" aria-hidden="true"><CircleAlert /></span>
      <div><h3>{title}</h3><p>{description}</p></div>
      {onRetry ? <Button variant="secondary" size="small" onClick={onRetry}>Yeniden dene</Button> : null}
    </div>
  );
}
