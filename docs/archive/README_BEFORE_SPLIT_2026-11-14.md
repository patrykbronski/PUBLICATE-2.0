# PUBLICATE — działający lokalny PoC

YouTube Shorts: integracja i pola kreatora są gotowe do konfiguracji. Zobacz [instrukcję Google i testu](docs/YOUTUBE_SETUP.md); dane wpisuje się lokalnie przez `scripts/Configure-YouTube.ps1`. Rzeczywiste połączenie i publikacja wymagają konfiguracji i testu właściciela kanału. Kolejność platform: Facebook, Instagram, TikTok, YouTube, X, LinkedIn.

React + TypeScript + Vite + Tailwind, ASP.NET Core 10, EF Core i PostgreSQL. Modularny monolit do testowania **oficjalnych** API społecznościowych.

**Status (potwierdzenie użytkownika 11.11.2026):** rzeczywista publikacja zbiorcza na Facebooku, Instagramie i LinkedInie jednym przyciskiem kreatora zakończyła się sukcesem. Minimum dwóch platform osiągnięte. Typ materiału i data samej próby nie zostały podane; nie oznacza to przetestowania wszystkich formatów. Szczegóły w [TEST_RESULTS](docs/TEST_RESULTS.md). Testy z atrapą HTTP pozostają oddzielną kategorią.

Nowy [dwuetapowy kreator i kopie robocze](docs/COMPOSER.md): post bazowy → niezależne warianty → potwierdzona publikacja zbiorcza. Szkice: 48 h, maksymalnie 10; magazyn: 1 GB na użytkownika. Uruchamianie jednym poleceniem opisuje [LOCAL_LAUNCHER.md](docs/LOCAL_LAUNCHER.md).

## Uruchomienie na Windows

Wymagane: Node.js 24, SDK .NET 10 LTS, PostgreSQL 17 (Docker Compose lub lokalny serwer), `ffprobe` z FFmpeg. SDK, przenośny PostgreSQL i FFmpeg pobrane podczas prac są w ignorowanym `.tools/`; nie są częścią kodu do przeniesienia na inny komputer. Instalacja tych narzędzi nie wymaga kupowania usługi.

W PowerShell, w katalogu repozytorium:

```powershell
# Pierwsza konfiguracja — tworzy losowe hasła, nie wypisuje sekretów.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Initialize-Local.ps1
docker compose up -d

dotnet restore src/Publicate.Api --configfile NuGet.Config
cd src/publicate-web
npm.cmd ci
cd ../..
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Backend.ps1 -MigrateOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Backend.ps1
```

