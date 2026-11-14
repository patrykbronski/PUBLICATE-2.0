# Wykonalność API PUBLICATE

**X, 12.11 — kod rozszerzony:** oficjalny upload v2 zdjęć/filmu i publikacja media_ids, dodatkowy scope media.write, licznik twitter-text; testy z atrapami PASSED. Nadal brak testu live X. [Zakres, źródła i procedura](X_SETUP.md). Starszy opis „tylko tekst ASCII” nie odzwierciedla już implementacji.

**Aktualizacja 12.11.2026 — PASSED live:** użytkownik potwierdził publikację filmu z jednego zatwierdzenia na Facebooku, Instagramie, TikToku (prywatnie), YouTube Shorts (unlisted) i LinkedInie, dostarczając screeny wszystkich pięciu wyników. Techniczna wykonalność tego scenariusza została wykazana na kontach testowych; nie jest to potwierdzenie audytu/dostępu komercyjnego, wszystkich formatów ani refresh/retry. [Dowody i granice potwierdzenia](TEST_RESULTS.md). Wcześniejsze blokady konfiguracji i brak testu publikacji poniżej są historyczne w tym zakresie.

**Aktualizacja 11.11.2026:** użytkownik potwierdził rzeczywistą publikację jednym przyciskiem kreatora na Facebooku, Instagramie i LinkedInie. Minimum dwóch integracji i optymalny zestaw tych trzech platform osiągnięte dla przetestowanego scenariusza. Typ materiału, data próby i ID nie zostały podane. [Wyniki i zakres potwierdzenia](TEST_RESULTS.md). Starsze wpisy poniżej zachowują historię analizy.

YouTube Shorts dodany 10.11.2026 jako szósta integracja. OAuth, kanał, upload i statusy zaimplementowane; rzeczywiste połączenie i publikacja BLOCKED do konfiguracji Google. Specyfikacja oraz oficjalne źródła: [YOUTUBE_SETUP.md](YOUTUBE_SETUP.md). Posty społecznościowe YouTube nie są obsługiwane przez oficjalne Data API.

11.11.2026: ponownie odczytano oficjalne dokumenty Google dotyczące resumable upload, zasobu Videos, Channels list i OAuth web-server. Uzupełniono obsługę 308/Range i odzyskiwanie w tej samej sesji oraz `status.containsSyntheticMedia`. Testy HTTP potwierdzają implementację; brak nowego dowodu live. Projekt Google `publicate-514411` / klient `PUBLICATE Web` już istnieją według użytkownika; nie tworzyć nowych. Konfiguracja lokalna oraz scope `youtube.readonly` wymagają uzupełnienia.

Aktualizacja limitów 2026-11-10: obowiązują [API_LIMITS.md](API_LIMITS.md) i katalog `PublicationRules`. Instagram i LinkedIn: do 20 zdjęć; Reels 3–900 s; LinkedIn Video 3–1800 s; TikTok: maksimum z Creator Info. Starsze konserwatywne wartości poniżej są historyczne. Nieweryfikowalne maksima Facebooka pozostają jawnie oznaczonymi ograniczeniami PUBLICATE.

Data pierwotnej analizy: 2026-11-04. Aktualizacja 2026-11-05: **potwierdzono rzeczywistą publikację pojedynczego zdjęcia na Instagramie przez Unified Login**. Użytkownik potwierdził widoczność na bronski.studio; Historia PUBLICATE pokazuje Published i ID posta. Mamy 1 z minimum 2 wymaganych platform. Kod adaptera ani test HTTP z atrapą nie dowodzą dostępu do pozostałych produktów. Starsze wpisy o braku publikacji opisują stan przed tym testem.

## Wniosek

Wykonalność podstawowego scenariusza wieloplatformowej publikacji została potwierdzona przez test użytkownika na Facebooku, Instagramie i LinkedInie, zgłoszony 11.11.2026. Pozostałe formaty, granice i scenariusze błędów wymagają odrębnych testów. Dostęp dla publicznych klientów pozostaje osobnym etapem od testów właściciela aplikacji.

## LinkedIn

