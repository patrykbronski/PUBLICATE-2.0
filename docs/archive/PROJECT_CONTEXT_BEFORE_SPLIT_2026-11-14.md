# PUBLICATE — kontekst projektu

**13.11 — Historia/Szczegóły:** diagnostyka prób rozwija się teraz w wybranym wierszu, ma osobny stan od raportu OAuth i przycisk Ukryj szczegóły. Poprzednio pojawiała się dopiero pod całą tabelą. UI wyjaśnia, że próby PUBLICATE nie są zestawieniem kosztów ani wszystkich HTTP. Build i scripts/test-history-ui.cjs PASSED (atrapy, bez wywołań dostawców). Screenshot użytkownika potwierdza Published X z ID 2121146796151865703; brak dodatkowego odczytu API.

**13.11 — Historia/Szczegóły:** diagnostyka prób rozwija się teraz w wybranym wierszu, ma osobny stan od raportu OAuth i przycisk Ukryj szczegóły. Poprzednio pojawiała się dopiero pod całą tabelą. UI wyjaśnia, że próby PUBLICATE nie są zestawieniem kosztów ani wszystkich HTTP. Build i scripts/test-history-ui.cjs PASSED (atrapy, bez wywołań dostawców). Screenshot użytkownika potwierdza Published X z ID 2121146796151865703; brak dodatkowego odczytu API.

**13.11 — X PASSED live według użytkownika:** użytkownik potwierdził udaną publikację na pięciu platformach, z pominięciem YouTube z powodu zgłoszonego dziennego limitu kanału po innych uploadach. Kontekst wskazuje FB/IG/TikTok/X/LinkedIn. Typ materiału X, ID i odpowiedź API nie zostały przekazane; nie rozszerzać tego na wszystkie formaty. Screenshot konsoli X pokazuje 2 Requests i saldo 9,97 USD (5 USD płatne + 4,97 darmowe), ale nie rozbicie endpointów. Nie ustalono dokładnie, jakie wywołania ujęto w liczniku; nie utożsamiać go z liczbą postów.

**13.11 — X PASSED live według użytkownika:** użytkownik potwierdził udaną publikację na pięciu platformach, z pominięciem YouTube z powodu zgłoszonego dziennego limitu kanału po innych uploadach. Kontekst wskazuje FB/IG/TikTok/X/LinkedIn. Typ materiału X, ID i odpowiedź API nie zostały przekazane; nie rozszerzać tego na wszystkie formaty. Screenshot konsoli X pokazuje 2 Requests i saldo 9,97 USD (5 USD płatne + 4,97 darmowe), ale nie rozbicie endpointów. Nie ustalono dokładnie, jakie wywołania ujęto w liczniku; nie utożsamiać go z liczbą postów.

**12.11 — rozszerzenie X:** XPublisher.cs zastępuje dawną klasę tekstową w Publishers.cs. JPEG/PNG do 4×5 MB albo MP4 0,5–1200 s (limit pliku produktu 64 MiB), upload v2 initialize/append/finalize/status, tekst Unicode/emoji/URL przez wspólny twitter-text (frontend + Jint/wbudowany bundle backendu), link do potwierdzonego posta. OAuth żąda dodatkowego media.write; konieczne ponowne połączenie wcześniej autoryzowanego konta. Polling nie publikuje automatycznie; marker x:posting chroni niepewne POST przed retry. 256 testów backendu, build i UI X/kreator/YouTube/TikTok PASSED z atrapami. Brak nowych żądań live i kosztów. Szczegóły, test ręczny i ograniczenia: docs/X_SETUP.md. Brak migracji. Stare wpisy „tylko ASCII/bez mediów” są historyczne.

