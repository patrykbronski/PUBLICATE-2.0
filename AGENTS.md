# Zasady pracy nad PUBLICATE

Przeczytaj najpierw [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md), następnie dokumentację właściwego modułu. Rozmawiaj i pisz instrukcje po polsku. Projekt jest lokalnym PoC pracy inżynierskiej z możliwością dalszej komercjalizacji; priorytetem są rzeczywiste oficjalne integracje i wiarygodna diagnostyka.

## Zakres i wiarygodność

- Realizuj aktualne polecenie użytkownika. Nie traktuj backlogu jako zgody na wdrożenie. Ustalenia przyszłych funkcji są w [DO_WDROZENIA_W_PRZYSZLOSCI.md](DO_WDROZENIA_W_PRZYSZLOSCI.md), w tym pomysły oznaczone `?`.
- Zachowaj modularny monolit React/TypeScript + ASP.NET Core/EF/PostgreSQL. Nie przebudowuj architektury przy lokalnej poprawce.
- Oddzielaj kod działający z atrapami HTTP od potwierdzonego działania na koncie dostawcy. `PASSED` rzeczywistego testu wymaga dowodu realnego wywołania; kompilacja nie wystarcza.
- Przy zmianie integracji sprawdzaj aktualne oficjalne źródła i datę weryfikacji. Rozróżniaj limit API, możliwości adaptera i limit produktu. Brak dostępu lub sprzeczne informacje zapisuj jawnie; nie zastępuj ich domysłami.
- Publikacja wymaga świadomego zatwierdzenia. Nie wykonuj realnych publikacji ani płatnych wywołań X/AI/R2, zakupów czy doładowań bez odpowiedniej zgody użytkownika. Nie stosuj nieoficjalnych botów ani automatyzacji logowania.

## Bezpieczeństwo i niezmienniki

- Nie wypisuj sekretów, tokenów, kodów OAuth, pełnych callbacków z query ani URL sesji uploadu. Nie czytaj całego `appsettings.Local.json` do wyjścia narzędzia. Do diagnostyki wystarczają wartości niesekretne lub obecność ustawienia.
- Dane dostępowe wpisuje użytkownik lokalnie przez `scripts/Configure-*.ps1` lub bezpieczną konfigurację. Nie proś o sekret w rozmowie. Wymiana kodów OAuth pozostaje POST; użytkownik odmówił testów z App Secret w URL.
- Zachowaj jednorazowy `state`, powiązanie z sesją i właścicielem, PKCE tam, gdzie wdrożone, oraz bezpieczny powrót do UI. Diagnostyka używa dozwolonych pól; opisy błędów mogą ujawniać dane i nie mogą być logowane bez ograniczeń.
- Dostęp do zasobów musi uwzględniać `OwnerId`. Nie zakładaj jednego konta społecznościowego na platformę. Cel publikacji i podłączone konto są osobnymi bytami.
- Nie zmieniaj istniejących numerów `Platform` ani `JobState`: są zapisane w bazie, a indeksy używają wartości stanów. Kolejność prezentacji jest osobna (`PlatformOrder`, `platforms.ts`).
- Nie ustawiaj `Published` ani fikcyjnego ID bez potwierdzenia dostawcy. Zachowaj `Processing`/`Unknown`, idempotencję `RequestKey` i blokadę niebezpiecznych ponowień. Nie usuwaj mediów używanych przez aktywne warianty lub operacje.
- Nie kadruj ani nie konwertuj plików automatycznie. Dopasowanie proporcji i PNG → JPEG są **niezatwierdzone**, zależne od analizy kosztów.

## Lokalne środowisko Windows

- Ten workspace to PUBLICATE 2.0. Osobna inżynierka: `C:\Dev\Projects\PROTOTYP PUBLICATE`. Sprawdź `instance.json` i `docs/WORKSPACES.md`; nie edytuj drugiego repo bez polecenia. Nowy czat ma pracować w wybranym folderze.
- Projekty działają naprzemiennie na jednej domenie ngrok. Porty bierz z `instance.json`; nie przywracaj sztywnego 5080/5173 w kopii 2.0. Przed przełączeniem dokończ OAuth i pobieranie/przetwarzanie mediów. Cookie i klucze Data Protection obu wersji są osobne.

