# Wyniki testów PUBLICATE

## 14.11.2026 — rozdzielenie inżynierki i PUBLICATE 2.0

PASSED lokalnie: 256/256 testów backendu; build obu frontendów i backendu 2.0; testy kreatora/historii z przechwyconym API w obu folderach. Uruchomiono kolejno oba launchery: health HTTP 200, pg_isready, tunel skierowany do właściwego Vite. Start 2.0 przy działającej inżynierce poprawnie zablokowany. Test skryptów sprawdza parser PowerShell, konflikt portu i normalizację historycznie zagnieżdżonego pliku PID. Naprawiono odczyt starego stanu launchera, dzięki czemu stop działa bez błędu null StartTime.

Baza 2.0: 7 migracji; zero użytkowników z hasłem, połączeń, zadań i mediów. Istniejącej bazy/kluczy inżynierki nie kopiowano do 2.0 i nie resetowano. Klucze developerskie zapisane tylko w lokalnej konfiguracji. Kontrola plików przygotowanych do Git nie wykryła wartości lokalnych sekretów ani katalogów danych/narzędzi. Nie jest to pełny audyt bezpieczeństwa.

Bez nowych wywołań API społecznościowych, publikacji lub testów OAuth. Nowa rejestracja/OAuth w 2.0 pozostają do wykonania przez użytkownika. npm zgłosił historyczną zależność core-js 2 w twitter-text; audyt npm 0 zgłoszeń nie stanowi gwarancji braku podatności. Pierwszy build 2.0 wymagał usunięcia BOM z instance.json; końcowy build PASSED. Po testach zatrzymano frontend/backend/tunel obu wersji; osobne lokalne PostgreSQL pozostają uruchomione.


## Historia — szczegóły wiersza, 13.11.2026

Build frontendu i test-history-ui.cjs PASSED: szczegóły rozwijane przy właściwej publikacji, przełączanie między wierszami, ukrywanie, bez wywoływania kontroli statusu. API przechwycone. Nie zmieniano backendu. Screen użytkownika pokazuje Published X, ID 2121146796151865703, operacja x:posting — dodatkowy dowód UI, nie nowy odczyt dostawcy.

## 13.11.2026 — publikacja X potwierdzona przez użytkownika

PASSED live według relacji użytkownika: publikacja zestawu na pięciu platformach, w kontekście FB/IG/TikTok/X/LinkedIn. YouTube nie opublikował — użytkownik wskazuje dzienny limit kanału osiągnięty także poza PUBLICATE; brak osobnego kodu błędu do potwierdzenia przyczyny. Typ materiału X i ID niepodane, poszczególne formaty/refresh/retry nadal wymagają osobnych dowodów. Konsola X: 2 Requests, saldo 9,97 USD; screen nie identyfikuje endpointów ani liczby utworzonych postów. Bez nowych płatnych zapytań ze strony agenta.

## X — rozszerzenie adaptera, 12.11.2026

PASSED lokalnie: 256/256 testów backendu, build frontendu, UI X/kreator/YouTube/TikTok. Nowe testy: Unicode/NFC, emoji ZWJ i granica 140 emoji, URL=23, limit obrazów, upload zdjęć/filmu i kolejność media_ids, media.write przed HTTP, odrzucenie przetwarzania bez POST, status gotowego materiału wymagający zgody, blokada Unknown, brak ID, bezpieczny błąd kredytów. UI potwierdza licznik 44 znaków ważonych dla polskiego tekstu + emoji rodzinnego + URL i zapis wariantu. Wcześniejszy błąd oczekiwanej długości polskiego zdania poprawiono w teście (17 znaków).

NOT_TESTED live: OAuth X, zdjęcie, 4 zdjęcia, film, refresh i retry na koncie. Nie wykonano płatnych wywołań. Instrukcja: X_SETUP.md. Ostrzeżenie xUnit2031 w istniejącym teście IG nie blokuje testów. Brak pełnej walidacji zaawansowanych parametrów wideo opisany w X_SETUP.md.

## PASSED live — zestaw pięciu publikacji wideo, 12.11.2026

