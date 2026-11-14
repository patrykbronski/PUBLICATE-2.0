# YouTube Shorts w PUBLICATE

Stan 11.11.2026: OAuth, odświeżanie, kanał, upload z odzyskiwaniem po przerwaniu i statusy są zaimplementowane. Projekt Google `publicate-514411` i klient Web `PUBLICATE Web` istnieją według użytkownika. Lokalna konfiguracja Client ID/Secret była pusta podczas przeglądu. Rzeczywiste OAuth i publikacja: **BLOCKED — oczekują na konfigurację lokalną i test użytkownika**, nie PASSED. Testy z atrapami: 200/200 całego backendu, build i dwa testy UI przeszły.

## Konfiguracja administratora

1. Otwórz [Google Cloud Console](https://console.cloud.google.com/). Wybierz istniejący projekt PUBLICATE (`publicate-514411`); nie twórz kolejnego.
2. **APIs & Services → Library → YouTube Data API v3 → Enable**.
3. W **Google Auth Platform → Branding** ustaw nazwę PUBLICATE oraz wymagane adresy kontaktowe. W **Audience** wybierz External; na początek Testing. Dodaj swój adres Google do Test users. Nie przesyłaj haseł ani sekretów do rozmowy.
4. W **Data Access → Add or remove scopes** dodaj:
   - `https://www.googleapis.com/auth/youtube.upload`
   - `https://www.googleapis.com/auth/youtube.readonly`
   Drugie uprawnienie służy rozpoznaniu kanału oraz odczytowi statusu prywatnego filmu. Zgody muszą być rzeczywiście udzielone.
5. **Google Auth Platform → Clients → PUBLICATE Web** (istniejący klient Web application). W **Authorized redirect URIs** dodaj dokładnie:
   `https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/YouTube/callback`
   Ten adres musi odpowiadać `OAuth:CallbackBase` backendu. Dla testu całkowicie lokalnego, jeżeli backend ma odpowiednią konfigurację, będzie to `http://localhost:5080/api/oauth/YouTube/callback`. Nie zmieniaj wspólnego CallbackBase tylko dla Google — korzystają z niego też pozostałe integracje. Ten przepływ backendowy nie wymaga dodania JavaScript origin.
6. Zapisz konfigurację klienta. Lokalnie, w katalogu PUBLICATE, uruchom:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Configure-YouTube.ps1
```

Wpisz Client ID i ukryty Client Secret. Skrypt uzupełnia tylko sekcję YouTube w ignorowanym `appsettings.Local.json`; wypisuje callback, nie sekret. Zrestartuj backend. Alternatywnie można ustawić zmienne `Providers__YouTube__ClientId` i `Providers__YouTube__ClientSecret` w jego środowisku; zmienne środowiskowe mają pierwszeństwo przed plikiem.

## Połączenie i pierwszy test

1. Zaloguj się do PUBLICATE przez ten sam adres, z którego rozpoczynasz OAuth. W „Połączonych kontach” wybierz **YouTube Shorts → Połącz konto**.
2. W Google wybierz właściwe konto/kanał, udziel obu zgód. Musisz mieć utworzony kanał YouTube. PUBLICATE wymaga jednoznacznego kanału zwróconego przez `channels.list?mine=true`; nie wybiera automatycznie pierwszego z wielu. Inny kanał można połączyć przez kolejną autoryzację.
3. Po powrocie sprawdź nazwę kanału i cel publikacji. Odświeżenie tokenu i odłączenie są dostępne jak w pozostałych integracjach. Odłączenie jest lokalne; zdalną zgodę możesz cofnąć w ustawieniach połączeń konta Google.
4. Dodaj własny krótki film MP4/H.264/AAC, pionowy lub kwadratowy, do **179,99 s** i **64 MiB**. Wybierz YouTube w pierwszym kroku kreatora.
5. W drugim kroku wpisz tytuł, sprawdź opis, wybierz widoczność i świadomie oznacz, czy materiał jest dla dzieci oraz czy zawiera realistyczne treści syntetyczne/zmodyfikowane. Domyślna widoczność: **prywatny**, jawnie wysyłana do API i widoczna przed zatwierdzeniem. Obie deklaracje nie mają automatycznie zaznaczonej odpowiedzi; starsze szkice wymagają uzupełnienia nowego pola.
6. Dopiero po sprawdzeniu kanału i treści zatwierdź publikację. Po uploadzie wynik jest `Processing`. Kliknij **Sprawdź status** po przetworzeniu. `Published` oznacza przetworzenie i potwierdzenie wybranej widoczności; prywatny film nadal jest prywatny.
7. Sprawdź film w YouTube Studio. Samo ID uploadu nie dowodzi ukończenia przetwarzania ani klasyfikacji Shorts. Klasyfikację wykonuje YouTube.

## Zachowanie i ograniczenia

- Nazwa wewnętrzna: YouTube, nazwa w interfejsie: YouTube Shorts. Obsługiwany wyłącznie jeden film. Posty społecznościowe, zdjęcia, długie filmy, harmonogram, miniatury i tagi nie są wdrożone.
- Tytuł 1–100 znaków, opis z CTA do **5000 bajtów UTF-8**, bez `<` i `>`. Polski znak może zajmować więcej niż jeden bajt. Limit 179,99 s jest decyzją PUBLICATE; YouTube dopuszcza pełne 3 minuty.
- Upload strumieniuje plik w ramach limitu 64 MiB. Po przerwaniu/wybranych błędach 5xx odpytuje tę samą sesję pustym PUT i wznawia od bajtu potwierdzonego przez Google (`308`/`Range`), z rosnącym opóźnieniem i maksymalnie sześcioma żądaniami upload/status w sesji. Respektuje Retry-After do 30 s; dłuższe oczekiwanie lub niepewność kończy operację bez nowej sesji. Nie ma trwałego wznawiania po restarcie: URL sesji zawiera dane dostępu i nie jest zapisywany w historii ani logach. Po wyczerpaniu prób wynik jest `Unknown`. Potwierdzone ID zostaje zapisane przed kontrolą przetwarzania. UI pokazuje etapy/wynik, bez procentowego postępu transferu.
- Pole `containsSyntheticMedia` trafia do `status.containsSyntheticMedia`; nie jest wywnioskowane automatycznie z pliku. `invalid_grant` kieruje do ponownego połączenia, `invalid_client` do konfiguracji, a `quotaExceeded`/`uploadLimitExceeded` wskazują limity zamiast błędu uprawnień.
- Sprawdzamy kanał ponownie przed uploadem; rozbieżność blokuje wysyłkę. Brak ID lub niezgodność widoczności nie daje `Published`.
- OAuth używa jednorazowego state powiązanego z użytkownikiem i przeglądarką, PKCE, kodu wymienianego przez POST oraz szyfrowania tokenów na backendzie. Token refresh zachowujemy, jeśli Google nie zwróci nowego przy ponownej zgodzie.
- W trybie Google Testing token odświeżania dla tych uprawnień wygasa po 7 dniach. Dostęp publicznych klientów i weryfikacja OAuth pozostają osobnym etapem.
- Domyślnie 100 wywołań `videos.insert` dziennie na projekt; dodatkowe ograniczenia mogą dotyczyć kanału. Nie wykonano zakupów ani rzeczywistych uploadów.
- Kolejność we wszystkich wyborach platform: Facebook, Instagram, TikTok, YouTube, X, LinkedIn. Dotychczasowe numery platform w bazie pozostają bez zmian; migracja bazy nie jest potrzebna.

## Źródła oficjalne

- [OAuth dla aplikacji webowych](https://developers.google.com/identity/protocols/oauth2/web-server)
- [Channels list](https://developers.google.com/youtube/data/reference/rest/v3/channels/list)
- [Videos insert](https://developers.google.com/youtube/data/reference/rest/v3/videos/insert)
- [Metadane i status filmu](https://developers.google.com/youtube/data/reference/rest/v3/videos)
- [Resumable uploads](https://developers.google.com/youtube/data/guides/using_resumable_upload_protocol)
- [Shorts](https://support.google.com/youtube/answer/15424877?hl=en)
- [Limity projektu](https://developers.google.com/youtube/data/guides/quota_and_compliance_audits)
- [Zmiana 2.11.2026: zniesienie wymuszonej prywatności dla niezweryfikowanych projektów](https://developers.google.com/youtube/data/revision_history)

Testy lokalne: `YouTubeTests.cs`, `YouTubeRecoveryTests.cs`, `BatchTests.cs` (także cztery platformy i częściowe powodzenie), `scripts/test-youtube-ui.cjs`. Publikacja na rzeczywistym kanale zawsze wymaga osobnego, świadomego zatwierdzenia w interfejsie. Nie wykonano realnych wywołań Google podczas wdrożenia.
