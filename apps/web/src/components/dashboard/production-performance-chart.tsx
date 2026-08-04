type ProductionPoint = {
  label: string;
  planned: number;
  actual: number;
};

const productionData: ProductionPoint[] = [
  { label: "29 Tem", planned: 46000, actual: 43200 },
  { label: "30 Tem", planned: 47500, actual: 44900 },
  { label: "31 Tem", planned: 48000, actual: 46300 },
  { label: "1 Ağu", planned: 49200, actual: 47100 },
  { label: "2 Ağu", planned: 50000, actual: 48600 },
  { label: "3 Ağu", planned: 50400, actual: 49200 },
  { label: "4 Ağu", planned: 50800, actual: 42680 },
];

const formatNumber = (value: number) =>
  new Intl.NumberFormat("tr-TR").format(value);

const chartWidth = 640;
const chartHeight = 228;
const padding = {
  top: 20,
  right: 22,
  bottom: 38,
  left: 46,
};

const chartInnerWidth =
  chartWidth - padding.left - padding.right;
const chartInnerHeight =
  chartHeight - padding.top - padding.bottom;

const allValues = productionData.flatMap((point) => [
  point.planned,
  point.actual,
]);

const minimumValue =
  Math.floor((Math.min(...allValues) - 3000) / 5000) * 5000;

const maximumValue =
  Math.ceil((Math.max(...allValues) + 1000) / 5000) * 5000;

const range = Math.max(maximumValue - minimumValue, 1);

const getX = (index: number) =>
  padding.left +
  (index / Math.max(productionData.length - 1, 1)) *
    chartInnerWidth;

const getY = (value: number) =>
  padding.top +
  chartInnerHeight -
  ((value - minimumValue) / range) * chartInnerHeight;

const createPath = (
  key: "planned" | "actual",
) =>
  productionData
    .map((point, index) => {
      const command = index === 0 ? "M" : "L";
      return `${command} ${getX(index)} ${getY(point[key])}`;
    })
    .join(" ");

const plannedPath = createPath("planned");
const actualPath = createPath("actual");

const actualAreaPath = [
  actualPath,
  `L ${getX(productionData.length - 1)} ${
    padding.top + chartInnerHeight
  }`,
  `L ${getX(0)} ${padding.top + chartInnerHeight}`,
  "Z",
].join(" ");

const latestPoint =
  productionData[productionData.length - 1];

const performanceRate = Math.round(
  (latestPoint.actual / latestPoint.planned) * 100,
);

