# Rozwój przed komercjalizacją

> Aktualizacja 2026-11-05: działa rejestracja i logowanie e-mail/hasło, bez AccessKey. Zasoby należą do zalogowanego użytkownika, OAuth state jest związany z jego sesją. Pełny opis: [USER_ACCOUNTS](USER_ACCOUNTS.md).

Najpierw zebrać dowody: LinkedIn tekst + obraz i Facebook Page tekst + obraz na rzeczywistych kontach; następnie Instagram JPEG/karuzela/Reel. Udokumentować dostęp produktu, role, rzeczywiste limity, błędy i koszt. Odmowa scope jest wynikiem eksperymentu, nie pretekstem do nieoficjalnej automatyzacji.

| Etap | Co rozbudować | Warunek przejścia |
|---|---|---|
| Dostęp API | App Review Meta, produkty LinkedIn, audyt TikTok, analiza kosztów X | Publikacja legalnie dostępna dla użytkownika spoza zespołu aplikacji |
| Użytkownicy | Rozbudowa istniejących kont o reset hasła, e-mail verification, MFA lub produkcyjne OIDC, audyt polityk autoryzacji i izolacji kluczy | Testy prób dostępu między tenantami i przegląd bezpieczeństwa |
| Zadania | Trwały worker, leases/row locks, outbox, per-step checkpoint, wznowienie uploadów, ręczne rozstrzyganie Unknown | Restart i awaria sieci bez utraty historii i duplikatów |
| Media | Tabele referencji/indeksy, pełne profile platform, kontrolowane transkodowanie po zgodzie, skanowanie plików, streaming R2 | Walidacja rzeczywistych plików i pełna retencja z purge CDN |
| OAuth | Zdalne revoke, token health, refresh locks, obsługa wygasłych scopes, webhooks deauthorization/deletion | Odłączenie i usuwanie danych zgodne z procesami dostawców |
| Publikacje | Organizacje LinkedIn, rzeczywista weryfikacja Instagram Login, TikTok photo, pełne UX zgód, media X, planowanie | Każda zdolność objęta aktualną dokumentacją i realnym testem |
| AI | Reprezentatywne klatki, transkrypcja, testy jakości/OCR, budżety per user, polityka przekazywania materiałów | Mierzalna jakość i przewidywalny koszt bez automatycznego publikowania wyników |
| Operacje | CI, migracje rolling, backup/restore, monitoring, bezpieczne logi, zarządzanie sekretami/KMS, alerty retencji | Sprawdzone RPO/RTO, rotacja kluczy i procedura incydentu |
| Produkt i rozliczenia | Limity planów, billing, odpowiedzialność za API costs, audyt dostępności, regulaminy i procesy prywatności | Ocena prawna i biznesowa przed przyjęciem publicznych klientów |

Nie wdrożono tych elementów w PoC. Monolit można rozbudowywać bez przepisywania domeny i adapterów; kolejka i worker mogą pozostać w jednym wdrożeniu na początku. Wyodrębnienie mikroserwisów ma sens dopiero po pomiarze rzeczywistego obciążenia.

Praca inżynierska może porównać modele publikowania (upload vs URL/pull), OAuth i review, semantykę potwierdzeń, odporność na niejednoznaczne wyniki i retencję współdzielonych mediów. Wyniki BLOCKED też są wartościowe, jeśli zawierają dowód wymagania dostawcy. Komercjalizacja wymaga osobnej walidacji dostępu dla klientów, kosztu i warunków usług.