Dowód: użytkownik potwierdził „poszło na 5 platform jednocześnie” i przekazał osiem zrzutów z 12.11.2026, 01:44:44–01:49:38. Widać pięć wariantów z oddzielnymi opisami/CTA, zgodę na publiczny HTTPS i jedno zatwierdzenie publikacji, a następnie film w każdym z pięciu serwisów.

| Platforma | Cel | Wynik potwierdzony przez użytkownika i screen |
|---|---|---|
| Facebook | Bronski | PASSED — film na stronie; etykieta „Opublikowane przez: PUBLICATE” |
| Instagram | bronski.studio | PASSED — opublikowany film z opisem i CTA wariantu |
| TikTok | patrykb78 | PASSED — prywatny film; w kreatorze wybrane SELF_ONLY |
| YouTube Shorts | Patryk B | PASSED — film w zakładce Shorts, 0:44, niepubliczny (unlisted) |
| LinkedIn | Patryk B | PASSED — film na profilu z opisem i CTA wariantu |

Scenariusz: jeden materiał wideo, pięć niezależnych wariantów, przycisk „Opublikuj na 5 platformach”. „Jednocześnie” oznacza wspólne uruchomienie zestawu; nie ustalono identycznego czasu zakończenia operacji dostawców. Nowa integracja TikTok i pobranie materiału w tym środowisku zadziałały end-to-end. Połączenie i cel TikToka są potwierdzone działającym scenariuszem użytkownika, bez osobnego raportu OAuth.

Nie wykonywano dodatkowych zapytań do API, odczytu bazy ani kolejnych publikacji. Screeny nie przedstawiają końcowych statusów zadań PUBLICATE/odpowiedzi status API ani pełnych ID wszystkich postów — ich zgodność pozostaje do osobnego sprawdzenia. Nie oznaczamy jako PASSED odświeżenia tokenów, retry, granic limitów, zdjęć TikTok ani dostępu dla innych użytkowników. X nie uczestniczył w teście. Wcześniejsze NOT_TESTED/BLOCKED poniżej są historyczne w zakresie rozstrzygniętym tym testem.

Źródła dowodów: pliki użytkownika `Zrzut ekranu 2026-11-12 014444.png`, `014502.png`, `014517.png`, `014702.png` (TikTok), `014748.png` (Facebook), `014811.png` (Instagram), `014900.png` (LinkedIn), `014938.png` (YouTube), w lokalnym katalogu Pictures/Screenshots. Plików nie kopiowano do repozytorium.

## Publiczne strony do konfiguracji TikToka — 12.11.2026

Build TypeScript/Vite PASSED. `test-public-pages.cjs`: terms/privacy/about bez sesji i bez żądań API, ponowne otwarcie, opis przy logowaniu, szerokość mobilna — PASSED. Regresje `test-composer-ui.cjs` i `test-youtube-ui.cjs` PASSED; API przechwycone, zero publikacji. Brak zmian backendu. Akceptacja dokumentów/weryfikacja ngrok przez TikTok NOT_TESTED. Dane operatora i kontakt odłożone przez użytkownika; nie deklarujemy gotowości dokumentów do publicznej usługi.

## Instagram — przetwarzanie filmu i retry, 11.11.2026

- Odczyt lokalnej bazy: próba IG `0806ba2d-f269-42f1-b65d-0237bfd339aa` zwróciła Processing po 32269 ms, zanim wysłano media_publish. Status przeszedł z IN_PROGRESS do FINISHED. Pierwszy odczyt FINISHED ustawił Failed/SafeToRetry=true, dalsze błędnie zmieniały flagę na false. Nie był to dowód odrzucenia kodeka przez Meta.
- W tym zestawie lokalne zadania FB, LinkedIn i YouTube mają Published; YouTube potwierdził widoczność unlisted. Nie wykonano ponownego odczytu tych platform ani nowej publikacji.
- Poprawka: dłuższy polling i utrzymanie potwierdzonego retry; odzyskanie historycznej flagi na podstawie zapisanych prób podczas żądania statusu. Status nigdy nie wywołuje media_publish. Ponowne zatwierdzenie tworzy nową próbę/kontener, a sukcesy innych platform pozostają pominięte.
- **PASSED lokalnie: 225/225 testów**, w tym opóźnione FINISHED, wyczerpane oczekiwanie, wielokrotny status, blokada Unknown/niepewnej publikacji i wymagana historia dla naprawy. NU1900: audyt NuGet niedostępny. **NOT_TESTED live:** naprawiona ścieżka ponowienia IG; wymaga zatwierdzenia użytkownika.

