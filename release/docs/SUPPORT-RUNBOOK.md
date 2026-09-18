# Nevaxis Zentra — Destek Runbook (Uygulama Açılmıyor)

## 1. Tanılama paketi

Müşteriden `Nevaxis-FactOS-Diagnostics-*.zip` isteyin veya hata penceresindeki **Tanılama Paketi** çıktısını alın.

## 2. Başlatma hata kodu

| Kod | Olası neden |
|-----|-------------|
| FACTOS-STARTUP-BUNDLE-001 | Kurulum dosyaları eksik/bozuk; yeniden kurulum |
| FACTOS-STARTUP-DB-001 | PostgreSQL başlatılamadı; disk/izin/port |
| FACTOS-STARTUP-API-001 | API başlatılamadı; port 5000 çakışması |
| FACTOS-STARTUP-WEB-001 | Web runtime başlatılamadı; port 3001/3011 |
| FACTOS-STARTUP-PORT-001 | Yerel port çakışması |
| FACTOS-STARTUP-PREREQ-001 | WebView2 veya .NET runtime eksik |

## 3. Bileşen sağlığı

Loglar: `C:\ProgramData\Nevaxis\FactOS\Logs\startup.log`

Kontrol sırası:

1. Desktop lifecycle log
2. PostgreSQL `database\logs\postgresql.log`
3. API log
4. Web stdout/stderr log

## 4. Kurulum bütünlüğü

MSI SHA256 değerini release manifest ile karşılaştırın.

## 5. Önkoşullar

- WebView2 Runtime
- Self-contained API/desktop (full release)
- Yazılabilir `C:\ProgramData\Nevaxis\FactOS`

## 6. Bilinen kök neden (2026.09 düzeltmesi)

Önceki MSI sürümleri masaüstü exe dosyasını `Program Files\Nevaxis Zentra\` köküne koyuyordu; runtime `desktop\` alt dizinini bekliyordu. Bu durum sessiz başlatma hatasına yol açıyordu. Düzeltilmiş MSI `desktop\NevaxisZentra.exe` yolunu kullanır.

## 7. Kurtarma

- Yeniden başlatma
- Port çakışması: ilgili süreçleri kapatın (yalnızca FactOS süreçleri)
- Veritabanı: yedekten geri yükleme
- Son çare: verileri koruyarak yeniden kurulum

Müşteriye PowerShell komutları önermeyin; tanılama paketi ve hata kodu yeterli olmalıdır.
