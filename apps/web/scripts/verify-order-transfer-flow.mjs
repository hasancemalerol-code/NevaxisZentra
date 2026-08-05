import {
  readFileSync,
} from "node:fs";
import {
  resolve,
} from "node:path";

const repositoryRoot = process.cwd();

const files = {
  readiness: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-readiness.ts",
  ),
  command: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-command.ts",
  ),
  executor: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-executor.ts",
  ),
  previewExecutor: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-preview-executor.ts",
  ),
  readinessPanel: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/components/order-transfer-readiness-panel.tsx",
  ),
  commandPanel: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/components/order-transfer-command-panel.tsx",
  ),
  executionPreviewPanel: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/components/order-transfer-execution-preview-panel.tsx",
  ),
  auditRecord: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-audit-record.ts",
  ),
  auditLabels: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/presentation/order-transfer-audit-labels.ts",
  ),
  auditCard: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/components/order-transfer-audit-preview-card.tsx",
  ),  auditRepository: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-audit-repository.ts",
  ),
  auditMemoryRepository: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-audit-memory-repository.ts",
  ),
  transferService: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-service.ts",
  ),
  previewService: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-preview-service.ts",
  ),  transferAuthorization: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-authorization.ts",
  ),
  transferServerCommand: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-server-command.ts",
  ),
  transferRetryPolicy: resolve(
    repositoryRoot,
    "apps/web/src/features/orders/application/order-transfer-retry-policy.ts",
  ),  detailPage: resolve(
    repositoryRoot,
    "apps/web/src/app/(app)/siparisler/[orderDraftId]/page.tsx",
  ),
};

function read(path) {
  return readFileSync(path, "utf8");
}

function assertContains(
  content,
  expected,
  message,
) {
  if (!content.includes(expected)) {
    throw new Error(message);
  }
}

function assertBefore(
  content,
  first,
  second,
  message,
) {
  const firstIndex = content.indexOf(first);
  const secondIndex = content.indexOf(second);

  if (
    firstIndex === -1 ||
    secondIndex === -1 ||
    firstIndex >= secondIndex
  ) {
    throw new Error(message);
  }
}

const readiness = read(files.readiness);
const command = read(files.command);
const executor = read(files.executor);
const previewExecutor = read(
  files.previewExecutor,
);
const readinessPanel = read(
  files.readinessPanel,
);
const commandPanel = read(files.commandPanel);
const executionPreviewPanel = read(
  files.executionPreviewPanel,
);
const auditRecord = read(files.auditRecord);
const auditLabels = read(files.auditLabels);
const auditCard = read(files.auditCard);const auditRepository = read(files.auditRepository);
const auditMemoryRepository = read(
  files.auditMemoryRepository,
);
const transferService = read(files.transferService);
const previewService = read(files.previewService);const transferAuthorization = read(
  files.transferAuthorization,
);
const transferServerCommand = read(
  files.transferServerCommand,
);
const transferRetryPolicy = read(
  files.transferRetryPolicy,
);const detailPage = read(files.detailPage);

assertContains(
  readiness,
  "evaluateOrderTransferReadiness",
  "Merkezi aktarım hazırlık değerlendirmesi bulunamadı.",
);

assertContains(
  readiness,
  "hasBlockingOrderIssues",
  "Engelleyici sipariş sorunları aktarım hazırlığında doğrulanmıyor.",
);

assertContains(
  command,
  "evaluateOrderTransferReadiness",
  "Aktarım komutu merkezi hazırlık değerlendirmesini kullanmıyor.",
);

assertContains(
  command,
  'orderDraft.approval.status !== "approved"',
  "Aktarım komutunda yetkili onay kontrolü bulunamadı.",
);

assertContains(
  command,
  "expectedUpdatedAt",
  "Aktarım komutunda optimistic concurrency alanı bulunamadı.",
);

assertContains(
  executor,
  "hasOrderTransferStateConflict",
  "Yürütücüde sipariş durum çakışması kontrolü bulunamadı.",
);

assertContains(
  executor,
  "command.expectedUpdatedAt",
  "Yürütücü güncel sipariş sürümünü doğrulamıyor.",
);

