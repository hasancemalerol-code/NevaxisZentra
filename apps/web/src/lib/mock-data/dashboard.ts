/**
 * UI-02 dashboard preview data.
 *
 * This module contains deterministic, presentation-only mock data.
 * It must not be treated as live factory, ERP, machine, or operational data.
 */

export type DashboardTone =
  | "neutral"
  | "info"
  | "success"
  | "warning"
  | "danger";

export type OperationalStatus =
  | "healthy"
  | "attention"
  | "critical"
  | "offline"
  | "pending";

export type RiskLevel = "low" | "medium" | "high" | "critical";

export type ReviewStatus =
  | "not-required"
  | "waiting"
  | "in-review"
  | "approved"
  | "rejected";

export type MachineState =
  | "producing"
  | "waiting"
  | "setup"
  | "maintenance"
  | "fault"
  | "offline";

export type ProductionOrderStatus =
  | "on-track"
  | "attention"
  | "delayed"
  | "setup";

export interface DashboardHeaderData {
  title: string;
  subtitle: string;
  shiftLabel: string;
  reportingDate: string;
  lastUpdatedLabel: string;
  previewLabel: string;
}

export interface DashboardMetric {
  id: string;
  label: string;
  value: string;
  supportingText: string;
  changeLabel?: string;
  tone: DashboardTone;
}

export interface DailyOperationMetric {
  id: string;
  label: string;
  value: string;
  supportingText: string;
  progress?: number;
  tone: DashboardTone;
}

export interface ShiftSummary {
  id: string;
  name: string;
  timeRange: string;
  plannedQuantity: number;
  completedQuantity: number;
  activeMachineCount: number;
  delayedOrderCount: number;
  status: OperationalStatus;
}

export interface AiTaskSummary {
  id: string;
  title: string;
  description: string;
  source: string;
  responsibleDepartment: string;
  priority: RiskLevel;
  confidence: number;
  reviewStatus: ReviewStatus;
  dueLabel: string;
  statusLabel: string;
}

export interface CriticalRisk {
  id: string;
  category:
    | "delivery"
    | "machine"
    | "material"
    | "mold"
    | "quality";
  title: string;
  description: string;
  affectedArea: string;
  expectedImpact: string;
  severity: RiskLevel;
  reviewStatus: ReviewStatus;
}

export interface PlanConfidenceFactor {
  id: string;
  label: string;
  value: string;
  impact: "positive" | "neutral" | "negative";
}

export interface PlanConfidenceData {
  score: number;
  classification: string;
  humanReviewRequired: boolean;
  expectedCompletionLabel: string;
  unresolvedConstraintCount: number;
  factors: PlanConfidenceFactor[];
}

export interface MachineStatusItem {
  id: string;
  code: string;
  name: string;
  state: MachineState;
  stateLabel: string;
  currentJob: string;
  moldCode: string;
  cycleTimeLabel: string;
  utilization: number;
  activeAlarmCount: number;
}

export interface ProductionOrder {
  id: string;
  orderNumber: string;
  productName: string;
  machineCode: string;
  moldCode: string;
  plannedQuantity: number;
  producedQuantity: number;
  remainingQuantity: number;
  dueLabel: string;
  status: ProductionOrderStatus;
  statusLabel: string;
  riskLevel: RiskLevel;
}

export interface MaterialRisk {
  id: string;
  materialCode: string;
  materialName: string;
  coverageLabel: string;
  expectedShortageLabel: string;
  affectedOrderCount: number;
  supplierStatus: string;
  riskLevel: RiskLevel;
  recommendation: string;
  sourceLabel: string;
}

export interface MoldStatusItem {
  id: string;
  moldCode: string;
  productFamily: string;
  currentMachine: string;
  cycleCount: number;
  maintenanceThreshold: number;
  lastMaintenanceLabel: string;
  conditionLabel: string;
  riskLevel: RiskLevel;
}

