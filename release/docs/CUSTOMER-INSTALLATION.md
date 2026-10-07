# Nevaxis Zentra — Müşteri Kurulum Kılavuzu

## Desteklenen sistem

- Windows 11 veya Windows 10 (64-bit)
- En az 8 GB RAM (önerilen 16 GB)
- En az 10 GB boş disk alanı
- İnternet bağlantısı yalnızca harici entegrasyonlar için gerekir; yerel çekirdek çalışma modu desteklenir

## Kurulum

1. `Nevaxis-Zentra-Setup-<sürüm>.msi` dosyasını indirin (SHA256 doğrulaması için `SHA256SUMS.txt` kullanın).
2. MSI dosyasını normal şekilde çalıştırın; Windows UAC (Kullanıcı Hesabı Denetimi) istendiğinde **Evet** deyin. Kurulum makine genelinde (Program Files) yapılır ve yönetici onayı gerektirir.
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

## İlk kurulum (üretim dağıtımı)

Yeni bir üretim kurulumu **hiç kullanıcı ve kuruluş olmadan** başlar. Varsayılan kullanıcı adı veya parola yoktur.

1. İlk açılışta uygulama, bu bilgisayara özel, **tek kullanımlık ve süreli** bir kurulum anahtarı üretir.
2. Anahtar yalnızca şu dosyaya yazılır (günlüklere, ekrana veya konsola yazılmaz; dosya yalnızca kurulumu yapan Windows kullanıcısına açıktır):
   `%LOCALAPPDATA%\NevaxisZentra\config\first-run-token.txt`
3. Giriş ekranındaki **İlk kurulum** bağlantısını açın; anahtarı, kuruluş kodunu, kuruluş adını ve yönetici bilgilerini girin. Parola en az 12 karakter olmalı ve büyük harf, küçük harf, rakam ve sembolden en az üçünü içermelidir.
4. Kurulum tamamlanınca anahtar dosyası silinir ve ilk kurulum yolu **kalıcı olarak kapanır**; uygulamayı yeniden başlatmak yeni anahtar üretmez.
5. Anahtarın süresi (24 saat) kullanılmadan dolarsa, uygulamayı yeniden başlatmanız yeterlidir: yeni bir anahtar üretilir ve eskisi geçersiz olur.
6. Anahtar dosyasını kaybederseniz uygulamayı yeniden başlatın; yeni anahtar üretilir.

İlk kurulum yalnızca bu bilgisayarın kendisinden (localhost) erişilebilir; ağdan erişilemez. Mevcut kurulumlar bu adımdan etkilenmez.
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