**12.11 — X odblokowane do testów:** użytkownik wpisał klucze OAuth 2.0, zaakceptował płatne połączenie i ręcznie zatwierdzane testy postów w omawianym budżecie 5 USD oraz potwierdził doładowanie. Ustawiono lokalnie Providers:X:AllowPaidCalls=true. Nie kupowano kredytów przez agenta i nie wykonano żądań do X. OAuth/publikacja nadal NOT_TESTED live. Flaga nie wymusza limitu kwotowego; saldo i ustawienie Spend Cap w portalu nie zostały odczytane. Publikacja nadal wymaga zatwierdzenia treści w UI. Następny krok: użytkownik łączy X na domenie ngrok, potem test krótkiego tekstu ASCII bez multimediów/linków. Wcześniejszy brak zgody kosztowej jest historyczny.

**12.11 — wznowiono X na prośbę użytkownika:** istnieją OAuth 2.0/PKCE S256, profil `/2/users/me`, refresh i adapter tekstowy; zdjęcia/filmy nadal poza zakresem. Dodano Configure-X.ps1 do lokalnego zapisu OAuth 2.0 Client ID/Secret, pozostawiający AllowPaidCalls=false. Aktualny cennik potwierdza płatne odczyty profilu; brak zgody na koszty, zakupów i wywołań live. Następny krok: konfiguracja Web App w portalu, lokalne klucze, osobna zgoda na koszt połączenia. Szczegóły docs/SETUP_ACCOUNTS.md. Nie rozszerzać pięciu potwierdzonych platform o X przed rzeczywistym testem.

**12.11.2026 — PASSED live: film na pięciu platformach z jednego zatwierdzenia.** Użytkownik potwierdził sukces i dostarczył osiem screenów kreatora, potwierdzenia i rzeczywistych publikacji: Facebook (Bronski), Instagram (bronski.studio), TikTok (patrykb78, prywatny / SELF_ONLY), YouTube Shorts (Patryk B, niepubliczny / unlisted), LinkedIn (Patryk B). Film widoczny w każdym serwisie; YouTube pokazuje 0:44. To potwierdzony test użytkownika end-to-end, nie test z atrapami. Rozstrzyga wcześniejsze NOT_TESTED dla publikacji filmu TikTok/IG oraz blokad konfiguracji YouTube w tym scenariuszu. Nie odczytywano ponownie API/bazy; screeny nie pokazują końcowych stanów zadań PUBLICATE ani pełnych ID wszystkich publikacji. Refresh, retry i inne formaty nie są tym testem potwierdzone. Szczegóły: docs/TEST_RESULTS.md. Poniższe wpisy opisują wcześniejsze etapy.

**12.11.2026 — PASSED live: film na pięciu platformach z jednego zatwierdzenia.** Użytkownik potwierdził sukces i dostarczył osiem screenów kreatora, potwierdzenia i rzeczywistych publikacji: Facebook (Bronski), Instagram (bronski.studio), TikTok (patrykb78, prywatny / SELF_ONLY), YouTube Shorts (Patryk B, niepubliczny / unlisted), LinkedIn (Patryk B). Film widoczny w każdym serwisie; YouTube pokazuje 0:44. To potwierdzony test użytkownika end-to-end, nie test z atrapami. Rozstrzyga wcześniejsze NOT_TESTED dla publikacji filmu TikTok/IG oraz blokad konfiguracji YouTube w tym scenariuszu. Nie odczytywano ponownie API/bazy; screeny nie pokazują końcowych stanów zadań PUBLICATE ani pełnych ID wszystkich publikacji. Refresh, retry i inne formaty nie są tym testem potwierdzone. Szczegóły: docs/TEST_RESULTS.md. Poniższe wpisy opisują wcześniejsze etapy.