## Kodeki wideo — 2026-11-11

**PASSED lokalnie:** 218/218 testów backendu i build frontendu po usunięciu globalnego wymogu H.264. `VideoCodecTests` weryfikuje przyjęcie metadanych HEVC/VP9/AV1, walidację osobną IG/TikTok, odrzucenie złego kontenera/czasu/formatu obrazu oraz pomijanie okładek przy wyborze strumienia wideo. NU1900: aktualny audyt NuGet niedostępny. **NOT_TESTED live:** publikacja nowych kodeków; pliku użytkownika nie analizowano. [Zakres i ograniczenia](VIDEO_CODECS.md).

## YouTube — uzupełnienie 2026-11-11

- **PASSED lokalnie:** 200/200 testów backendu, build TypeScript/Vite, `test-composer-ui.cjs`, `test-youtube-ui.cjs`. API zastąpione atrapami; żadnej publikacji ani wywołania Google.
- Nowe przypadki: utrata odpowiedzi i odzyskanie ID tej samej sesji, dosłanie tylko brakujących bajtów, błędny Range, wygasła sesja bez duplikowania uploadu, 503 z odczytem pozycji, błędy OAuth/limitów bez wycieku opisu, jawna deklaracja treści syntetycznych. Ponowne połączenie zachowuje jeden kanał i refresh token, lokalne odłączenie usuwa tokeny. Zestaw czterech platform potwierdza niezależne dane, częściowy błąd YouTube i idempotencję.
- Pierwszy przebieg w sandboxie: 16 błędów hosta testowego z powodu odmowy dostępu do Windows Event Log. Po uruchomieniu poza sandboxem cały zestaw przeszedł. Ostrzeżenie NU1900 oznacza brak aktualnego audytu zależności z NuGet.
- **BLOCKED live:** Google OAuth, kanał, refresh i upload/status oczekują na lokalny Client ID/Secret oraz świadomy test użytkownika. Google Cloud skonfigurowany według użytkownika; nie zweryfikowano portalu. Brak procentowego postępu i trwałego wznawiania po restarcie procesu.


## Publikacja zbiorcza FB + IG + LinkedIn — potwierdzenie 2026-11-11

**PASSED — rzeczywisty test wykonany i potwierdzony przez użytkownika w rozmowie.** Użytkownik wybrał Facebook, Instagram i LinkedIn, użył przycisku „Opublikuj na x platformach” (x = 3) i potwierdził sukces na wszystkich trzech serwisach.

- Potwierdza publikację zbiorczą z kreatora i rzeczywistą publikację na trzech platformach. **Minimum dwóch platform i optymalny zestaw FB/IG/LinkedIn osiągnięte.**
- 11.11.2026 to data przekazania potwierdzenia. Data samej próby, typ mediów, ID i URL postów nie zostały podane. Nie wykonywano teraz niezależnego odczytu API.
- Nie rozszerzać wyniku na wszystkie formaty, granice limitów, scenariusze częściowych błędów ani dostęp komercyjnych użytkowników.
- LinkedIn OAuth także został potwierdzony screenshotem z 10.11: raport `18346927e09541b1825e1a8fc640ca65`, code_exchange/profile/token_inspection HTTP 200 po poprawieniu konfiguracji klienta.
- Poniższe wpisy o braku testu zbiorczego i nieosiągnięciu minimum opisują wcześniejszy stan. Zachowano je jako historię.

## Diagnostyka LinkedIn OAuth — 2026-11-10

- Rzeczywista próba użytkownika: FAILED, HTTP 401 w pierwszym wywołaniu po callbacku. Pierwotny raport nie zachował kodu błędu; dokładna przyczyna nieustalona.
- Naprawiono zapisywanie tekstowych kodów OAuth, nazwano etapy code_exchange/profile/token_inspection/token_refresh i usunięto mylącą etykietę „kod Meta” dla innych dostawców. Opisy mogące zawierać sekret są pomijane; zapisywany jest hash.
- PASSED: 187/187 testów backendu i build frontendu. Kolejna rzeczywista autoryzacja wymagana do diagnozy; nie odtwarzano starego kodu ani callbacku.

