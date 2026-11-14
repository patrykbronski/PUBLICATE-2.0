# Limity publikacji — weryfikacja 10.11.2026

Dodano szóstą platformę **YouTube Shorts**: jeden film pionowy lub kwadratowy, maksymalnie 179,99 s (limit PUBLICATE), 64 MiB, tytuł 100 znaków, opis z CTA 5000 bajtów UTF-8. Wymagane wybory widoczności i odbiorców. Szczegóły i źródła: [YOUTUBE_SETUP.md](YOUTUBE_SETUP.md).

Rozróżniamy wymagania dostawcy, możliwości zaimplementowanego adaptera i limity magazynu PUBLICATE. Odczyt dokumentacji nie oznacza udanego testu publikacji. Nowe granice opisane poniżej mają testy jednostkowe i atrapy HTTP; testy rzeczywistych publikacji na tych granicach: **NOT_TESTED**.

## Macierz używana przez kreator i walidację

| Platforma | Opis z CTA | Zdjęcia w PUBLICATE | Film w PUBLICATE | Status wartości |
|---|---:|---:|---|---|
| Instagram | 2200 | 1–20 JPEG | jeden Reel, 3–900 s | Dokumentacja Meta potwierdza te wartości |
| LinkedIn | 3000 | 1–20 JPEG/PNG | jeden MP4, 3–1800 s | MultiImage 2–20; pojedyncze zdjęcie przez Images API |
| TikTok | 2200 jednostek UTF-16 dla filmu | brak adaptera zdjęć | jeden MP4; maksimum z Creator Info | Minimum 3 s pozostaje limitem PUBLICATE; nie traktujemy go jako potwierdzonego minimum Direct Post |
| Facebook Pages | 5000 | 1–10 JPEG/PNG | jeden MP4, 3–60 s | Zachowane ograniczenia PUBLICATE; nie potwierdzono maksimów Graph API |
| X | 280 ASCII w obecnym adapterze | brak adaptera mediów | brak adaptera | API stosuje 280 znaków ważonych; obecny licznik nie implementuje pełnego twitter-text |

**Magazyn PUBLICATE:** 64 MiB na plik, 1 GiB na użytkownika, 10 szkiców, 48 h życia szkicu. To decyzje produktu, a nie limity serwisów. Nie podniesiono rozmiaru uploadu do gigabajtów. MB/kB w nowych walidacjach Instagram/LinkedIn interpretujemy dziesiętnie, zgodnie z oznaczeniem jednostki; dokumentacja nie definiuje dokładnego progu bajtowego. Granice bajtowe wymagają potwierdzenia na rzeczywistym API.

## Instagram