Potwierdzone: [Posts API](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/posts-api?view=li-lms-2026-07) zastępuje ugcPosts; tekst, obraz, MultiImage i film są dostępne. Profil: `w_member_social`; organizacja: osobne `w_organization_social` i odpowiednia rola. Nagłówki `Linkedin-Version: 202607`, `X-Restli-Protocol-Version: 2.0.0`. Utworzenie: HTTP 201 i `x-restli-id`. Odczyt postów członka wymaga ograniczonego `r_member_social`; nie uzależniać sukcesu tworzenia od tego odczytu.

Portal: produkt Share on LinkedIn oraz Sign In with LinkedIn using OpenID Connect; `openid profile` służą do `/v2/userinfo`. [OIDC](https://learn.microsoft.com/en-us/linkedin/consumer/integrations/self-serve/sign-in-with-linkedin-v2). Konto osobiste, aplikacja powiązana ze stroną LinkedIn. Dostęp produktu trzeba potwierdzić w portalu; nie zakładamy automatycznej akceptacji organizacji. OAuth authorization code po stronie serwera, dokładny redirect URI, state. Odświeżanie zależy od przyznania refresh token; bez niego ponowny login.

[Images API](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/images-api?view=li-lms-2026-07): initializeUpload, PUT pliku, URN obrazu. JPG/PNG/GIF, poniżej 36 152 320 pikseli. Samo `w_member_social` może nie pozwolić odczytać statusu obrazu; 403 odczytu nie jest dowodem błędu uploadu. [Videos API](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/videos-api?view=li-lms-2026-07): MP4, 3 s–30 min, 75 KB–500 MB; inicjalizacja, części, finalizeUpload i status. Limity wywołań zależą od aplikacji i członka — sprawdzić Analytics w portalu. Brak potwierdzonej opłaty jednostkowej w tych źródłach; nie gwarantujemy bezpłatnego dostępu komercyjnego.

## Facebook Pages

Zakres wyłącznie strony, bez prywatnych osi czasu. Planowany protokół: Facebook Login, `pages_show_list pages_read_engagement pages_manage_posts`, `/me/accounts`, Page Access Token, `/PAGE/feed`, `/PAGE/photos`, `/PAGE/videos`. Dla wielu zdjęć najpierw unpublished photos, następnie attached_media. Film wymaga osobnego potwierdzenia przetwarzania.

[Oficjalna dokumentacja Pages](https://developers.facebook.com/docs/pages-api/posts/) zwróciła podczas analizy HTTP 429. Szczegółowe bieżące limity plików i wersja Graph NIEZWERYFIKOWANE. Konfiguracja wymaga jawnego wpisania wersji widocznej w portalu, bez zgadywania najnowszej. [Kolekcja należąca do Meta](https://www.postman.com/meta/instagram/documentation/6yqw8pt/instagram-api) potwierdza odczyt stron i tokenów przez `/me/accounts`. Implementacja Pages wymaga testu kontraktu z rzeczywistą aplikacją.

Ręcznie: aplikacja Meta, use case zarządzania stronami, Facebook Login, redirect URI, rola administratora/testera, dostęp do testowej strony. Tryb developerski nie dowodzi dostępu dla dowolnego użytkownika; App Review/Advanced Access i ewentualna weryfikacja biznesowa muszą być potwierdzone w dashboardzie dla wybranych uprawnień. Tokeny mogą wygasnąć lub zostać odwołane; ponowny OAuth i ponowne pobranie Page tokenów. Brak potwierdzonego cennika per-post; nie wykonano operacji płatnych.

## Instagram

**Potwierdzony rzeczywisty wynik 2026-11-05, 17:36 CET:** pełny OAuth Unified Login zakończył się sukcesem (raport 844db7ae859540dd8701c3f8fa3fc1c5). Wymiana kodu POST, weryfikacja tokenu, odczyt client_business_id oraz assigned_instagram_accounts: HTTP 200. Publikacja nadal nieprzetestowana; nie zaliczamy tego jako jednej z dwóch wymaganych integracji z potwierdzoną publikacją. Starsze wpisy poniżej opisują kolejne etapy weryfikacji.

**Najnowsza weryfikacja diagnostyczna (2026-11-05):** oficjalna dokumentacja dostępna pod `/documentation/instagram-platform/instagram-api-with-unified-login/get-started` została odczytana bezpośrednio HTTP 200. Potwierdza v27.0, client_business_id i discovery `/me/assigned_instagram_accounts`. Poprzednia hipoteza granular_scopes została zastąpiona tym endpointem. Rzeczywisty test POST client_credentials najpierw zwrócił `Error validating client secret.`, po poprawieniu lokalnego sekretu HTTP 200. Jest to potwierdzenie danych aplikacji, nie pełnego OAuth konta ani publikacji. [Raport](OAUTH_DIAGNOSIS.md). Poniższy opis wcześniejszych trudności z dokumentacją jest historyczny.

**Aktualizacja Unified Login, 2026-11-05:** użytkownik przekazał link wygenerowany w Meta: facebook.com/dialog/oauth z client_id, config_id, override_default_response_type=true, prompt=login i response_type=code. Ten etap jest potwierdzony panelem. Dodano `LoginMode=Unified` obok Direct, osobną tożsamość autoryzacji w bazie i wybór hosta publikacji według zapisanej metody konta. Oficjalne strony Unified Login, Facebook Login for Business i debug_token nadal niedostępne (m.in. 429). Kod wymiany tokenu i discovery jest **niezweryfikowany rzeczywistym API**. Discovery zakłada obecność IG target_ids w granular_scopes/instagram_basic i weryfikuje id/username; brak danych oznacza RequiresApproval, bez fallbacku przez Page i bez pozornego sukcesu. Jest to hipoteza do pierwszego testu, nie potwierdzona właściwość konfiguracji. Szczegóły i lokalne ustawienia: SETUP_ACCOUNTS.md.

[Oficjalna kolekcja Meta](https://www.postman.com/meta/instagram/documentation/6yqw8pt/instagram-api) potwierdza profesjonalne Business/Creator, połączenie ze stroną dla Facebook Login i scopes `instagram_basic instagram_content_publish pages_show_list pages_read_engagement`. Stories tylko dla Business (poza PoC). Plan: `/IG/media`, odczyt `status_code`, `/IG/media_publish`, odczyt permalink. Sam identyfikator kontenera nie jest ID posta. Zdjęcie JPEG, karuzela i Reel wymagają przetwarzania i publicznego URL dla crawlera.

[Content publishing](https://developers.facebook.com/docs/instagram-platform/instagram-api-with-facebook-login/content-publishing/) niedostępne w narzędziu podczas analizy. Bieżące limity publikacji, wymiary i maksima karuzel wymagają sprawdzenia w portalu/dokumentacji przed testem; lokalne limity PoC są świadomie węższe, a nie przedstawiane jako pełne limity API. `/content_publishing_limit` służy diagnostyce limitu konta.

Aktualna ścieżka [Instagram Login](https://developers.facebook.com/docs/instagram-platform/instagram-api-with-instagram-login/): konto Professional bez powiązania z Page, odrębne scopes `instagram_business_basic instagram_business_content_publish`, host graph.instagram.com i odrębny cykl tokenów. Od 2026-11-05 to podstawowa zaimplementowana ścieżka IG; nie jest jeszcze zweryfikowana na rzeczywistym koncie. Facebook Login nie jest już używany do nowych połączeń Instagram. Dostęp dla zewnętrznych użytkowników wymaga odpowiedniego review; konto testowe i role skonfigurować ręcznie.

## TikTok

[Direct Post](https://developers.tiktok.com/docs/en/content-posting-api-get-started): produkt Content Posting API, włączony Direct Post, zatwierdzony scope `video.publish` ORAZ zgoda użytkownika. [Wymagania](https://developers.tiktok.com/docs/en/content-sharing-guidelines): nieaudytowani klienci mają ograniczenia SELF_ONLY; prywatność nie zwalnia z zatwierdzenia scope. Audyt nie akceptuje narzędzia wyłącznie do własnych kont/zespołu — przyszły produkt musi uczciwie spełnić wymagania szerokiego zastosowania. Nie ma gwarantowanej ścieżki uzyskania zgody dla tego PoC.

[API](https://developers.tiktok.com/docs/en/content-posting-api-reference-direct-post): najpierw creator_info/query, potem video/init, FILE_UPLOAD albo PULL_FROM_URL, następnie status/fetch. `publish_id` śledzi zadanie, nie jest dowodem publikacji. Limit inicjalizacji 6/min/użytkownika. SELF_ONLY musi występować w opcjach twórcy. Długość filmu ogranicza również odpowiedź creator info. Zdjęcia i PULL_FROM_URL wymagają zweryfikowanej domeny/prefiksu. OAuth v2 z refresh tokenami. Dodatkowe limity twórców i audytu sprawdzić w panelu; nie uruchomiono żadnego realnego wywołania.

## X

[Cennik](https://docs.x.com/x-api/getting-started/pricing) odczytany 2026-11-04: pay-per-use, kredyty z góry; create post 0,015 USD, create z URL 0,200 USD; odczyt użytkownika 0,010 USD. Stawki mogą się zmienić — panel jest rozstrzygający. Domyślnie cała integracja sieciowa zablokowana flagą zgody na koszty, także pobranie użytkownika. Nie zakupiono kredytów.

OAuth 2 authorization code + PKCE, scopes `tweet.read tweet.write users.read offline.access`; `/2/users/me`, `/2/tweets`. Adapter tekstowy opcjonalny, media poza pierwszym zakresem. Portal wymaga aplikacji i ustawień OAuth. Dostęp i limity zależą od konta. Przed odblokowaniem odczytać cenę i ustawić limit wydatków, wyłączyć auto-recharge. Budżet całości 100–200 PLN nie jest zgodą na płatności.

## Magazyn i domena

[R2 public buckets](https://developers.cloudflare.com/r2/buckets/public-buckets/): bucket prywatny domyślnie, własna domena albo r2.dev do developmentu. Prywatny bucket i publiczny staging muszą być rozdzielone. Publiczny HTTPS może obsłużyć crawler Instagrama, lecz wymaga testu konkretnego pliku. TikTok wymaga dodatkowo udowodnienia własności domeny/prefiksu. Presigned URL S3 nie jest zamiennikiem własnej zweryfikowanej domeny. Nie kupiono domeny; jej konkretna cena zależy od wybranego TLD i rejestratora i pozostaje niezweryfikowana. Nie zakładamy ceny odnowienia równej promocji.

## Aktualizacja po implementacji

Kod pięciu adapterów, OAuth i wspólnych mechanizmów został skompilowany. Testy HTTP używają wyłącznie atrap; rzeczywisty PostgreSQL i lokalne pliki przeszły test akceptacyjny. **Nie uzyskano nowych dowodów dostępu do żadnego API społecznościowego.** Szczegółowa macierz jest w [TEST_RESULTS](TEST_RESULTS.md).

LinkedIn dodatkowo sprawdza scopes przez [Token Introspection](https://learn.microsoft.com/en-us/linkedin/shared/authentication/token-introspection). Obsługa filmu zachowuje pusty uploadToken i ostatni zakres uploadu zgodnie z przykładami dostawcy. Meta Graph version pozostaje celowo bez wartości domyślnej. TikTok photo, organizacje LinkedIn i media X nie są zaimplementowane. Instagram Login ma oddzielny adapter OAuth; szczegółowa dokumentacja Business Login zwróciła 429 także 2026-11-05, więc kontrakt tokenów wymaga rzeczywistego testu.

Profile walidacji PoC (testowalne w PostValidator):

| Platforma | Tekst | Zdjęcia | Film | Dodatkowy wymóg |
|---|---|---|---|---|
| LinkedIn | 3000 znaków razem z CTA | 1–10 JPEG/PNG, <36 152 320 px | Jeden MP4 | Profil, w_member_social |
| Facebook | 5000 znaków PoC | 1–10 JPEG/PNG | Jeden MP4 | Page i jej token |
| Instagram | Opis 2200, bez samodzielnego tekstu | 1–10 JPEG, 4:5–1,91:1, szer. 320–1440 | Jeden Reel 9:16 | Publiczny HTTPS, konto Professional |
| TikTok | Opis 2200, bez samodzielnego tekstu | Niezaimplementowane | Jeden MP4 | Creator info, SELF_ONLY, video.publish |
| X | 280 ASCII | Niezaimplementowane | Niezaimplementowane | Jawna zgoda kosztowa |

Wspólnie: zdjęcie do 8 MiB, upload do 64 MiB, filmy 3–60 s H.264/AAC. Wartości są **ograniczeniami implementacji PoC**, nie twierdzeniem o najnowszym maksimum API. Bieżące ograniczenia konta lub dostawcy mogą być węższe. Nie ma automatycznej modyfikacji mediów.