## YouTube Shorts — 2026-11-10

- PASSED: 184/184 testów backendu. Nowe: 179,99 s, proporcje, UTF-8, wymagane ustawienia, OAuth/PKCE/state, token refresh, zgodność kanału, status Processing po uploadzie, potwierdzenie widoczności, blokada obcego hosta uploadu.
- PASSED: TypeScript/build Vite i izolowany `scripts/test-youtube-ui.cjs` (sześć platform w kolejności, prywatna widoczność domyślna, świadomy wybór odbiorców, zapis ustawień). Żadnych wywołań Google.
- BLOCKED: rzeczywisty OAuth, pobranie kanału, upload, status i odświeżenie tokenu — brak konfiguracji Google. Nie oznaczać ich PASSED na podstawie atrap.
- UNSUPPORTED: posty społecznościowe ze zdjęciami/tekstem przez oficjalne API. Długie filmy wyłączone decyzją produktu.

## Aktualizacja limitów API — 2026-11-10

- PASSED: 163/163 testów backendu; nowe granice mediów oraz wysłanie 20 elementów Instagram/LinkedIn sprawdzono z atrapami HTTP. TikTok odmawia przed uploadem po przekroczeniu dynamicznego limitu twórcy.
- PASSED: TypeScript i build Vite; test przeglądarkowy kreatora z przechwyconymi wszystkimi żądaniami API, bez publikacji.
- NOT_TESTED: rzeczywiste publikacje przy nowych maksymalnych wartościach. Nie wykonywano płatnych wywołań X/AI ani publikacji na kontach.
- Pierwszy przebieg testów w sandboxie: 144/160; 16 testów hosta zatrzymała odmowa zapisu do Windows Event Log. Pełny przebieg w kontekście użytkownika Windows przeszedł. Ostrzeżenie NU1900: niedostępny feed audytu podatności NuGet; nie oznacza wyniku audytu bezpieczeństwa.
- Źródła, rozbieżności dokumentacji i brakujące walidacje: [API_LIMITS.md](API_LIMITS.md).

## Kreator dwuetapowy — 2026-11-10

- PASSED: 145/145 testów backendu (w tym quota, 48 h, 10 szkiców, niezależność wariantów, częściowy sukces, idempotencja i blokada całego zestawu przed wywołaniami API).
- PASSED: TypeScript i produkcyjny build Vite.
- PASSED: `scripts/test-composer-ui.cjs`, całkowicie przechwycone API; dwa etapy, blokada niepodłączonych serwisów, niezależne opisy, anulowanie powrotu, reset przez Wstecz, zapis/odczyt, komunikat podmiany celu, responsywność 390 px. Zero wywołań publikujących.
- PASSED: kopia bazy przed migracją, migracja ComposerDrafts i transakcyjny test PostgreSQL (ROLLBACK).
- PASSED: backend uruchomiony ponownie pod kontem Windows użytkownika, `/api/health` odpowiada. Brak błędów w logu startu.
- NOT_TESTED: rzeczywista publikacja zbiorcza w nowym kreatorze. Nie wykonano jej automatycznie.

Pełne zasady i procedura: [COMPOSER.md](COMPOSER.md). Ostrzeżenie NU1900 nadal oznacza brak aktualnego odczytu bazy podatności, nie pozytywny wynik audytu.

Poniższe wyniki opisują wcześniejsze etapy projektu.

## Pierwsza rzeczywista publikacja — Instagram

2026-11-05 o 17:48 CET: użytkownik opublikował test pojedynczego zdjęcia na `bronski.studio` i potwierdził jego widoczność bezpośrednio na profilu. Przekazany zrzut Historii PUBLICATE pokazuje jedną próbę, status Published, zewnętrzny ID posta oraz osobny identyfikator kontenera. To potwierdzony test rzeczywisty, nie atrapa HTTP. Wklejony równocześnie raport `844db7ae859540dd8701c3f8fa3fc1c5` dotyczy wcześniejszego OAuth, nie samej publikacji. Oddzielny odczyt statusu/permalink nie został jeszcze potwierdzony.

