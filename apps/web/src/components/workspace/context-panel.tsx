type ContextPanelRow = Readonly<{
  label: string;
  value: string | number | null | undefined;
}>;

type ContextPanelProps = Readonly<{
  title: string;
  rows: readonly ContextPanelRow[];
}>;

export function ContextPanel({ title, rows }: ContextPanelProps) {
  return (
    <aside className="fact-context-panel" data-testid="fact-context-panel">
      <header className="fact-context-panel__header">{title}</header>
      <dl className="fact-context-panel__body">
        {rows.map((row) => (
          <div key={row.label} className="fact-context-panel__row">
            <dt>{row.label}</dt>
            <dd>{row.value ?? "—"}</dd>
          </div>
        ))}
      </dl>
    </aside>
  );
}
