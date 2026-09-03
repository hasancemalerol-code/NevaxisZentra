import type {
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from "react";

type AiTaskDetailFieldProps = Readonly<{
  label: string;
  helper?: string;
  children: ReactNode;
  className?: string;
}>;

export function AiTaskDetailField({
  label,
  helper,
  children,
  className = "",
}: AiTaskDetailFieldProps) {
  return (
    <label className={`ai-task-detail-field ${className}`.trim()}>
      <span className="ai-task-detail-field__label">{label}</span>
      {children}
      {helper ? (
        <span className="ai-task-detail-field__helper">{helper}</span>
      ) : null}
    </label>
  );
}

type AiTaskDetailSelectProps = SelectHTMLAttributes<HTMLSelectElement>;

export function AiTaskDetailSelect({
  className = "",
  ...props
}: AiTaskDetailSelectProps) {
  return (
    <select
      className={`ai-task-detail-field__control ai-task-detail-field__select ${className}`.trim()}
      {...props}
    />
  );
}

type AiTaskDetailInputProps = InputHTMLAttributes<HTMLInputElement>;

export function AiTaskDetailInput({
  className = "",
  ...props
}: AiTaskDetailInputProps) {
  return (
    <input
      className={`ai-task-detail-field__control ai-task-detail-field__input ${className}`.trim()}
      {...props}
    />
  );
}

type AiTaskDetailTextareaProps = TextareaHTMLAttributes<HTMLTextAreaElement>;

export function AiTaskDetailTextarea({
  className = "",
  ...props
}: AiTaskDetailTextareaProps) {
  return (
    <textarea
      className={`ai-task-detail-field__control ai-task-detail-field__textarea ${className}`.trim()}
      {...props}
    />
  );
}

type AiTaskDetailSectionCardProps = Readonly<{
  title: string;
  eyebrow?: string;
  children: ReactNode;
  className?: string;
  actions?: ReactNode;
}>;

export function AiTaskDetailSectionCard({
  title,
  eyebrow,
  children,
  className = "",
  actions,
}: AiTaskDetailSectionCardProps) {
  return (
    <section className={`ai-task-detail-section ${className}`.trim()}>
      <header className="ai-task-detail-section__header">
        <div>
          {eyebrow ? (
            <p className="ai-task-detail-section__eyebrow">{eyebrow}</p>
          ) : null}
          <h3 className="ai-task-detail-section__title">{title}</h3>
        </div>
        {actions ? (
          <div className="ai-task-detail-section__actions">{actions}</div>
        ) : null}
      </header>
      <div className="ai-task-detail-section__body">{children}</div>
    </section>
  );
}

type AiTaskDetailValueRowProps = Readonly<{
  label: string;
  value: ReactNode;
}>;

export function AiTaskDetailValueRow({
  label,
  value,
}: AiTaskDetailValueRowProps) {
  return (
    <div className="ai-task-detail-value-row">
      <span className="ai-task-detail-value-row__label">{label}</span>
      <span className="ai-task-detail-value-row__value">{value}</span>
    </div>
  );
}