Wynik projektu: **1 z minimum 2 platform z rzeczywiście potwierdzoną publikacją**. Karuzela i film pozostają nieprzetestowane. Starsze opisy braku publikacji poniżej są historyczne.

Aktualizacja: 2026-11-05 — niezależne Instagram Login oraz ponowne testy kont użytkowników. **Lokalne testy nie są dowodem publikacji przez dostawcę.**

## Wykonane lokalnie

| Sprawdzenie | Wynik | Dowód / zakres |
|---|---|---|
| Backend .NET 10 | PASSED | Kompilacja bez błędów; SDK lokalne 10.0.403. NuGet NU1900: nie udało się ponownie pobrać danych o podatnościach |
| xUnit / atrapy HTTP | PASSED | **117/117**, 0 pominiętych; `tests/Publicate.Tests/TestResults/unit-tests.trx` |
| Frontend TypeScript / Vite | PASSED | `npm.cmd run build`; produkcyjny bundle w dist |
| Migracje PostgreSQL 17.11 | PASSED | InitialCreate + TrackIncompleteUploads + UserAccounts + InstagramAuthMethod + InstagramClientBusiness zastosowane; ostatni skrypt migracji zakończył się kodem 0 |
| Relacyjne ograniczenia | PASSED | Test transakcyjny: unikalność aktywnego wariantu, RequestKey i RESTRICT FK; zakończony ROLLBACK |
| Lokalny test akceptacyjny | PASSED | **29/29**, prawdziwy HTTP/PostgreSQL/Chromium; [raport JSON](LOCAL_SMOKE_RESULTS.json) |
| Sesja i ochrona endpointów | PASSED | 401 bez sesji, 403 bez custom header i dla obcego Origin, callback OAuth wymaga sesji PUBLICATE. Jednorazowość/wygaśnięcie state sprawdzone oddzielnie w xUnit |
| Pliki | PASSED | JPEG 1080×1080, MP4 H.264 3 s, prywatny podgląd, brak publicznego dostępu przed zgodą; fałszywy JPG odrzucony |
| Przegląd i tester | PASSED | Renderowanie pięciu platform, brak możliwości testu bez celu, brak błędów runtime JS |
| Widok mobilny | PASSED | Szerokość 390 px bez poziomego przewijania; nie jest to pełny audyt dostępności |
| Audyt zależności | NOT_TESTED | Poprzedni audyt NuGet/npm nie zgłosił podatności; w bieżącym uruchomieniu NuGet NU1900 — nie potwierdzono ponownie aktualnego stanu |
| R2 / AI live | BLOCKED | Brak kluczy, bucketów i zgody na koszt; brak wywołań |

Zrzuty: [desktop](screenshots/dashboard.png), [tester](screenshots/tester.png), [mobile](screenshots/mobile.png). Zrzuty pokazują testowego użytkownika PUBLICATE, **0 połączonych kont społecznościowych, 0 potwierdzonych publikacji**. Lokalne szkice i pliki testowe nie są postami społecznościowymi.

Instagram Login: 12 nowych przypadków testowych sprawdza osobną konfigurację i host OAuth, wymianę tokenu (odpowiedź płaska i opakowana), tworzenie wyłącznie konta IG, rzeczywiste pola zgód z atrapy, odrzucenie obcego browser/state, replay, odmowę API, niespójną tożsamość, brak potwierdzonych scopes, blokadę tokenu starej ścieżki, odświeżanie, wygaśnięcie oraz host publikacji i statusu. Brak połączenia do Meta podczas tych testów. Rzeczywiste OAuth, zdjęcie, karuzela, Reel, status i refresh Instagram Login: **BLOCKED — brak konfiguracji aplikacji Instagram**.

W trakcie prac poprawiono błędy kompilacji testów, zgodność wersji EF, kodowanie skryptów PowerShell, selektor przycisku testu UI oraz odczyt ConnectionString w skrypcie PostgreSQL. Częsty rerun smoke początkowo trafił na działający rate limit logowania (429); finalny przebieg przeszedł po restarcie testowym. Nie ukrywamy tych prób jako sukcesów API.

## Konta PUBLICATE — 2026-11-05

