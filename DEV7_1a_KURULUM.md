# BlockPuzzle v2.0.0-dev.7.1a — Static Localization

GitHub temel alındı: `24ac85f` (2026-11-20).

## Bu paketin kapsamı
- GameUI tablosunda yanlış yerleştirilmiş `game.over` ve `game.continue_ad` değerlerini düzeltir.
- Türkçe karakterleri tamamlar.
- 50'den fazla UI çeviri anahtarını ekler.
- MainMenu, ClassicGame, AdventureMap, AdventureGame sahnelerinde **yalnızca sabit** TMP metinlerini GameUI'ye bağlar.
- Mevcut `LocalizeStringEvent` bağlarını korur; aynı etikete ikinci bileşen eklemez.
- C# oyun scriptlerini, skoru, ödülleri, kayıtları veya görev hesaplarını değiştirmez.

## Kurulum
1. Unity Editor'ü kapatın.
2. ZIP içindeki Assets klasörünü Unity proje köküyle birleştirin.
3. Unity'yi açıp Console'da derleme hatası olmadığını kontrol edin.
4. Menü: `Tools > Block Puzzle > v2.0 > Dev7.1 > 1. Audit Translation Tables (Read Only)`.
5. Menü: `Tools > Block Puzzle > v2.0 > Dev7.1 > 2. Fix and Expand GameUI Tables`.
6. Yeniden Audit çalıştırın: `mismatches=0`.
7. Menü: `Tools > Block Puzzle > v2.0 > Dev7.1 > 3. Bind Static UI Texts`.
8. MainMenu sahnesinde Play ile `TÜRKÇE` ve `ENGLISH` butonlarını deneyin.
9. Classic/Adventure sahnelerinde Settings, Game Over, Coin Shop sabit etiketlerini kontrol edin.
10. Window > General > Test Runner -> EditMode (73), PlayMode (12) testlerini tekrar çalıştırın.

## Önemli sınırlar
- Dinamik `ClassicRewardedTools`, Adventure hedefleri, ilerleme, skor, coin değerleri ve oyun mesajları 
  *bu sürümde çevrilmez*: dev.7.1b'de kod üzerinden yerelleştirilecek.
- Bazı Türkçe karakterler kare görünürse TMP Font Asset atlasında Ç, Ğ, İ, Ö, Ş, Ü, ç, ğ, ı, ö, ş, ü karakterlerini kapsayın.
- Önce `Git commit` ile mevcut dosyaları saklayın; değişiklikler karşılaştırılabilir ve geri alınabilir.
- Scriptler Unity 6000.6.5f1 / Localization 1.5.14 API'lerine göre hazırlanmıştır; Unity Editor üzerinde bu ortamda çalıştırılamadı.

## Başarılı commit mesajı
`v2.0.0-dev.7.1a — Expand TR/EN GameUI and bind static labels`
