# Diagnostyka Instagram Unified Login — 2026-11-05

## Potwierdzone połączenie

2026-11-05, 17:36 CET: raport `844db7ae859540dd8701c3f8fa3fc1c5`, success=true. Potwierdzono w wiadomości użytkownika i lokalnym logu: POST authorization_code, token_inspection, client_business oraz assigned_accounts zakończyły się HTTP 200. Połączenie zostało zapisane; to pierwszy udany pełny OAuth Instagram Unified Login. Nie opublikowano materiału ani nie potwierdzono jeszcze wszystkich uprawnień publikacyjnych. Wcześniejsze opisy błędów poniżej stanowią historię diagnozy.

## Kolejna rzeczywista próba: opcjonalne id w /me

Raport `4f93c9636e0e4f41a0b6651495c196d3` z 2026-11-05 17:32 CET: wymiana authorization_code POST, inspekcja tokenu i odczyt client_business zwróciły HTTP 200. Backend odrzucił odpowiedź /me, ponieważ bezwarunkowo wymagał pola id. To potwierdzony błąd lokalnego parsera; nie dowodzi jeszcze obecności client_business_id, bo parser zatrzymał się wcześniej.

Poprawka: żądamy fields=client_business_id, a tożsamość konta pochodzi ze sprawdzonego debug_token.user_id. Jeżeli /me dodatkowo zwróci id, nadal sprawdzamy zgodność. Wymagany client_business_id nie jest pomijany ani zgadywany. Trzy nowe przypadki testowe: brak id zaakceptowany, niezgodne id odrzucone, brak client_business_id odrzucony. 117/117 testów backendu i build frontendu przeszły. Pobranie listy przypisanych kont nadal wymaga nowego przejścia OAuth.

## Wynik końcowy diagnostyki danych aplikacji

Po ponownym lokalnym wpisaniu App Secret użytkownika ten sam test **POST client_credentials** zwrócił **HTTP 200** i token aplikacji. Nie wypisano ani nie zapisano tokenu. Zmienił się sekret; endpoint, metoda i wersja pozostały takie same. Potwierdza to przyczynę odtworzonego błędu: poprzedni sekret był nieprawidłowy lub niedopasowany do aplikacji. Nie ma podstaw, aby przypisywać ten błąd metodzie POST. GET z sekretem nie wykonano zgodnie z decyzją użytkownika. Wymiana authorization_code nadal wymaga nowej sesji użytkownika; sukces client_credentials nie jest jeszcze sukcesem połączenia konta IG.

114 testów automatycznych przeszło, frontend został zbudowany. Obejmują one bezpieczne logi błędu, rozróżnienie etapu wymiany/inspekcji, callback sukces/błąd, brak sesji/state i izolację raportów między użytkownikami. Migracja InstagramClientBusiness zastosowana. Facebook i Direct Instagram zachowują swoje ścieżki; testy regresji przeszły.

## Fakty przed poprawką

Użytkownik potwierdził logowanie i akceptację dostępu, następnie callback pokazał surowy JSON z `diagnostic.httpStatus=400`, `providerCode=1`. Backend mapował ProviderException na HTTP 502; 400 w polu diagnostycznym oznacza odpowiedź Meta. Nie zapisano etapu historycznej próby, więc nie można dowieść, które wywołanie tej próby zawiodło. Błąd dostawcy wskazuje, że lokalna walidacja state, właściciela i konfiguracji została wykonana.

Sprawdzono publiczne identyfikatory: Client ID 2082875755677881, Configuration ID 3295601310634577, callback `https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/Instagram/callback`. Są zgodne z linkiem użytkownika. Sekret obecny, bez otaczających spacji; jego wartości nie wyświetlano. Dialog używa config_id, prompt=login, response_type=code, override_default_response_type=true; nie dodaje scope. State losowy, jednorazowy, z terminem ważności i powiązaniem z sesją oraz przeglądarką.

## Dokumentacja potwierdzona

