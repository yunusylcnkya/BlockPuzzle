# BlockPuzzle v2.0.0-dev.5.1 — Gameplay Polish

**Kaynak sürüm:** GitHub `main` commit `819b97b` (v2.0.0-dev.5), 19 Kasım 2026 incelemesi.

## Kapsam

- MainMenu: sadece `CLASSIC` butonu. `NewClassicButton` silinmez, deaktivasyon yapılır. Normal Classic girişinde kayıt varsa kaldığı yerden devam edilir. Game Over `RESTART` temiz Classic oturumu açmaya devam eder.
- Drag: blok hücre ortasına zıplamaz. Parmağın konumu için kısa SmoothDamp kullanılır, yerleştirme hesabı ve yeşil/kırmızı hücre ön izlemesi korunur.
- Board shake: sadece çizgi temizlenince BoardContainer üzerinde 0.13 sn, maksimum 6 UI pikseli; Inspector'dan kapatılabilir.
- Classic WATCH AD & CONTINUE: eski son hamleyi geri alır ve oyuncunun yerleştirebileceği 3 yeni bloğu verir. Ücretsiz ek `AD SHUFFLE` kullanımını düşürmez. `WATCH AD - UNDO` ve `AD SHUFFLE (5)` normal davranışları korunur.
- Ses: ödüllü devam sonrası GameFeedbackController tekrar efekt çalabilir; reklam kapanışında AudioListener ile BackgroundMusicController ayarları toparlanır; odak ve ses cihazı değişiminde müzik yenilenir.
- Adventure coin, yıldız, ilerleme ve Dev5 101–120 bölüm kataloğuna dokunulmaz.

## Kurulum

1. Unity Editor'ü kapat. GitHub Desktop üzerinden **`fix/v2.0.0-dev.5.1-polish`** adında, güncel dev5 commitinden yeni dal aç; önce commit almadan çalışma dosyalarının temiz olduğundan emin ol.
2. ZIP içerisindeki **tüm dosyaları** Unity proje köküne çıkar (`Assets`, `Packages`, `ProjectSettings` dizinlerinin bulunduğu yer). `apply_polish.py` de bu düzeyde bulunmalıdır.
3. Windows PowerShell'de:

```powershell
cd "D:\Unity Projects\Mobile\BlockPuzzle"
Test-Path .\apply_polish.py
py -3 .\apply_polish.py --check-only
```

`Preflight OK` çıktısı olmadan bir sonraki komuta geçme. Uyuşmayan kod bulunursa kurulum **hiçbir dosyaya yazmaz**.

```powershell
py -3 .\apply_polish.py
```

Mevcut kaynak dosyalarının yedekleri `Library/BlockPuzzleDev51Backups/` altında tutulur. `.meta` dosyaları değiştirilmez. Yeniden çalıştırma güvenlidir.

4. Unity'yi aç ve Console'da derleme hatalarını kontrol et.
5. **`Tools > Block Puzzle > v2.0 > Dev5.1 Install Gameplay Polish`** menüsünü çalıştır. Bu menü `MainMenu`, `ClassicGame` ve `AdventureGame` sahnelerini kaydeder; Classic ikinci butonu pasifleştirir, `CLASSIC` başlığı yazar ve iki oyun sahnesine `BoardShakeFeedback` ekler.
6. `Window > General > Test Runner > EditMode > Run All` çalıştır. Önceki testler hatasız olmalı. Bu pakette yeni otomatik test eklenmemiştir; önceki test sayısı korunur.

## Unity Play Mode kontrolü (reklamsız)

- `MainMenu` sadece tek CLASSIC butonu gösteriyor mu? Kayıtlı Classic oyuna dönebiliyor musun?
- Yeni başlayan Classic'te oyun normal mi, Game Over > Restart temiz başlıyor mu?
- Sürüklenen renkli blok artık ızgara hücresine zıplamadan parmağı takip ediyor mu?
- Tahta üzerinde **geçerli** yerleşim yeşil, dolu/uygunsuz hücreye taşınan yerleşim kırmızı görünüyor mu?
- Tek satır ve çift satır temizlemede hafif bir board shake var mı? `BoardContainer > BoardShakeFeedback > Enable Shake` kapatıldığında duruyor mu?
- Adventure'ın önceki bölüm kuralları, Undo, Extra Moves ve Classic Continue hâlâ çalışıyor mu?

## Android testleri (sonraki test turunda)

- Rewarded reklam tamamlandıktan sonra `WATCH AD & CONTINUE` eski son hamleyi geri alıp **3 yeni uygun blok** veriyor mu?
- Bu işlem oyuncunun normal beş karıştırma hakkından düşmüyor mu?
- Game Over sonrası devam eden Classic'te yerleştirme ve çizgi temizleme sesleri geri geliyor mu?
- Reklam yarıda kapatılınca ödül verilmeden oyun aynı kalıyor mu?
- Reklam sonrası müzik ve efekt sesleri, Settings'teki açık/kapalı ve ses seviyesi tercihlerine uyuyor mu?
- Android 60/120 Hz cihazlarda ortalama FPS, anlık takılmalar ve güç tüketimi kontrol edilecek.

## Olası hata ve geri dönüş

- Preflight `ANCHOR MISMATCH` verirse çalıştırmayı bırak; Console/PowerShell çıktısını paylaş. Başka sürüm için körlemesine patch uygulama.
- Yeni kodlardan kaynaklı sorun oluşursa GitHub Desktop üzerinden commit öncesi kaynakları geri al veya `Library/BlockPuzzleDev51Backups` yedeklerini kullan.
- Asset Store VFX'leri bu pakette yoktur. Önce performans ve temel oyun akışını doğrulayacağız.

## Önerilen commit

`v2.0.0-dev.5.1 — Smooth drag, Classic single entry, audio recovery and 3-block rewarded rescue`
