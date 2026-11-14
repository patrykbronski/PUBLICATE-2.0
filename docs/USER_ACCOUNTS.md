# Konta PUBLICATE — aktualizacja 2026-11-05

## Jak korzystać

1. Otwórz http://localhost:5173 i wybierz **Załóż konto**.
2. Podaj imię, adres e-mail i własne hasło (12–128 znaków). Nie jest to hasło do LinkedIna ani Facebooka.
3. Po rejestracji jesteś zalogowany. W **Moje konto** widzisz swój profil; w **Połączone konta** znajdują się integracje społecznościowe.
4. **Połącz konto** otwiera stronę dostawcy, gdzie logujesz się i zatwierdzasz dostęp. Podłączony profil trafia do Twojego konta PUBLICATE.

AccessKey nie jest już używany. Nie trzeba odczytywać klucza z pliku konfiguracyjnego. Istniejące wpisy AccessKey w prywatnej konfiguracji nie pozwalają się zalogować. Stare sesje lokalnego testera są odrzucane. Stare dane testera zachowano w bazie; nie przypisujemy ich automatycznie pierwszemu nowemu użytkownikowi.

## Użytkownik a administrator aplikacji

Zwykły użytkownik zakłada konto PUBLICATE i udziela zgody OAuth. Nie rejestruje własnej aplikacji developerskiej i nie wpisuje ClientSecret. Właściciel PUBLICATE musi jednorazowo zarejestrować aplikację u każdego dostawcy, skonfigurować callbacki i uzyskać wymagane uprawnienia. Dopóki to nie nastąpi, integracja ma status **Integracja nieaktywna**. Logowanie e-mailem nie znosi wymagań Meta, LinkedIna czy TikToka. Użytkownicy spoza zespołu developerskiego mogą wymagać zatwierdzenia produktu przez dostawcę.

Nie wykonano rzeczywistego OAuth ani publikacji w ramach tej zmiany. Nie skonfigurowano sekretów platform i nie wykonano płatnych wywołań.

## Implementacja

- Users rozszerzono migracją UserAccounts o adres, znormalizowany unikalny e-mail, hash hasła oraz licznik błędów i termin blokady.
- Hasła: PasswordHasher<User> z ASP.NET Identity, PBKDF2 w formacie IdentityV3, 210000 iteracji i losowa sól; żadnych haseł w JSON odpowiedzi czy logach. [Dokumentacja Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration).
- Cookie HttpOnly, SameSite=Lax, Secure dla konfiguracji HTTPS, ważność 8 h. Każda sesja jest sprawdzana względem istniejącego konta posiadającego hash hasła.
- Rejestracja i login mają limit 10 żądań/min; po pięciu złych hasłach konto jest blokowane na 15 minut. Nadal działa ochrona Origin i nagłówek X-Publicate dla mutacji.
- Endpointy odczytują właściciela z uwierzytelnionej sesji. Nowe posty, warianty, multimedia, konta, cele, joby i próby otrzymują jego identyfikator. Właściciel nie jest przyjmowany z formularza.
- OAuth state zapisuje właściciela. Callback wymaga zalogowanej sesji tego samego użytkownika, a także jednorazowego state i cookie przeglądarki. Zmiana użytkownika w trakcie autoryzacji odrzuca callback przed wymianą kodu.
- Cleanup pozostaje usługą administracyjną działającą dla wszystkich użytkowników. Publiczny staging mediów jest wyjątkiem od prywatnego dostępu, udostępnianym dopiero po zatwierdzeniu publikacji.

## Zakres prototypu

Brak wysyłki e-mail, potwierdzania adresu, odzyskiwania/zmiany hasła, MFA, usuwania konta i zarządzania aktywnymi sesjami. Konto można utworzyć i używać lokalnie bez usługi pocztowej. Przed publicznym wdrożeniem trzeba wdrożyć te mechanizmy i pełny przegląd bezpieczeństwa. Nadal uruchamiaj jedną instancję backendu ze względu na koordynację publikacji i retencji.

## Endpointy

| Endpoint | Działanie |
|---|---|
| POST /api/register | Imię, e-mail, hasło; tworzy konto i sesję |
| POST /api/session | E-mail i hasło; tworzy sesję |
| GET /api/session | Bezpieczne dane bieżącego użytkownika |
| DELETE /api/session | Wylogowanie |
| POST /api/oauth/{platform}/start | Rozpoczęcie łączenia dla bieżącego użytkownika |
| GET /api/oauth/{platform}/callback | Zakończenie OAuth wyłącznie w tej samej sesji użytkownika |

Skrypt Test-Live.ps1 pyta teraz o dane konta PUBLICATE przez Get-Credential, a następnie nadal wymaga osobnego potwierdzenia publikacji. Test smoke tworzy własne losowe konta `@example.test`; nie odczytuje już AccessKey ani haseł użytkownika z konfiguracji.
