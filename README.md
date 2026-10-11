<div align="center">

<img src="web/assets/mimo-mark.svg" width="84" height="84" alt="Mimo'nun dört renkli kareden oluşan logosu" />

# Mimo

**Küçük bir dost. Güzel bir odak.**

Windows masaüstünün bir köşesinde duran, sürükleyip istediğin yere bırakabildiğin Pomodoro uygulaması. On farklı maskottan birini seç; odaklanırken, kısa molada ve uzun molada sana eşlik etsin.

**Mimo v1.0.1 · Windows 10/11 · 64 bit**

[**Kurulumu indir**](https://github.com/serhatkochan/mimo/releases/latest/download/Mimo-Setup.exe) · [Web sitesi](https://mimo.serhatkochan.com/) · [v1.0.1 sürümü](https://github.com/serhatkochan/mimo/releases/tag/v1.0.1) · [Kullanım](#kullanım) · [Kaynaktan çalıştırma](#kaynaktan-çalıştırma)

</div>

<p align="center">
  <a href="https://mimo.serhatkochan.com/"><img src="docs/assets/landing-hero.png" width="960" alt="Mimo tanıtım sayfası: dört parçalı logo, odak mesajı, arı maskotu ve etkileşimli zaman paneli" /></a>
</p>
<p align="center"><sub>Tanıtım sayfasındaki etkileşimli önizleme. Gerçek uygulama, masaüstünde yalnız maskot ve küçük zaman paneli olarak durur.</sub></p>

## Kullanım

1. [Mimo-Setup.exe kurulum dosyasını indir](https://github.com/serhatkochan/mimo/releases/latest/download/Mimo-Setup.exe) ve kurulumu tamamla. Ayrı bir .NET yüklemesi gerekmez.
2. Masaüstündeki **Mimo** kısayolundan uygulamayı aç. Maskotu ya da altındaki küçük zaman panelini tutup istediğin yere taşı.
3. **Oynat** ile turu başlat. Sağ tık → **Süre ayarları** ile ritmini, **Maskot seç** ile arkadaşını değiştir.

Panel tur numarasını ve kalan süreyi gösterir; hep diğer pencerelerin üzerinde kalır. Maskotu gizlersen panel yerinde durur. Uygulamayı kapatmak için maskotta veya panelde sağ tık → **Çıkış** seç.

## Zaman paneli

| Kontrol | Ne yapar? |
| --- | --- |
| **Oynat / duraklat** | Sayacı bulunduğu yerden başlatır veya durdurur. Duraklatınca maskotun hareketi de durur. |
| **Sonraki adım** | Odak → mola → sonraki odak sırasını izler. Sayaç çalışıyorsa yeni adım da hemen çalışır; duraklatılmışsa bekler. |
| **Sıfırla** | Bulunduğun adımı başlangıç süresine alır ve sayacı durdurur. |

Varsayılan ritim **25 dakika odak, 5 dakika kısa mola**. Dördüncü odak turundan sonra **15 dakika uzun mola** gelir. Her tur ve mola süresi ile uzun molaya kadar geçecek tur sayısı sağ tık menüsündeki **Süre ayarları** penceresinden değiştirilebilir; geçerli değerler otomatik kaydedilir. Süreler 1–180 dakika, uzun mola aralığı 2–12 tur olabilir.

İlk turu başlattıktan sonra odak ve mola süreleri bittiğinde Mimo bir sonraki adıma geçip sayacı otomatik başlatır. Ayarladığın tur sayısının ardından uzun mola da tamamlanınca 1. tura döner ve duraklar; yeni döngüyü sen başlatırsın.

## Maskotlar

İlk açılışta **Arı** gelir. Diğerleri **Kaktüs, Jeli, Hayalet, Salyangoz, Kapibara, Mantar, Yengeç, Fok** ve **Demlik**. Sağ tık → **Maskot seç** penceresinde birine tıkladığında seçim hemen uygulanır; pencereyi kapatmadan diğerlerini de deneyebilirsin.

Her maskotun odak, kısa mola ve uzun mola için ayrı çizimi ve kendine özgü sakin hareketi vardır. Sayaç durduğunda maskot da bulunduğu karede durur. Karakterlerin üç hâlini [web sitesindeki maskot galerisinde](https://mimo.serhatkochan.com/#maskotlar) görebilirsin.

## Ayarlar ve gizlilik

Mimo; seçtiğin maskotu, maskotun görünürlüğünü, pencere konumunu ve süre ayarlarını `%LOCALAPPDATA%\Mimo\state.json` dosyasında tutar. Günlük çalışma geçmişi, hesap veya çevrimiçi eşitleme yoktur. Uygulama açıldığında yeni birinci turla başlar ve kullanmak için internet gerekmez.

Kısa ve uzun molaların başlangıcında ve bitişinde dört ayrı ses çalar. **Sonraki adım** ile elle geçiş yaptığında da ilgili geçiş sesini duyarsın. **Oynat / duraklat** ve **Sıfırla** düğmeleri kısa bir kontrol sesi verir. Sesler belirgin ve tutarlı bir seviyeye getirilmiş, uygulamanın içine gömülmüştür; ayrı ses dosyası veya internet gerekmez. Duyduğun son ses seviyesi Windows'un ve Mimo'nun ses mikserindeki seviyesine bağlıdır.

Seslerin MP3 kaynakları `Mimo/Assets/Sounds/Sources` altında saklanır. WAV dosyalarını yeniden hazırlamak için FFmpeg kurulu bir bilgisayarda `powershell -ExecutionPolicy Bypass -File scripts/Prepare-NotificationSounds.ps1` çalıştır. Geçiş sesleri −14 LUFS ve en fazla −1 dBTP hedefiyle dengelenir.

**Güncelleme:** Sağ tık → **Çıkış** ile uygulamayı kapatıp [güncel kurulum dosyasını](https://github.com/serhatkochan/mimo/releases/latest) çalıştır. Ayarlar uygulama dosyasından ayrı tutulduğu için korunur.

## Kaynaktan çalıştırma

Masaüstü uygulaması **C# / WPF / .NET 9**, tanıtım sitesi ise bağımlılıksız **HTML, CSS ve JavaScript** kullanır. Geliştirme için Windows ve .NET 9 SDK gerekir.

```powershell
dotnet build Mimo\Mimo.csproj
dotnet run --project Mimo\Mimo.csproj
dotnet run --project Mimo.Tests\Mimo.Tests.csproj
```

Kurulum dosyasını üretmek için [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) ve [Inno Setup 6](https://jrsoftware.org/isinfo.php) gerekir:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\Build-Installer.ps1
```

Çıktı `publish\Mimo-Setup.exe` dosyasına yazılır.

Siteyi yerel olarak hazırlamak için `node web/build.mjs` çalıştır; dosyalar `web/dist` klasörüne yazılır. [Site geliştirme notları](web/README.md) ve [güncel kurulum dosyası](https://github.com/serhatkochan/mimo/releases/latest) ayrı sayfalardadır.
