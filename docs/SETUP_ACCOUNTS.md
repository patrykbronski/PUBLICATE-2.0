# Konfiguracja kont, OAuth, magazynu i AI

> Aktualizacja 2026-11-05: działa rejestracja i logowanie e-mail/hasło, bez AccessKey. Zasoby należą do zalogowanego użytkownika, OAuth state jest związany z jego sesją. Pełny opis: [USER_ACCOUNTS](USER_ACCOUNTS.md).

Stan instrukcji: 2026-11-04. Nazwy sekcji portali mogą różnić się zależnie od typu konta, kraju i dostępu produktu. Nie mamy dostępu do Twoich dashboardów. W szczególności kreator Meta i przydział produktów trzeba potwierdzić w Twoim panelu; nie obiecujemy przyznania scope.

Sekrety wpisuj wyłącznie do `src/Publicate.Api/appsettings.Local.json` (ignorowany przez Git), user-secrets albo środowiska backendu. **Nie wysyłaj ich do rozmowy.** Po zmianie konfiguracji uruchom backend ponownie. Hasło konta PUBLICATE jest niezależne od haseł i tokenów serwisów społecznościowych. Integracje z tego dokumentu konfiguruje właściciel aplikacji; użytkownicy tylko udzielają zgody OAuth.

## Wartości wspólne

| Ustawienie | Lokalna wartość |
|---|---|
| Local:Origin | `http://localhost:5173` |
| OAuth:CallbackBase | `http://localhost:5080` |
| Callback LinkedIn | `http://localhost:5080/api/oauth/LinkedIn/callback` |
| Callback Facebook | `http://localhost:5080/api/oauth/Facebook/callback` |
| Callback Instagram Login | `http://localhost:5080/api/oauth/Instagram/callback` |
| Callback TikTok | `https://TWOJ_HOST/api/oauth/TikTok/callback` — wymaga rzeczywistego HTTPS zgodnego z portalem |
| Callback X | dokładny URI zaakceptowany w ustawieniach X, ścieżka `/api/oauth/X/callback` |

Wielkość liter, port, protokół i końcowy slash muszą pasować. PUBLICATE nie przyjmuje callback URL z frontendu ani z parametru użytkownika. Klucze `ClientId` i `ClientSecret` w konfiguracji są nazwami ogólnymi; dla TikToka `ClientId` to **Client key**.

## LinkedIn — pierwszy dowód