- [IG User Media](https://developers.facebook.com/documentation/instagram-platform/instagram-graph-api/reference/ig-user/media): JPEG do 8 MB, proporcje 4:5–1,91:1. Szerokość 320–1440 px jest zakresem skalowania po stronie Meta, nie powodem do odrzucenia szerszego zdjęcia. Usunięto taki błąd walidacji. Meta może też zmienić przestrzeń kolorów na sRGB i kadrować karuzelę względem pierwszego zdjęcia.
- Ta sama referencja potwierdza 20 kontenerów w `children`, opis 2200 znaków, do 30 hashtagów i 20 wzmianek. Walidator sprawdza długość opisu; osobnego parsera hashtagów/wzmianek jeszcze nie ma.
- Reel: 3–900 s, do 300 MB, szerokość do 1920 px, proporcje 0,01:1–10:1. 9:16 to zalecenie. Wymagania obejmują 23–60 FPS, H.264/HEVC, AAC, odpowiedni układ MP4 i bitrate. PUBLICATE przyjmuje MP4/H.264/AAC do 64 MiB; kontrola FPS, GOP, bitrate, chroma i położenia `moov` nie jest jeszcze pełna.
- [Content Publishing](https://developers.facebook.com/documentation/instagram-platform/content-publishing): karuzela może zawierać do 20 zdjęć/filmów/mieszanych elementów. PUBLICATE rozszerza zdjęcia do 20. Nie włącza mieszanych karuzel: obecna ścieżka filmu tworzy REELS, a referencja zabrania Reels jako elementów karuzeli.
- Dokumentacja Content Publishing jest wewnętrznie sprzeczna: ogólna sekcja podaje 100 publikacji/24 h, sekcja karuzel 50. Nie implementujemy blokady opartej na żadnej z tych stałych. Stan konta można odczytać przez `/{ig-user-id}/content_publishing_limit`; automatyczny odczyt w panelu nie jest jeszcze wdrożony. Referencja Media wskazuje osobno 400 kontenerów/24 h i wygaśnięcie kontenera po 24 h.
- Oficjalne strony Meta były niedostępne dla narzędzia web (429), ale zostały skutecznie pobrane jako publiczne dokumenty HTTPS i odczytane lokalnie. Nie wykorzystano tokenów ani sekretów.

## LinkedIn

- [MultiImage API](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/multiimage-post-api?view=li-lms-2026-10): 2–20 obrazów. Rozszerzono walidację i prezentację do 20; adapter wysyła wszystkie referencje w kolejności.
- [Images API](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/images-api?view=li-lms-2026-10): mniej niż 36 152 320 pikseli; JPG, PNG, GIF (do 250 klatek). PUBLICATE nie przyjmuje GIF. Usunięto wspólny, nieuzasadniony limit 8 MiB dla zdjęć LinkedIn; nadal obowiązuje lokalne 64 MiB. Nie znaleziono jednoznacznego maksimum bajtowego obrazu w tej referencji.
- [Videos API](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/videos-api?view=li-lms-2026-10): MP4, 3 sekundy–30 minut, 75 kB–500 MB. Dodano minimum rozmiaru i właściwy zakres czasu.
- [Pomoc LinkedIn dotycząca postów](https://www.linkedin.com/help/lms/answer/a528176) potwierdza 3000 znaków. Nie zmieniano nagłówka wersji skonfigurowanego lokalnie ani dostępu do produktów.
- Limity zapytań i dostępu do organizacji zależą od produktu/uprawnień; nie ma wspólnego uniwersalnego limitu publikacji wpisanego w kreator.

## TikTok

- [Direct Post Video](https://developers.tiktok.com/docs/en/content-posting-api-reference-direct-post): opis filmu 2200 jednostek UTF-16, do 6 inicjalizacji/min na token. Maksymalna długość filmu pochodzi z `max_video_post_duration_sec` w Creator Info; nie wpisujemy 60 s, 3 min ani 10 min jako uniwersalnego maksimum.
- Adapter ponownie odczytuje Creator Info przed wysyłką. Kreator pokazuje błąd po pobraniu danych twórcy, jeżeli film przekracza limit. SELF_ONLY nadal wymaga uprawnień i dostępności tej opcji dla konta.
- [Photo Post](https://developers.tiktok.com/docs/en/content-posting-api-reference-photo-post): do 35 zdjęć, opis 4000 jednostek UTF-16. To oddzielny endpoint; brak implementacji zdjęć nie oznacza zakazu API.
- [Media Transfer Guide](https://developers.tiktok.com/docs/en/content-posting-api-media-transfer-guide): film do 4 GB, 23–60 FPS, oba wymiary 360–4096 px, MP4/MOV/WebM oraz H.264/H.265/VP8/VP9. PUBLICATE przyjmuje tylko MP4/H.264/AAC i weryfikuje wymiary; nie waliduje jeszcze FPS. Zdjęcia JPEG/WebP do 20 MB, maksymalnie 1080p, wymagają zweryfikowanej domeny/prefiksu URL. Limit 10 minut opisany dla Upload Video nie jest stałym maksimum Direct Post.

## Facebook Pages

[Pages API Posts](https://developers.facebook.com/documentation/pages-api/posts) opisuje publikowanie i `attached_media`, ale odczytana wersja nie określa maksymalnej liczby załączników ani długości `message`. Nie potwierdziliśmy popularnych wartości 10/20 zdjęć i 63 206 znaków jako granic Graph API. Zachowano limity PUBLICATE i wyraźnie je oznaczono. Podobnie 8 MiB na zdjęcie i 3–60 s filmu są ograniczeniami aplikacji. Dalszy test większych wartości wymaga osobnej publikacji za zgodą użytkownika. Nie stosujemy specyfikacji Facebook Reels do obecnego adaptera `/videos`.

## X

[Counting Characters](https://docs.x.com/fundamentals/counting-characters): 280 znaków ważonych, wykryty URL liczy się jako 23, emoji zwykle jako 2, normalizacja NFC. Obecne ograniczenie ASCII nie realizuje pełnego algorytmu, w tym rozpoznawania URL; musi zostać zastąpione zgodną implementacją twitter-text przed rozszerzeniem produkcyjnym. Brak mediów jest ograniczeniem adaptera.

[Pricing](https://docs.x.com/x-api/getting-started/pricing), stan odczytu 10.11.2026: model pay-per-use; Content Create 0,015 USD za żądanie, Content Create with URL 0,200 USD. Ceny wymagają ponownej kontroli przed płatnym testem. Nie wykonano płatnych zapytań ani zakupów. Nie podniesiono żadnych flag zgody na koszty.

## Sprawdzenie

Testy `ApiLimitsTests`: granice 20/21 zdjęć, 900/1800 s, skalowanie szerokiego JPEG, poziomy Reel, rozmiary zdjęć i filmu, wymiary TikToka, NaN czasu, lokalny limit uploadu, zachowanie nieobsługiwanych formatów. Atrapy HTTP potwierdzają wysłanie 20 kontenerów Instagram i 20 referencji LinkedIn oraz odmowę TikToka przed uploadem przy przekroczeniu limitu twórcy. **Nie są to testy rzeczywistych publikacji.**
# Aktualizacja kodeków 11.11.2026

Globalne ograniczenie wideo do H.264 zostało usunięte. Aktualne reguły per platforma, źródła i niepewności: [VIDEO_CODECS.md](VIDEO_CODECS.md). Starsze wzmianki o MP4/H.264 opisują stan historyczny. Limit kontenera MP4 i dźwięku AAC produktu pozostał.

