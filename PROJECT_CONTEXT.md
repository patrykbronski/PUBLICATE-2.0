# PUBLICATE — kontekst dla kolejnego Codexa

Stan: **14.11.2026**. Ten folder to **PUBLICATE 2.0 — wersja rozwojowa**. Repozytorium: `patrykbronski/PUBLICATE-2.0`.
Osobna wersja inżynierska: `C:\Dev\Projects\PROTOTYP PUBLICATE`, repo `patrykbronski/INZYNIERKA-PUBLICATE`.
Zakres tej wersji: dalszy rozwój aplikacji na polecenie użytkownika; nie modyfikuj przy okazji inżynierki. Backlog nie jest zgodą na wdrożenie. Podział nie usuwa AI/R2/testera ani innych istniejących modułów; czyszczenie będzie osobnym zadaniem.

## Lektura i wiarygodność

Najpierw `AGENTS.md`, ten plik, `docs/WORKSPACES.md`, potem dokument właściwego modułu.
Poprzedni kontekst zachowano w `docs/archive/PROJECT_CONTEXT_BEFORE_SPLIT_2026-11-14.md`. Jego historyczne NOT_TESTED nie zastępują późniejszych dowodów. Wyniki chronologiczne: `docs/TEST_RESULTS.md`. Nie udawaj testów live na podstawie kompilacji/atrap.

## Mapa kodu

Modularny monolit: ASP.NET Core 10 / EF Core / Npgsql / PostgreSQL 17 oraz React 19 / TypeScript / Vite / Tailwind 4. HttpClientFactory, ffprobe, ASP.NET Data Protection z Windows DPAPI.

| Plik/moduł | Odpowiedzialność |
|---|---|
| `Program.cs`, `UserAccounts.cs` | REST, DI, cookie, rejestracja/logowanie, rate limiting |
| `Domain.cs`, `Database.cs`, `Migrations/` | modele, capabilities, właściciele, indeksy/migracje |
| `OAuthService.cs`, `InstagramLogin.cs`, `InstagramUnified.cs` | OAuth, discovery, refresh/odłączenie |
| `ProviderHttp.cs`, `OAuthDiagnostics.cs` | transport i bezpieczna diagnostyka |
| `Publishers.cs`, `XPublisher.cs`, `YouTube.cs`, `YouTubeUpload.cs` | sześć adapterów, upload, publikacja/status |
| `PublicationService.cs`, `Drafts.cs` | zadania, snapshoty, idempotencja, zestawy, szkice |
| `PublicationRules.cs`, `VideoCodecRules.cs`, `TikTokRules.cs`, `XText.cs` | walidacja, Creator Info, twitter-text |
| `MediaStorage.cs`, `AiDescriptions.cs` | Local/R2, inspekcja/cleanup; AI zdjęć |
| `src/publicate-web/src/main.tsx` | powłoka, konta, tester, historia |
| `Composer.tsx`, `TikTokFields.tsx`, `YouTubeFields.tsx` | kreator dwuetapowy i pola platform |
| `platforms.ts`, `xText.ts` | kolejność platform i licznik X |
| `instance.json`, `scripts/Instance.ps1` | nazwa, porty, domena i blokada drugiej wersji |

Pliki C# znajdują się w `src/Publicate.Api/`. Osiem encji: Users, ConnectedAccounts, PublishingTargets, Posts, MediaAssets, PostVariants, PublicationJobs, PublicationAttempts. OwnerId pochodzi z sesji. Wiele kont/platformę jest dozwolone; kreator używa wybranego celu domyślnego. Media współdzielone przez ID. Najnowsza migracja: `20261110104052_ComposerDrafts`.

## Kod a dowody live

Kolejność UI: **Facebook, Instagram, TikTok, YouTube, X, LinkedIn**. Numery enum są inne, utrwalone w bazie — nie zmieniaj.

| Platforma | Zaimplementowane | Dowód rzeczywisty |
|---|---|---|
| Facebook | Pages: tekst, zdjęcia, wiele zdjęć, film | publikacja zbiorcza FB/IG/LI potwierdzona przez użytkownika; 12.11 screenshot filmu na Bronski |
| Instagram | niezależny Direct/Unified Login, JPEG, karuzela zdjęć, Reel | OAuth/zdjęcie 05.11; 12.11 film na bronski.studio po poprawce polling kontenera |
| TikTok | OAuth, Creator Info, Direct Post wideo PULL_FROM_URL, SELF_ONLY/status | 12.11 film prywatny patrykb78; prefiks zweryfikowany. Zdjęcia niezaimplementowane |
| YouTube Shorts | OAuth/PKCE/refresh, kanał, resumable upload, status/widoczność | 12.11 film 44 s niepubliczny Patryk B; 13.11 użytkownik zgłosił dzienny limit kanału |
| X | OAuth/PKCE, twitter-text, 1–4 JPEG/PNG lub MP4, upload v2/status | 13.11 relacja użytkownika i screenshot Published ID 2121146796151865703. Format nieudokumentowany |
| LinkedIn | profil osobisty: Posts/Images/MultiImage/Videos | OAuth 10.11; 12.11 film Patryk B. Organizacje nieobjęte tym testem |

