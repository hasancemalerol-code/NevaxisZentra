import { formatNumber } from "@/lib/presentation/format";

export type TrendBarPoint = Readonly<{ label: string; passed: number; failed: number; marker?: number }>;

/**
 * Stacked pass/fail bars per period with an optional marker value (for example open NCR count).
 * Pure SVG from rows the screen already has; the matching table stays the accessible source of truth.
 */
export function TrendBars({
  points,
  passedLabel,
  failedLabel,
  markerLabel,
  ariaLabel,
}: Readonly<{ points: readonly TrendBarPoint[]; passedLabel: string; failedLabel: string; markerLabel?: string; ariaLabel: string }>) {
  if (points.length === 0 || points.every((point) => point.passed + point.failed + (point.marker ?? 0) === 0)) {
    return null;
  }

  const width = 640;
  const height = 168;
  const pad = { top: 12, right: 12, bottom: 28, left: 34 };
  const innerW = width - pad.left - pad.right;
  const innerH = height - pad.top - pad.bottom;
  const peak = Math.max(1, ...points.map((point) => Math.max(point.passed + point.failed, point.marker ?? 0)));
  const slot = innerW / points.length;
  const barW = Math.min(36, slot * 0.6);
  const y = (value: number) => pad.top + innerH - (value / peak) * innerH;

  return (
    <figure className="zx-trend">
      <svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label={ariaLabel} className="zx-trend__svg">
        {[0, 0.5, 1].map((fraction) => (
          <g key={fraction}>
            <line x1={pad.left} x2={width - pad.right} y1={y(peak * fraction)} y2={y(peak * fraction)} className="zx-trend__grid" />
            <text x={pad.left - 6} y={y(peak * fraction) + 4} textAnchor="end" className="zx-trend__tick">
              {formatNumber(peak * fraction, 0)}
            </text>
          </g>
        ))}
        {points.map((point, index) => {
          const x = pad.left + slot * index + (slot - barW) / 2;
          const passedH = (point.passed / peak) * innerH;
          const failedH = (point.failed / peak) * innerH;
          return (
            <g key={point.label}>
              <rect x={x} y={y(point.passed)} width={barW} height={passedH} rx={3} className="zx-trend__passed" />
              <rect x={x} y={y(point.passed + point.failed)} width={barW} height={failedH} rx={3} className="zx-trend__failed" />
              {point.marker !== undefined && point.marker > 0 ? (
                <circle cx={x + barW / 2} cy={y(point.marker)} r={5} className="zx-trend__marker" />
              ) : null}
              <text x={x + barW / 2} y={height - 8} textAnchor="middle" className="zx-trend__tick">
                {point.label}
              </text>
            </g>
          );
        })}
      </svg>
      <figcaption className="zx-trend__legend">
        <span><i className="zx-trend__key zx-trend__key--passed" aria-hidden="true" />{passedLabel}</span>
        <span><i className="zx-trend__key zx-trend__key--failed" aria-hidden="true" />{failedLabel}</span>
        {markerLabel ? <span><i className="zx-trend__key zx-trend__key--marker" aria-hidden="true" />{markerLabel}</span> : null}
      </figcaption>
    </figure>
  );
}