assertContains(
  executor,
  "approval-invalid",
  "Yürütücüde son onay doğrulaması bulunamadı.",
);

assertContains(
  executor,
  "target-unavailable",
  "Yürütücüde hedef sistem doğrulaması bulunamadı.",
);

assertContains(
  previewExecutor,
  "validateOrderTransferExecutionInput",
  "Önizleme yürütücüsü ortak yürütme doğrulamasını kullanmıyor.",
);

assertContains(
  previewExecutor,
  "PREVIEW-",
  "Önizleme harici sipariş kimliği açık biçimde işaretlenmiyor.",
);

assertContains(
  readinessPanel,
  "evaluateOrderTransferReadiness",
  "Hazırlık paneli merkezi uygulama kuralına bağlı değil.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);
assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);
assertContains(
  executionPreviewPanel,
  "gerçek ERP veya MES sistemine veri",
  "Önizleme panelindeki gerçek sistem güvenlik uyarısı bulunamadı.",
);

assertBefore(
  detailPage,
  "<OrderTransferReadinessPanel",
  "<OrderTransferCommandPanel",
  "Aktarım hazırlık paneli, aktarım komutu panelinden önce bulunmalıdır.",
);

assertBefore(
  detailPage,
  "<OrderTransferCommandPanel",
  "<OrderTransferExecutionPreviewPanel",
  "Aktarım komutu paneli, yürütme önizlemesinden önce bulunmalıdır.",
);

assertBefore(
  detailPage,
  "<OrderTransferExecutionPreviewPanel",
  "<OrderTransferTargetCard",
  "Yürütme önizlemesi, hedef sistem özetinden önce bulunmalıdır.",
);


assertContains(
  auditRecord,
  "createOrderTransferAuditRecord",
  "Aktarım sonucundan audit kaydı üreten fonksiyon bulunamadı.",
);

assertContains(
  auditRecord,
  "approvalId: command.approvalId",
  "Audit kaydı aktarım onay referansını korumuyor.",
);

assertContains(
  auditRecord,
  "requestedByUserId",
  "Audit kaydı işlemi başlatan kullanıcıyı korumuyor.",
);

assertContains(
  auditRecord,
  "externalOrderId",
  "Audit kaydı harici sipariş numarasını desteklemiyor.",
);

assertContains(
  auditRecord,
  "failureCode",
  "Audit kaydı aktarım hata kodunu desteklemiyor.",
);

assertContains(
  auditRecord,
  "completedSteps",
  "Audit kaydı kısmi aktarım adımlarını desteklemiyor.",
);

assertContains(
  auditLabels,
  "getOrderTransferAuditOutcomeLabel",
  "Audit sonuç etiketleyicisi bulunamadı.",
);

assertContains(
  auditLabels,
  "getOrderTransferAuditFollowUpLabel",
  "Audit takip gereksinimi etiketi bulunamadı.",
);

assertContains(
  auditCard,
  "OrderTransferAuditPreviewCard",
  "Ayrı aktarım audit kartı bileşeni bulunamadı.",
);

assertContains(
  auditCard,
  "getOrderTransferAuditOutcomeDescription",
  "Audit kartı merkezi sonuç açıklamalarını kullanmıyor.",
);