export interface MaintenanceStatusItem {
  id: string;
  title: string;
  assetCode: string;
  type: "planned" | "active" | "unplanned" | "overdue";
  typeLabel: string;
  scheduledLabel: string;
  owner: string;
  status: OperationalStatus;
}

export interface QuickAction {
  id: string;
  label: string;
  description: string;
  href: string;
  availability: "preview" | "available";
}

export interface SystemHealthItem {
  id: string;
  name: string;
  description: string;
  status: OperationalStatus;
  statusLabel: string;
}

export const dashboardHeader: DashboardHeaderData = {
  title: "Operasyon Merkezi",
  subtitle:
    "Üretim, risk, makine, kalıp ve bakım sinyallerini tek operasyon görünümünde izleyin.",
  shiftLabel: "Gündüz vardiyası · 08:00–16:00",
  reportingDate: "4 Ağustos 2026",
  lastUpdatedLabel: "Son güncelleme: 15:42",
  previewLabel: "Önizleme verisi",
};

export const dashboardMetrics: DashboardMetric[] = [
  {
    id: "active-production-orders",
    label: "Aktif Üretim Emri",
    value: "18",
    supportingText: "12 emir plan dahilinde",
    changeLabel: "3 kritik inceleme",
    tone: "info",
  },
  {
    id: "plan-realization",
    label: "Plan Gerçekleşme",
    value: "%84",
    supportingText: "Günlük hedefe göre",
    changeLabel: "+%6 önceki vardiyaya göre",
    tone: "success",
  },
  {
    id: "critical-risks",
    label: "Kritik Risk",
    value: "4",
    supportingText: "İnsan incelemesi gerekiyor",
    changeLabel: "1 yeni risk",
    tone: "danger",
  },
  {
    id: "machine-utilization",
    label: "Makine Kullanımı",
    value: "%78",
    supportingText: "14 aktif makine",
    changeLabel: "2 makine bakımda",
    tone: "success",
  },
  {
    id: "delivery-risk",
    label: "Gecikme Riski",
    value: "3",
    supportingText: "Etkilenen müşteri siparişi",
    changeLabel: "En yakın termin: 6 saat",
    tone: "warning",
  },
  {
    id: "daily-production",
    label: "Bugünkü Üretim",
    value: "42.680",
    supportingText: "adet tamamlandı",
    changeLabel: "Hedef: 50.800",
    tone: "neutral",
  },
];

export const dailyOperationMetrics: DailyOperationMetric[] = [
  {
    id: "planned",
    label: "Planlanan üretim",
    value: "50.800 adet",
    supportingText: "3 vardiya toplamı",
    progress: 100,
    tone: "neutral",
  },
  {
    id: "completed",
    label: "Tamamlanan",
    value: "42.680 adet",
    supportingText: "Planın %84'ü",
    progress: 84,
    tone: "success",
  },
  {
    id: "remaining",
    label: "Kalan",
    value: "8.120 adet",
    supportingText: "Tahmini 5 saat 20 dakika",
    progress: 16,
    tone: "info",
  },
  {
    id: "quality-exceptions",
    label: "Kalite istisnası",
    value: "2",
    supportingText: "1 kritik, 1 incelemede",
    tone: "warning",
  },
];

export const shiftSummaries: ShiftSummary[] = [
  {
    id: "shift-morning",
    name: "Gündüz vardiyası",
    timeRange: "08:00–16:00",
    plannedQuantity: 18200,
    completedQuantity: 15760,
    activeMachineCount: 14,
    delayedOrderCount: 1,
    status: "healthy",
  },
  {
    id: "shift-evening",
    name: "Akşam vardiyası",
    timeRange: "16:00–00:00",
    plannedQuantity: 17600,
    completedQuantity: 0,
    activeMachineCount: 12,
    delayedOrderCount: 2,
    status: "attention",
  },
  {
    id: "shift-night",
    name: "Gece vardiyası",
    timeRange: "00:00–08:00",
    plannedQuantity: 15000,
    completedQuantity: 0,
    activeMachineCount: 10,
    delayedOrderCount: 0,
    status: "pending",
  },
];