**12.11 — TikTok wdrożenie filmu:** zastąpiono FILE_UPLOAD przez PULL_FROM_URL (media przechowywane na backendzie). `TikTokRules.cs`: walidacja ustawień, Creator Info, prefiksu HTTPS. `TikTokFields.tsx`: wspólne pola kreatora/testera, automatyczny Creator Info, SELF_ONLY bez domyślnego wyboru, interakcje/AI/własna marka/zgody. PublicationService/Drafts wymagają zgody na publiczny HTTPS i sprawdzają Creator Info przed startem zestawu; nie zapewnia to atomowości zewnętrznych publikacji. Statusy nieznane→Unknown; PUBLISH_COMPLETE→Published nawet bez ID prywatnego posta, FAILED→świadome ponowienie. Bez automatycznego retry. Użytkownik wpisał klucze skryptem Configure-TikTok.ps1; potwierdzono obecność i zgodność prefiksu z magazynem, nie ważność kluczy. 241/241 testów backendu + build + UI TikTok/kreator/YouTube PASSED z atrapami. OAuth/upload/publikacja TikTok nadal NOT_TESTED live; zdjęcia kolejny etap. Szczegóły i aktualna instrukcja na początku docs/TIKTOK_SETUP.md. Brak migracji.

**12.11 — TikTok URL prefix VERIFIED:** użytkownik pokazał potwierdzenie portalu dla `https://cofounder-morse-reflex.ngrok-free.dev/`. Weryfikacja własności zakończona rzeczywistym sukcesem, mimo wcześniejszego ostrzeżenia ngrok dla niektórych klientów. Nie rozszerzać tego dowodu na OAuth, transfer mediów lub publikację — te nadal wymagają wdrożenia/konfiguracji i testów.

**12.11 — plik weryfikacji TikTok:** dostarczony plik `tiktokf4gpPnsRYfKCmbjvTbkZhpq3o6pEpTr8.txt` umieszczono bez zmian w `src/publicate-web/public/`. Lokalny GET zgodny z oryginałem. Wynik Verify w portalu nadal niepotwierdzony; domyślny klient PowerShell przez ngrok otrzymał ostrzeżenie zamiast treści.

**Blokada tunelu 12.11:** anonimowy odczyt HTTPS `/terms` i `/privacy` zwraca HTTP 200 z ostrzeżeniem ngrok ERR_NGROK_6024. Strony działają lokalnie, ale dostępność dla weryfikatora TikTok jest niepotwierdzona. Nie deklarować, że same poprawne ścieżki rozwiązały konfigurację portalu.

**12.11 — przygotowanie TikTok:** dodano publiczne `/terms`, `/privacy`, `/about` oraz opis projektu/linki przed logowaniem. Użytkownik odłożył kontakt i dane operatora; strony jawnie opisują prywatny prototyp, nie gotowy regulamin komercyjny. Build i trzy testy UI PASSED. Screeny potwierdzają Direct Post/video.publish, użytkownik dodał Target User. Nadal wymagane zapisanie portalu, weryfikacja prefiksu ngrok i dokończenie adaptera PULL_FROM_URL/UX/preflight; nie uruchamiać starego FILE_UPLOAD. Wideo priorytetem, zdjęcia po nim, opcjonalne przy blokadzie. Instrukcje: [TIKTOK_SETUP](docs/TIKTOK_SETUP.md).

**Instagram — diagnoza próby filmu 11.11:** odczyt lokalnej bazy potwierdził Published dla FB, LinkedIn i YouTube (unlisted), natomiast IG zakończył oczekiwanie przed media_publish. Następne odczyty FINISHED błędnie usuwały SafeToRetry. Naprawiono polling (do 36 kontroli / około 3 min zamiast 6 / 18 s) i zachowanie flagi. Przy odczycie statusu starych Failed możliwość ponowienia odzyskuje się tylko z jednoznacznej historii Processing sprzed media_publish, bez Unknown/Published. Nie wykonywano ponownej publikacji. 225/225 testów lokalnych; użytkownik powinien sprawdzić status i świadomie zatwierdzić wyłącznie pozostały wariant IG.

**Instagram — diagnoza próby filmu 11.11:** odczyt lokalnej bazy potwierdził Published dla FB, LinkedIn i YouTube (unlisted), natomiast IG zakończył oczekiwanie przed media_publish. Następne odczyty FINISHED błędnie usuwały SafeToRetry. Naprawiono polling (do 36 kontroli / około 3 min zamiast 6 / 18 s) i zachowanie flagi. Przy odczycie statusu starych Failed możliwość ponowienia odzyskuje się tylko z jednoznacznej historii Processing sprzed media_publish, bez Unknown/Published. Nie wykonywano ponownej publikacji. 225/225 testów lokalnych; użytkownik powinien sprawdzić status i świadomie zatwierdzić wyłącznie pozostały wariant IG.