export function ProductionPerformanceChart() {
  return (
    <section
      className="dashboard-panel dashboard-panel--production-performance"
      style={{
        display: "flex",
        minWidth: 0,
        minHeight: 370,
        height: 370,
        flexDirection: "column",
        overflow: "hidden",
      }}
    >
      <header
        className="dashboard-section-heading"
        style={{
          display: "flex",
          minHeight: 60,
          alignItems: "center",
          justifyContent: "space-between",
          gap: 12,
          padding: "10px 14px",
          borderBottom: "1px solid #d7e1ed",
        }}
      >
        <div style={{ minWidth: 0 }}>
          <p
            className="dashboard-section-heading__eyebrow"
            style={{
              margin: "0 0 3px",
              color: "#738198",
              fontSize: 10,
              fontWeight: 760,
              letterSpacing: "0.055em",
              lineHeight: 1.2,
              textTransform: "uppercase",
            }}
          >
            SON 7 GÜNLÜK ÜRETİM
          </p>

          <h2
            className="dashboard-section-heading__title"
            style={{
              margin: 0,
              color: "#10213d",
              fontSize: 16,
              fontWeight: 820,
              letterSpacing: "-0.025em",
              lineHeight: 1.2,
            }}
          >
            Üretim Performansı
          </h2>
        </div>

        <div
          style={{
            display: "grid",
            minWidth: 74,
            justifyItems: "end",
            gap: 2,
          }}
        >
          <strong
            style={{
              color:
                performanceRate >= 95
                  ? "#079765"
                  : performanceRate >= 85
                    ? "#d97706"
                    : "#dc3545",
              fontSize: 18,
              fontWeight: 850,
              lineHeight: 1,
            }}
          >
            %{performanceRate}
          </strong>

          <span
            style={{
              color: "#738198",
              fontSize: 9,
              fontWeight: 680,
            }}
          >
            plan karşılama
          </span>
        </div>
      </header>

      <div
        style={{
          display: "grid",
          minHeight: 0,
          flex: "1 1 auto",
          gridTemplateRows: "auto 1fr",
          gap: 8,
          padding: "12px 14px 10px",
        }}
      >
        <div
          style={{
            display: "flex",
            flexWrap: "wrap",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 10,
          }}
        >
          <div
            style={{
              display: "flex",
              flexWrap: "wrap",
              alignItems: "center",
              gap: 14,
            }}
          >
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 6,
                color: "#4f6078",
                fontSize: 10,
                fontWeight: 700,
              }}
            >
              <span
                aria-hidden="true"
                style={{
                  width: 16,
                  height: 3,
                  borderRadius: 999,
                  background: "#8da2bf",
                }}
              />
              Planlanan
            </span>

            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 6,
                color: "#4f6078",
                fontSize: 10,
                fontWeight: 700,
              }}
            >
              <span
                aria-hidden="true"
                style={{
                  width: 16,
                  height: 3,
                  borderRadius: 999,
                  background: "#1769e0",
                }}
              />
              Gerçekleşen
            </span>
          </div>

          <span
            style={{
              color: "#738198",
              fontSize: 9,
              fontWeight: 680,
            }}
          >
            Son gün farkı:{" "}
            <strong
              style={{
                color: "#dc3545",
                fontSize: 10,
                fontWeight: 800,
              }}
            >
              -
              {formatNumber(
                latestPoint.planned - latestPoint.actual,
              )}
            </strong>
          </span>
        </div>

        <div
          style={{
            minHeight: 0,
            overflow: "hidden",
            border: "1px solid #e1e8f1",
            borderRadius: 10,
            background:
              "linear-gradient(180deg, #ffffff 0%, #f8fbff 100%)",
          }}
        >
          <svg
            aria-label="Son yedi gün planlanan ve gerçekleşen üretim grafiği"
            role="img"
            viewBox={`0 0 ${chartWidth} ${chartHeight}`}
            style={{
              display: "block",
              width: "100%",
              height: "100%",
              minHeight: 220,
            }}
          >
            <defs>
              <linearGradient
                id="productionActualArea"
                x1="0"
                x2="0"
                y1="0"
                y2="1"
              >
                <stop
                  offset="0%"
                  stopColor="#1769e0"
                  stopOpacity="0.18"
                />
                <stop
                  offset="100%"
                  stopColor="#1769e0"
                  stopOpacity="0.01"
                />
              </linearGradient>
            </defs>

            {[0, 0.25, 0.5, 0.75, 1].map(
              (position) => {
                const y =
                  padding.top +
                  chartInnerHeight * position;
                const value =
                  maximumValue -
                  range * position;

                return (
                  <g key={position}>
                    <line
                      x1={padding.left}
                      x2={chartWidth - padding.right}
                      y1={y}
                      y2={y}
                      stroke="#dfe7f0"
                      strokeDasharray="4 5"
                      strokeWidth="1"
                    />

                    <text
                      x={padding.left - 8}
                      y={y + 4}
                      fill="#8290a4"
                      fontSize="10"
                      fontWeight="650"
                      textAnchor="end"
                    >
                      {Math.round(value / 1000)}K
                    </text>
                  </g>
                );
              },
            )}

            <path
              d={actualAreaPath}
              fill="url(#productionActualArea)"
            />

            <path
              d={plannedPath}
              fill="none"
              stroke="#8da2bf"
              strokeDasharray="6 5"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="2.5"
            />

            <path
              d={actualPath}
              fill="none"
              stroke="#1769e0"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="3.2"
            />

            {productionData.map((point, index) => {
              const x = getX(index);
              const plannedY = getY(point.planned);
              const actualY = getY(point.actual);

              return (
                <g key={point.label}>
                  <circle
                    cx={x}
                    cy={plannedY}
                    fill="#ffffff"
                    r="3.5"
                    stroke="#8da2bf"
                    strokeWidth="2"
                  />

                  <circle
                    cx={x}
                    cy={actualY}
                    fill="#ffffff"
                    r="4"
                    stroke="#1769e0"
                    strokeWidth="2.5"
                  />

                  <text
                    x={x}
                    y={chartHeight - 15}
                    fill="#738198"
                    fontSize="10"
                    fontWeight="650"
                    textAnchor="middle"
                  >
                    {point.label}
                  </text>
                </g>
              );
            })}

            <g>
              <rect
                x={getX(productionData.length - 1) - 40}
                y={getY(latestPoint.actual) - 30}
                width="80"
                height="22"
                rx="7"
                fill="#1769e0"
              />

              <text
                x={getX(productionData.length - 1)}
                y={getY(latestPoint.actual) - 15}
                fill="#ffffff"
                fontSize="10"
                fontWeight="800"
                textAnchor="middle"
              >
                {formatNumber(latestPoint.actual)}
              </text>
            </g>
          </svg>
        </div>
      </div>
    </section>
  );
}