export const aiTasks: AiTaskSummary[] = [
  {
    id: "ai-task-001",
    title: "Hammadde stoğu üretim planını karşılamıyor",
    description:
      "Mevcut kapsama süresi iki aktif üretim emrinin planlanan tüketimini karşılamıyor.",
    source: "Malzeme Risk Analizi",
    responsibleDepartment: "Satın Alma",
    priority: "critical",
    confidence: 0.94,
    reviewStatus: "waiting",
    dueLabel: "45 dakika içinde",
    statusLabel: "Onay bekliyor",
  },
  {
    id: "ai-task-002",
    title: "Enjeksiyon makinesinde çevrim süresi sapması",
    description:
      "M-12 çevrim süresi son 90 dakikada hedef değerin yüzde 14 üzerine çıktı.",
    source: "AI Üretim Planlayıcı",
    responsibleDepartment: "Planlama",
    priority: "high",
    confidence: 0.88,
    reviewStatus: "in-review",
    dueLabel: "2 saat içinde",
    statusLabel: "İnceleniyor",
  },
  {
    id: "ai-task-003",
    title: "Kalıp için kestirimci bakım önerisi",
    description:
      "KLP-104 kalıbında bakım ihtimalini yükselten basınç ve çevrim anomalileri tespit edildi.",
    source: "Kalıp Yaşam Döngüsü",
    responsibleDepartment: "Bakım",
    priority: "high",
    confidence: 0.91,
    reviewStatus: "waiting",
    dueLabel: "Vardiya bitmeden",
    statusLabel: "İnceleme gerekli",
  },
  {
    id: "ai-task-004",
    title: "SO-260804-18 teslimat riskini yönlendir",
    description:
      "Makine duruşu nedeniyle mevcut planda tahmini 6 saatlik termin sapması oluşuyor.",
    source: "Teslimat Risk Motoru",
    responsibleDepartment: "Sipariş Operasyonları",
    priority: "medium",
    confidence: 0.82,
    reviewStatus: "not-required",
    dueLabel: "Bugün 17:00",
    statusLabel: "Yönlendirme bekliyor",
  },
];

export const criticalRisks: CriticalRisk[] = [
  {
    id: "risk-delivery",
    category: "delivery",
    title: "SO-260804-18 termin riski",
    description:
      "M-04 arızası üretim tamamlanma tahminini müşteri termininden 6 saat sonraya taşıyor.",
    affectedArea: "Sipariş Operasyonları",
    expectedImpact: "1 müşteri siparişi",
    severity: "critical",
    reviewStatus: "waiting",
  },
  {
    id: "risk-material",
    category: "material",
    title: "PP GF30 kapsama açığı",
    description:
      "Mevcut ve teyitli tedarik miktarı iki aktif emir için yeterli görünmüyor.",
    affectedArea: "Satın Alma / Planlama",
    expectedImpact: "2 üretim emri",
    severity: "high",
    reviewStatus: "in-review",
  },
  {
    id: "risk-mold",
    category: "mold",
    title: "KLP-204 bakım eşiğine yaklaştı",
    description:
      "Çevrim sayısı bakım eşiğinin %96'sında ve son kalite kontrollerinde sapma oluştu.",
    affectedArea: "Bakım / Kalite",
    expectedImpact: "1 ürün ailesi",
    severity: "high",
    reviewStatus: "waiting",
  },
  {
    id: "risk-quality",
    category: "quality",
    title: "P-118 yüzey hatası artışı",
    description:
      "Son üç kontrolde yüzey hatası oranı vardiya ortalamasının üzerine çıktı.",
    affectedArea: "Kalite",
    expectedImpact: "2 parti",
    severity: "medium",
    reviewStatus: "in-review",
  },
];

