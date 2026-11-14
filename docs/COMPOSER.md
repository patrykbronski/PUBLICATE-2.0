# Dwuetapowy kreator PUBLICATE

Wdrożenie: 2026-11-10. Nie obejmuje abonamentów ani 7-dniowej blokady podmiany kont.

## Przepływ

1. W „Połączonych kontach” ustaw cel publikacji dla każdego używanego serwisu. Jeśli serwis ma kilka dostępnych celów, wybór jest jawny. Facebook zawsze wymaga wyboru strony.
2. W „Kreatorze postów” przygotuj szablon, opcjonalne CTA i materiały. Zaznacz platformy. Brak dostępnego celu blokuje checkbox; błędy treści nie blokują przejścia dalej.
3. Krok 2 zawiera tylko wybrane platformy. Opisy, CTA, media i kolejność są niezależnymi kopiami danych; pliki są współdzielone przez identyfikatory. Publikacja odczytuje wariant, nie bieżący post bazowy.
4. Powrót do kroku 1 wymaga potwierdzenia resetu wariantów. Treść bazowa i wybór platform pozostają. Obsługiwany jest też przycisk Wstecz w przeglądarce w ramach przejścia między etapami. Przy zamknięciu/odświeżeniu przeglądarka może pokazać własne ostrzeżenie o niezapisanych danych; nie gwarantujemy przechwycenia zamknięcia procesu przeglądarki lub systemu.
5. Przycisk zbiorczej publikacji jest dostępny dopiero po sprawdzeniu wszystkich niewysłanych wariantów. Końcowe okno pokazuje cele i wymaga świadomego potwierdzenia. Instagram wymaga dodatkowo zgody na publiczny HTTPS dla mediów.
6. Backend ponownie sprawdza stan szkicu, rewizję, bieżące cele i poprawność wszystkich niewysłanych wariantów. Wywołania adapterów odbywają się równolegle, z osobnymi kontekstami EF. Każdy wynik zapisuje się osobno.

Nie jest to transakcja obejmująca dostawców. Udane posty nie są ponawiane. Processing i Unknown blokują ponowną wysyłkę. Po potwierdzonym błędzie dopuszczającym retry można poprawić wariant i ponowić tylko nieudaną część. Nie ma automatycznego ponawiania.

## Kopie robocze i pliki

- Automatyczny zapis po około 900 ms bezczynności i przycisk ręcznego zapisu. Zapisy są kolejkowane, a rewizja chroni przed nadpisaniem zmian z drugiej karty.
- Nowy pusty kreator nie tworzy szkicu. Limit: 10 aktywnych szkiców na użytkownika; jedenasty archiwizuje najstarszy i pokazuje informację.
- Termin wygaśnięcia: 48 godzin od utworzenia; edycja go nie przedłuża.
- „Kopie robocze” pokazują listę, termin, kontynuowanie i usuwanie.
- Po pełnym potwierdzonym sukcesie szkic znika z listy. Rekordy potrzebne historii i kontroli idempotencji pozostają w bazie; „usunięcie szkicu” jest archiwizacją, nie kasowaniem historii.
- Po częściowym sukcesie szkic jest dostępny do swojego terminu. Wysłane warianty są zablokowane do edycji/resetu. Całego zestawu z próbami publikacji nie można zresetować do nowej wysyłki.
- Limit fizycznego magazynu użytkownika: domyślnie 1 GiB (1 073 741 824 bajty, etykieta produktu „1 GB”), `Storage:UserQuotaBytes`. Pojedynczy upload pozostaje ograniczony do 64 MiB. Odrzucone/incomplete uploady zarejestrowane w bazie również rezerwują miejsce do sprzątnięcia.
- Worker sprząta co godzinę. Wygasły szkic staje się niedostępny dokładnie po upływie 48 h, ale fizyczne usunięcie pliku następuje podczas kolejnego cyklu. Nie usuwa się plików współdzielonych przez aktywne szkice, materiałów trwających/niepewnych publikacji ani plików przed upływem retencji zakończonych zadań (minimum 48 h).
- Materiały osierocone po uploadzie usuwane są po 48 h, jeśli nie chroni ich żaden szkic lub zadanie. Brak globalnego limitu liczby materiałów w szablonie; obowiązują quota, limit pliku i limity konkretnego wariantu.