Rejestracja, logowanie, wylogowanie i ekran Moje konto przeszły test przeglądarkowy. Test drugiego konta potwierdził izolację szkiców i multimediów. AccessKey nie pozwala się zalogować. OAuth state jest związany z użytkownikiem; próbę przejęcia callbacku przez innego użytkownika sprawdzono z atrapą HTTP, bez wywołania dostawcy. Dodano 9 testów xUnit (łącznie 84) i rozszerzono smoke do 29 przypadków. [Opis](USER_ACCOUNTS.md).

## Macierz rzeczywistych usług

PASSED oznacza realne wywołanie. Instagram Unified Login potwierdzono 2026-11-05 o 17:36 CET, raport `844db7ae859540dd8701c3f8fa3fc1c5` (potwierdzony przez użytkownika i lokalny log). Nie wykonano publikacji. Odczyt scopes nie jest dowodem przyznania wszystkich uprawnień do publikowania.

| Scenariusz | LinkedIn profil | Facebook Page | Instagram Professional | TikTok prywatne | X |
|---|---|---|---|---|---|
| OAuth | BLOCKED | BLOCKED | PASSED | BLOCKED | BLOCKED |
| Dane konta i scopes | BLOCKED | BLOCKED | PASSED (inspekcja tokenu i odczyt portfolio/kont) | BLOCKED | BLOCKED |
| Pobranie miejsc publikacji | BLOCKED | BLOCKED | PASSED (assigned_instagram_accounts) | BLOCKED | BLOCKED |
| Wybór celu w testerze | BLOCKED | BLOCKED | PASSED | BLOCKED | BLOCKED |
| Tekst bez mediów | BLOCKED | BLOCKED | UNSUPPORTED | UNSUPPORTED | BLOCKED |
| Jedno zdjęcie | BLOCKED | BLOCKED | PASSED | NOT_TESTED¹ | NOT_TESTED¹ |
| Wiele zdjęć | BLOCKED | BLOCKED | NOT_TESTED | NOT_TESTED¹ | NOT_TESTED¹ |
| Film | BLOCKED | BLOCKED | NOT_TESTED | BLOCKED | NOT_TESTED¹ |
| Potwierdzenie publikacji | BLOCKED | BLOCKED | PASSED (Published + ID, widoczność potwierdzona przez użytkownika) | BLOCKED | BLOCKED |
| Odnowienie autoryzacji² | BLOCKED | BLOCKED | BLOCKED | BLOCKED | BLOCKED |
| Retry po jednoznacznym błędzie | BLOCKED | BLOCKED | BLOCKED | BLOCKED | BLOCKED |

¹ API oferuje media, ale konkretny adapter PoC nie implementuje tej ścieżki. Nie oznaczamy jej jako niedostępnej w API. TikTok dodatkowo wymaga zweryfikowanej domeny dla zdjęć. X wymaga zgody na koszt oraz rozszerzenia o upload mediów.

² LinkedIn: refresh jeśli token przyznany, inaczej ponowne OAuth. Meta Facebook Login: ponowne OAuth i pobranie tokenów stron. TikTok/X: refresh z rotacją. Lokalne testy rotacji używają atrap. Użytkownik wykonał rzeczywisty Unified Login, który zakończył się błędem callbacku; osobny test danych aplikacji potwierdził odrzucenie App Secret, a po poprawieniu HTTP 200. Nie wykonano jeszcze potwierdzonego odświeżania ani publikacji.

Przyczyny BLOCKED: brak ClientId/ClientSecret, aplikacji developerskich, kont testowych, zakresów i dostępu produktów. Instagram dodatkowo: brak potwierdzonego stagingu HTTPS. TikTok: brak przyznanego video.publish i potencjalna niezgodność zastosowania prywatnego z audytem. X: wyłączona zgoda kosztowa i brak kredytów/konfiguracji. Brak testu nie oznacza FAILED.

## Raport faz