export const planConfidence: PlanConfidenceData = {
  score: 86,
  classification: "Yüksek güven",
  humanReviewRequired: true,
  expectedCompletionLabel: "Tahmini tamamlanma: bugün 23:20",
  unresolvedConstraintCount: 3,
  factors: [
    {
      id: "factor-machine-capacity",
      label: "Makine kapasitesi",
      value: "Planla uyumlu",
      impact: "positive",
    },
    {
      id: "factor-material-coverage",
      label: "Malzeme kapsaması",
      value: "2 emirde riskli",
      impact: "negative",
    },
    {
      id: "factor-mold-availability",
      label: "Kalıp uygunluğu",
      value: "1 bakım riski",
      impact: "negative",
    },
    {
      id: "factor-shift-capacity",
      label: "Vardiya kapasitesi",
      value: "%91 yeterlilik",
      impact: "positive",
    },
  ],
};

export const machineStatuses: MachineStatusItem[] = [
  {
    id: "machine-01",
    code: "M-01",
    name: "Haitian Mars 160",
    state: "producing",
    stateLabel: "Üretimde",
    currentJob: "P-104 Gövde",
    moldCode: "KLP-118",
    cycleTimeLabel: "28,4 sn",
    utilization: 91,
    activeAlarmCount: 0,
  },
  {
    id: "machine-04",
    code: "M-04",
    name: "Engel Victory 200",
    state: "fault",
    stateLabel: "Arızalı",
    currentJob: "P-118 Kapak",
    moldCode: "KLP-204",
    cycleTimeLabel: "—",
    utilization: 46,
    activeAlarmCount: 3,
  },
  {
    id: "machine-07",
    code: "M-07",
    name: "Haitian Jupiter 280",
    state: "setup",
    stateLabel: "Ayar",
    currentJob: "P-220 Teknik Parça",
    moldCode: "KLP-311",
    cycleTimeLabel: "Hazırlanıyor",
    utilization: 62,
    activeAlarmCount: 1,
  },
  {
    id: "machine-09",
    code: "M-09",
    name: "Arburg Allrounder 470",
    state: "maintenance",
    stateLabel: "Bakımda",
    currentJob: "Planlı bakım",
    moldCode: "—",
    cycleTimeLabel: "—",
    utilization: 0,
    activeAlarmCount: 0,
  },
  {
    id: "machine-12",
    code: "M-12",
    name: "Sumitomo SE180",
    state: "waiting",
    stateLabel: "Beklemede",
    currentJob: "Hammadde bekliyor",
    moldCode: "KLP-402",
    cycleTimeLabel: "31,7 sn",
    utilization: 34,
    activeAlarmCount: 1,
  },
  {
    id: "machine-14",
    code: "M-14",
    name: "Fanuc Roboshot 150",
    state: "offline",
    stateLabel: "Bağlantı Yok",
    currentJob: "Telemetri alınamıyor",
    moldCode: "KLP-507",
    cycleTimeLabel: "—",
    utilization: 0,
    activeAlarmCount: 0,
  },
];

