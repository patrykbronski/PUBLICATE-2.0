# Facebook Login for Business — konfiguracja testowa

2026-11-10: konfiguracja `29698534673097784`, utworzona przez użytkownika z typem **User access token**. Backend obsługuje opcjonalne `Providers:Meta:ConfigId`: wysyła `config_id`, `response_type=code`, `override_default_response_type=true` i jednorazowy `state`; zakresy są określone w konfiguracji Meta zamiast parametru `scope`. Bez ConfigId pozostaje standardowy przepływ. Wymiana kodu nadal POST z sekretem w treści, nie w URL. Instagram ma niezależną konfigurację.

W Facebook Login for Business → Settings dodaj Valid OAuth Redirect URI:

```
https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/Facebook/callback
```

Konfiguracja Meta powinna obejmować pages_show_list, pages_read_engagement i pages_manage_posts. Sekcja Providers:Meta w ignorowanym appsettings.Local.json zawiera ClientId, ClientSecret, Version i ConfigId. Dla tej samej aplikacji Meta użyto lokalnie istniejących danych aplikacji, bez ujawniania sekretu. Nie przenoszono tokenu Instagrama.

Po uruchomieniu backendu i tunelu zaloguj się do PUBLICATE pod publicznym adresem, wybierz Połącz Facebook, udziel dostępu do strony i sprawdź listę celów. Samo połączenie nie publikuje treści.

Testy automatyczne: 120/120, w tym 3 nowe testy konfiguracji Facebooka. Podczas testów wyłączono Windows EventLog przez Logging__EventLog__LogLevel__Default=None ze względu na odmowę dostępu środowiska. NU1900: nie pobrano bieżącej bazy podatności NuGet.

Rzeczywisty OAuth Facebooka i publikacja: NOT_TESTED. Nie wykonano wywołań publikujących. Dokumentacja Meta podczas tej zmiany była niedostępna (HTTP 429 / brak połączenia); zgodność konfiguracji wymaga testu z panelem użytkownika.
