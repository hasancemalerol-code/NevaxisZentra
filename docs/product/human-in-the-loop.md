# CrevaFactOS Human-in-the-Loop ve Birim Yönlendirme Modeli

- Durum: Kilitli
- Sürüm: 2.0
- Tarih: 2026-08-04
- Ürün: CrevaFactOS
- İletişim uygulaması: CrevaFactOS Teams
- Kategori: AI Factory Operations Assistant
- Sektör: Plastik enjeksiyon ve kalıp üretimi

## Amaç

Bu belge CrevaFactOS içindeki Human-in-the-Loop çalışma modelini tanımlar.

Model aşağıdaki süreçleri kapsar:

- AI Agent tarafından veri toplanması
- Verilerin analiz edilmesi
- İlgili iş biriminin belirlenmesi
- Analizin CrevaFactOS Teams üzerinden yönlendirilmesi
- Görev, uyarı, öneri ve onay taleplerinin oluşturulması
- Yetkili insan incelemesi
- Onaylanan işlemin kontrollü uygulanması
- Sonucun doğrulanması
- Audit ve AI Karar Defteri kaydı

CrevaFactOS tam otonom fabrika yönetim sistemi değildir.

AI Agent:

- Verileri toplar.
- Verileri birleştirir.
- Riskleri analiz eder.
- Plan ve işlem taslakları hazırlar.
- İlgili birimi belirler.
- Görev veya uyarı oluşturur.
- Yetkili kullanıcıdan onay ister.
- Onaylanan işlemi kontrollü biçimde uygular.
- Sonucu doğrular.
- Bütün süreci kayıt altına alır.

AI kendi işlemini onaylayamaz.

## Temel Çalışma Modeli

CrevaFactOS’un standart çalışma modeli:

```text
ERP + Makineler + Sipariş Kanalları + Belgeler
                         ↓
              Integration Hub
                         ↓
             Operational Data Hub
                         ↓
                    AI Agent
                         ↓
          Analiz + Risk + Öneri + Taslak
                         ↓
       İlgili Birim / Rol / Kullanıcı Belirleme
                         ↓
              CrevaFactOS Teams
                         ↓
Mesaj / Görev / Uyarı / İnceleme / Onay Talebi
                         ↓
             Yetkili İnsan İncelemesi
                         ↓
            Onay / Ret / Düzeltme
                         ↓
          Mevcut Durumu Yeniden Doğrulama
                         ↓
CrevaFactOS İşlemi / ERP Write-back / Edge İşlemi
                         ↓
              Sonuç Doğrulama
                         ↓
          Audit + AI Karar Defteri