## Zmiana celu

Otwarcie szkicu i przygotowanie publikacji sprawdzają aktualny cel platformy. Niewysłane warianty są aktualizowane z komunikatem „stare → nowe”, zachowując treść i materiały. Zmiana podczas końcowego potwierdzenia przerywa wysyłkę i wymaga ponownego przejrzenia podsumowania. Opublikowane, przetwarzane i niepewne warianty zachowują pierwotny cel. Brak dostępnego celu blokuje publikację.

## Reguły i dostęp do API

`PublicationRules`, `Capabilities` i `PostValidator` opisują zakres aktualnych adapterów. Frontend pobiera reguły z backendu. Automatyczna walidacja po krótkiej przerwie w edycji odpytuje wyłącznie PUBLICATE, nie zewnętrzne API. Licznik tekstu obejmuje CTA i dwie nowe linie stosowane przez adapter. X pozostaje ograniczony do ASCII; nie udajemy obsługi pełnego ważonego Unicode.

Sprawdzenie dokumentacji 2026-11-10:

- [LinkedIn MultiImage](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/multiimage-post-api?view=li-lms-2026-10): 2–20 zdjęć w API; zachowano dotychczasowe ograniczenie adaptera do 10, bez rozszerzania integracji w tym zadaniu.
- [TikTok Direct Post — video](https://developers.tiktok.com/docs/en/content-posting-api-reference-direct-post): opis do 2200 jednostek UTF-16, wymagane dane twórcy i wybór spośród jego opcji widoczności. Kreator pozwala pobrać te dane; obecny adapter dopuszcza tylko SELF_ONLY. Obsługa zdjęć nie została dodana.
- [X — tworzenie posta](https://docs.x.com/x-api/posts/create-post): potwierdzony endpoint; zakres adaptera nadal tylko tekst i jawna konfiguracja zgody na koszt.
- [Meta content publishing](https://developers.facebook.com/documentation/instagram-platform/content-publishing): ponowny odczyt dokumentacji zablokowany HTTP 429. Nie przedstawiamy wartości z makiet jako potwierdzonych maksimów Meta. Utrzymano konserwatywne limity adapterów.

## Aktualizacja i testy

Migracja `ComposerDrafts` dodaje stan kreatora i rewizję do Posts oraz preferencje celów do Users. Starsze niezakończone szkice otrzymują 48 godzin od migracji; zakończone posty pozostają w historii. Jeśli wcześniej istniało ponad 10 szkiców, aktywnych pozostaje 10 najnowszych. Starsze warianty są odczytywane z istniejących rekordów; przy kilku celach tej samej platformy kreator pokazuje jeden wariant, pozostałych rekordów nie usuwa.

Aktualizacja z kopią bazy (nie uruchamia ani nie zatrzymuje usług):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Update-Composer.ps1
```

Następnie restart istniejącego backendu i odświeżenie przeglądarki. Kopia bazy jest lokalnie w ignorowanym `.data/backups`; zawiera dane prywatne i nie powinna być publicznie udostępniana.

Testy backendu, bez rzeczywistych publikacji:

```powershell
$env:DOTNET_ROOT = Join-Path $PWD '.tools/dotnet'
$env:DOTNET_CLI_HOME = Join-Path $PWD '.tools/cli'
$env:NUGET_PACKAGES = Join-Path $PWD '.tools/packages'
$env:Logging__EventLog__LogLevel__Default = 'None'
& .tools/dotnet/dotnet.exe test tests/Publicate.Tests/Publicate.Tests.csproj -c Composer --no-restore
```

Test przeglądarkowy z atrapami wszystkich żądań `/api`, przy uruchomionym frontendzie 5173 i przygotowanym lokalnym Playwright:

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $PWD '.tools/browsers'
node scripts/test-composer-ui.cjs
```

Wynik: 145 testów backendu; kompilacja TypeScript/Vite; scenariusz przeglądarkowy (niezależność, reset, Wstecz, szkice, zmiana celu, telefon); transakcyjny test PostgreSQL zakończony ROLLBACK. Testy nie dowodzą rzeczywistej publikacji zbiorczej — taka próba wymaga osobnego świadomego potwierdzenia użytkownika. Pozostaje ostrzeżenie NU1900 o niedostępnej aktualnej bazie podatności NuGet.