export const productionOrders: ProductionOrder[] = [
  {
    id: "order-01",
    orderNumber: "UE-260804-021",
    productName: "P-104 Gövde",
    machineCode: "M-01",
    moldCode: "KLP-118",
    plannedQuantity: 12000,
    producedQuantity: 9860,
    remainingQuantity: 2140,
    dueLabel: "Bugün 18:00",
    status: "on-track",
    statusLabel: "Plan dahilinde",
    riskLevel: "low",
  },
  {
    id: "order-02",
    orderNumber: "UE-260804-018",
    productName: "P-118 Kapak",
    machineCode: "M-04",
    moldCode: "KLP-204",
    plannedQuantity: 8500,
    producedQuantity: 4200,
    remainingQuantity: 4300,
    dueLabel: "Bugün 17:30",
    status: "delayed",
    statusLabel: "Gecikme riski",
    riskLevel: "critical",
  },
  {
    id: "order-03",
    orderNumber: "UE-260804-026",
    productName: "P-220 Teknik Parça",
    machineCode: "M-07",
    moldCode: "KLP-311",
    plannedQuantity: 6400,
    producedQuantity: 0,
    remainingQuantity: 6400,
    dueLabel: "Yarın 08:00",
    status: "setup",
    statusLabel: "Ayar aşamasında",
    riskLevel: "medium",
  },
  {
    id: "order-04",
    orderNumber: "UE-260804-029",
    productName: "P-305 Bağlantı Parçası",
    machineCode: "M-12",
    moldCode: "KLP-402",
    plannedQuantity: 9200,
    producedQuantity: 7140,
    remainingQuantity: 2060,
    dueLabel: "Yarın 12:00",
    status: "attention",
    statusLabel: "Malzeme bekliyor",
    riskLevel: "high",
  },
];

export const materialRisks: MaterialRisk[] = [
  {
    id: "material-risk-01",
    materialCode: "HM-PPGF30-01",
    materialName: "PP GF30 Siyah",
    coverageLabel: "7,5 saat",
    expectedShortageLabel: "Bugün 23:10",
    affectedOrderCount: 2,
    supplierStatus: "Teyit bekleniyor",
    riskLevel: "critical",
    recommendation: "Alternatif tedarik ve plan sırası birlikte incelenmeli.",
    sourceLabel: "ERP projeksiyonu",
  },
  {
    id: "material-risk-02",
    materialCode: "HM-ABS-12",
    materialName: "ABS Naturel",
    coverageLabel: "1,4 gün",
    expectedShortageLabel: "6 Ağustos",
    affectedOrderCount: 3,
    supplierStatus: "Sevkiyat yolda",
    riskLevel: "medium",
    recommendation: "Sevkiyat zamanı vardiya başlangıcında doğrulanmalı.",
    sourceLabel: "ERP projeksiyonu",
  },
  {
    id: "material-risk-03",
    materialCode: "HM-PA66-08",
    materialName: "PA66 V0",
    coverageLabel: "2,1 gün",
    expectedShortageLabel: "7 Ağustos",
    affectedOrderCount: 1,
    supplierStatus: "Onaylı",
    riskLevel: "low",
    recommendation: "Mevcut plan korunabilir.",
    sourceLabel: "ERP projeksiyonu",
  },
];

export const moldStatuses: MoldStatusItem[] = [
  {
    id: "mold-01",
    moldCode: "KLP-204",
    productFamily: "P-118 Kapak",
    currentMachine: "M-04",
    cycleCount: 192440,
    maintenanceThreshold: 200000,
    lastMaintenanceLabel: "18 Temmuz 2026",
    conditionLabel: "Bakım eşiğine yakın",
    riskLevel: "critical",
  },
  {
    id: "mold-02",
    moldCode: "KLP-311",
    productFamily: "P-220 Teknik Parça",
    currentMachine: "M-07",
    cycleCount: 114300,
    maintenanceThreshold: 150000,
    lastMaintenanceLabel: "9 Temmuz 2026",
    conditionLabel: "İzleniyor",
    riskLevel: "medium",
  },
  {
    id: "mold-03",
    moldCode: "KLP-118",
    productFamily: "P-104 Gövde",
    currentMachine: "M-01",
    cycleCount: 88420,
    maintenanceThreshold: 160000,
    lastMaintenanceLabel: "27 Temmuz 2026",
    conditionLabel: "Normal",
    riskLevel: "low",
  },
];

