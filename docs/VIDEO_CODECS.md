# Kodeki wideo — 11.11.2026

Usunięto globalne wymaganie H.264 z uploadu i PostValidator. Było ograniczeniem prototypu, nie wspólnym wymogiem API. `MediaInspector` sprawdza sygnaturę, kontener MP4, rozpoznany kodek, wymiary i dodatni skończony czas; pomija okładki attached_pic przy wyborze strumienia. Obrazy nadal muszą odpowiadać formatowi JPEG/PNG. Nie konwertujemy plików.

`VideoCodecRules` sprawdza kodek osobno dla platformy:

| Platforma | Reguła |
|---|---|
| Instagram | H.264 / HEVC (H.265) |
| TikTok | H.264 / HEVC / VP8 / VP9 |
| YouTube | Brak lokalnej wyłącznej listy kodeków; Google obsługuje również HEVC. Status API rozstrzyga przetwarzanie. |
| Facebook / LinkedIn | Pełna lista kodeków używanego endpointu niepotwierdzona. Nie udajemy, że H.264 jest jedynym dozwolonym; rozstrzyga odpowiedź/przetwarzanie API. |
| X | Adapter nadal nie publikuje filmów. |

Pozostają niezależne limity platform, 64 MiB/pliki i MP4 jako jedyny kontener wideo adapterów. Dźwięk AAC (lub brak audio) pozostaje jawnym ograniczeniem PUBLICATE; nie rozszerzano obsługi kodeków audio ani schematu bazy. Obsługa HEVC w podglądzie zależy od przeglądarki/systemu, nie od możliwości wysłania pliku do API. Brak automatycznej konwersji i dodatkowych kosztów transkodowania.

Źródła sprawdzone podczas analizy w tej rozmowie:
- [Meta — Reels Publishing](https://www.postman.com/meta/instagram/folder/830j7my/reels-publishing): HEVC lub H.264.
- [TikTok — Media Transfer Guide](https://developers.tiktok.com/docs/en/content-posting-api-media-transfer-guide): H.264 zalecany, H.265/VP8/VP9 wspierane.
- [YouTube — formaty](https://support.google.com/youtube/troubleshooter/2888402) i [videos.insert](https://developers.google.com/youtube/data/reference/rest/v3/videos/insert): m.in. HEVC; MIME video/*.
- [LinkedIn Videos API](https://learn.microsoft.com/en-us/linkedin/marketing/integrations/community-management/shares/videos-api): MP4, nie dowód wyłączności H.264.
- Próba pobrania dokumentacji Facebook Reels zwróciła 429; nie przenosimy wymagań Reels na używany endpoint Page videos bez potwierdzenia.

Weryfikacja: 218/218 testów backendu i build frontendu przeszły. Nowe testy obejmują HEVC, VP9, AV1 przy uploadzie, różnice IG/TikTok, błędny kontener/czas, niezgodny format obrazu i okładkę filmu. To testy lokalne, nie dowód publikacji tych kodeków na rzeczywistych kontach. Plik użytkownika `Rolka 15.mp4` nie był dostępny do inspekcji; jego kodek pozostaje niepotwierdzony.
