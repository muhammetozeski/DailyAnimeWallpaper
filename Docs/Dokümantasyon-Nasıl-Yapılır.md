# Dokümantasyon nasıl yapılır?

## 1. Hangi bilgi belgeye girer?

Yalnızca sonraki çalışmada yeniden araştırılması veya deneme yanılmayla bulunması gerekecek, projede doğrulanmış bilgiler yazılır:

- Dış API uç noktalarının beklediği zorunlu HTTP davranışları, şema farkları ve sayfalama kuralları.
- `WallpaperClient`, `WallpaperEngine`, `SourceCatalogManager` ve taşınabilir veri düzeni arasındaki sözleşmeler.
- Karşılaşılan gerçek hataların ayırt edici belirtisi, kanıtlanmış nedeni ve kalıcı çözümü.

Genel C#/.NET öğretileri, geçici görev durumu, yapılacaklar listesi ve ham konsol çıktıları `Docs/` içine eklenmez.

## 2. Doğrulama ve bakım kuralları

1. Uygulamalı olarak test edilmemiş davranış kesin sonuç gibi yazılmaz; kod sözleşmesi ile canlı ağ/sistem testi sonucu açıkça belirtilir.
2. Kod veya API davranışı değiştiğinde çelişen ikinci bir dosya açmak yerine ilgili konu belgesi ve [İçindekiler.md](İçindekiler.md) güncellenir.
3. Aynı kural hem `README.md` hem birden fazla alt belgede kopyalanmaz; ayrıntı tek konu belgesinde tutulur, diğer yerlerden bağlantı verilir.
4. Türkçe belgelerde Türkçe karakterler eksiksiz kullanılır; kod, dosya, API ve tür adları gerçek yazımıyla korunur.