**Aktualizacja 11.11 — kodeki:** usunięto globalną blokadę H.264. Upload MP4 rozpoznaje kodek; `VideoCodecRules` ogranicza IG do H.264/HEVC i TikTok do H.264/HEVC/VP8/VP9. Dla FB/LinkedIn brak potwierdzonej pełnej listy; nie narzucać H.264. YouTube przyjmuje również HEVC. AAC pozostaje jawnym ograniczeniem audio produktu. Bez konwersji i migracji. 218/218 testów backendu oraz build frontendu przeszły; brak realnych publikacji nowych kodeków. Szczegóły: [VIDEO_CODECS](docs/VIDEO_CODECS.md).

Stan aktualizowany **2026-11-11**, na podstawie kodu, dokumentacji i rozmowy. Ostatnia zmiana: uzupełnienie YouTube (odzyskiwanie uploadu, diagnostyka, deklaracja treści syntetycznych), testy lokalne bez realnych wywołań Google.

Aktualizacja konfiguracji YouTube: użytkownik uzupełnił lokalny Client ID i Client Secret; potwierdzono obecność obu ustawień bez odczytu wartości do logów, zrestartowano backend. Wcześniejsze wpisy o pustej konfiguracji są historyczne. Poprawność danych, OAuth oraz publikacja nadal czekają na rzeczywisty test użytkownika.

## Cel i aktualny etap

Uruchamialny lokalny PoC do publikowania z jednego panelu przez oficjalne API. Praca inżynierska; architektura ma umożliwiać komercjalizację. **Minimum dwóch platform osiągnięte: użytkownik potwierdził rzeczywistą publikację na Facebooku, Instagramie i LinkedInie jednym przyciskiem „Opublikuj na 3 platformach”.** Potwierdzenie przekazane w rozmowie 11.11.2026; data samej próby, typ materiału i ID postów nie zostały podane. To dowód użytkownika z testu live, nie nowy odczyt API przez agenta ani potwierdzenie wszystkich formatów.

Gotowe są konta PUBLICATE, panel połączeń, tester API, kreator dwuetapowy, szkice, historia, adaptery sześciu platform, lokalny magazyn i szkielety R2/AI. Trwają konfiguracja oraz ręczne testy integracji. To nie gotowy produkt publiczny.

## Mapa kodu i architektura

Modularny monolit: **ASP.NET Core 10**, EF Core/Npgsql, PostgreSQL; **React 19, TypeScript, Vite, Tailwind 4**. Konkretne zależności: csproj, `package.json` i lockfile. HttpClientFactory do API, ffprobe do kontroli plików.

| Miejsce | Odpowiedzialność |
|---|---|
| `src/Publicate.Api/Program.cs`, `UserAccounts.cs` | REST, DI, cookie sesji, rejestracja/logowanie, zabezpieczenia, endpointy starego testera |
| `Domain.cs`, `Database.cs`, `Migrations/` | modele, możliwości adapterów, relacje i indeksy EF |
| `OAuthService.cs`, `InstagramLogin.cs`, `InstagramUnified.cs` | autoryzacja, tokeny, odkrywanie celów; Facebook i Instagram niezależne |
| `ProviderHttp.cs`, `OAuthDiagnostics.cs` | wywołania HTTP, bezpieczne błędy i raporty OAuth |
| `Publishers.cs`, `YouTube.cs` | `ISocialPublisher`, sześć adapterów; YouTube zawiera też własne OAuth/discovery i walidację |
| `PublicationService.cs` | walidacja, snapshot, idempotencja, próby i statusy, brak automatycznego ryzykownego retry |
| `Drafts.cs` | szkice, cele domyślne, walidacja i publikacja zestawu |
| `PublicationRules.cs`, `MediaStorage.cs` | limity, `PostValidator`, inspekcja mediów, lokalny/R2 storage, cleanup |
| `AiDescriptions.cs` | `IAiDescriptionService`, integracja OpenAI Responses dla zdjęć |
| `src/publicate-web/src/main.tsx` | konta, profil, tester, historia i powłoka UI |
| `Composer.tsx`, `YouTubeFields.tsx`, `platforms.ts` | nowy kreator, pola Shorts, wspólna kolejność platform |
| `tests/Publicate.Tests/`, `scripts/` | xUnit/atrapy HTTP, testy UI, konfiguracja i lokalne uruchamianie |

