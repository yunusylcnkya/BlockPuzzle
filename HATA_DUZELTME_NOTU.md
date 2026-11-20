# BlockPuzzle dev.7.1a - CS0200 düzeltmesi

**Hata:** `TableEntry.LocalizedValue` yalnızca okunabilir (`CS0200`).

**Düzeltme:** `Dev71StaticLocalizationInstaller.cs` dosyasında mevcut çevirileri güncelleyen iki satırda `en.Value = t.English` ve `tr.Value = t.Turkish` kullanıldı. Yeni anahtarlar için `AddEntry()` korunmuştur.

## Kurulum
1. Unity Editor'ü kapatın.
2. ZIP içindeki `Assets` klasörünü projenizle birleştirip önceki dosyanın üzerine yazın.
3. Unity'yi açın, Console'da kırmızı hata olmadığını kontrol edin.
4. Sırayla: `Tools > Block Puzzle > v2.0 > Dev7.1 > 1. Audit Translation Tables (Read Only)`; sonra `2. Fix and Expand GameUI Tables`; sonra `1. Audit Translation Tables (Read Only)` (mismatches=0); sonra `3. Bind Static UI Texts`.
5. EditMode ve PlayMode testlerini tekrar çalıştırın.

Yalnızca bu derleme hatası için ilgili C# dosyasını değiştirmeniz de yeterlidir.

Not: Bu düzeltme Unity'de burada derlenememiştir.
