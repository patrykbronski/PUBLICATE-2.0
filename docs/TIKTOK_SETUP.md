# TikTok — bieżąca konfiguracja, 12.11.2026

**Późniejszy wynik 12.11.2026: PASSED live.** Użytkownik połączył konto, uruchomił zestaw pięciu platform i potwierdził prywatny film na TikToku `patrykb78` screenshotem. Kreator pokazuje SELF_ONLY, dane twórcy i zgodę HTTPS. Publikacja w tej konfiguracji jest potwierdzona end-to-end; wcześniejsze NOT_TESTED dla tego scenariusza zostało rozstrzygnięte. Nie otrzymano osobnego raportu status API; refresh/retry i zdjęcia nadal niepotwierdzone. Patrz TEST_RESULTS.md.

## Aktualny stan po wdrożeniu (zastępuje poniższe historyczne „pozostała implementacja”)

Zaimplementowano film **PULL_FROM_URL**, ścisłą kontrolę zweryfikowanego prefiksu HTTPS, zgodę na publiczne udostępnienie, automatyczny Creator Info w panelu i preflight przed uruchomieniem zestawu. Dostępne są świadomy wybór SELF_ONLY bez wartości domyślnej, interakcje domyślnie wyłączone (z blokadami konta), oznaczenie AI/własnej marki oraz zgody muzyczne/reklamowe. Płatna współpraca niedostępna w SELF_ONLY. Status nieznany to Unknown, dopiero PUBLISH_COMPLETE oznacza Published; prywatny post może nie zwracać ID/linku. FAILED umożliwia świadome ponowienie, bez automatycznego wysyłania.

Użytkownik uzupełnił klucze Sandbox. Lokalnie potwierdzono obecność Client key/secret, PublishingApproved=true i właściwy VerifiedUrlPrefix, bez wypisywania sekretów. To nie potwierdza poprawności kluczy wobec API. Environment=Sandbox jest opisem, nie przełącznikiem adresów API. Flaga PublishingApproved oznacza potwierdzenie konfiguracji Direct Post, nie audyt; udzielone scopes nadal sprawdza backend.

### Następny test użytkownika

1. Zapisz Apply changes w istniejącym Sandboxie, jeżeli nie zostało zapisane. Użyj dodanego Target User i **prywatnego konta TikTok**.
2. Otwórz PUBLICATE na domenie ngrok, zaloguj się i kliknij **Połącz konto** przy TikToku. OAuth żąda user.info.basic i video.publish. Nie wysyłaj kodów/tokenów do rozmowy.
3. Po udanym połączeniu wybierz cel i przygotuj krótki MP4 (maks. 64 MiB produktu, długość wg Creator Info). Ustaw SELF_ONLY, prawdziwe oznaczenia treści i zgody. Dotychczasowy inspektor audio PUBLICATE wymaga AAC. Nie konwertujemy plików.
4. Zatwierdź tymczasowy publiczny HTTPS i publikację. Tunel musi działać przez cały czas pobierania filmu. Sprawdzaj status; Processing nie jest sukcesem. Po PUBLISH_COMPLETE sprawdź prywatny film w TikToku. Brak publicznego ID nie uzasadnia powtórnej publikacji.

Konfiguracja na przyszłość:
```powershell
cd "C:\Dev\Projects\PROTOTYP PUBLICATE"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Configure-TikTok.ps1
```
Klucze muszą pochodzić z tego samego Sandboxa. Skrypt nie wywołuje API. Po zmianie restart backendu pod oryginalnym kontem Windows.

**Testy lokalne:** 241/241 backendu, build frontendu, UI TikToka/kreatora/YouTube PASSED. Atrapy HTTP, zero publikacji. **Live NOT_TESTED:** OAuth, pobranie filmu, publikacja, refresh. Weryfikacja własności prefiksu pozostaje PASSED na podstawie screena użytkownika.

Zdjęcia odłożono do następnego etapu; ograniczenie obecnej wersji PUBLICATE, nie zakaz API. Preflight nie zapewnia atomowej publikacji na wielu platformach. Przetwarzanie/Unknown chroni pliki przed cleanup. Odłączenie usuwa lokalne tokeny; zdalną zgodę można cofnąć w ustawieniach TikToka, zdalnego revoke nie dodano. Produkcyjny audyt nie jest gwarantowany dla narzędzia prywatnego, dokumenty nadal bez odłożonego kontaktu/operatora. Media przez ngrok wymagają rzeczywistego testu mimo sukcesu weryfikacji prefiksu.