Baza: `Users`, `ConnectedAccounts`, `PublishingTargets`, `Posts`, `MediaAssets`, `PostVariants`, `PublicationJobs`, `PublicationAttempts`. Zasoby mają właściciela; media współdzielone przez ID, bez kopiowania dużych plików. Możliwe wiele kont na platformie; kreator używa skonfigurowanego celu per platforma. Najnowsza migracja w kodzie: `20261110104052_ComposerDrafts`; według historii została zastosowana lokalnie. Dodanie YouTube nie zmieniło schematu; wartości enum dopisano na końcu.

## Integracje: kod a rzeczywiste wyniki

Kolejność **wszędzie w wyborach platform i kartach**: Facebook → Instagram → TikTok → YouTube → X → LinkedIn. UI YouTube opisuje „YouTube Shorts”; wewnętrzna platforma to `YouTube`. Historia prób pozostaje chronologiczna.

| Platforma | Zaimplementowane | Potwierdzenie live / blokady |
|---|---|---|
| Facebook | Login (opcjonalny ConfigId), strony i Page tokens; tekst, zdjęcia, wiele zdjęć, film | **PASSED live według użytkownika:** publikacja w zestawie FB + IG + LinkedIn, potwierdzenie 11.11.2026; typ materiału niepodany. Wcześniej OAuth i lista stron. Dostęp do NEURO w innym portfolio odłożony. Brak publikacji na prywatnej osi czasu. |
| Instagram | Direct Instagram Login oraz Unified Login; jedno zdjęcie, karuzela zdjęć, Reel, kontenery/statusy | **PASSED live:** Unified OAuth i pojedyncze zdjęcie na `bronski.studio` 05.11.2026. Dodatkowo użytkownik potwierdził 11.11 publikację w zestawie FB + IG + LinkedIn. Karuzela, Reel i granice limitów niepotwierdzone. Nie wymaga wspólnego wyboru Facebooka. |
| TikTok | OAuth, Creator Info, film Direct Post, SELF_ONLY, upload/status; walidacja zgody muzycznej | Brak potwierdzonego testu live. Wymagane uprawnienie i konfiguracja dostępu. Zdjęcia niezaimplementowane. |
| YouTube | OAuth Google/state/PKCE/refresh, kanał, upload Shorts z odzyskiwaniem w tej samej sesji, kontrola kanału/przetwarzania/widoczności; deklaracja treści syntetycznych | Testy z atrapami i UI. Użytkownik utworzył Google `publicate-514411` / klient `PUBLICATE Web`; lokalne ID/Secret były puste przy przeglądzie 11.11. Oczekuje na lokalną konfigurację i test OAuth/publikacji. Tylko Shorts. |
| X | OAuth, tekst ASCII, status; blokada kosztów | Opcjonalny i **odłożony przez użytkownika**. Brak zdjęć/filmów i pełnego licznika twitter-text; brak potwierdzonego testu live. |
| LinkedIn | OAuth `openid profile w_member_social`, userinfo/introspekcja; Posts, Images, MultiImage, Videos API | **PASSED live:** OAuth 10.11 (raport `18346927e09541b1825e1a8fc640ca65`, trzy etapy HTTP 200); publikacja w zestawie FB + IG + LinkedIn według potwierdzenia użytkownika 11.11, typ materiału niepodany. `invalid_client` ustąpiło po poprawieniu danych lokalnych. Cel: profil osobisty; strony firmowe to osobny zakres. |

## Zatwierdzone zasady produktu