| Faza | Co rzeczywiście sprawdzono | Co wymaga działania / pozostaje niezweryfikowane |
|---|---|---|
| 1. Analiza | Odczytane źródła LinkedIn, TikTok, X, R2; oficjalna kolekcja Meta; raport wykonalności | Część dokumentacji Meta niedostępna; aktualna wersja Graph, produkty i scopes w portalu użytkownika |
| 2. Szkielet | React, backend, PostgreSQL, migracje, health/readiness, sesja | Docker nie był dostępny; użyto przenośnego PostgreSQL, Compose nieuruchomione |
| 3. LinkedIn | Rzeczywisty kod OAuth/Posts/Images/Videos, testy kontraktów z atrapą | Rejestracja aplikacji, produkty, OAuth i publikacja na Twoim profilu |
| 4. Meta | Kod Pages/Instagram, wieloetapowe media, testy HTTP z atrapą | App ID/Secret, wersja Graph, role, Page/Professional, HTTPS, realne potwierdzenia |
| 5. TikTok/X | Adaptery i lokalne bramki zgód/kosztów, testy zakazu wywołań | Zatwierdzony video.publish; osobna zgoda na płatne X; realne wywołania |
| 6. Panel | Desktop/mobile, zapis szkicu, upload, tester, historia; jednostkowe reguły wariantów | Interaktywne przejście OAuth i publikacji na prawdziwym koncie |
| 7. AI/media | ffprobe i magazyn lokalny, testy retencji, adapter R2, Responses dla zdjęć z atrapą | R2/AI live; klatki wideo i transkrypcja niezaimplementowane |
| 8. Testy | 84 xUnit, 29 smoke, transakcyjny PostgreSQL, build i audyty zależności | Realne testy pięciu dostawców, dostęp dla użytkowników spoza zespołu, audyt komercyjny |

## Werdykt

Uruchamialny prototyp lokalny jest przygotowany do dalszych eksperymentów. **Warunek minimum dwóch rzeczywistych publikacji nie jest spełniony.** Najbardziej bezpośredni następny krok to autoryzacja LinkedIn i testowej Facebook Page, a następnie dwie oddzielnie zatwierdzone publikacje tekstowe. Projekt ma sens jako temat inżynierski, ale faktyczny dostęp oraz gotowość komercyjna wymagają tych dowodów i review dostawców.

## Miejsce na kolejne dowody

| Data | Platforma | Operacja | Job ID | HTTP / kod | Potwierdzone ID | Wynik | Uwagi |
|---|---|---|---|---|---|---|---|
| — | — | Nie wykonano testów zewnętrznych | — | — | — | NOT_TESTED | Bez wpisywania sekretów |

Aktualizacja diagnostyki OAuth: rzeczywisty POST client_credentials z poprawionym App Secret otrzymał HTTP 200; wcześniej HTTP 400/code 1 i „Error validating client secret.”. Nie jest to test publikacji ani pełnego authorization_code. 114 testów obejmuje również callback HTTP 302 po sukcesie i błędzie, brak sesji, błędny state, izolację raportu i redakcję sekretów. GET z App Secret nie wykonywano. Szczegóły: [OAUTH_DIAGNOSIS.md](OAUTH_DIAGNOSIS.md).
Dodatkowo 3 testy Chromium interfejsu OAuth z atrapami: sukces, blad Meta, brak sesji — wszystkie PASSED. Nie sa to testy rzeczywistego logowania Meta.
# TikTok — aktualizacja 12.11.2026

241/241 testów backendu PASSED, build frontendu PASSED. Testy UI TikTok, kreator i YouTube PASSED; przechwycone API, żadnej publikacji. Nowe przypadki: PULL_FROM_URL bez PUT, granice pochodzenia/prefiksu HTTPS, zgody, limit twórcy, blokady interakcji, status nieznany/FAILED/prywatny PUBLISH_COMPLETE, bezpieczny komunikat wymagania prywatnego konta. UI: automatyczne dane twórcy, brak domyślnej widoczności, utrwalenie AI/marki/interakcji i konieczna zgoda HTTPS.

Live: weryfikacja prefiksu PASSED wg użytkownika; OAuth, konto/cele, film, potwierdzenie publikacji, refresh i retry TikTok NOT_TESTED. Klucze wpisane lokalnie, ich obecność sprawdzona bez ujawnienia wartości. Zdjęcia nieobsługiwane przez obecną wersję adaptera. NU1900: nie udało się pobrać aktualnego audytu pakietów; istniejące ostrzeżenie xUnit2031 bez wpływu na wynik testów.