export const maintenanceStatuses: MaintenanceStatusItem[] = [
  {
    id: "maintenance-01",
    title: "M-04 hidrolik basınç kontrolü",
    assetCode: "M-04",
    type: "active",
    typeLabel: "Aktif müdahale",
    scheduledLabel: "Başlangıç: 14:35",
    owner: "Mekanik Bakım",
    status: "critical",
  },
  {
    id: "maintenance-02",
    title: "KLP-204 periyodik bakım incelemesi",
    assetCode: "KLP-204",
    type: "overdue",
    typeLabel: "Eşik yaklaşıyor",
    scheduledLabel: "Vardiya bitmeden",
    owner: "Kalıp Bakım",
    status: "attention",
  },
  {
    id: "maintenance-03",
    title: "M-09 planlı bakım",
    assetCode: "M-09",
    type: "planned",
    typeLabel: "Planlı",
    scheduledLabel: "15:00–17:00",
    owner: "Elektrik Bakım",
    status: "healthy",
  },
];

export const quickActions: QuickAction[] = [
  {
    id: "action-ai-task",
    label: "Yeni AI görevi oluştur",
    description: "İnceleme veya öneri taslağı başlatın.",
    href: "/ai-gorev-merkezi",
    availability: "preview",
  },
  {
    id: "action-plan-review",
    label: "Plan incelemesi başlat",
    description: "Üretim planı kısıtlarını değerlendirin.",
    href: "/planlama",
    availability: "preview",
  },
  {
    id: "action-risk-review",
    label: "Risk değerlendirmesi aç",
    description: "Kritik operasyon risklerini inceleyin.",
    href: "/dashboard",
    availability: "preview",
  },
  {
    id: "action-shift-handover",
    label: "Vardiya devrini görüntüle",
    description: "Son vardiya aktarım notlarına ulaşın.",
    href: "/vardiyalar",
    availability: "preview",
  },
  {
    id: "action-system-status",
    label: "Sistem durumunu kontrol et",
    description: "Bağlantı ve servis durumlarını görüntüleyin.",
    href: "/entegrasyonlar",
    availability: "preview",
  },
];

export const systemHealth: SystemHealthItem[] = [
  {
    id: "health-web",
    name: "Web",
    description: "Masaüstü web uygulaması",
    status: "healthy",
    statusLabel: "Sağlıklı",
  },
  {
    id: "health-api",
    name: "API",
    description: "Uygulama servis sınırı",
    status: "healthy",
    statusLabel: "Sağlıklı",
  },
  {
    id: "health-worker",
    name: "Worker",
    description: "Arka plan işlem hostu",
    status: "healthy",
    statusLabel: "Sağlıklı",
  },
  {
    id: "health-postgresql",
    name: "PostgreSQL",
    description: "Operasyonel veri altyapısı",
    status: "healthy",
    statusLabel: "Sağlıklı",
  },
  {
    id: "health-rabbitmq",
    name: "RabbitMQ",
    description: "Mesajlaşma altyapısı",
    status: "pending",
    statusLabel: "Bağlantı bekleniyor",
  },
  {
    id: "health-edge-agent",
    name: "Edge Agent",
    description: "Fabrika yerel bağlantı katmanı",
    status: "attention",
    statusLabel: "Uyarı",
  },
  {
    id: "health-erp-connector",
    name: "ERP Connector",
    description: "ERP okuma ve eşitleme sınırı",
    status: "offline",
    statusLabel: "Devre dışı",
  },
  {
    id: "health-machine-gateway",
    name: "Machine Gateway",
    description: "Makine telemetri geçidi",
    status: "pending",
    statusLabel: "Bağlantı bekleniyor",
  },
];

export const dashboardPreviewData = {
  header: dashboardHeader,
  metrics: dashboardMetrics,
  dailyOperations: dailyOperationMetrics,
  shifts: shiftSummaries,
  aiTasks,
  criticalRisks,
  planConfidence,
  machines: machineStatuses,
  productionOrders,
  materialRisks,
  molds: moldStatuses,
  maintenance: maintenanceStatuses,
  quickActions,
  systemHealth,
} as const;
