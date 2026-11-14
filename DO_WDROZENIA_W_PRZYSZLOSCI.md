# PUBLICATE — do wdrożenia w przyszłości

Aktualizacja: 2026-11-13.

Rejestr funkcji świadomie odłożonych podczas rozmów o projekcie. Wpis nie oznacza, że funkcja jest wdrożona ani że należy wdrażać ją przy bieżącym zadaniu. Kolejne uzgodnienia dopisujemy tutaj; zmiany decyzji aktualizują istniejące wpisy.

## Uzgodnione: ograniczenie zmiany kont docelowych

Status: odłożone; nie wdrażać w obecnej przebudowie kreatora.

- Użytkownik może zmienić podpięty profil lub stronę. Po zmianie kolejna zmiana jest zablokowana przez **7 dni**.
- Blokada obowiązuje **osobno dla każdej platformy**, nie dla całego konta PUBLICATE.
- Zmiana oznacza podmianę rzeczywistego celu publikacji, ustalaną po identyfikatorze dostawcy. Dotyczy również wyboru innej strony Facebooka w ramach tego samego konta OAuth.
- Odświeżenie tokenu lub ponowne OAuth tego samego celu nie jest zmianą celu.
- Odłączenie i ponowne podłączenie nie powinno omijać aktywnej blokady. Pierwsze podłączenie należy odróżnić od podmiany.
- Informację o blokadzie i terminie kolejnej dostępnej zmiany należy przechowywać oraz egzekwować po stronie backendu.
- Cel biznesowy: ograniczyć częste współdzielenie jednego abonamentu przez przepinanie kont. Właściciel produktu akceptuje możliwość zmiany raz na tydzień; nie wymaga całkowitego wyeliminowania tego zachowania.
- **Zastępuje wcześniejszą propozycję 30 dni.**
- Przed implementacją doprecyzować wyjątki obsługiwane przez wsparcie oraz wpływ usunięcia konta PUBLICATE i ponownej rejestracji. Nie obiecywać automatycznej weryfikacji własności firmy na podstawie nazw profili.

## Kierunek komercyjny: abonament

Status: założenie produktu, szczegóły do ustalenia.

- Docelowo miesięczny abonament dla jednej firmy lub osoby.
- Ceny, operator płatności, struktura planów i dodatkowe przestrzenie marek nie zostały zatwierdzone.
- Nie wdrażać płatności ani blokad abonamentowych przy przebudowie kreatora.

## Odłożone: kontrola statusów i ochrona kosztów API

Ustalenie 13.11.2026: użytkownik pozostawia obecny przycisk „Sprawdź status” w prototypie, ale akceptuje zapis kierunku do późniejszego wdrożenia. **Nie wdrażać teraz; zabezpieczenia wymagane przed udostępnieniem aplikacji innym użytkownikom.**

- Dla Published zwykły użytkownik widzi wynik z bazy i „Otwórz post”, bez ponownego odpytywania dostawcy.
- Dla Processing docelowo ograniczone automatyczne sprawdzanie przez backend: odstępy zgodne z API, maksymalna liczba prób i czas oczekiwania.
- Dla Unknown kontrolowana weryfikacja tylko tam, gdzie API pozwala ustalić wynik. Nigdy automatyczne powtórzenie publikacji o niepewnym rezultacie.
- Ręczne odpytanie dostawcy przenieść do diagnostyki administratora; jasno oznaczać możliwość kosztu. „Szczegóły” i odświeżenie historii mają czytać lokalną bazę.
- Ochrona musi działać na backendzie, niezależnie od ukrycia przycisku: blokada równoległych kontroli tego samego zadania, minimalny odstęp/cache wyniku, limity na zadanie i użytkownika oraz globalny budżet wywołań/kosztów. Wielokrotne kliknięcia nie mogą generować osobnych płatnych zapytań.
- Po wyczerpaniu limitu zatrzymać odpytania i pokazać stan oczekiwania/konieczność interwencji, nie fikcyjny sukces ani automatyczne doładowanie.
- Przed wdrożeniem ustalić wartości limitów, zasady budżetu i obsługę restartu. Spend Cap w konsoli dostawcy jest dodatkowym zabezpieczeniem, nie zamiennikiem kontroli aplikacji.
- Testy: seria 300 kliknięć i równoległych żądań nie omija limitów; Published nie wywołuje API; limity egzekwowane również bez UI; Unknown nie odblokowuje ponownej publikacji.

## Odłożone: rozszerzenie karuzel i limitów zdjęć