Nowy oficjalny adres [Get started](https://developers.facebook.com/documentation/instagram-platform/instagram-api-with-unified-login/get-started) został pobrany HTTP 200 bezpośrednio z Meta przez PowerShell. Wyszukiwarka nadal zwracała 429. Potwierdzono:

- wersja przykładowa/latest w dokumencie: v27.0;
- wymiana kodu: GET graph.facebook.com/v27.0/oauth/access_token, Meta App ID i App Secret;
- `/me` reprezentuje business integration system user, nie konto IG;
- odczyt i zapis `client_business_id` z `/me`;
- konta udostępnione przez `/me/assigned_instagram_accounts?fields=id,username`, wymagany instagram_basic;
- jeden token może obsługiwać kilka kont Instagram;
- [Authentication](https://developers.facebook.com/documentation/instagram-platform/instagram-api-with-unified-login/authentication) potwierdza ważność zależną od konfiguracji i osobne API zarządzania tokenami biznesowymi. Nie stosować ig_refresh_token.

To potwierdza błąd wcześniejszego discovery opartego na granular_scopes oraz niezgodność metody POST z przykładem wymiany kodu GET. Nie dowodzi jeszcze, że metoda HTTP była jedyną przyczyną historycznej próby.

## Rzeczywisty test diagnostyczny

Osobny POST client_credentials na tym samym endpointcie, z lokalnymi danymi aplikacji, zwrócił HTTP 400, OAuthException, code 1. Nie użyto kodu OAuth użytkownika, nie utworzono publikacji, nie ujawniono sekretów. To odtwarza symptom przy wymianie tokenu, ale nie jest testem pełnego authorization_code.

Automatyczna kontrola bezpieczeństwa odrzuciła przygotowanie porównawczego GET z sekretem w URL. Użytkownik odmówił testu GET; nie został wykonany i wymiana kodu pozostaje POST. Nie traktujemy przykładu GET w dokumentacji jako dowodu, że POST spowodował historyczny błąd.

**Ustalona przyczyna odtworzonego błędu:** po bezpiecznej redakcji komunikatu z oddzielnego POST Meta zwróciła `Error validating client secret.`, HTTP 400, code 1, OAuthException, wersja odpowiedzi v27.0. To rzeczywiste odrzucenie danych aplikacji na etapie token endpoint, nie błąd state/discovery. Historyczna próba użytkownika nie miała znacznika etapu, ale bieżący test jednoznacznie pokazuje błędny lub niedopasowany App Secret. Poproszono o ponowne lokalne wpisanie sekretu dla App ID 2082875755677881, bez ujawniania w rozmowie. Skorygowane dane wymagają ponownego testu.

## Diagnostyka i interfejs

Callback wraca do PUBLICATE z losowym ID raportu w fragmencie URL. Nie przekazuje kodu, tokenu ani surowego błędu w adresie. Brak sesji wraca do logowania, bez wymiany tokenu. Odmowa dostawcy zużywa state i nie uruchamia żądań API. Raport dostępny tylko zalogowanemu właścicielowi, przez 20 minut.

Lokalny plik `.data/oauth-diagnostics.jsonl` zapisuje etapy, status HTTP, code, error_subcode, type, fbtrace_id, is_transient i skrót wiadomości. Wolny tekst odpowiedzi może zawierać sekrety, więc zachowywane są tylko dokładnie rozpoznane ogólne komunikaty. Nie zapisuje się request URL, query, nagłówków, body ani odpowiedzi tokenowej. Plik rotowany po 1 MiB, jedna poprzednia kopia.

Znaczniki etapów: `instagram.unified.code_exchange`, `token_inspection`, `client_business`, `assigned_accounts` (wszystkie z prefiksem instagram.unified). Nie ma automatycznego ponawiania kodu OAuth. Nowa próba wymaga przycisku Połącz przez Instagram i nowego state/code.

Discovery poprawiono na oficjalne `/me/assigned_instagram_accounts` z paginacją po kursorach. Nie podążamy za pełnym URL `paging.next`. Pusta lista oznacza RequiresApproval, bez zgadywania identyfikatorów. `client_business_id` jest zapisywany dzięki nullable kolumnie dodanej migracją InstagramClientBusiness. Dotychczasowy Direct Instagram i Facebook nie korzystają z nowego discovery.
