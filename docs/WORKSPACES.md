# Dwa niezależne projekty PUBLICATE

Ustalenie 14.11.2026: inżynierka dopracowywana w obecnym zakresie, nowe funkcje w 2.0. Podział nie usuwa istniejących funkcji.

| Zasób | Inżynierka | Rozwój |
|---|---|---|
| Folder | `C:\Dev\Projects\INZYNIERKA PUBLICATE` | `C:\Dev\Projects\PROTOTYP PUBLICATE` |
| Repo | `patrykbronski/INZYNIERKA-PUBLICATE` | `patrykbronski/PUBLICATE-2.0` |
| Backend / Vite | 5080 / 5173 | 5081 / 5174 |
| PostgreSQL | 55432, istniejący klaster | 55433, nowy klaster, baza publicate2 |
| Cookie | publicate.session | publicate2.session |
| Protection application name | PUBLICATE (zachowane) | PUBLICATE-2.0 |
| Dane | istniejące | nowa rejestracja, ponowne połączenie kont |

Każdy folder ma własne pliki, `.data/media`, `.data/keys`, `.data/postgres`, logi, konfigurację, narzędzia i Git. Bez junctionów do bazy/mediów. 2.0 nie kopiuje użytkowników, tokenów, historii i plików. Migracje mogą tworzyć technicznego użytkownika historycznego bez hasła; to nie kopia konta użytkownika.

## Przełączanie

1. Dokończ publikację/OAuth. Przy Processing/Unknown upewnij się, że dostawca nie pobiera materiału. Stop chroni Uploading/Publishing, nie dowodzi zakończenia pobierania.
2. W aktualnym folderze: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Stop-Publicate.ps1`.
3. Procesy uruchomione ręcznie zatrzymaj w ich terminalach; stop nie zabija obcych node/dotnet. PostgreSQL może pozostać (osobne porty).
4. W docelowym folderze: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Publicate.ps1`.
5. Otwórz ponownie domenę ngrok; sprawdź INZYNIERKA/2.0 w panelu. Nie używaj starej karty po przełączeniu.

Start blokowany, gdy port backendu/frontendu drugiej wersji jest zajęty. Wspólny ngrok 4040/domena kieruje do wybranego frontendu; callbacki pozostają te same. Nie startuj obu naraz.
Osobne cookies/klucze chronią przed zaakceptowaniem sesji drugiej wersji. Nie zmieniaj application name inżynierki ani jej keys — uszkodzi to możliwość odczytu dotychczasowych tokenów.

## Co wspólne

Client ID/Secret skopiowane lokalnie, nie do Git. Limity, budżety X i portale pozostają wspólne. Rotacja sekretu/cofnięcie zgody może dotknąć obu wersji. Pełna izolacja u dostawcy wymaga osobnych aplikacji developerskich.
Ten sam użytkownik społecznościowy z tym samym Client ID może zależnie od dostawcy wpłynąć na poprzednie tokeny. Nie gwarantujemy ich niezależności; refresh tokenów nie kopiujemy. W razie potrzeby ponowny OAuth.
Przy pierwszym starcie 2.0 zarejestruj nowe konto PUBLICATE, połącz platformy. Klucze są lokalnie ustawione, ale ważność/uprawnienia potwierdza OAuth.

## Nowy Codex i Git

Otwórz właściwy folder i napisz: „Przeczytaj AGENTS.md i PROJECT_CONTEXT.md. Pracujemy tylko w tej wersji”. Kontekst/backlog aktualizuj w odpowiednim repo; brak automatycznej synchronizacji kodu.
Nie wysyłaj `.data`, `.tools`, konfiguracji lokalnych, backupów ani mediów. Klon repo nie odtwarza danych/sekretów. Backupy danych inżynierki przechowuj osobno i bezpiecznie.
