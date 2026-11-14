# PUBLICATE przez ngrok

Adres testowy: https://cofounder-morse-reflex.ngrok-free.dev

Frontend działa na 5173 i przekazuje `/api` oraz `/media` do backendu 5080. Tunel uruchamiaj na 5173. Zaloguj się do PUBLICATE pod publicznym adresem; sesja localhost nie przechodzi do domeny ngrok.

## Przygotowana konfiguracja

- Vite dopuszcza dokładny host ngrok, bez `allowedHosts=true`.
- Lokalny backend ma `Local:Origin` i `OAuth:CallbackBase` ustawione na publiczny HTTPS, a host jest dopisany do `AllowedHosts`.
- Ciasteczka sesji i OAuth są HttpOnly, Secure i SameSite=Lax. `state` jest jednorazowy, związany z użytkownikiem i przeglądarką.
- `Local:AllowRegistration=false` blokuje nowe konta; istniejące konta nadal mogą się logować. Włączenie rejestracji wymaga świadomej zmiany lokalnej konfiguracji.
- Backend używa jawnie skonfigurowanego origin; nie ufa dowolnym nagłówkom X-Forwarded-Host/Proto. Adres klienta nie jest odtwarzany z proxy, więc limity żądań są współdzielone w tym teście.
- Nie ustawiono publicznego magazynu zdjęć. Test OAuth nie udostępnia multimediów.

## Autoryzacja ngrok i start

Zainstalowano ngrok z Microsoft Store. Pobierany wcześniej ZIP został zablokowany przez ochronę Windows; nie wyłączano ochrony ani nie dodawano wyjątków.

W swoim PowerShellu skonfiguruj token z https://dashboard.ngrok.com/get-started/your-authtoken:

```powershell
ngrok config add-authtoken TWOJ_TOKEN
```

Nie wysyłaj tokenu do rozmowy ani nie zapisuj w repozytorium. Następnie uruchom backend i frontend zwykłymi skryptami, jeśli nie działają, oraz:

```powershell
ngrok http 5173 --url https://cofounder-morse-reflex.ngrok-free.dev
```

Alternatywa: `powershell -ExecutionPolicy Bypass -File scripts/Start-Tunnel.ps1`. Pozostaw proces działający. Ctrl+C w jego terminalu zamyka publiczny tunel. Jeżeli uruchomiono go w tle, znajdź właściwy proces przez `Get-Process ngrok` i zatrzymaj jego PID przez `Stop-Process -Id PID`.

Otwórz domenę, zaakceptuj ewentualny ekran ngrok, zaloguj się istniejącym kontem PUBLICATE. Callback Instagram w panelu Meta:

```text
https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/Instagram/callback
```

Facebook, gdy będzie konfigurowany osobno:

```text
https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/Facebook/callback
```

## Wynik przygotowania 2026-11-05

96 testów backendu oraz build frontendu przeszły. Lokalny frontend i `/api/health` odpowiadają 200 z publicznym nagłówkiem Host. Próba rejestracji z prawidłowym Origin i nagłówkiem CSRF zwraca 403. Test publicznego adresu zależy jeszcze od lokalnej autoryzacji agenta ngrok; nie potwierdzono działania tunelu ani OAuth dostawcy. Nie publikowano materiałów.

Powrót do samego localhost: w `appsettings.Local.json` ustaw `Local:Origin=http://localhost:5173`, `OAuth:CallbackBase=http://localhost:5080`, opcjonalnie `Local:AllowRegistration=true` i zrestartuj backend. Wtedy publiczny tunel powinien być zatrzymany.
