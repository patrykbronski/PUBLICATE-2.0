# Uruchamianie jednym poleceniem

> Od 14.11.2026 porty i nazwa pochodzą z `instance.json`. Dla 2.0: baza 55433, backend 5081, frontend 5174. Domena i panel ngrok 4040 są wspólne, używane naprzemiennie. [Przełączanie projektów](WORKSPACES.md). Poniższe wartości 55432/5080/5173 dotyczą inżynierki.

Z katalogu projektu, w PowerShellu na swoim dotychczasowym koncie Windows:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Start-Publicate.ps1
```

Skrypt uruchamia istniejącą lokalną bazę PostgreSQL (port 55432), backend (5080), frontend (5173) oraz ngrok (lokalny interfejs kontrolny 4040). Usługi działają w tle bez dodatkowych okien terminala. Na końcu otwiera publiczną stronę PUBLICATE. Opcja `-NoBrowser` pomija otwarcie przeglądarki.

Wymagane są wcześniej przygotowane zależności, konfiguracja lokalna oraz zainstalowany i zalogowany ngrok. Skrypt nie instaluje pakietów ani nie konfiguruje kont. Uruchamiaj go na tym samym koncie Windows co wcześniej ze względu na szyfrowanie DPAPI.

Zajęte porty są traktowane jako istniejące usługi, bez uruchamiania duplikatów. Skrypt dodatkowo sprawdza w lokalnym API ngrok adres domeny i port docelowy. Sam otwarty port backendu nie potwierdza sprawności bazy ani OAuth. Przy konflikcie portów sprawdź proces, który go zajmuje.

Logi: `.data/launcher/backend.out.log`, `backend.err.log`, analogicznie `frontend` i `ngrok`. Log bazy: `.data/postgres.log`. Pliki te pozostają poza repozytorium. Nie udostępniaj pełnych logów bez sprawdzenia, czy nie zawierają danych wrażliwych.

Zatrzymanie:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Stop-Publicate.ps1
```

Zatrzymuje tylko procesy uruchomione przez launcher i ich procesy potomne, po sprawdzeniu PID oraz czasu startu. PostgreSQL pozostaje uruchomiony; procesy uruchomione ręcznie również. W przypadku błędu uruchamiania już uruchomione składniki pozostają dostępne; można zatrzymać je tym samym skryptem. Nie uruchamiaj dwóch launcherów jednocześnie.

Weryfikacja 2026-11-10: sprawdzono składnię PowerShell. Na wyraźne życzenie użytkownika nie uruchamiano ani nie zatrzymywano usług podczas przygotowywania skryptów. Pełny test start/stop pozostaje do wykonania przy następnym uruchomieniu aplikacji.