Dokumentacja ponownie sprawdzona: [Direct Post](https://developers.tiktok.com/docs/en/content-posting-api-reference-direct-post), [Content Sharing Guidelines](https://developers.tiktok.com/docs/en/content-sharing-guidelines). Test UI: `scripts/test-tiktok-ui.cjs`; reguły i atrapy: `TikTokTests.cs`, `AdapterTests.cs`.

## Historia przygotowania (wcześniejsze blokady częściowo rozwiązane powyżej)

**PASSED — weryfikacja prefiksu 12.11.2026:** użytkownik dostarczył screeny „Your property has been verified” i wpis w Verified properties dla `https://cofounder-morse-reflex.ngrok-free.dev/`. TikTok zaakceptował plik weryfikacyjny. Wcześniejsze niepewności dotyczące tej weryfikacji są rozstrzygnięte; nie oznacza to jeszcze potwierdzenia OAuth, pobrania filmu ani publikacji. Zachować plik w public/. Następny etap: konfiguracja lokalna i dokończenie adaptera przed testem.

Uzupełnienie testu pliku: anonimowy HTTPS GET z User-Agent `curl/8.0`, bez cookies i bez nagłówka ngrok-skip-browser-warning, zwrócił 200 i dokładną treść pliku, bez ostrzeżenia. Wynik zależy więc od klienta; nie jest to jeszcze dowód akceptacji przez weryfikator TikTok.

Plik weryfikacji dostarczony przez użytkownika `tiktokf4gpPnsRYfKCmbjvTbkZhpq3o6pEpTr8.txt` skopiowano bajtowo do `src/publicate-web/public/`, więc jest serwowany w katalogu głównym domeny przez Vite i trafia do builda. Lokalny GET: 200, text/plain, identyczna treść; hash kopii zgodny. Anonimowy GET przez ngrok z domyślnym klientem PowerShell nadal zwrócił ostrzeżenie ERR_NGROK_6024 zamiast pliku. Akceptacja przez TikTok wymaga kliknięcia Verify i potwierdzenia wyniku; nie traktować samego skopiowania pliku jako weryfikacji własności.

Istniejąca aplikacja PUBLICATE / Sandbox: użytkownik pokazał włączony Direct Post i scopes user.info.basic, video.publish, video.upload oraz potwierdził dodanie konta do Target Users. Zapis Apply changes, weryfikacja URL, lokalne klucze, rzeczywisty OAuth i publikacja pozostają niepotwierdzone. Nie tworzyć nowej aplikacji.

## Adresy

| Pole | Wartość |
|---|---|
| Terms of Service URL | https://cofounder-morse-reflex.ngrok-free.dev/terms |
| Privacy Policy URL | https://cofounder-morse-reflex.ngrok-free.dev/privacy |
| Web/Desktop URL | https://cofounder-morse-reflex.ngrok-free.dev/ |
| Login Kit Web Redirect URI | https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/TikTok/callback |
| Prefiks do sprawdzenia przez Verify domains | https://cofounder-morse-reflex.ngrok-free.dev/ |

Strony terms/privacy/about są dostępne bez logowania w aplikacji Vite. Strona główna przed zalogowaniem ma opis produktu i widoczne linki. Nie wymagają sesji API. Po zmianie hosta należy zaktualizować Vite allowedHosts, CallbackBase, publiczny magazyn i wpisy portalu, z uwzględnieniem innych integracji.

Dokumenty opisują rzeczywisty prywatny prototyp. Użytkownik odłożył publikację danych operatora i kontaktu. Brak tych danych jest jawnie zaznaczony, nie zastąpiono ich fikcyjnymi. To nie deklaracja prawnej kompletności ani gotowości do produkcyjnego App Review. Nie otwierać usługi dla innych osób bez uzupełnienia dokumentów i procedur.

## Kolejny krok użytkownika

1. Otwórz adresy przez ngrok w osobnym oknie bez logowania; upewnij się, że tunel działa i nie wyświetla błędu lub strony pośredniej zamiast PUBLICATE.
2. Zapisz konfigurację Sandbox przez Apply changes. Jeśli panel odrzuci dokumenty lub adresy, zachowaj dokładny komunikat; nie obiecujemy akceptacji.
3. Content Posting API → Verify domains → Verify: sprawdź dostępność weryfikacji **URL prefix**. Pokaż ekran instrukcji; jeśli TikTok udostępni plik weryfikacyjny, trzeba wystawić jego dokładną zawartość pod wskazaną ścieżką. Nie wymyślać nazwy/treści pliku ani używać dowolnego proxy do plików. Nie kontrolujemy DNS ngrok-free.dev.
4. Dopiero po weryfikacji prefiksu dokończyć PULL_FROM_URL i potwierdzić rzeczywiste zakresy OAuth. Klucze wpisuje się lokalnie, nie w rozmowie.

## Pozostała implementacja

Obecny adapter TikTok nadal używa FILE_UPLOAD. Przed testem publikacji należy wdrożyć PULL_FROM_URL dla przechowywanych przez backend mediów, zgodę na publiczne udostępnienie, walidację zweryfikowanego prefiksu, ustawienia interakcji/AI/reklamy, aktualne Creator Info przed całym zestawem i dokładniejsze błędy/statusy. Nie uruchamiać jeszcze publikacji starym adapterem.

Priorytet: prywatny film. Zdjęcia także są celem, lecz ich blokada nie zatrzymuje PoC. Jeśli niedostępne, komunikat ma mówić o zakresie obecnej wersji PUBLICATE, nie o fikcyjnym zakazie zdjęć w aplikacjach niekomercyjnych.

## Źródła i weryfikacja

- https://developers.tiktok.com/docs/en/app-review-guidelines — dokumenty, strona publiczna i rozdzielenie Sandbox/produkcji.
- https://developers.tiktok.com/docs/en/content-sharing-guidelines — PULL_FROM_URL dla mediów na serwerze, wymagany UX i audyt.
- https://developers.tiktok.com/docs/en/content-posting-api-media-transfer-guide — weryfikacja domeny/prefiksu i pobieranie HTTPS.

12.11: build frontendu i testy publicznych stron, kreatora oraz YouTube PASSED z przechwyconym API. Żadnej publikacji ani OAuth TikTok nie wykonano. Brak zmian schematu bazy.

Kontrola anonimowego HTTPS przez ngrok 12.11: `/terms` i `/privacy` zwróciły HTTP 200, ale treść była ostrzeżeniem `ERR_NGROK_6024`, nie aplikacją. Nie utożsamiać 200 z dostępnością dokumentów dla TikToka. W przeglądarce po zaakceptowaniu ostrzeżenia strony mogą działać; zachowanie weryfikatora TikTok trzeba sprawdzić. Nagłówek omijający ostrzeżenie w naszym teście nie byłby dowodem, że TikTok też go wysyła. Nie wykupiono planu ani domeny.
