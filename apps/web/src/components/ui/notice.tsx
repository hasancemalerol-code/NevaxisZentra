import { CircleAlert, Info, ShieldCheck, Sparkles, TriangleAlert } from "lucide-react";
import type { ReactNode } from "react";

export type NoticeTone = "info" | "warning" | "danger" | "success" | "ai" | "neutral";

const ICONS: Record<NoticeTone, ReactNode> = {
  info: <Info aria-hidden="true" />,
  warning: <TriangleAlert aria-hidden="true" />,
  danger: <CircleAlert aria-hidden="true" />,
  success: <ShieldCheck aria-hidden="true" />,
  ai: <Sparkles aria-hidden="true" />,
  neutral: <Info aria-hidden="true" />,
};

type NoticeProps = Readonly<{
  tone?: NoticeTone;
  title?: string;
  children: ReactNode;
  /** Announces the notice to assistive technology (use for errors that appear after an action). */
  alert?: boolean;
  icon?: ReactNode;
  "data-testid"?: string;
}>;

/** Information callout used for disclaimers, read-only notes, action errors and AI/rules-based provenance. */
export function Notice({ tone = "info", title, children, alert = false, icon, ...props }: NoticeProps) {
  return (
    <div className={`zx-notice zx-notice--${tone}`} role={alert ? "alert" : undefined} {...props}>
      {icon ?? ICONS[tone]}
      <div className="zx-notice__body">
        {title ? <span className="zx-notice__title">{title}</span> : null}
        <p>{children}</p>
      </div>
    </div>
  );
}
