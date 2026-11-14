# Znane ograniczenia

Unified Login (2026-11-05): poprawiono discovery na oficjalne /me/assigned_instagram_accounts i zapis client_business_id. Dokumentacja została odczytana. Rzeczywisty POST client_credentials po poprawieniu App Secret: HTTP 200. Pełny OAuth konta i publikacja wymagają osobnego potwierdzenia. Patrz OAUTH_DIAGNOSIS.md.

> Aktualizacja 2026-11-05: działa rejestracja i logowanie e-mail/hasło, bez AccessKey. Zasoby należą do zalogowanego użytkownika, OAuth state jest związany z jego sesją. Pełny opis: [USER_ACCOUNTS](USER_ACCOUNTS.md).

## Zaimplementowane, ale niepotwierdzone na realnych kontach

OAuth, identyfikacja, scopes, discovery celów, publikacje i statusy mają rzeczywiste endpointy, bez trybu symulacji w aplikacji. **Nie wykonano żadnego rzeczywistego OAuth ani publikacji**. LinkedIn/Facebook/Instagram wymagają Twoich aplikacji i uprawnień. Schematy HTTP przeszły testy z atrapami, które nie dowodzą zgodności z bieżącym serwerem ani dostępności produktu.

- Meta: części dokumentacji nie udało się pobrać (429/niedostępna strona). Wersję Graph należy potwierdzić i wpisać ręcznie. Standardowy OAuth scope flow może nie wystarczyć dla narzuconego przez dashboard Facebook Login for Business/config_id; nie zaimplementowano tego wariantu.
- LinkedIn: profil osobisty, nie discovery organizacji. Odczyt posta i odczyt obrazu mogą wymagać uprawnień niedostępnych w Share on LinkedIn. Wysłanie obrazu nie korzysta z nieobsługiwanego synchronous upload; późniejszy Posts API może odrzucić niegotowy obraz.
- Długie przetwarzanie LinkedIn/Instagram kończy pierwszą operację w Processing. Nie wdrożono trwałego wznowienia końcowej publikacji na istniejących kontenerach. Po odczycie gotowego kontenera, tylko jeśli finalny POST nie był rozpoczęty, można ręcznie wykonać nową próbę (nowy upload). Nie dotyczy Unknown.
- TikTok: rzeczywisty adapter filmu FILE_UPLOAD i statusu, ale brak dostępu do video.publish. Prywatne konto nie zwalnia z przyznania scope. Audyt może odrzucić narzędzie wyłącznie do własnych kont; wymagania szerokiej publiczności są odrębnym ryzykiem biznesowym. Zdjęcia i PULL_FROM_URL niezaimplementowane, choć API je oferuje.
- X: tylko tekst ASCII. Brak uploadu mediów i pełnego liczenia ważonych znaków Unicode. Wszystkie płatne wywołania wymagają flagi; cena nie jest wyliczana na bieżąco w UI. Korzystanie z aktualnego cennika i limitu konta jest obowiązkiem przed odblokowaniem.
- R2: adapter S3 skompilowany, bez rzeczywistego bucketu i testu zgodności. Wymaga odrębnych bucketów, AllowOperations, HTTPS i ustawienia cache stagingu. PoC nie wykonuje purge CDN.
- AI: rzeczywisty Responses API dla zdjęć/karuzeli, bez klucza i bez płatnego testu. Analiza klatek filmu i transkrypcja niezaimplementowane. Detail=low może gorzej odczytywać małe litery; użytkownik ręcznie sprawdza propozycję.

## Zakres lokalnego produktu

- Działające konta użytkowników z rejestracją/logowaniem i rozdzieleniem zasobów. Brak resetu hasła, potwierdzania e-mail, MFA i zarządzania sesjami. Nadal jedna instancja backendu ze względu na koordynację zadań.
- Posty i historia są trwałe; wykonanie zadania pozostaje w pojedynczym żądaniu z timeoutem 5 min. Restart nie wznawia automatycznie publikacji; stan staje się Unknown.
- Globalny gate serializuje mutacje, aby uniknąć wyścigu z cleanup. Przy długim uploadzie inne zapisy czekają. To świadomy kompromis PoC, nie architektura dla dużej skali.
- Obsługiwany format to węższy profil PoC: zdjęcia do 8 MiB, maks. 10, filmy do 64 MiB, 3–60 s, MP4/H.264/AAC. Nie są to kompletne maksima wszystkich platform. Nie ma transkodowania, cropu, GIF, dokumentów, Stories, planowania i analityki.
- Warianty można publikować indywidualnie po zgodzie. Nie ma jednego atomowego „opublikuj wszędzie”; API platform nie zapewniają wspólnej transakcji.
- Snapshot zawiera media IDs i tekst, ale nie utrwala kopii wszystkich pól konta. Cel ma stabilny zewnętrzny ID, nazwa celu jest zapisana w jobie. Zmiana nazwy konta nie zmienia treści historycznego posta.
- Surowe odpowiedzi dostawców nie są pokazywane: kody/HTTP/stan są wystarczające do pierwszej diagnostyki i ograniczają ryzyko ujawnienia tokenów. Błędy aplikacji celowo nie ujawniają stack trace.
- Lokalny disconnect nie odwołuje zdalnej zgody. Cofnij ją również w panelu dostawcy. Nie ma webhooków odwołania dostępu ani zdalnego usuwania danych na potrzeby App Review.
- Stan OAuth jest w pamięci i znika po restarcie. Publiczny HTTPS wymaga prawidłowego reverse proxy, hosta, cookies i callbacków. Nie przetestowano konkretnego tunelu ani procesu App Review.
- Nie ma twardego maksymalnego usuwania pliku przypiętego przez Unknown/Processing; takie pliki wymagają ręcznej diagnostyki. Automatyczne usunięcie groziłoby przerwaniem przetwarzania dostawcy.
- Ostrzeżenia retencji są widoczne przy materiałach w edytorze; brak e-maili/push. Nie ma panelu ręcznego rozstrzygania Unknown. Nie usuwaj rekordu z bazy, aby wymusić ponowienie bez sprawdzenia konta.
- Testy jednostkowe EF InMemory nie testują blokad PostgreSQL; rzeczywisty smoke sprawdza migracje i podstawowy CRUD. Brak testów obciążeniowych, penetracyjnych, pełnej dostępności i audytu zgodności prawnej.

## Nieosiągnięty warunek sukcesu

Minimum dwóch potwierdzonych publikacji na rzeczywistych platformach nie zostało osiągnięte. Nie ma podstaw, by oznaczyć te testy PASSED. Można przeprowadzić eksperyment inżynierski po konfiguracji LinkedIn i Meta; o gotowości komercyjnej nie przesądza nawet sukces na kontach administratora.

Instagram Login (2026-11-05): osobne dane aplikacji Instagram, bez Page. Rzeczywiste OAuth i publikacja nadal BLOCKED przez brak konfiguracji. Stare połączenia IG przez Facebook wymagają ponownego połączenia. Odświeżenie tokenu nie sprawdza ponownie cofniętych scopes; ostatecznie rozstrzyga odpowiedź API. Przy braku potwierdzonych zgód publikacja jest blokowana.