- Logowanie do PUBLICATE: e-mail/hasło, hash ASP.NET Identity, HttpOnly cookie, izolacja właścicieli. Brak weryfikacji e-mail i odzyskiwania hasła; dawny AccessKey nie służy do logowania. Rejestracja może być wyłączona konfiguracją podczas testów przez tunel.
- Krok 1: post bazowy (opis, opcjonalne CTA, media i kolejność) oraz wybór podłączonych platform. Niepodłączone są wyszarzone. Błędy wariantów można poprawić w kroku 2.
- Krok 2: tylko zaznaczone platformy, niezależne warianty, edycja opisów/CTA/mediów, podgląd, wymiana/usunięcie/kolejność. Publikowane są warianty, nie baza. Bez narzędzi edycji obrazu.
- Powrót do kroku 1, także Wstecz przeglądarki, wymaga ostrzeżenia i usuwa zmiany kroku 2, zachowując bazę. Wariantów z próbami publikacji nie resetujemy.
- Przed wysyłką całego zestawu wszystkie pozostałe warianty muszą przejść walidację. Wyniki są niezależne; sukcesów nie wysyła się ponownie. Globalny `OperationGate` i osobne konteksty DB dla zadań; to jeszcze nie trwała kolejka workerów.
- Szkice automatycznie i ręcznie zapisywane; osobny ekran. **48 h od utworzenia**, bez przedłużania przez edycję; maks. **10 na użytkownika**, kolejny zastępuje najstarszy. Zakończony szkic archiwizowany, historia pozostaje.
- Zmiana domyślnego celu aktualizuje niewysłane warianty po otwarciu/synchronizacji, z komunikatem i ponowną walidacją. Opublikowane, przetwarzane i niepewne pozostają przy swoim celu.
- Upload **64 MiB**, magazyn **1 GiB/użytkownika**; cleanup porzuconych/wygasłych plików uwzględnia współdzielenie i trwające/niepewne operacje oraz okres bezpieczeństwa. Szczegóły w `MediaStorage.cs` i [COMPOSER](docs/COMPOSER.md).
- YouTube: film pionowy lub kwadratowy **do 179,99 s**, tytuł do 100 znaków, opis z CTA do 5000 **bajtów UTF-8**, świadomy wybór „dla dzieci”; domyślnie prywatny. Upload daje `Processing`; `Published` dopiero po kontroli statusu i widoczności. Brak trwałego wznawiania po restarcie.

## Limity i funkcje pomocnicze

Aktualne egzekwowane reguły: `Capabilities`, `PublicationRules`, `PostValidator`, `YouTubeRules`; [API_LIMITS](docs/API_LIMITS.md) zawiera źródła i niepewności z 10.11.2026. To nie bezterminowe potwierdzenie dokumentacji dostawców.

- IG/LinkedIn: do 20 zdjęć; IG Reel 3–900 s, LinkedIn film 3–1800 s; brak mieszanych karuzel IG. Obecny walidator IG wymaga JPEG i proporcji 4:5–1,91:1; użytkownik zakwestionował wygodę. Nie traktować nieprzetestowanej granicy jako potwierdzonego zachowania live. Ponowny odczyt stron Meta bywa blokowany 429.
- Facebook: 5000 znaków, 10 zdjęć i 3–60 s filmu są zachowanymi **limitami PUBLICATE**, nie potwierdzonymi maksimami Graph API. TikTok: maksimum czasu filmu zależy od Creator Info.
- R2: adapter S3 z prywatnym i publicznym magazynem, konfiguracja i flaga kosztów; brak dowodu testu live. Lokalny magazyn działa; Instagram korzysta z publicznego HTTPS po zgodzie na udostępnienie.
- AI: adapter rzeczywistego modelu konfigurowanego przez użytkownika, 1–10 zdjęć w kolejności; bez analizy wideo/transkrypcji. Wymaga klucza, modelu i zgody na koszt; brak dowodu testu live. Nie generuje fikcyjnych wyników, gdy niedostępny.

## Uruchamianie i ostatnia weryfikacja

