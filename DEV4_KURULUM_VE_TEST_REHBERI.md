# BlockPuzzle v2.0.0-dev.4 — Adventure Özel Bölüm Mekanikleri

## Kaynak / korunacaklar
- Doğrulanan GitHub `main` HEAD: `20e3ddc5013fd76339bb79716491908904ff8459` (`v2.0.0-dev.3 — Classic resume with versioned save and backup recovery`).
- GitHub'da bu inceleme sırasında yalnızca `main` dalı göründü. Önce yeni `develop/v2.0` dalını oluşturup Publish Branch yapmanız önerilir.
- Bu paket **v2.0.0-dev.3 üzerine** uygulanır; bütün eski `*.meta` dosyaları, `AdventureSaveService`, `ClassicRunSaveService`, `ClassicRunController`, `BlockSpawner`, Unity sahneleri, 100 eski level asset'i ve Adventure catalog **aynen kalır**.
- Unity Editor veya Android üzerinde henüz çalıştırılmış test sonucu yok; aşağıdaki sayılar hedef ve kontrol senaryolarıdır.

## Yeni özellikler
- `SpecialCellKind.None`: mevcut normal hücre; eski davranış.
- `SpecialCellKind.Locked`: yerleştirme yasak, koyu mor renkte, satır/sütun dolu sayılır, temizlense bile **kalıcıdır**.
- `SpecialCellKind.BreakableObstacle`: yerleştirme yasak, turuncu renkte, satır/sütun tamamlanınca **bir kez** temizlenir.
- `AdventureObjective` mevcut ilk üç indeks (`ReachScore`, `ClearLines`, `PlaceBlocks`) sabit; sadece yeni `ClearObstacles`, `ClearRows`, `ClearColumns` sona eklendi.
- Yeni `enableSpecialBoard` bayrağı varsayılan **false**, dolayısıyla eski 1–100 bölümler otomatik olarak değişmez.
- Undo hem özel hücre türünü hem temizlenmiş engel, satır ve sütun sayılarını geri yükler.
- `AdventureGameController.Start()` ilk üçlü blok üretilmeden önce çalışacak şekilde sıralanır (`DefaultExecutionOrder(-100)`); böylece ilk tepsi bile özel tahta düzenine göre seçilir.

## Kurulum (Windows PowerShell 5.1+)
1. **GitHub Desktop**: son değişiklikleri Fetch/Pull yapın. İdeal çalışma dalı `develop/v2.0`; `main` üzerinde devam etmeyin.
2. Projeyi kapatın. ZIP dosyasını Unity projenizin **kök klasörüne** çıkarın. `Assets`, `Packages` ve `ProjectSettings` klasörlerinin olduğu yerde `apply_dev4.ps1` bulunmalı.
3. Dosyaları çıkarmak yalnızca yeni `AdventureRules`, test ve Editor QA dosyaları ekler; mevcut büyük C# dosyalarını henüz değiştirmez.
4. Proje klasöründe PowerShell açın:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\apply_dev4.ps1 -CheckOnly
   ```

   Çıktıda `Preflight OK: 30 patches pending` beklenir. Eğer zaten kuruluysa `already applied` sayısı artabilir.

5. Yama kaynak kodlarını uygulayın:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\apply_dev4.ps1
   ```

6. Orijinal dosyalar `Library/BlockPuzzleDev4Backups/` konumuna bir defa yedeklenir. `.meta` dosyaları, oyun kayıtları ve asset'ler silinmez.
7. Unity 6.6.5f1 projesini açın; Console'da `error` olmadığını ve sahne referanslarının hâlâ bağlı olduğunu kontrol edin.
8. Alternatif Python kuruluysa `python apply_dev4.py --check` ve `python apply_dev4.py --apply` kullanılabilir. **PowerShell ve Python yükleyicilerini art arda uygulamak gerekmez**; ikisi de tekrar çalıştırıldığında zaten uygulanmış değişiklikleri atlar.

## Yeni dosyalar
- `Assets/_Project/Scripts/AdventureRules/SpecialBoardRules.cs`
- `Assets/_Project/Scripts/AdventureRules/BlockPuzzle.AdventureRules.asmdef`
- `Assets/_Project/Tests/EditModeAdventure/SpecialBoardRulesTests.cs`
- `Assets/_Project/Tests/EditModeAdventure/BlockPuzzle.AdventureRules.EditModeTests.asmdef`
- `Assets/_Project/Scripts/Editor/AdventureSpecialBoardQA.cs`

## Güncellenen mevcut dosyalar
1. `Adventure/AdventureLevelDefinition.cs`
2. `Adventure/AdventureGameController.cs`
3. `Adventure/AdventureUndoController.cs`
4. `Grid/GridCell.cs`
5. `Grid/GridCellView.cs`
6. `Grid/GridManager.cs`

## Editor veri denetimi
Unity menüsünden:

- `Tools > Block Puzzle > v2.0 > Audit Special Boards (Read Only)` — Eski ve yeni bölüm varlıklarını tarar, katalog ve kayıtlara yazmaz.
- `Tools > Block Puzzle > v2.0 > Create Dev4 Example Assets (No Catalog Changes)` — `Assets/_Project/Data/Adventure/Dev4Samples/` altında yeni `Level_101_ObstacleBreaker.asset` ve `Level_102_LockedColumns.asset` örneklerini üretir; **AdventureCatalog değişmez**. Bu örnekleri otomatik olarak oyun menüsüne eklemez.

