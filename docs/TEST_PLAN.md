# Plan testów PUBLICATE

## Niezależne Instagram Login

Skonfiguruj wyłącznie `Providers:Instagram`, pozostawiając Facebook nieaktywny. Użyj konta profesjonalnego IG bez powiązanej strony. Połącz przez przycisk Instagram i sprawdź username oraz jeden cel IG, bez utworzonego celu Facebooka. Ponów odczyt celu. Cofnij jedną zgodę i sprawdź diagnostykę przed publikacją lub odmowę dostawcy. Dopiero po osobnym potwierdzeniu opublikuj JPEG, następnie karuzelę i Reel; sprawdź prawdziwe ID i widoczność w Instagramie. Odśwież token po co najmniej 24 godzinach, sprawdź odłączenie. Nie traktuj lokalnych atrap jako PASSED tej procedury. Testuj oddzielnie Facebooka — zmiana jego konfiguracji nie może wyłączać Instagram Login.

> Aktualizacja 2026-11-05: działa rejestracja i logowanie e-mail/hasło, bez AccessKey. Zasoby należą do zalogowanego użytkownika, OAuth state jest związany z jego sesją. Pełny opis: [USER_ACCOUNTS](USER_ACCOUNTS.md).

## Kategorie wyników

Dla usług zewnętrznych wyłącznie: PASSED (rzeczywiste wywołanie potwierdzone), FAILED (rzeczywista odmowa/błąd), BLOCKED (konfiguracja/uprawnienia), UNSUPPORTED (API nie obsługuje funkcji), NOT_TESTED (nie wykonano). Brak implementacji funkcji dostępnej w API nie jest „UNSUPPORTED w API”. Matryca rozdziela te sytuacje.

## Automatyczne testy bez publikacji

`dotnet test PUBLICATE.slnx` lub `scripts/Test-Local.ps1`. xUnit, EF InMemory dla reguł usług, kontrolowany HttpMessageHandler dla adapterów. Nigdy nie korzystają z sekretów portali. Testy kont obejmują hashing haseł, unikalność e-mail, blokadę po błędnych logowaniach, izolację danych i przypisanie OAuth do użytkownika. Kontroler transportu testowego zgłasza nieoczekiwane żądanie; nie ma fallbacku do sieci.

Zakres: dziedziczenie vs puste nadpisania, niezależna edycja i kolejność, formaty/wymiary/limity/CTA, błędy HTTP i TikTok error przy HTTP 200, brak ID, statusy Processing/Unknown/Published, klucze idempotencji, zakaz retry wyniku nieznanego, kontrolowany retry odmowy uprawnień, owner isolation, serializacja bez tokenów, state jednorazowy i związany z przeglądarką, SSRF upload URL, retencja współdzielonych plików, ostrzeżenie przed usunięciem, zachowanie metadanych, wieloetapowe uploady oraz zgody kosztowe.

Dodatkowy lokalny test akceptacyjny: `scripts/smoke-local.cjs`, działający backend, frontend i prawdziwy PostgreSQL. Skrypt tworzy własne losowe konta testowe, sprawdza ich izolację i testuje sesję, CSRF, readiness, upload/ffprobe, prywatność pliku, zapis szkicu i responsywny UI w Chromium. Nie wywołuje żadnego endpointu publikacji. Zakłada pustą historię nowych kont testowych i wyłączone AI; uruchamiaj na izolowanej bazie testowej. Tworzy oznaczone lokalne szkice i pliki.

Narzędzie browser smoke nie jest zależnością aplikacji. Na nowym komputerze:

```powershell
npm.cmd install --no-save --no-package-lock --prefix .tools/browser playwright
$env:PLAYWRIGHT_BROWSERS_PATH=Join-Path $PWD '.tools/browsers'
node .tools/browser/node_modules/playwright/cli.js install chromium
# Przygotuj .data/test-fixtures/test.jpg (1080x1080) i test.mp4 (H.264, >=3 s).
node scripts/smoke-local.cjs
```

Test relacyjnej migracji: nowa lokalna baza PostgreSQL → `Start-Backend.ps1 -MigrateOnly` → `/api/readiness` musi zwrócić database=true i pustą listę pendingMigrations. EF InMemory nie zastępuje tego testu.

`scripts/Test-Database.ps1` sprawdza na rzeczywistym PostgreSQL indeks aktywnego wariantu, idempotencję RequestKey i RESTRICT klucza obcego. Fixture jest w jednej transakcji zakończonej ROLLBACK, nie staje się kontem ani publikacją w panelu i nie kontaktuje się z dostawcą.

## Testy rzeczywistych usług — ręczny gate