W drugim terminalu:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Frontend.ps1
```

Otwórz **http://localhost:5173** → **Załóż konto**. Podaj imię, e-mail i własne hasło (minimum 12 znaków). Następnie korzystaj z zakładek **Moje konto** i **Połączone konta**. Sesja używa HttpOnly cookie; hasło jest przechowywane jako hash ASP.NET Identity. Nie potrzebujesz lokalnego klucza dostępu.

Jeżeli masz lokalne SDK w `.tools`, skrypty używają go automatycznie. Do ręcznych poleceń:

```powershell
$env:DOTNET_ROOT = Join-Path $PWD '.tools/dotnet'
$env:DOTNET_CLI_HOME = Join-Path $PWD '.tools/cli'
$env:NUGET_PACKAGES = Join-Path $PWD '.tools/packages'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet --version
```

Jeżeli Docker nie jest dostępny: zainstaluj PostgreSQL i ustaw ConnectionStrings:Database, albo użyj przygotowanego lokalnie `.tools/postgresql/pgsql/bin`:

```powershell
# Tylko dla nowej konfiguracji, zamiast standardowej inicjalizacji:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Initialize-Local.ps1 -PortableDatabase
# Baza działa na localhost:55432. Po ponownym uruchomieniu Windows:
& .tools/postgresql/pgsql/bin/pg_ctl.exe -D .data/postgres -l .data/postgres.log -w start
# Zatrzymanie:
& .tools/postgresql/pgsql/bin/pg_ctl.exe -D .data/postgres -m fast -w stop
```

**Na komputerze użytym do realizacji projektu konfiguracja i baza są już utworzone. Nie uruchamiaj Initialize-Local ponownie.** Skrypt odmówi nadpisania istniejących sekretów. Backend: localhost:5080, frontend: localhost:5173, baza przenośna: localhost:55432. Klucze dostawców pozostawiono puste.

## Konta użytkowników

Od 2026-11-05 panel ma rejestrację i logowanie e-mail/hasło, zakładki Moje konto i Połączone konta oraz izolację zasobów użytkowników. AccessKey nie jest używany. Jeśli aktualizujesz wcześniejszy prototyp, zastosuj migrację przez `scripts/Start-Backend.ps1 -MigrateOnly`, a potem uruchom backend ponownie. [Szczegóły](docs/USER_ACCOUNTS.md).

## Konfiguracja i bezpieczeństwo

`appsettings.json` zawiera puste ustawienia. `appsettings.Local.json` jest ignorowany przez Git. Można zamiast niego stosować zmienne środowiskowe (`Providers__LinkedIn__ClientSecret`) lub .NET User Secrets w środowisku Development. Lokalny plik ma pierwszeństwo nad user-secrets; usuń z niego odpowiedni wpis, jeśli korzystasz z user-secrets. Nie podawaj sekretów w poleceniach zapisywanych do historii; edytuj plik lokalnie.

Backend nasłuchuje na loopback. Konta użytkowników mają oddzielne zasoby. To nadal **nie wdrożenie publiczne**: brak odzyskiwania hasła i weryfikacji e-mail. Tokeny są szyfrowane ASP.NET Data Protection; na Windows klucze są dodatkowo chronione DPAPI dla użytkownika procesu. Klucze w `.data/keys` zachowaj między restartami. Zmiana użytkownika Windows lub usunięcie kluczy wymaga ponownego OAuth.

`Media:FfprobePath` ustaw na pełną ścieżkę do ffprobe.exe, jeśli nie jest w PATH. Upload bez dostępnego ffprobe nie jest akceptowany. JPEG/PNG: do 8 MiB na publikację; cały upload maks. 64 MiB. Filmy PoC: MP4/H.264, AAC jeśli jest audio, 3–60 s. Nie następuje automatyczne kadrowanie ani transkodowanie.

Instrukcja kont i callbacków: [SETUP_ACCOUNTS](docs/SETUP_ACCOUNTS.md). Przed testami przeczytaj [API_FEASIBILITY](docs/API_FEASIBILITY.md) i [KNOWN_LIMITATIONS](docs/KNOWN_LIMITATIONS.md).

## Pierwszy test rzeczywisty

1. Skonfiguruj LinkedIn zgodnie z instrukcją, uruchom ponownie backend.
2. Połączone konta → LinkedIn → Połącz konto. Autoryzacja następuje w serwisie LinkedIn.
3. Social API Tester → wybierz cel → wpisz jednoznaczny tekst testowy → Sprawdź walidację.
4. Podgląd i publikacja → przeczytaj treść i cel → zaznacz świadomą zgodę → Publikuj teraz.
5. W Historii sprawdź stan, prawdziwy identyfikator i odpowiedź dostawcy. Zweryfikuj post także bezpośrednio na koncie.
6. Powtórz dla testowej Facebook Page. Dopiero dwa rzeczywiste sukcesy potwierdzają minimalny cel projektu.

Instagram dodatkowo wymaga zgody na publiczny staging HTTPS. TikTok wymaga przyznanego `video.publish`, danych twórcy i wyboru SELF_ONLY. X i AI są domyślnie zablokowane finansowo. Nigdy nie używaj przycisku ponowienia po timeout bez weryfikacji — stan Unknown celowo blokuje nową próbę tego samego wariantu.

## Testy

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Local.ps1
```

Zestaw nie publikuje postów. [TEST_PLAN](docs/TEST_PLAN.md) opisuje testy i osobne wywołanie `Test-Live.ps1`, wymagające wpisania `PUBLIKUJ`. [TEST_RESULTS](docs/TEST_RESULTS.md) rozdziela lokalne wyniki od blokad zewnętrznych.

## Dokumentacja

- [Architektura](docs/ARCHITECTURE.md)
- [Wykonalność API i źródła](docs/API_FEASIBILITY.md)
- [Konfiguracja kont i magazynu](docs/SETUP_ACCOUNTS.md)
- [Plan testów](docs/TEST_PLAN.md), [wyniki](docs/TEST_RESULTS.md)
- [Ograniczenia](docs/KNOWN_LIMITATIONS.md)
- [Droga do komercjalizacji](docs/COMMERCIAL_ROADMAP.md)

Wyniki zrzutów lokalnego panelu znajdują się w `docs/screenshots`. SDK, zależności, baza, multimedia i sekrety są ignorowane przez Git. Dane testowe w bazie są oznaczone „TEST LOKALNY”; nie są publikacjami społecznościowymi.

Instagram podłączasz niezależnie przez Instagram Login (konto Firma/Twórca, bez strony Facebooka). Administrator konfiguruje osobne `Providers:Instagram` według [instrukcji](docs/SETUP_ACCOUNTS.md). `Providers:Meta` służy Facebookowi.