12.11 użytkownik pokazał film na FB/IG/TikTok/YouTube/LinkedIn z jednego zatwierdzenia. To dowód użytkownika, nie nowy odczyt API przez agenta. X potwierdzony później. Wszystkie formaty, limity, refresh/retry nie są automatycznie PASSED. R2/AI mają adaptery, bez dowodu live; AI analizuje zdjęcia, nie wideo/transkrypcję.

## Decyzje i niezmienniki

- E-mail/hasło, HttpOnly cookie; brak weryfikacji e-mail/resetu hasła.
- Krok 1 jest szablonem. Krok 2 ma niezależne warianty wybranych podłączonych platform; publikowane są warianty.
- Powrót do bazy ostrzega i resetuje krok 2; warianty z próbami chronione.
- Walidacja wszystkich pozostałych wariantów przed zestawem; wyniki zewnętrzne nie są atomowe. Nie wysyłamy ponownie sukcesów.
- RequestKey, snapshoty i indeksy chronią przed duplikacją. Processing/Unknown nie oznacza Published. Status check nie publikuje. Brak automatycznego ryzykownego retry.
- Jeden backend na bazę; OperationGate procesowy, brak trwałej kolejki workerów/wznowienia uploadów po restarcie.
- Szkice: 48 h od utworzenia, maks. 10/użytkownika, kolejny usuwa najstarszy. Zakończone archiwizowane; historia pozostaje.
- Plik 64 MiB, magazyn 1 GiB/użytkownika. Retencja chroni współdzielenie i aktywne/niepewne operacje.
- Bez automatycznej konwersji/kadrowania. PNG→JPEG i proporcje są pomysłami `?`, niezatwierdzonymi.
- Shorts pion/kwadrat do 179,99 s; tytuł 100 znaków, opis 5000 bajtów UTF-8. Brak Community.
- Limity produktu bywają węższe od API: `API_LIMITS.md`, `VIDEO_CODECS.md`, `X_SETUP.md`.
- X: „Szczegóły” czyta lokalną bazę; „Sprawdź status” odpytuje dostawcę i może kosztować. Cache/budżety odłożone do backlogu, nie wdrożone.

## Środowisko

Inżynierka: 5080 backend, 5173 Vite, 55432 PostgreSQL; istniejąca baza i klucze zachowane.
2.0: 5081/5174/55433; nowa baza, media, klucze i sesje. Client ID/Secret lokalnie skopiowane; tokeny kont nie.
Naprzemiennie ta sama domena `https://cofounder-morse-reflex.ngrok-free.dev`, ngrok 4040. Nie przełączaj podczas OAuth/pobierania mediów/publikacji.
Start/stop z właściwego folderu: `scripts/Start-Publicate.ps1`, `scripts/Stop-Publicate.ps1`; szczegóły `WORKSPACES.md`.
Uruchamiaj jako właściciel Windows, nie codexsandbox. Nie resetuj bazy/kluczy DPAPI.
`.data`, `.tools`, `appsettings.Local.json` ignorowane przez Git; nie są częścią klonu repo. Konfiguracja lokalna i env nadpisują domyślną.

## Testy i następne kroki

Przed podziałem: 256/256 backend (atrapy), build i UI kreatora/YouTube/TikTok/X; 13.11 UI historii PASSED. Wyniki podziału dopisywane w TEST_RESULTS po wykonaniu.
Zwykłe testy nie publikują. Skrypty live/database czytaj przed użyciem. Backlog `DO_WDROZENIA_W_PRZYSZLOSCI.md`: m.in. zmiana celu raz/7 dni, kontrola kosztów X, konwersja `?`. Nie implementuj przy okazji.

**Weryfikacja podziału 14.11:** oba środowiska uruchomiono kolejno, health/baza/tunel poprawne, blokada równoczesnego startu działa. 256 testów backendu, build obu frontendów, UI kreatora/historii obu wersji PASSED z atrapami. Nowa baza 7 migracji i brak danych użytkownika. Bez nowych publikacji/OAuth. Szczegóły TEST_RESULTS.
