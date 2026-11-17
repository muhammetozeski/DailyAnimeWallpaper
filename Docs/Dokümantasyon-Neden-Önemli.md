# Dokümantasyon neden önemli?

Bu klasör, `DailyAnimeWallpaper` üzerinde çalışan geliştirici ve ajanların önceki deneme yanılmaları tekrarlamadan doğru değişikliği yapabilmesi için tutulur.

- **Tekrar araştırmayı önler:** Dış anime ve booru API'lerinin başlık gereksinimleri, eksik şemalı URL dönüşleri, ikili dosya biçimleri ve kapanmış servisler yeniden taranmadan doğrudan görülür.
- **Doğrulama sınırını korur:** Kaynak kod sözleşmesi ile canlı HTTP testinde kanıtlanmış davranış birbirinden ayrılır; hangi bilginin hangi koşulda geçerli olduğu açık kalır.
- **Ortak kavram dili kurar:** Taşınabilir kök (`AppPaths.Root`), kalıcı katalog (`UserData/sources.json`), `ProviderResponseMode` türleri ve `UInt128` ağırlıklı seçim kuralları tek kaynaktan izlenir.
- **Geçici günlükten ayrıdır:** Oturum adımları, geçici denemeler ve genel .NET/WinForms bilgisi buraya yazılmaz; yalnızca tamamlanmış çalışmadan süzülen tekrar kullanılabilir bilgi korunur.