Nie uruchamiać w CI ani przy zwykłym dotnet test. Utwórz konta i produkty według SETUP_ACCOUNTS. Każdy scenariusz tekst/zdjęcia/film ma osobny post bazowy, jednoznaczny opis „PUBLICATE test YYYY-MM-DD …”, własny materiał i świadome potwierdzenie publikacji.

Preferowana ścieżka: Social API Tester. Alternatywnie po zapisaniu i obejrzeniu wariantu:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Live.ps1 -VariantId IDENTYFIKATOR_Z_WLASNEJ_BAZY
```

Skrypt wymaga wpisania PUBLIKUJ. Zgoda na publiczny HTTPS jest oddzielna. X wymaga uprzednio jawnego odblokowania kosztów i zgody na koszt. Skrypt nie ponawia żądania po timeout. Nie wklejaj tokenów do terminalowego logu wyników.

## Scenariusze każdej platformy

| Test | Procedura | Dowód zaliczenia |
|---|---|---|
| OAuth | Połącz konto; sprawdź callback; anuluj zgodę; spróbuj ponownie tego samego state | Poprawny token/identyfikacja, odrzucenie ponownego state |
| Konto | Odczytaj konto/scopes | Rzeczywisty identyfikator i nazwa; brak tokenów w JSON |
| Cel | Odkryj profile/strony i wybierz cel | Cel faktycznie należy do konta i ma wymagany dostęp |
| Tekst | Świadomie opublikuj marker testowy | Potwierdzenie API i wizualna obecność na właściwym koncie |
| Jedno zdjęcie | JPEG 1080x1080, bez cudzych praw | Właściwy obraz, ID prawdziwej publikacji |
| Wiele zdjęć | Dwa różne obrazy z oznaczoną kolejnością | Zachowana kolejność, poprawny post wieloobrazowy |
| Film | MP4/H.264/AAC, 3–10 s; Reel 9:16 | Zakończone przetwarzanie i publikacja, nie tylko upload |
| Status | Odczytaj ponownie stan | ID lub jawne potwierdzenie publish complete; permalink gdy zwracany |
| Autoryzacja | Refresh jeśli dostępny, w innym wypadku reautoryzacja; odwołaj zgodę | Prawidłowa rotacja lub RequiresAuthorization, bez ujawnienia sekretów |
| Retry | Wywołaj jednoznaczną odmowę uprawnień, napraw dostęp, ponów świadomie | Brak duplikatu; Unknown pozostaje zablokowane |

LinkedIn: status tworzenia 201+x-restli-id wystarcza jako potwierdzenie API; dodatkowy odczyt posta może być BLOCKED przez r_member_social. Facebook: tylko Page, sprawdź także widoczność poza rolą aplikacji. Instagram: odróżnij ID kontenera od ID posta, sprawdź dostęp crawlera do HTTPS. TikTok: prywatne konto, rzeczywiste creator_info, wybrane SELF_ONLY, zgody muzyczne i PUBLISH_COMPLETE. X: sprawdź bieżący koszt także odczytu statusu przed testem.

## Awaria i retencja

Testy stanów i timeoutów wykonuj najpierw z atrapami HTTP. Nie odcinaj sieci w czasie prawdziwej publikacji tylko po to, by wyprodukować niepewny wynik. Ręcznie sprawdź restart backendu z testowym zadaniem Uploading/Publishing w izolowanej bazie: powinno przejść w Unknown i nie wykonać żadnego żądania do dostawcy. Przypadek zdalnego sukcesu i utraty odpowiedzi wymaga ręcznego sprawdzenia konta, nie wymuszania retry.

Retencję testuj na plikach testowych: dwa warianty współdzielą plik, jeden zakończony, drugi Processing → brak usunięcia; wszystkie zakończone + okres bezpieczeństwa + ostrzeżenie → usunięty obiekt, metadane zachowane; aktywny szkic → ochrona do limitu nieaktywności; zapis odnawia retencję. Nie przyspieszaj zegara systemowego na maszynie użytkownika. Testy jednostkowe przyjmują jawny czas.

## Rejestr dowodów

Do TEST_RESULTS dopisz datę, platformę, własny ID joba, typ operacji, status HTTP/kod, ID posta jeśli istnieje, wynik kontroli konta i wynik. Nigdy access_token, refresh_token, client_secret, cookies, publiczny link do prywatnego pliku ani pełny payload OAuth. Screenshot posta może być przechowywany lokalnie po usunięciu danych prywatnych. Usunięcie testowego posta wykonaj świadomie w serwisie; PoC nie kasuje go automatycznie.