Aktualizacja 10.11.2026: na nowe zlecenie użytkownika wdrożono 20 zdjęć dla Instagrama i LinkedIn oraz aktualizację limitów czasu. Testy z atrapami przeszły; rzeczywiste testy graniczne pozostają do wykonania. Nadal odłożone: mieszane karuzele IG, zdjęcia TikTok, multimedia i pełny licznik twitter-text X, odczyt bieżącego `content_publishing_limit` IG, pełna walidacja FPS/bitrate/GOP oraz potwierdzenie większych limitów Facebooka. Szczegóły: [docs/API_LIMITS.md](docs/API_LIMITS.md).

Status: użytkownik odłożył osobne rozszerzenie integracji na później.

- Zweryfikować i rozszerzyć obsługę większych karuzel Instagrama oraz wielozdjęciowych postów Facebooka zgodnie z faktycznie używanym API.
- Nie utożsamiać limitu obecnego adaptera z limitem API ani z możliwościami aplikacji mobilnej dostawcy.
- Zmiana liczby w interfejsie nie stanowi potwierdzenia obsługi; potrzebne testy adaptera i osobno zatwierdzony test rzeczywistej publikacji.
- Bieżąca przebudowa kreatora nadal obejmuje uczciwe prezentowanie i weryfikowanie limitów, ale nie oznacza automatycznego rozszerzenia wszystkich adapterów.

## Pozostałe kierunki techniczne

### ? Dopasowanie proporcji zdjęć do Instagrama

Status: **pomysł do rozważenia, niezatwierdzony do wdrożenia** (10.11.2026). Decyzja zależy od wpływu na koszty utrzymania aplikacji.

- Opcjonalne dodawanie marginesów lub przycinanie kadru, z podglądem i świadomym zatwierdzeniem użytkownika. Oryginał pozostaje bez zmian; przygotowana kopia dotyczy wariantu Instagrama.
- Najpierw ponownie zweryfikować aktualne wymagania używanego API i w razie potrzeby przeprowadzić osobno zatwierdzony test. Nie zakładać, że każda blokada obecnego walidatora nadal odpowiada rzeczywistemu ograniczeniu dostawcy.

### ? Konwersja PNG → JPEG dla Instagrama

Status: **pomysł do rozważenia, niezatwierdzony do wdrożenia** (10.11.2026). Użytkownik nie zdecydował, czy chce takiej funkcji; decyzja zależy od kosztów utrzymania.

- Opcjonalne przygotowanie kopii JPEG dla wariantu Instagrama z zachowaniem oryginalnego PNG. W przypadku przezroczystości potrzebny wybór tła i podgląd wyniku.
- Przed decyzją o obu funkcjach oszacować koszt przetwarzania CPU/RAM, przechowywania dodatkowych kopii, operacji magazynu i transferu przy zakładanej liczbie użytkowników oraz publikacji. Uwzględnić retencję i usuwanie kopii oraz możliwość wykonania części operacji w przeglądarce.
- Porównać te koszty z pozostawieniem ręcznego przygotowania pliku przez użytkownika. Nie zakładać, że koszt będzie zerowy lub pomijalny. **Nie wdrażać żadnej z tych funkcji bez kolejnej decyzji użytkownika.**

Dotychczasowe propozycje dotyczące komercjalizacji są w [docs/COMMERCIAL_ROADMAP.md](docs/COMMERCIAL_ROADMAP.md). To lista obszarów do rozważenia, nie zestaw dodatkowych funkcji zatwierdzonych do bieżącej implementacji.

## Poza tą listą: zatwierdzony bieżący zakres

Przebudowa kreatora na dwa etapy, szkice (48 h od utworzenia, maksymalnie 10), limit magazynu 1 GB na użytkownika oraz automatyczna aktualizacja celu niewysłanych wariantów z komunikatem i ponowną walidacją należą do **obecnego zakresu**, nie do funkcji odłożonych. Zatwierdzenie nie oznacza jeszcze wdrożenia.
# Aktualizacja YouTube — 10.11.2026

Wdrożono adapter YouTube Shorts i interfejs do konfiguracji kanału oraz publikacji filmu do 179,99 s. Test rzeczywisty wymaga danych Google. Długie filmy nie należą do bieżącego zakresu; nazwa wewnętrzna platformy pozostaje YouTube. Do rozważenia później: trwałe wznawianie uploadu po restarcie, większe pliki, miniatury, tagi i planowanie. Nie wdrażać automatycznie. Posty społecznościowe wymagają przyszłego oficjalnego wsparcia API.

