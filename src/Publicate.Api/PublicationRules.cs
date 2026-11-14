namespace Publicate;
// Adapter limits are intentionally separate from the provider's full capabilities.
public record PublicationRule(Platform Platform,int TextLimit,int Images,bool Video,string[] Formats,long MaxFileBytes,long MaxImageBytes,double VideoMinSeconds,double? VideoMaxSeconds,bool MixedMedia,string Note,string ApiVerification);
public static class PublicationRules {
 public const long MaxFileBytes=64L*1024*1024;
 public const long UserQuotaBytes=1024L*1024*1024;
 public static PublicationRule Get(Platform p){var c=Capabilities.Get(p);return new(p,c.TextLimit,c.Images,c.Video,p==Platform.Instagram?["image/jpeg","video/mp4"]:p is Platform.TikTok or Platform.YouTube?["video/mp4"]:["image/jpeg","image/png","video/mp4"],MaxFileBytes,p==Platform.X?5_000_000:p==Platform.Instagram?8_000_000:p==Platform.Facebook?8L*1024*1024:MaxFileBytes,p==Platform.YouTube?0:p==Platform.X?0.5:3,p switch{Platform.X=>1200,Platform.YouTube=>YouTubeRules.MaxSeconds,Platform.Instagram=>900,Platform.LinkedIn=>1800,Platform.Facebook=>60,_=>null},false,
 p switch{
 Platform.YouTube=>"YouTube Shorts: jeden film MP4, pionowy lub kwadratowy, do 2:59,99. Tytuł do 100 znaków; opis z CTA do 5000 bajtów UTF-8. Widoczność i oznaczenie dla dzieci wybierasz osobno. Posty ze zdjęciami i samym tekstem niedostępne.",
 Platform.Instagram=>"JPEG do 8 MB, proporcje 4:5–1,91:1. Meta skaluje szerokość do 320–1440 px; plik źródłowy pozostaje bez zmian. Do 20 zdjęć. Reel: H.264 lub HEVC (H.265), 3–900 s, szerokość do 1920 px, proporcje 0,01:1–10:1; 9:16 zalecane. Publiczny HTTPS. Karuzele z filmami nie są jeszcze obsługiwane przez adapter.",
 Platform.LinkedIn=>"Do 20 zdjęć, każde poniżej 36 152 320 pikseli. Videos API: MP4, 3–1800 s, 75 kB–500 MB. PUBLICATE: JPEG/PNG i MP4 (dźwięk AAC), do 64 MiB na plik. Pełna lista kodeków LinkedIn niepotwierdzona; wynik przetwarzania API jest rozstrzygający.",
 Platform.TikTok=>"Film: opis 2200 jednostek UTF-16. Maksymalna długość zależy od konta — pobierz dane twórcy. Kodeki wideo: H.264, H.265, VP8, VP9. Wymiary 360–4096 px. API dopuszcza do 4 GB, PUBLICATE do 64 MiB. Zdjęcia (do 35, opis 4000) istnieją w API, ale adapter ich jeszcze nie obsługuje. Testy SELF_ONLY.",
 Platform.X=>"280 znaków ważonych (twitter-text), polskie znaki i emoji; URL liczy się jako 23. Do 4 zdjęć JPEG/PNG po 5 MB albo 1 MP4, 0,5–1200 s. Limit produktu: 64 MiB na plik, bez mieszanego posta. Wymagany media.write. Bez publicznego magazynu HTTPS. API płatne; upload i utworzenie posta są osobnymi operacjami.",
 _=>"5000 znaków, 10 zdjęć, 8 MiB na obraz i 3–60 s filmu to zachowane limity PUBLICATE, nie potwierdzone maksima API. Strony Facebooka; film osobno. Pełna lista kodeków tego endpointu niepotwierdzona; wynik przetwarzania API jest rozstrzygający. Dokumentacja Pages nie określa znalezionego maksimum opisu ani liczby attached_media."
 },p switch{
 Platform.YouTube=>"Dokumentacja Google 2026-11-10: videos.insert / videos / Shorts. Do 100 uploadów dziennie na projekt; limit 179,99 s i pliku 64 MiB pochodzi z PUBLICATE. Brak testu na rzeczywistym kanale.",
 Platform.Instagram=>"Dokumentacja 2026-11-10: IG User Media / Content Publishing. Sprzeczne limity dobowe 50/100 w dokumentacji; bieżący limit konta dostępny przez content_publishing_limit. Nowe granice nieprzetestowane na żywym API.",
 Platform.LinkedIn=>"Dokumentacja 2026-11-10: MultiImage, Images i Videos API (2026-10). Nowe granice nieprzetestowane na żywym API.",
 Platform.TikTok=>"Dokumentacja 2026-11-10: Direct Post, Photo Post, Media Transfer Guide. Creator Info sprawdzane ponownie przed wysyłką. 6 inicjalizacji/min na token; limit dzienny zależny od twórcy.",
 Platform.X=>"Dokumentacja 2026-11-12: Media Upload v2 / Best practices / Counting Characters. Przyjęto zakres zwykłego konta (20 min); Premium 125 min nie jest automatycznie wykrywane. Specyfikacja wymiarów filmu jest niespójna; API ostatecznie potwierdza format. Bez testu live mediów.",
 _=>"Dokumentacja 2026-11-10: Pages API Posts. Nie wpisujemy niepotwierdzonych 20 zdjęć ani 63 206 znaków jako limitów Graph API."
 });}
 public static bool FitsQuota(long used,long incoming,long limit=UserQuotaBytes)=>incoming>0&&used>=0&&incoming<=limit&&used<=limit-incoming;
}
