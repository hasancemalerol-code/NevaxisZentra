# RC3-g packaged Production sweep (fresh tenant)

| Route | Class | HTTP | Result | Auth bridge | Entitlement | Authorization | Backend | Notes |
|---|---|---|---|---|---|---|---|---|
| `/` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Kayıt bulunamadı Seçili görünümde gösterilecek kayıt bulunmuyor. / Kayıt bulunamadı Seçili görünümde gösterile |
| `/abonelik-erisimi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ai-gorevleri` | A | 307 | OK (canonical redirect) | ok | ok | ok | ok | → / |
| `/ai-gorevleri/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/ai-gorevleri/otomasyon` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Kural bulunamadı Henüz otomasyon kuralı tanımlanmadı. / Geçmiş boş Henüz çalıştırma kaydı yok. / Kural bulunam |
| `/ai-gorevleri/playbooklar` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Playbook bulunamadı Henüz playbook tanımlanmadı. |
| `/ai-gorevleri/playbooklar/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | Playbook yüklenemedi Playbook kaydı bulunamadı. / Playbook yüklenemedi Playbook kaydı bulunamadı. |
| `/ai-gorevleri/playbooklar/calisma/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | Playbook çalışması bulunamadı Playbook çalışması bulunamadı. / Playbook çalışması bulunamadı Playbook çalışmas |
| `/ai-gorevleri/teslimatlar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ai-gorevleri/yeni` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ayarlar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ayarlar/entegrasyonlar` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Bağlantılar Henüz entegrasyon bağlantısı yok. |
| `/ayarlar/entegrasyonlar/{id}/dosya-aktarimi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ayarlar/entegrasyonlar/gercek-fabrika` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Henüz gerçek fabrika bağlantısı yok Bu tesis için yapılandırılmış bir gerçek fabrika bağlantısı bulunmuyor. Bu |
| `/ayarlar/entegrasyonlar/merkez` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Bağlantı Sağlığı Operasyon verisi henüz yok. / Canlı Olay Monitörü Henüz işlenmiş fabrika olayı yok. |
| `/ayarlar/faturalandirma/odeme-donusu` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ayarlar/sistem-durumu` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ayarlar/veri-aktarim-merkezi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim/arizalar/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/bakim/arizalar/yeni` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim/durum-zekasi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim/emirler/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/bakim/emirler/yeni` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim/kestirimci-riskler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim/planlama` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim/planlar/yeni` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim/raporlama` | A | 200 | OK | ok | ok | ok | ok |  |
| `/bakim/varliklar/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | Varlık geçmişi yüklenemedi Maintenance record was not found. |
| `/dashboard` | A | 307 | OK (canonical redirect) | ok | ok | ok | ok | → / |
| `/dijital-is-gucu` | A | 200 | OK | ok | ok | ok | ok |  |
| `/dijital-is-gucu/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/dijital-is-gucu/aktivite` | A | 200 | OK | ok | ok | ok | ok |  |
| `/dijital-is-gucu/calisanlar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/dijital-is-gucu/onaylar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/dijital-is-gucu/sonuclar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/dijital-is-gucu/tamamlanan` | A | 200 | OK | ok | ok | ok | ok |  |
| `/dijital-is-gucu/vakalar/{id}` | A | 307 | OK (canonical redirect) | ok | ok | ok | ok | → /isler?is={id} |
| `/enerji` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/baglanti` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/belgeler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/belgeler/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | ERP belgesi yüklenemedi ERP request failed (404). |
| `/erp-entegrasyonlari/eslestirme` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/hata-kuyrugu` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/onayli-islemler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/onayli-islemler/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | ERP writeback isteği yüklenemedi ERP request failed (404). |
| `/erp-entegrasyonlari/saglik` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/satin-alma` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/satin-alma/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | ERP satın alma siparişi yüklenemedi ERP request failed (404). |
| `/erp-entegrasyonlari/senkronizasyonlar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/siparisler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/siparisler/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | ERP siparişi yüklenemedi ERP request failed (404). |
| `/erp-entegrasyonlari/stok` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/uretim` | A | 200 | OK | ok | ok | ok | ok |  |
| `/erp-entegrasyonlari/uretim/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | ERP üretim emri yüklenemedi ERP request failed (404). |
| `/erp-entegrasyonlari/veri-eslestirme` | A | 200 | OK | ok | ok | ok | ok |  |
| `/fabrika` | A | 200 | OK | ok | ok | ok | ok |  |
| `/fabrika-agi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/fabrika-karar-merkezi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/fabrika-karar-merkezi/vakalar/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | Karar vakası bulunamadı. / Karar vakası bulunamadı. |
| `/fabrika-vitrini` | A | 200 | OK | ok | ok | ok | ok |  |
| `/fabrika-zekasi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/golden-demo/gumruk-dosyasi` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Veri yüklenemedi No customs file golden demo outcome found for this facility. Operasyon Yönetim Merkezi / Veri |
| `/golden-demo/uretim-kurtarma` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Veri yüklenemedi No active golden demo production event found for this facility. Operasyon Yönetim Merkezi / V |
| `/ihracat` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ihracat/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/ihracat/ulke-arastirma` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ihracat/ulke-arastirma/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/ihracat/ulke-bilgisi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/intelligence-runtime` | A | 200 | OK | ok | ok | ok | ok |  |
| `/internal/erisim-tanilama` | B | 404 | OK (404, class B) | ok | ok | ok | ok | development-only diagnostics, 404 outside development-api |
| `/iot-plc` | A | 200 | OK | ok | ok | ok | ok |  |
| `/isler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/kaliplar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/kaliplar/takip-ve-gorevler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/kalite` | A | 200 | OK | ok | ok | ok | ok |  |
| `/kalite/kontroller/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | Kontrol yüklenemedi Quality request failed (404). |
| `/kalite/kontroller/{id}/ozet` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | Özet yüklenemedi Quality request failed (404). |
| `/kalite/kontroller/yeni` | A | 200 | OK | ok | ok | ok | ok |  |
| `/kalite/ncr` | A | 200 | OK | ok | ok | ok | ok |  |
| `/kalite/ncr/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | NCR yüklenemedi Quality request failed (404). |
| `/kalite/planlar/yeni` | A | 200 | OK | ok | ok | ok | ok |  |
| `/kalite/raporlama` | A | 200 | OK | ok | ok | ok | ok |  |
| `/kalite/takip-ve-gorevler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/makineler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/malzemeler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/operasyon/yonetim-merkezi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/operasyon/yonetim-merkezi/kararlar/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | Karar yüklenemedi HTTP 404 / Karar yüklenemedi HTTP 404 |
| `/operasyon/yonetim-merkezi/surekli-iyilestirme` | A | 200 | OK | ok | ok | ok | ok |  |
| `/operasyon/yonetim-merkezi/vakalar/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ok | Fabrika vakası yüklenemedi Case not found. / Fabrika vakası yüklenemedi Case not found. |
| `/operasyonel-riskler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/operational-value` | A | 200 | OK | ok | ok | ok | ok |  |
| `/planlama` | A | 200 | OK | ok | ok | ok | ok |  |
| `/planlama/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/planlama/{id}/duzenle` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/planlama/yeni` | A | 200 | OK | ok | ok | ok | ok |  |
| `/proses-oee` | A | 200 | OK | ok | ok | ok | ok |  |
| `/satin-alma/tedarik-riskleri` | A | 200 | OK | ok | ok | ok | ok |  |
| `/senaryo-kurtarma` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Senaryo Kurtarma Hat veya senaryo tanımlayıcısı gerekli. |
| `/siparisler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/siparisler/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/stok-ve-depo/malzeme-riskleri` | A | 200 | OK | ok | ok | ok | ok |  |
| `/sunum-modu` | A | 200 | OK | ok | ok | ok | ok |  |
| `/surekli-optimizasyon` | A | 200 | OK | ok | ok | ok | ok |  |
| `/teams` | A | 200 | OK | ok | ok | ok | ok |  |
| `/teams/bildirimler` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Bildirim yok Yeni Teams olayları oluştuğunda burada görünür. / Bildirim yok Yeni Teams olayları oluştuğunda bu |
| `/teams/devretme` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Vardiya devri yok Görüntüleyebileceğiniz vardiya devri yok. / Vardiya devri yok Görüntüleyebileceğiniz vardiya |
| `/teams/devretme/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/teams/devretme/yeni` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Uygun kanal yok Erişilebilir operasyon veya departman kanalı yok. / Uygun kanal yok Erişilebilir operasyon vey |
| `/teams/is-kutum` | A | 307 | OK (canonical redirect) | ok | ok | ok | ok | → /?gorunum=assigned&kaynak=all |
| `/teams/onaylar` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Onay isteği yok Görüntüleyebileceğiniz onay isteği yok. / Onay isteği yok Görüntüleyebileceğiniz onay isteği y |
| `/teams/onaylar/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ok | expected not-found for the probe id |
| `/teams/onaylar/yeni` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Kaynak bağlam gerekli Onay isteği için bir Teams mesajı veya bir AI görevi seçin. / Kaynak bağlam gerekli Onay |
| `/teams/ozel-mesajlar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/fiyat-koruma-ve-onay` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/karlilik-uyarilari` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/karlilik-yonetim-merkezi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/malzeme-ve-maliyet-karliligi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/musteri-karliligi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/satin-alma-fiyat-analizi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/satis-fiyat-analizi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/tedarikci-karsilastirmasi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/teklif-riskleri` | A | 200 | OK | ok | ok | ok | ok |  |
| `/ticari-zeka/urun-karliligi` | A | 200 | OK | ok | ok | ok | ok |  |
| `/uretim` | A | 200 | OK | ok | ok | ok | ok |  |
| `/uretim/{id}` | A | 404 | OK (404 for unknown id) | ok | ok | ok | ERRORS | expected not-found for the probe id; API log: 2 error(s) |
| `/uretim/hatlar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/uretim/hatlar/{id}` | A | 200 | OK (unknown-id state) | ok | ok | ok | ERRORS | Hat detayı yüklenemedi Line not found.; API log: 1 error(s) |
| `/uretim/istisnalar-ve-gorevler` | A | 200 | OK | ok | ok | ok | ok |  |
| `/uretim/kaynaklar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/uretim/raporlama` | A | 200 | OK | ok | ok | ok | ok |  |
| `/uygulamalar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/vardiya-devir` | A | 307 | OK (canonical redirect) | ok | ok | ok | ok | → /vardiyalar/teslim |
| `/vardiya-devir/{id}` | A | 307 | OK (canonical redirect) | ok | ok | ok | ok | → /vardiyalar/teslim |
| `/vardiyalar` | A | 200 | OK | ok | ok | ok | ok |  |
| `/vardiyalar/acik-konular` | A | 200 | OK | ok | ok | ok | ok |  |
| `/vardiyalar/aktif` | A | 200 | OK (empty/explained state) | ok | ok | ok | ok | Aktif vardiya bulunamadı. / Aktif vardiya bulunamadı. |
| `/vardiyalar/gecmis` | A | 200 | OK | ok | ok | ok | ok |  |
| `/vardiyalar/kaynak` | A | 200 | OK | ok | ok | ok | ok |  |
| `/vardiyalar/performans` | A | 200 | OK | ok | ok | ok | ok |  |
| `/vardiyalar/plan` | A | 200 | OK | ok | ok | ok | ok |  |
| `/vardiyalar/teslim` | A | 200 | OK | ok | ok | ok | ok |  |
| `/yonetici-komuta-merkezi` | A | 200 | OK | ok | ok | ok | ok |  |

Total routes: 143; FAIL/CHECK: 0
