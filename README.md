# PUBLICATE 2.0 — wersja rozwojowa

Lokalny PoC publikacji przez oficjalne API: Facebook Pages, Instagram, TikTok, YouTube Shorts, X i LinkedIn. React/TypeScript/Vite/Tailwind + ASP.NET Core 10/EF Core/PostgreSQL.

Stan 14.11.2026: użytkownik potwierdził rzeczywiste publikacje na wszystkich sześciu platformach w różnych testach; 12.11 film trafił na pięć platform jednym zatwierdzeniem. Nie oznacza to przetestowania wszystkich formatów i scenariuszy. [Wyniki](docs/TEST_RESULTS.md).

Ta wersja służy dalszemu rozwojowi aplikacji. Wersja inżynierska jest osobno w PROTOTYP PUBLICATE. [Kontekst Codexa](PROJECT_CONTEXT.md), [zasady](AGENTS.md), [architektura](docs/ARCHITECTURE.md), [rozdzielenie projektów](docs/WORKSPACES.md).

## Obecny komputer Windows

W PowerShell w folderze właściwego projektu:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Publicate.ps1
```

Startuje bazę, backend, frontend i ngrok; `-NoBrowser` pomija przeglądarkę. Uruchamiaj jako właściciel danych Windows, nie sandbox. Logi: `.data/launcher`. Nie uruchamiaj ponownie Initialize-Local na przygotowanych projektach.

Przed przejściem do drugiej wersji zakończ OAuth/publikacje i uruchom z bieżącego folderu:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Stop-Publicate.ps1
```

Procesy ręczne i PostgreSQL pozostają; bazy mają osobne porty. Potem uruchom launcher w drugim folderze. [Instrukcja](docs/WORKSPACES.md).

## Klon GitHub na nowym komputerze

Repo nie zawiera kluczy, tokenów, bazy, mediów ani `.tools`. Wymagane Node.js 24, .NET SDK 10, PostgreSQL 17, ffprobe, ngrok. Launcher lokalny oczekuje PostgreSQL w `.tools/postgresql/pgsql/bin`.

Przywróć biblioteki: `npm ci` w `src/publicate-web`, `dotnet restore PUBLICATE.slnx`. Porty: `instance.json`. `Initialize-Local.ps1` tylko dla nowej instalacji; Docker Compose to alternatywa, nie druga baza na tym samym porcie.

Konfiguracja lokalna: `src/Publicate.Api/appsettings.Local.json`, nigdy w repo/rozmowie. Ustaw connection string, Local:Origin, OAuth:CallbackBase, publiczny HTTPS magazynu, Media:FfprobePath. Skrypty Configure-* przyjmują klucze dostawców lokalnie. Na nowej bazie `scripts/Start-Backend.ps1 -MigrateOnly`; przed zmianą istniejącej bazy backup.

[Konta](docs/SETUP_ACCOUNTS.md), [YouTube](docs/YOUTUBE_SETUP.md), [TikTok](docs/TIKTOK_SETUP.md), [X](docs/X_SETUP.md). Klucze developerskie nie zastępują połączenia konta w UI. X/AI/R2 mogą kosztować. Publikacja wymaga zgody.

## Testy i ograniczenia

Polecenia z lokalnym SDK/Playwright: [AGENTS](AGENTS.md). Backend `dotnet test tests/Publicate.Tests -c Composer`, frontend `npm run build`. Testy UI przechwytują API; live osobno: [TEST_PLAN](docs/TEST_PLAN.md).

Brak automatycznej konwersji, trwałych workerów i gotowości publicznej usługi komercyjnej. [Ograniczenia](docs/KNOWN_LIMITATIONS.md), [backlog](DO_WDROZENIA_W_PRZYSZLOSCI.md).