- Główny start: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Publicate.ps1 -NoBrowser`. Uruchamia bazę, backend, frontend i ngrok, zachowując istniejące procesy. Bez `-NoBrowser` otwiera stronę.
- Realny backend uruchamiaj pod kontem Windows właściciela projektu, **nie jako `codexsandbox*`**. Klucze Data Protection w `.data/keys` chroni DPAPI powiązane z użytkownikiem. Nie usuwaj ani nie regeneruj kluczy w ramach naprawy.
- Baza i lokalne sekrety już istnieją. Nie uruchamiaj ponownie `Initialize-Local.ps1`, nie resetuj bazy. Przed migracją zachowaj kopię; sprawdź aktywne publikacje przed restartem backendu.
- Backend 5081, Vite 5174, PostgreSQL 55433, panel agenta ngrok 4040. Sprawdzaj faktyczny stan; launcher obsługuje loopback IPv4 i IPv6. `/api/health` potwierdza proces, nie sprawność bazy; PostgreSQL sprawdzaj `pg_isready`, a readiness wymaga sesji.
- OAuth z callbackiem ngrok rozpoczynaj w UI na tej samej domenie. Zmiana hosta może zgubić cookie/state.
- `.data` i `.tools` to lokalne dane/narzędzia, nie przenośny kod projektu. Nie publikuj ich, logów, kopii bazy ani konfiguracji lokalnej.

## Weryfikacja zmian

Testy nie mogą automatycznie publikować. Dla zmiany backendu uruchom odpowiedni zestaw; dla frontendu build i test UI adekwatny do zmiany. Nie uruchamiaj testów aplikacji wyłącznie z powodu korekty dokumentacji.

```powershell
# Z katalogu projektu, jeśli używasz lokalnego SDK:
$env:DOTNET_ROOT = Join-Path $PWD '.tools/dotnet'
$env:DOTNET_CLI_HOME = Join-Path $PWD '.tools/cli'
$env:NUGET_PACKAGES = Join-Path $PWD '.tools/packages'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
& .tools/dotnet/dotnet.exe test tests/Publicate.Tests -c Composer --no-restore
# Oddzielna konfiguracja nie nadpisuje uruchomionego backendu bin/Debug.
# --no-restore stosuj tylko, gdy zależności zostały już przywrócone.

Push-Location src/publicate-web
npm.cmd run build
Pop-Location

# Wymaga działającego Vite oraz lokalnie zainstalowanego Playwright:
$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $PWD '.tools/browsers'
node scripts/test-composer-ui.cjs
node scripts/test-youtube-ui.cjs
```

Oba powyższe testy UI przechwytują żądania API. Inne skrypty smoke/database/live przeczytaj przed użyciem: nie zakładaj, że są bez skutków ubocznych. Odmowa Windows Event Log w sandboxie jest ograniczeniem środowiska; NU1900 oznacza brak aktualnego wyniku audytu zależności, a nie brak podatności.

## Utrzymywanie kontekstu

Po istotnej zmianie aktualizuj **PROJECT_CONTEXT.md**: datę, stan funkcji/integracji, dowody testów, blokady i decyzje. Uzupełnij właściwy dokument szczegółowy (`TEST_RESULTS`, `API_LIMITS`, instrukcje konfiguracji). `AGENTS.md` zmieniaj, gdy zmieniają się zasady lub sposób pracy, nie przy każdym drobnym commicie.

Zapisuj rozróżnienie: zaimplementowane / testowane lokalnie / potwierdzone live / zablokowane / nieprzetestowane. Nie zmieniaj historycznych niepowodzeń w sukcesy; dopisz późniejsze rozstrzygnięcie. Ostatnie wyraźne ustalenie użytkownika ma pierwszeństwo przed starym opisem; rozbieżność dokumentacji z kodem lub dowodami nazwij wprost.
