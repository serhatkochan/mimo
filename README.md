# Mimo

Windows masaüstünde istediğiniz yere taşıyabileceğiniz küçük bir Pomodoro maskotu. Birbirinden farklı 10 özgün piksel karakter içerir; yeni kurulumda Arı ilk ve varsayılan maskottur.

[Windows için Mimo'yu indir](https://github.com/serhatkochan/mimo/releases/latest/download/Mimo.exe) · [Tanıtım sayfası](https://mimo.serhatkochan.com)

## Kullanım

Son sürümdeki `Mimo.exe` dosyasını Windows 10/11 (64 bit) üzerinde açın. .NET çalışma zamanı uygulamaya dahildir; kurulum gerekmez.

- Maskotu veya altındaki paneli tutup sürükleyerek yerini değiştirin. İmleç yalnızca sürükleme sırasında el simgesine döner.
- Küçük alt panelde tur numarasını ve kalan süreyi görün; oynat/duraklat, sonraki oturum ve sıfırla düğmelerini kullanın. Ayarlanan tur sayısından sonra uzun mola gelir. İleri düğmesi sayaç çalışıyorsa yeni aşamayı otomatik başlatır; duraklatılmışsa duraklatılmış bırakır.
- Sağ tık menüsünden **Süre ayarları** ve **Maskot seç** pencerelerini açın. Geniş ayar penceresinde odak, kısa mola, uzun mola ve set başına odak sayısını girin; geçerli değerler otomatik kaydedilir. Seçim penceresi 10 maskotu adlarıyla birlikte gösterir.
- Maskotlar odak, kısa mola ve uzun mola için farklı çizimler kullanır. Her oturumda karakterin ilgili çiziminden saniyede 60 ayrı hareket karesi üretilir; karaktere göre antenler, kanatlar, kollar veya alt kısım hafifçe salınır. Görüntü ekranın çizim döngüsüyle güncellenir; uzun molada hareket sakinleşir. Oynat/duraklat düğmesi her aşamada animasyonu bulunduğu yerde durdurur.
- Sağ tık menüsündeki **Mimo'yu gizle/göster** maskotu kaldırır veya geri getirir; alt panel ekranda kalır. **Çıkış** uygulamayı kapatır.

Seçilen maskot, maskotun görünürlüğü, panelin konumu ve süre ayarları `%LOCALAPPDATA%\Mimo\state.json` dosyasına kaydedilir. Günlük oturum geçmişi tutulmaz. Uygulama sessizdir; ses veya müzik çalma özelliği yoktur.

## Kaynaktan derleme

Windows üzerinde .NET 9 SDK ile:

```powershell
dotnet build Mimo\Mimo.csproj
dotnet run --project Mimo.Tests\Mimo.Tests.csproj
dotnet publish Mimo\Mimo.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```
