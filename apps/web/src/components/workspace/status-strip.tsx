type StatusStripItem = Readonly<{
  label: string;
  value: string | number;
}>;

type StatusStripProps = Readonly<{
  items: readonly StatusStripItem[];
  "aria-label"?: string;
}>;

export function StatusStrip({ items, "aria-label": ariaLabel }: StatusStripProps) {
  return (
    <section className="fact-status-strip" aria-label={ariaLabel}>
      {items.map((item) => (
        <article key={item.label} className="fact-status-strip__item">
          <span className="fact-status-strip__label">{item.label}</span>
          <strong className="fact-status-strip__value">{item.value}</strong>
        </article>
      ))}
    </section>
  );
}
