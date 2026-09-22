import type {
  AppsOperationalQuantityBar,
  AppsOperationalTrendPoint,
} from "../domain/apps-operational-analytics-types";

const CHART_WIDTH = 520;
const CHART_HEIGHT = 220;
const PADDING = { top: 16, right: 16, bottom: 36, left: 44 };

function hasTrendData(points: readonly AppsOperationalTrendPoint[]): boolean {
  return points.some((point) => point.goodQuantity > 0);
}

export function AppsOperationalOutputTrendChart({
  points,
}: Readonly<{ points: readonly AppsOperationalTrendPoint[] }>) {
  if (!hasTrendData(points)) {
    return (
      <p className="apps-analytics-chart__empty" data-chart-empty="true" data-testid="apps-output-trend-empty">
        Bu dönem için üretim trendi henüz oluşmadı.
      </p>
    );
  }

  const innerWidth = CHART_WIDTH - PADDING.left - PADDING.right;
  const innerHeight = CHART_HEIGHT - PADDING.top - PADDING.bottom;
  const values = points.flatMap((point) => [point.goodQuantity, point.targetQuantity ?? 0]);
  const minValue = Math.min(...values, 0);
  const maxValue = Math.max(...values, 1);
  const range = Math.max(maxValue - minValue, 1);

  const getX = (index: number) =>
    PADDING.left + (index / Math.max(points.length - 1, 1)) * innerWidth;
  const getY = (value: number) =>
    PADDING.top + innerHeight - ((value - minValue) / range) * innerHeight;

  const actualPath = points
    .map((point, index) => `${index === 0 ? "M" : "L"} ${getX(index)} ${getY(point.goodQuantity)}`)
    .join(" ");

  const targetPoints = points.filter((point) => point.targetQuantity !== null);
  const targetPath = targetPoints.length >= 2
    ? targetPoints
        .map((point, index) => {
          const pointIndex = points.indexOf(point);
          return `${index === 0 ? "M" : "L"} ${getX(pointIndex)} ${getY(point.targetQuantity ?? 0)}`;
        })
        .join(" ")
    : "";

  const yTicks = 4;
  const tickValues = Array.from({ length: yTicks + 1 }, (_, index) =>
    minValue + (range / yTicks) * index,
  );

  return (
    <div className="apps-analytics-chart" data-chart-kind="output-trend" data-testid="apps-output-trend-chart">
      <div className="apps-analytics-chart__plot">
        <svg
          aria-label="İyi çıktı trend grafiği"
          role="img"
          viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`}
          preserveAspectRatio="xMidYMid meet"
        >
          {tickValues.map((tick) => {
            const y = getY(tick);
            return (
              <g key={tick}>
                <line
                  x1={PADDING.left}
                  x2={CHART_WIDTH - PADDING.right}
                  y1={y}
                  y2={y}
                  className="apps-analytics-chart__grid"
                />
                <text className="apps-analytics-chart__axis-y" x={PADDING.left - 6} y={y + 4} textAnchor="end">
                  {Math.round(tick).toLocaleString("tr-TR")}
                </text>
              </g>
            );
          })}
          {targetPath ? (
            <path className="apps-analytics-chart__line apps-analytics-chart__line--target" d={targetPath} fill="none" />
          ) : null}
          <path className="apps-analytics-chart__line apps-analytics-chart__line--actual" d={actualPath} fill="none" />
          {points.map((point, index) => (
            <circle
              key={`${point.label}-${index}`}
              className="apps-analytics-chart__point"
              cx={getX(index)}
              cy={getY(point.goodQuantity)}
              r="3.5"
            />
          ))}
          {points.map((point, index) =>
            index % Math.max(1, Math.floor(points.length / 6)) === 0 ? (
              <text
                key={`label-${point.label}-${index}`}
                className="apps-analytics-chart__axis-x"
                x={getX(index)}
                y={CHART_HEIGHT - 8}
                textAnchor="middle"
              >
                {point.label}
              </text>
            ) : null,
          )}
        </svg>
      </div>
      <div className="apps-analytics-chart__legend">
        <span><i className="apps-analytics-chart__legend-line apps-analytics-chart__legend-line--actual" />İyi çıktı</span>
        {targetPath ? (
          <span><i className="apps-analytics-chart__legend-line apps-analytics-chart__legend-line--target" />Hedef</span>
        ) : null}
      </div>
    </div>
  );
}

export function AppsOperationalQuantityBarChart({
  title,
  bars,
  testId,
}: Readonly<{
  title: string;
  bars: readonly AppsOperationalQuantityBar[];
  testId: string;
}>) {
  if (bars.length === 0) {
    return (
      <p className="apps-analytics-chart__empty" data-chart-empty="true" data-testid={`${testId}-empty`}>
        Dağılım için kayıt yok.
      </p>
    );
  }

  const max = Math.max(...bars.map((bar) => bar.count), 1);
  const chartWidth = 360;
  const chartHeight = 200;
  const padding = { top: 16, right: 12, bottom: 34, left: 12 };
  const innerHeight = chartHeight - padding.top - padding.bottom;
  const barWidth = Math.min(48, (chartWidth - padding.left - padding.right - (bars.length - 1) * 10) / bars.length);
  const gap = bars.length > 1
    ? (chartWidth - padding.left - padding.right - barWidth * bars.length) / (bars.length - 1)
    : 0;

  return (
    <div className="apps-analytics-chart" data-chart-kind="quantity-bars" data-testid={testId}>
      <h3 className="apps-analytics-chart__title">{title}</h3>
      <div className="apps-analytics-chart__plot">
        <svg
          aria-label={title}
          role="img"
          viewBox={`0 0 ${chartWidth} ${chartHeight}`}
          preserveAspectRatio="xMidYMid meet"
        >
          {bars.map((bar, index) => {
            const barHeight = (bar.count / max) * innerHeight;
            const x = padding.left + index * (barWidth + gap);
            const y = padding.top + innerHeight - barHeight;
            return (
              <g key={bar.label}>
                <rect className="apps-analytics-chart__bar" height={barHeight} rx="3" width={barWidth} x={x} y={y} />
                <text className="apps-analytics-chart__bar-value" x={x + barWidth / 2} y={y - 4} textAnchor="middle">
                  {bar.count}
                </text>
                <text
                  className="apps-analytics-chart__axis-x"
                  x={x + barWidth / 2}
                  y={chartHeight - 8}
                  textAnchor="middle"
                >
                  {bar.label.length > 14 ? `${bar.label.slice(0, 12)}…` : bar.label}
                </text>
              </g>
            );
          })}
        </svg>
      </div>
      <ul className="apps-analytics-chart__table">
        {bars.map((bar) => (
          <li key={bar.label}>
            <span>{bar.label}</span>
            <strong>{bar.count}</strong>
          </li>
        ))}
      </ul>
    </div>
  );
}