Örnek 101:
- Difficulty: Easy
- Objective: ClearObstacles / target 2 / 18 moves
- Obstacles: (row 0, col 1) ve (row 0, col 5)
- Lock: (row 4, col 4)

Örnek 102:
- Difficulty: Medium
- Objective: ClearColumns / target 2 / 24 moves
- Locks: (0,1) ve (7,6)
- Obstacle: (2,3)

Satır/sütun koordinatları **0 tabanlıdır**: `(0,0)` sol üst köşe; `(7,7)` sağ alt köşe.

## Önemli: oynanabilir QA testi
101 ve 102 örneklerini bu aşamada doğrudan üretim kataloğuna bağlamayın: normal oyuncu için ilerleme sırası 1–100'dür. Dev.5'te gerçek yeni bölüm paketlerini kataloğa ekleyeceğiz.

Dev.4'ü şimdiden oyun içinde görmek isterseniz *asıl proje dışında geçici bir Unity QA kopyası* kullanın:
1. Projenin `Assets`, `Packages`, `ProjectSettings` klasörlerinin bir kopyasını ayrı dizine alın; orijinali değiştirmeyin.
2. QA kopyasında `Project Settings > Player` içinde geçici `Company Name = RaBaGamesQA`, `Product Name = BlockPuzzleQA` kullanın. Böylece Editor PlayerPrefs ve `persistentDataPath` asıl projeden ayrılır.
3. QA kopyasında Level 1 varlığının yedeğini alın; sadece QA kopyasının Level 1'inde `Enable Special Board = true` açıp `(0,1) BreakableObstacle`, `(0,5) BreakableObstacle`, `(4,4) Locked` tanımlayın.
4. QA kopyasında hedefi `ClearObstacles`, `Target Value = 2`, `Move Limit = 18` yapın; `Boot` sahnesinden Play Mode'a girin, Level 1'i açın.
5. Normal oyun ve Undo, satır temizliği, hedef ilerlemesi, menü, restart davranışlarını gözlemleyin. QA kopyasını test sonrası kaldırabilirsiniz.

Bu test yalnızca QA kopyasında çalıştırılmalı; eski 100 level asset'leri üretim projesinde olduğu gibi kalmalıdır.

## Otomatik testler
`Window > General > Test Runner > EditMode > Run All`
- Dev.1: 10 (daha önce onaylanan)
- Dev.2: 10 (daha önce onaylanan)
- Dev.3: 17 (daha önce onaylanan)
- Dev.4: **16 yeni**
- Beklenen toplam: **53/53**.

Yeni 16 test: boş ve karışık layout, null, sıfır boyut, negatif/dışarı koordinatlar, tekrar eden hücre, geçersiz kind, tamamen kilitli satır/sütun, geçerli 7-kilitli satır, lock/breakable semantiği, yerleştirme izni.

## Elle regresyon (Editor ve Android)
- [ ] Eski Adventure 1–100 asset'lerinde `Enable Special Board` kapalı (Audit çalıştırın).
- [ ] Classic oyunu, skor, combo, otomatik Continue (dev.3) ve Game Over hâlâ çalışıyor.
- [ ] Eski Adventure `ReachScore/ClearLines/PlaceBlocks` hedefleri aynen çalışıyor.
- [ ] Kilitli hücre mor, kırılabilir engel turuncu görünüyor.
- [ ] Kilit veya engele blok bırakılamıyor.
- [ ] Dolan satır/sütunda kırılabilir engel yalnızca bir kez temizleniyor, sayaç artıyor.
- [ ] Kilitli hücre satır temizlenince kaybolmuyor.
- [ ] Aynı anda satır+sütun temizlenince kesişimdeki engel iki kez sayılmıyor.
- [ ] Undo, kaybolan engeli ve kilidi, blok tepsisini, skoru ve yeni sayaçları geri getiriyor.
- [ ] Reroll coin tüketimi, Undo coin tutarı ve Extra Moves eskisi gibi.
- [ ] Adventure Back -> Map, Map Back -> MainMenu.
- [ ] Öncesi/sonrası coin, yıldız ve unlocked level bilgileri aynı; V1 kayıt anahtarları değişmedi.
- [ ] Android gerçek cihazda sürükleme, dokunmatik ve sahne geçişleri sorunsuz.

## Bilinen sınırlar ve dev.5
- Bu sürüm iki yeni hücre türü ve üç yeni hedefin **motorunu** kurar; 101–120 gibi kalıcı özel bölüm paketleri dev.5'te tasarlanıp test edilecek.
- Kilitler tamamlanan satır/sütunun dolu hücreleri sayılır; ancak kendileri silinmez. Bu tasarım bilerek seçildi, saf kilitlerden oluşan tam bir satır/sütun otomatik doğrulamada reddedilir.
- ScriptableObject düzenlemelerini kayıt sistemiyle karıştırmayın: mevcut SaveData.V1 ve Classic V2 kayıtları değiştirilmedi.
- Otomatik testler çekirdek layout doğrulamasını kontrol eder; sahne entegrasyonu ve hareket sınırları için PlayMode/Android testi zorunlu.

## GitHub commit
Tüm testler başarılı olduğunda:

`v2.0.0-dev.4 — Adventure special cells, objectives and Undo safety`

Commit'e `Assets/_Project/Scripts/AdventureRules`, `Editor/AdventureSpecialBoardQA.cs`, `Assets/_Project/Tests/EditModeAdventure`, altı güncellenmiş C# dosyası ve isteğe bağlı örnek level asset'lerini alın. `Library` yedeklerini ve geçici QA kopyasını commit'e eklemeyin.