assertContains(
  auditCard,
  "getOrderTargetSystemLabel",
  "Audit kartı hedef sistem etiketini merkezi kaynaktan almıyor.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);
assertContains(
  executionPreviewPanel,
  "<OrderTransferAuditPreviewCard",
  "Yürütme önizleme paneli ayrı audit kartını kullanmıyor.",
);

if (
  executionPreviewPanel.includes(
    '<section className="order-transfer-execution-preview__audit">',
  )
) {
  throw new Error(
    "Gömülü audit JSX bloğu hâlâ yürütme önizleme panelinde bulunuyor.",
  );
}

assertContains(
  auditRepository,
  "export interface OrderTransferAuditRepository",
  "Aktarım audit repository sözleşmesi bulunamadı.",
);

assertContains(
  auditRepository,
  "normalizeOrderTransferAuditQuery",
  "Audit sorgu normalizasyonu bulunamadı.",
);

assertContains(
  auditRepository,
  "isOrderTransferAuditQueryDateRangeValid",
  "Audit tarih aralığı doğrulaması bulunamadı.",
);

assertContains(
  auditMemoryRepository,
  "createOrderTransferAuditMemoryRepository",
  "Bellek içi audit repository implementasyonu bulunamadı.",
);


assertContains(
  auditMemoryRepository,
  "const existingRecord = records.get(",
  "Bellek içi repository mevcut audit kaydını kontrol etmiyor.",
);

assertContains(
  auditMemoryRepository,
  "created: false",
  "Bellek içi repository tekrar kayıtta created=false döndürmüyor.",
);

assertContains(
  auditMemoryRepository,
  "records.set(record.id, record)",
  "Bellek içi repository yeni audit kaydını saklamıyor.",
);

assertContains(
  auditMemoryRepository,
  "created: true",
  "Bellek içi repository yeni kayıtta created=true döndürmüyor.",
);
assertContains(
  auditMemoryRepository,
  "findByCommandId",
  "Bellek içi repository komut kimliği sorgusunu desteklemiyor.",
);

assertContains(
  transferService,
  "createOrderTransferService",
  "Aktarım uygulama servisi bulunamadı.",
);

assertContains(
  transferService,
  "executor.execute",
  "Aktarım servisi yürütücüyü çağırmıyor.",
);

assertContains(
  transferService,
  "createOrderTransferAuditRecord",
  "Aktarım servisi yürütme sonucundan audit kaydı üretmiyor.",
);

assertContains(
  transferService,
  "auditRepository.save",
  "Aktarım servisi audit kaydını repository üzerinden saklamıyor.",
);

assertContains(
  transferService,
  '"audit-save-failed"',
  "Aktarım servisi audit kayıt hatasını ayrı sonuç olarak yönetmiyor.",
);

assertContains(
  previewService,
  "createOrderTransferPreviewService",
  "Kontrollü önizleme servis fabrikası bulunamadı.",
);

assertContains(
  previewService,
  "createOrderTransferAuditMemoryRepository",
  "Önizleme servisi bellek içi audit repository kullanmıyor.",
);

assertContains(
  previewService,
  "createOrderTransferService",
  "Önizleme servisi ortak aktarım servisini kullanmıyor.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);
assertContains(
  executionPreviewPanel,
  "createOrderTransferPreviewService",
  "Yürütme önizleme paneli önizleme servis fabrikasını kullanmıyor.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);
assertContains(
  executionPreviewPanel,
  "executeOrderTransferPreview",
  "Yürütme önizleme paneli servis üzerinden yürütme yapmıyor.",
);

if (
  executionPreviewPanel.includes(
    "createOrderTransferPreviewExecutor",
  )
) {
  throw new Error(
    "Yürütme önizleme paneli doğrudan executor oluşturmamalıdır.",
  );
}

if (
  executionPreviewPanel.includes(
    "createOrderTransferAuditRecord",
  )
) {
  throw new Error(
    "Yürütme önizleme paneli audit kaydını doğrudan üretmemelidir.",
  );
}

assertContains(
  transferAuthorization,
  "authorizeOrderTransfer",
  "Merkezi sipariş aktarım yetki politikası bulunamadı.",
);

assertContains(
  transferAuthorization,
  '"tenant-mismatch"',
  "Aktarım yetki politikası tenant kapsamını doğrulamıyor.",
);

assertContains(
  transferAuthorization,
  '"target-system-not-allowed"',
  "Aktarım yetki politikası hedef sistem bazlı rol kontrolü yapmıyor.",
);

assertContains(
  transferAuthorization,
  "ALLOWED_TRANSFER_ROLES",
  "Aktarım yapabilecek roller merkezi olarak tanımlanmamış.",
);

assertContains(
  transferServerCommand,
  "prepareOrderTransferServerCommand",
  "Sunucu aktarım komutu hazırlayıcısı bulunamadı.",
);

assertContains(
  transferServerCommand,
  "authorizeOrderTransfer",
  "Sunucu komutu aktarım yetkisini yeniden doğrulamıyor.",
);

assertContains(
  transferServerCommand,
  "validateOrderTransferExecutionInput",
  "Sunucu komutu güncel sipariş durumunu yeniden doğrulamıyor.",
);

assertContains(
  transferServerCommand,
  "command.requestedByUserId !== actor.userId",
  "Sunucu komutu oturum kullanıcısı ile komut sahibini karşılaştırmıyor.",
);

assertContains(
  transferServerCommand,
  "createOrderTransferIdempotencyKey",
  "Aktarım için deterministik idempotency anahtarı üretilmiyor.",
);

assertContains(
  transferServerCommand,
  "idempotencyKey",
  "Sunucu aktarım komutunda idempotency alanı bulunamadı.",
);

assertContains(
  transferRetryPolicy,
  "evaluateOrderTransferRetry",
  "Merkezi aktarım yeniden deneme politikası bulunamadı.",
);

assertContains(
  transferRetryPolicy,
  "MAXIMUM_RETRY_ATTEMPTS = 3",
  "Maksimum yeniden deneme sayısı üç olarak sınırlandırılmamış.",
);

assertContains(
  transferRetryPolicy,
  '"target-unavailable"',
  "Geçici hedef sistem hatası retry politikasında bulunamadı.",
);

assertContains(
  transferRetryPolicy,
  '"timeout"',
  "Zaman aşımı hatası retry politikasında bulunamadı.",
);

assertContains(
  transferRetryPolicy,
  '"manual-review-required"',
  "İnsan incelemesi gerektiren hata sonucu bulunamadı.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);
assertContains(
  executionPreviewPanel,
  "evaluateOrderTransferRetry",
  "Yürütme önizleme paneli merkezi retry politikasını kullanmıyor.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);
assertContains(
  executionPreviewPanel,
  "retryDecision",
  "Yürütme önizleme panelinde retry kararı gösterilmiyor.",
);

assertContains(
  commandPanel,
  "createOrderTransferCommand",
  "Aktarım komutu paneli güvenli komut oluşturucuyu kullanmıyor.",
);

assertContains(
  commandPanel,
  "disabled={!result.success}",
  "Aktarım başlatma düğmesi başarısız komutta devre dışı bırakılmıyor.",
);

assertContains(
  commandPanel,
  "Gerçek",
  "Aktarım komutu panelinde gerçek sistem işlemi sınırı açıklanmıyor.",
);

assertContains(
  commandPanel,
  "sunucu",
  "Aktarım komutu panelinde sunucu tarafı işlem sınırı açıklanmıyor.",
);
assertContains(
  executionPreviewPanel,
  "Manuel inceleme gerekiyor",
  "Kullanıcıya manuel inceleme geri bildirimi gösterilmiyor.",
);

if (
  transferRetryPolicy.includes(
    "setTimeout(",
  )
) {
  throw new Error(
    "Retry politikası doğrudan otomatik zamanlayıcı başlatmamalıdır.",
  );
}

if (
  executionPreviewPanel.includes(
    "setTimeout(",
  )
) {
  throw new Error(
    "Önizleme paneli gerçek otomatik yeniden deneme başlatmamalıdır.",
  );
}

assertContains(
  executionPreviewPanel,
  "createOrderTransferPreviewService",
  "Yürütme önizleme paneli kontrollü önizleme servisini kullanmıyor.",
);

assertContains(
  executionPreviewPanel,
  "executeOrderTransferPreview",
  "Yürütme önizleme paneli aktarımı servis üzerinden yürütmüyor.",
);

assertContains(
  executionPreviewPanel,
  "executeOrderTransferPreview",
  "Yürütme önizleme paneli audit üreten servis akışını çağırmıyor.",
);

assertContains(
  executionPreviewPanel,
  "serviceResult.auditRecord",
  "Yürütme önizleme paneli servis tarafından üretilen audit kaydını kullanmıyor.",
);

assertContains(
  transferService,
  "createOrderTransferAuditRecord",
  "Aktarım servisi yürütme sonucundan audit kaydı üretmiyor.",
);

assertContains(
  transferService,
  "auditRepository.save",
  "Aktarım servisi audit kaydını repository üzerinden saklamıyor.",
);
console.log(
  "ORDER transfer integration verification passed.",
);