- Lokalizacja tego workspace: `C:\Dev\Projects\PROTOTYP PUBLICATE`; PowerShell/Windows. SDK, PostgreSQL i przeglądarki testowe w `.tools`; dane/klucze/logi w `.data`. Nie ma podstaw, by zakładać, że są dostępne na innym komputerze.
- Backend `http://localhost:5080`, Vite `http://localhost:5173`, PostgreSQL `localhost:55432`. Aktualna publiczna domena developerska: `https://cofounder-morse-reflex.ngrok-free.dev`, tunel do Vite, proxy `/api` i `/media` do backendu. Callback: `/api/oauth/{Platform}/callback` na skonfigurowanym `OAuth:CallbackBase`.
- Konfiguracja: `appsettings.json` → opcjonalny `appsettings.Local.json` → zmienne środowiskowe (ostatnie mają pierwszeństwo). Sekrety nie należą do tych dokumentów. Skrypty konfiguracji: Instagram, LinkedIn, YouTube.
- 11.11.2026 po restarcie komputera uruchomiono DB/backend/frontend/ngrok. Potwierdzono `pg_isready`, HTTP 200 backendu/frontendu i aktywny tunel w lokalnym API ngrok. Poprawiono `Test-Port` launchera o IPv6; wcześniejszy timeout nie oznaczał niedziałającego Vite. **To zapis historyczny, nie gwarancja bieżącego stanu procesów.**
- Ostatnia weryfikacja **11.11: 200/200 testów backendu, build frontendu i oba testy UI PASSED**, wyłącznie z atrapami/przechwyconym API. Testy obejmują odzyskiwanie sesji uploadu, ponowny OAuth/odłączenie i zestaw FB+IG+LinkedIn+YouTube z częściowym powodzeniem. Sandbox blokował Event Log; poza sandboxem cały zestaw przeszedł. NU1900: aktualny audyt zależności niedostępny.

## Odłożone i następny krok

Podstawowa publikacja zbiorcza FB + IG + LinkedIn jest potwierdzona przez użytkownika. Pozostają testy konkretnych formatów i granic oraz konfiguracja Google dla YouTube. Nie wykonywać kolejnych publikacji samodzielnie. Obecny priorytet zawsze ustala najnowsze polecenie użytkownika.

Backlog: [DO_WDROZENIA_W_PRZYSZLOSCI](DO_WDROZENIA_W_PRZYSZLOSCI.md). Blokada zmiany celu na 7 dni osobno dla każdego serwisu i abonament są przyszłością, nie wdrożonymi funkcjami. **? Dopasowanie proporcji oraz ? PNG → JPEG** są niezdecydowane; przed zgodą potrzebna analiza CPU/RAM, magazynu, transferu i alternatywy po stronie przeglądarki. Nie implementować ich przy okazji innych zmian.

## Gdzie czytać dalej i znane rozbieżności

[README](README.md), [ARCHITECTURE](docs/ARCHITECTURE.md), [COMPOSER](docs/COMPOSER.md), [SETUP_ACCOUNTS](docs/SETUP_ACCOUNTS.md), [YOUTUBE_SETUP](docs/YOUTUBE_SETUP.md), [TEST_RESULTS](docs/TEST_RESULTS.md), [API_FEASIBILITY](docs/API_FEASIBILITY.md), [KNOWN_LIMITATIONS](docs/KNOWN_LIMITATIONS.md).

Starsze sekcje README nadal mówią o pustych kluczach i wspólnych limitach 8 MiB/3–60 s; to nie opis aktualnej konfiguracji ani wszystkich platform. Starsze macierze obejmują pięć platform i historyczne blokady Instagrama. Aktualizacja wyników z 11.11 w `TEST_RESULTS.md` zastępuje wcześniejsze stwierdzenia o braku potwierdzenia publikacji zbiorczej i LinkedIna/Facebooka. Przy kolejnych zmianach uzupełniaj odpowiednie dokumenty; nie odtwarzaj starego stanu na podstawie historycznych wpisów.
