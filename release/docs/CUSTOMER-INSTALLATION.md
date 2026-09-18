# Nevaxis Zentra — Müşteri Kurulum Kılavuzu

## Desteklenen sistem

- Windows 11 veya Windows 10 (64-bit)
- En az 8 GB RAM (önerilen 16 GB)
- En az 10 GB boş disk alanı
- İnternet bağlantısı yalnızca harici entegrasyonlar için gerekir; yerel çekirdek çalışma modu desteklenir

## Kurulum

1. `Nevaxis-FactOS-Setup.msi` dosyasını indirin.
2. Kurulum sihirbazını yönetici olarak çalıştırın.
3. Lisansı onaylayın ve kurulumu tamamlayın.
4. Başlat menüsünden veya masaüstü kısayolundan **Nevaxis Zentra** uygulamasını açın.

Kurulum sırasında Microsoft Edge WebView2 Runtime eksikse otomatik olarak yüklenir.

## İlk açılış

İlk açılışta FactOS:

- veri dizinlerini oluşturur (`C:\ProgramData\Nevaxis\FactOS`)
- yerel veritabanını hazırlar
- API ve web uygulamasını başlatır
- giriş ekranını açar

Terminal, PowerShell veya ek yazılım kurulumu gerekmez.

## Giriş

Demo/presentation dağıtımlarında kurulum belgesinde belirtilen demo kimliği kullanılır. Üretim dağıtımlarında kuruluş yöneticinizin verdiği kimlik bilgileri kullanılır.

## Güncelleme

Yeni MSI sürümünü mevcut kurulumun üzerine çalıştırın. Müşteri verileri `C:\ProgramData\Nevaxis\FactOS` altında korunur.

## Yedekleme

Yedekler varsayılan olarak `C:\ProgramData\Nevaxis\FactOS\Backups` altında saklanır. Kuruluşunuzun yedekleme politikasına göre bu klasörü düzenli olarak dış ortama kopyalayın.

## Tanılama

Uygulama açılmazsa:

1. FactOS hata penceresinde **Tanılama Paketi** düğmesini kullanın.
2. Oluşan ZIP dosyasını destek ekibinize gönderin.

Loglar: `C:\ProgramData\Nevaxis\FactOS\Logs`

## Kaldırma

Windows Ayarlar → Uygulamalar üzerinden kaldırın. Varsayılan politika müşteri verilerini silmez; veriler `C:\ProgramData\Nevaxis\FactOS` altında kalır.

## Destek

Kurulum sorunları için tanılama paketi, FactOS sürümü ve Windows sürümünü paylaşın.

Verbose MSI log:

```text
msiexec /i Nevaxis-FactOS-Setup.msi /L*v install.log
```