1. Otwórz [LinkedIn Developers / My Apps](https://www.linkedin.com/developers/apps). Create app. Podaj nazwę PUBLICATE, stronę LinkedIn powiązaną z aplikacją, logo, kontakt i wymagane adresy. Jeśli portal wymaga potwierdzenia przez administratora strony, wykonaj je; samo konto osobiste nie zastępuje wymagań rejestracji aplikacji.
2. Aplikacja → **Products** → wystąp o **Share on LinkedIn** oraz **Sign In with LinkedIn using OpenID Connect**. Poczekaj na status udostępniający scopes.
3. **Auth** → Authorized redirect URLs → dodaj callback LinkedIn z tabeli (lub identyczny HTTPS). Skopiuj Client ID i Client Secret lokalnie do `Providers:LinkedIn:ClientId` i `ClientSecret`.
4. Pozostaw `Providers:LinkedIn:Version=202607`, wersję wybraną na podstawie odczytanej dokumentacji. Przed dłuższym wykorzystaniem sprawdź [wersjonowanie](https://learn.microsoft.com/en-us/linkedin/marketing/versioning); nie używaj wersji po sunset.
5. Auth/OAuth scopes musi pokazywać `w_member_social`, `openid`, `profile`. Email nie jest potrzebny. Konto osobiste będzie celem `urn:li:person:{sub}` z userinfo; nie wpisuj ręcznie własnego URN.
6. Połączone konta w PUBLICATE → LinkedIn → Połącz konto. Po powrocie sprawdź nazwę konta, scopes, listę celów. Backend dodatkowo używa [introspekcji tokenu](https://learn.microsoft.com/en-us/linkedin/shared/authentication/token-introspection).
7. Najpierw tekst, potem JPEG, dwa zdjęcia, krótki MP4. Dla każdego osobny nowy post testowy. Film dłużej przetwarzany może wymagać sprawdzenia statusu i nowej ręcznej próby, jeśli post nie został jeszcze utworzony.

`r_member_social` nie jest standardowym warunkiem publikowania i może nie być dostępne. Nie żądaj go tylko po to, aby potwierdzić HTTP 201 tworzenia posta. Organizacje i `w_organization_social` są oddzielną zdolnością poza bieżącym discovery profilu; wymagają odpowiedniego produktu i uprawnień administratora. Nie udajemy, że Share on LinkedIn daje dostęp do stron firmowych.

## Meta — Facebook Pages i Instagram Professional

1. Utwórz prawdziwą testową **Facebook Page** i nadaj swojemu kontu odpowiedni dostęp do tworzenia treści. To nie jest prywatna oś czasu.
2. W [Meta for Developers / Apps](https://developers.facebook.com/apps/) → Create App wybierz use case zarządzania stronami/publikowania i Facebook Login, zgodnie z aktualnym kreatorem. Jeśli wymagany jest Business portfolio, podłącz własny. Nie wpisuj fikcyjnego biznesu.
3. Aplikacja → Settings / Basic: App ID i App Secret trafiają do `Providers:Meta:ClientId` i `ClientSecret`. Uzupełnij wymagane adresy polityki prywatności, danych kontaktowych i usuwania danych przed review.
4. Facebook Login → Settings → Valid OAuth Redirect URIs: dodaj callback Facebooka. Callback Instagrama skonfiguruj oddzielnie w produkcie Instagram Login poniżej. Jeśli portal wymaga HTTPS lub Facebook Login for Business z konfiguracją `config_id`, **ten wariant wymaga dostosowania adaptera Facebooka**; obecny adapter używa standardowego `scope` flow.
5. Odczytaj aktualną wspieraną wersję Graph w dashboardzie i wpisz **dokładną wartość**, np. format `vNN.0`, do `Providers:Meta:Version`. Nie wpisuj dosłownie `vNN.0`. Pozostawiono pustą wartość, ponieważ oficjalny endpoint dokumentacji wersji nie został wiarygodnie odczytany.
6. App Roles: dodaj swojego użytkownika jako administratora/developera/testera i zaakceptuj zaproszenie. Korzystaj z legalnego własnego konta i prawdziwej strony testowej. Nie twórz fałszywych profili osobistych.
7. App Review / Permissions and Features: sprawdź dostęp do `pages_show_list`, `pages_read_engagement`, `pages_manage_posts`. Dla testów ról aplikacji może wystarczyć dostęp developerski; dostęp dla zwykłych przyszłych użytkowników wymaga odpowiedniego Advanced Access/review, a czasem Business Verification. Status konkretnego produktu rozstrzyga dashboard.
8. W PUBLICATE połącz Facebook. `/me/permissions` weryfikuje zgody, `/me/accounts` pobiera strony, ich tokeny i zadania. Brak strony oznacza sprawdzenie jej dostępu, ograniczeń biznesu, zakresu przyznanych zasobów i zgód użytkownika — nie ręczne wklejanie nieznanego Page ID.
9. Opublikuj tekst na wybranej testowej Page, sprawdź ID oraz widoczność bezpośrednio na stronie. Następnie JPEG, dwa zdjęcia, MP4. Filmy wymagają oddzielnego statusu; `Processing` nie jest sukcesem publikacji.

## Instagram — Unified Login dla bieżącej aplikacji PUBLICATE

Aktualizacja 2026-11-05: panel użytkownika oferuje Unified Login z konfiguracją Instagram Login Compatible. To inna metoda niż Direct poniżej. Nie należy używać danych aplikacji Unified z endpointem autoryzacji api.instagram.com.

W lokalnej konfiguracji zapisano publiczne wartości z wygenerowanego Login URL: `Providers:Instagram:LoginMode=Unified`, `ClientId=2082875755677881`, `ConfigId=3295601310634577`. `ClientSecret` ma być **App Secret tej aplikacji Meta**, a `Version` wersją Graph wskazaną w panelu. Uzupełnij je bez przekazywania do rozmowy: `powershell -ExecutionPolicy Bypass -File scripts/Configure-Instagram.ps1`. Skrypt pyta o sekret w ukrytym polu. Uruchom ponownie backend po zmianie.

Callback: `https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/Instagram/callback`. Zaloguj się do PUBLICATE przez tę domenę, kliknij Połącz przez Instagram. Aplikacja generuje świeży state; nie otwieraj ręcznie przykładu z `<STATE>`. Konfiguracja portalu określa udostępniane konta i zgody. Nie dodajemy `scope` do URL z `config_id`.

Implementacja wymienia kod przez POST w Graph Facebook, sprawdza token przez debug_token (is_valid i app_id), pobiera tożsamość systemową i client_business_id przez /me, a udostępnione konta przez /me/assigned_instagram_accounts?fields=id,username. Odczytuje wszystkie strony po kursorze. Nie używa już granular_scopes jako listy kont. Pusta lista nie jest sukcesem połączenia. Metoda, token i portfolio są zapisane oddzielnie od kont Facebooka; publikacja i status Unified korzystają z graph.facebook.com.

Oficjalny [Get started](https://developers.facebook.com/documentation/instagram-platform/instagram-api-with-unified-login/get-started) odczytano HTTP 200 z Meta; opisuje GET przy wymianie kodu. Na życzenie użytkownika nie wykonywano GET z App Secret. Test POST client_credentials po poprawieniu sekretu zwrócił HTTP 200. Nie potwierdza to jeszcze wymiany authorization_code ani publikacji. Nie odświeżaj starego callbacku: zawsze rozpocznij nową próbę przyciskiem PUBLICATE. Raport OAuth pojawia się w panelu i w bezpiecznym lokalnym pliku .data/oauth-diagnostics.jsonl. Szczegółowy raport: [OAUTH_DIAGNOSIS.md](OAUTH_DIAGNOSIS.md).

## Instagram — bezpośrednie Instagram Login (Direct, gdy dostępne w panelu)

Instagram podłącza się niezależnie od Facebooka. Nie wymaga strony FB ani podłączenia Facebooka w PUBLICATE. Wymagane konto profesjonalne Firma lub Twórca; konta osobiste nie są obsługiwane.

1. W Meta for Developers otwórz aplikację PUBLICATE i dodaj produkt/przypadek użycia Instagram. Wybierz **API setup with Instagram login**, a nie ścieżkę Facebook Login. Nazwy kreatora mogą się różnić; dokładny ekran portalu pozostaje do weryfikacji na Twojej aplikacji.
2. W konfiguracji Instagram znajdź **Instagram App ID** i **Instagram App Secret**. Nie zastępuj ich identyfikatorem i sekretem produktu Facebook Login.
3. W sekcji konfiguracji Business Login for Instagram / redirect URI wpisz dokładny callback: `http://localhost:5080/api/oauth/Instagram/callback`. Jeśli portal wymaga HTTPS, zastosuj sekcję OAuth HTTPS poniżej i wpisz tamten adres również w portalu. Nie zaczynaj logowania na localhost, jeśli callback wraca do innego hosta.
4. Skonfiguruj dostęp do `instagram_business_basic` i `instagram_business_content_publish`. Nie potrzebujesz scopes `pages_*`. Dodaj własne konto do testerów zgodnie z panelem i zaakceptuj zaproszenie po stronie Instagrama. Użytkownicy spoza ról aplikacji wymagają dostępu/review wskazanego w portalu; nie gwarantujemy publicznego dostępu w trybie developerskim.
5. W lokalnym pliku backendu `appsettings.Local.json` uzupełnij `Providers:Instagram:ClientId`, `ClientSecret` i `Version` (dokładna wspierana wersja Graph widoczna w portalu). Sekcja `Providers:Meta` dotyczy teraz wyłącznie Facebooka. Nie wysyłaj sekretów do rozmowy i nie dodawaj pliku lokalnego do Git.
6. Uruchom ponownie backend, odśwież PUBLICATE, otwórz **Połączone konta → Instagram → Połącz przez Instagram**. Zatwierdź zgody na stronie Instagrama. Po powrocie powinien pojawić się username i osobny cel publikacji IG; żadne konto Facebook nie jest automatycznie tworzone.
7. Do samego połączenia nie potrzebujesz magazynu publicznych zdjęć. Do publikacji potrzebny jest staging HTTPS i świadome potwierdzenie. Pierwszy test: JPEG 1080×1080. Sam kontener nie jest potwierdzeniem publikacji.

Poprzednie połączenia IG przez Facebooka wymagają odłączenia i ponownego połączenia przez Instagram. Historia pozostaje. Stare tokeny Page nie są wysyłane do graph.instagram.com.

Token krótkoterminowy jest wymieniany na długoterminowy; odświeżanie korzysta z `ig_refresh_token`. Wygasły token wymaga ponownego logowania. Zgody zapisujemy z odpowiedzi dostawcy, nigdy z samej listy żądanych scopes. Brak listy zgód oznacza UNVERIFIED i blokuje publikację do wyjaśnienia rzeczywistej odpowiedzi API.

Stan: implementacja i testy HTTP lokalne; rzeczywisty OAuth/publikacja **BLOCKED — brak konfiguracji**. Oficjalna kolekcja Meta potwierdza odrębne scopes i host. Szczegółowa strona Business Login zwracała 429 podczas aktualizacji 2026-11-05; pełny kontrakt wymaga testu na koncie.

## TikTok — dostęp eksperymentalny

1. [TikTok Developer Portal](https://developers.tiktok.com/) → Manage apps / Connect an app. Podaj rzeczywisty opis publicznego produktu, witrynę, privacy policy, terms i wymagane materiały demonstracyjne.
2. Dodaj **Login Kit** i **Content Posting API**; w Content Posting włącz **Direct Post**. Wystąp o `user.info.basic` oraz `video.publish`. Samo dodanie produktu ani prywatne konto nie potwierdza przyznania scope.
3. W ustawieniach Login Kit Web wpisz dokładny callback HTTPS. Zapisz Client key i Client secret lokalnie w `Providers:TikTok`.
4. Skonfiguruj konto testowe jako prywatne i dodaj je do użytkowników testowych/sandbox, jeśli dashboard udostępnia tę opcję. Gdy dashboard nie udostępnia Direct Post w danym trybie, oznacz test BLOCKED; nie obchodź wymagań.
5. Dopiero po przyznaniu produktu i scope ustaw `Providers:TikTok:PublishingApproved=true`. Ta flaga **nie przyznaje** dostępu; jest lokalną bramką.
6. W testerze po połączeniu pobierz dane twórcy. Wybierz SELF_ONLY z rzeczywiście zwróconych opcji, zaakceptuj Music Usage Confirmation i użyj własnego niereklamowego, niegenerowanego przez AI filmu. PoC wyłącza komentarze, Duet i Stitch.
7. Upload FILE_UPLOAD nie wymaga hostowania filmu na Twojej domenie. Po przyjęciu materiału sprawdź status do PUBLISH_COMPLETE. Film prywatny może nie zwrócić publicznego ID ani URL — nie tworzymy ich sztucznie.

Nie ma implementacji zdjęć TikTok ani PULL_FROM_URL; wymagają zweryfikowanej domeny/prefiksu i osobnego UX. Audyt ma ograniczenia dotyczące narzędzi do własnych kont; patrz [oficjalne wytyczne](https://developers.tiktok.com/docs/en/content-sharing-guidelines). Brak przyznanego scope jest legalną blokadą, nie błędem do obejścia.

## X — świadomy koszt

**Aktualny zakres po rozszerzeniu 12.11:** tekst Unicode, 4 zdjęcia JPEG/PNG lub MP4; ponowny OAuth z media.write. [Aktualna instrukcja i ograniczenia](X_SETUP.md). Poniższy opis adaptera ASCII jest historyczny. Użytkownik potwierdził doładowanie i zgodę na testy w budżecie 5 USD; lokalnie odblokowano AllowPaidCalls.

Aktualizacja 12.11.2026: użytkownik wznowił prace nad połączeniem X. OAuth/PKCE i adapter tekstowy istnieją; brak testu live i zgody kosztowej. Skrypt `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Configure-X.ps1` zapisuje OAuth 2.0 Client ID/Secret lokalnie i jawnie pozostawia AllowPaidCalls=false (również po ponownej konfiguracji). Nie używać API Key/Secret OAuth 1.0a ani Bearer Token zamiast tych danych.

Callback: `https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/X/callback`; Website URL: `https://cofounder-morse-reflex.ngrok-free.dev/`. Terms/Privacy: `/terms` i `/privacy` na tej domenie. Web App/confidential client, Read and write; bez uprawnienia wiadomości prywatnych. Dokumenty opisują obecny prywatny prototyp, nie gotowy produkt komercyjny.

Cennik sprawdzony ponownie 12.11: pay-per-use, User Read 0,010 USD/zasób, Post Create 0,015 USD/żądanie, Create with URL 0,200 USD/żądanie. Stawki konta sprawdzić w Developer Console. Callback po wymianie kodu pobiera `/2/users/me`, więc nie obiecujemy bezpłatnego połączenia. Samo polecenie podłączenia nie zastępuje wcześniejszego wymogu jawnej zgody na płatne wywołania. Nie kupiono kredytów ani nie odblokowano kosztów.

1. [Developer Console](https://developer.x.com/) → aplikacja → User authentication settings. OAuth 2.0, aplikacja Web App/confidential client, uprawnienia odczyt/zapis, dokładny callback URL.
2. Client ID i Secret zapisz w `Providers:X`. Scopes adaptera: `tweet.read tweet.write users.read offline.access`. PKCE S256 jest generowane przez backend.
3. **Przed odblokowaniem** odczytaj [cennik](https://docs.x.com/x-api/getting-started/pricing) i stawki konta. Na dzień analizy: create tekstu 0,015 USD, create z URL 0,200 USD, odczyt użytkownika 0,010 USD. Właśnie dlatego nawet połączenie konta ma blokadę kosztową.
4. Nie kupuj kredytów wyłącznie na podstawie niniejszej instrukcji. Gdy sam zatwierdzisz koszt, ustaw budżet, wyłącz auto-recharge, a potem `Providers:X:AllowPaidCalls=true`. W oknie publikacji ponownie zatwierdzasz konkretny post i koszt.
5. PoC obsługuje krótki tekst ASCII (do 280) i odczytuje prawdziwe ID. Media i ważone Unicode pozostają poza zakresem. Brak kredytów lub produktu → BLOCKED.

## Publiczny HTTPS oraz R2

Najpierw udowodnij LinkedIn i Facebook przez bezpośrednie uploady — nie potrzebują publicznego hostowania plików. Dla Instagrama możesz użyć lokalnego magazynu z publicznym HTTPS reverse proxy/tunelem albo R2.

**Lokalny magazyn:** `Storage:Provider=Local`, `Storage:PublicBaseUrl=https://TWOJ_HOST`. Proxy musi kierować `/media/*` do backendu na localhost:5080. Endpoint jest publiczny wyłącznie dla plików udostępnionych przez potwierdzoną publikację. Zewnętrzny odbiorca nie ma dostępu do reszty katalogu ani listingu. Nie umieszczaj Cloudflare Access/challenge/cookie logowania przed `/media/*`, bo crawler nie pobierze pliku. Sprawdź Content-Type, GET bez cookie i dostęp z innej sieci. Nie testowano konkretnego tunelu ani dostawcy hostingu.

**OAuth HTTPS:** start OAuth i callback muszą działać pod tą samą nazwą hosta w przeglądarce, ponieważ cookie state jest związane z hostem. Nie zaczynaj logowania na localhost, jeśli callback wróci na publiczną domenę. Ustaw `Local:Origin` na origin panelu HTTPS, `OAuth:CallbackBase` na ten sam origin, dodaj host do `AllowedHosts` backendu. Reverse proxy powinno kierować `/api/*` do backendu i pozostałe ścieżki do panelu. Jeśli tunel prowadzi do Vite, dopisz dokładny własny host do `server.allowedHosts` w vite.config.ts — nie ustawiaj allow-all. Vite ma proxy `/api` oraz `/media`. Cookies Secure ustaw zgodnie z HTTPS przed wystawieniem; PoC nie jest gotowym publicznym hostingiem dla klientów.

**R2:**

1. Cloudflare dashboard → R2 Object Storage → utwórz dwa buckety Standard: prywatny i publiczny staging. Nazwy wybierz sam.
2. R2 → Manage R2 API Tokens → ograniczony token odczytu/zapisu do tych dwóch bucketów. Sekrety wyłącznie w backendzie.
3. Publiczny staging → Settings → Custom Domains → Add → wpisz np. `media.TWOJA_DOMENA` → potwierdź rekord DNS → zaczekaj na Active. Domena musi być w Twoim koncie Cloudflare. Prywatny bucket pozostaje bez publicznego dostępu.
4. Dla testów Instagrama można rozważyć development URL r2.dev, ale jego działanie z crawlerem wymaga sprawdzenia. Dla TikTok weryfikacja własności prefiksu/domeny jest dodatkowym warunkiem; nie zakładamy weryfikowalności domeny r2.dev.
5. Konfiguracja (wartości celowo puste):

```json
{
  "Storage": {
    "Provider": "R2",
    "RetentionHours": 48,
    "InactiveDays": 30,
    "R2": {
      "AllowOperations": false,
      "AccountId": "",
      "AccessKeyId": "",
      "SecretAccessKey": "",
      "PrivateBucket": "",
      "PublicBucket": "",
      "PublicBaseUrl": ""
    }
  }
}
```

6. Po zaakceptowaniu warunków i kosztów zmień AllowOperations na true. Nie zrobiono tego w obecnym środowisku. Wyłącz cache publicznego stagingu albo ustaw krótkie Cache-Control, aby usunięty obiekt nie pozostawał w CDN. Adapter nie wykonuje purge Cloudflare; konfiguracja omijania cache jest warunkiem retencji.

[Cennik R2](https://developers.cloudflare.com/r2/pricing/) odczytany podczas prac: Standard 10 GB-month, 1 mln Class A i 10 mln Class B miesięcznie w free tier; ponad to 0,015 USD/GB-month, 4,50 USD/mln A, 0,36 USD/mln B. Transfer do Internetu bez opłaty R2. To nie gwarantuje zerowego rachunku: zależy od całego konta i innych produktów. Domeny nie kupiono; koszt konkretnego TLD i odnowienia pozostaje do sprawdzenia przed zakupem w budżecie 100–200 PLN.

[Presigned URL](https://developers.cloudflare.com/r2/api/s3/presigned-urls/) działa z domeną S3 R2, nie z custom domain. Nie jest równoważny zweryfikowanej domenie TikTok. Nie konfiguruj bezwarunkowej reguły kasowania całego bucketu po 48 h — usunęłaby materiały aktywnych operacji. Retencją steruje backend.

## AI — wyłącznie po zgodzie

Adapter korzysta z [OpenAI Responses / vision](https://developers.openai.com/api/docs/guides/images-vision). Proponowana jawna konfiguracja: `Ai:Model=gpt-4.1-mini`, którego [karta](https://developers.openai.com/api/docs/models/gpt-4.1-mini) potwierdza wejście obrazowe i Responses. Klucz utwórz w projekcie API, ustaw limity wydatków, wpisz lokalnie do `Ai:ApiKey`. Po własnym zatwierdzeniu kosztu ustaw `Ai:AllowPaidCalls=true`.

Panel wymaga dodatkowej zgody na każde wywołanie i przesłanie zdjęć. Nie wykonano płatnego testu. Do 10 zdjęć w kolejności, detail=low, maks. 1000 tokenów wyjścia, store=false. Model może pominąć drobny tekst lub popełnić błąd — wynik służy ręcznej edycji. Klatki wideo i transkrypcja pozostają niezaimplementowane.

## Odłączenie i odwołanie

Przycisk Odłącz usuwa lokalne tokeny i blokuje cele. Dodatkowo cofnij zgodę w ustawieniach połączonych aplikacji konta dostawcy (LinkedIn Settings & Privacy → Data privacy → Permitted services; Meta Settings → Apps and websites/Business integrations; TikTok Security → Manage app permissions; X Security and account access → Apps and sessions). Nazwy mogą się zmieniać. Nie zakładamy, że lokalne usunięcie tokenu odwołuje zgodę po stronie platformy.
# YouTube Shorts — dodano 10.11.2026

Konfiguracja Google Cloud, bezpieczny lokalny skrypt i procedura testu: [YOUTUBE_SETUP.md](YOUTUBE_SETUP.md).

