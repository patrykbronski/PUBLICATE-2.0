# X — zakres publikowania PUBLICATE, 12.11.2026

## Wdrożone

- OAuth 2.0 confidential Web App, state/PKCE S256, profil i istniejący refresh. Zakresy: `tweet.read tweet.write users.read offline.access media.write`. Konto autoryzowane wcześniej należy połączyć ponownie dla `media.write`. Brak tego zakresu blokuje media przed uploadem, ale nie tekst.
- Tekst, polskie znaki, emoji i URL: oficjalny `twitter-text` 3.1.0. Frontend i backend używają tego samego parsera (NFC, wagi Unicode, URL, sekwencje emoji), limit 280. CTA jest częścią tekstu.
- 1–4 JPEG/PNG, każde do 5 000 000 bajtów, z zachowaniem kolejności; także post bez opisu.
- Jeden MP4 bez zdjęć, 0,5–1200 s. Limit PUBLICATE 64 MiB; 20 min to zakres zwykłego konta w aktualnej dokumentacji, nie dawny limit 140 s. Uprawnień Premium do dłuższych filmów nie wykrywamy. Audio AAC jest dotychczasowym ograniczeniem magazynu PUBLICATE.
- Upload v2: initialize JSON, append multipart w porcjach 4 MiB, finalize, status z check_after_secs. Bez publicznego HTTPS materiału i bez zmian pliku.
- Post powstaje dopiero po przygotowaniu mediów. Potwierdzenie wymaga HTTP 201 i prawidłowego data.id; zapisywany jest link `https://x.com/i/status/{id}`. Brak ID/niepewna odpowiedź to Unknown.
- Oczekiwanie na pojedynczy materiał do 120 s / 12 odczytów, respektuje wskazany czas. Po wyczerpaniu: Processing, bez wysłania posta. Sprawdzenie statusu nie publikuje; gotowy materiał umożliwia ponowne świadome zatwierdzenie z nowym uploadem. To może ponownie kosztować.
- Zapis granicy `x:posting` przed POST chroni przed ponowieniem po niepewnym wyniku. Odczyt statusu materiału nie odblokowuje takiej próby. Błędy przed POST można bezpiecznie ponowić w sensie braku duplikatu posta; nie oznacza to braku kosztu uploadu.
- Bezpieczna diagnostyka 400/401/402/403/429/5xx, bez kopiowania treści odpowiedzi zawierających dane użytkownika.

## Test lokalny a rzeczywisty

256 testów backendu PASSED; build i testy UI kreatora, X, YouTube i TikTok PASSED. Wszystkie te testy używają atrap HTTP. Upload zdjęć/wideo i połączenie X pozostają **NOT_TESTED live**. Nie wydawano kredytów w ramach implementacji.

Użytkownik potwierdził doładowanie i zaakceptował testy w budżecie 5 USD. AllowPaidCalls=true w konfiguracji lokalnej. Flaga nie stanowi limitu finansowego — Spend Cap ustawiany jest w konsoli X. Upload, status i publikacja mogą generować osobne koszty. Post z URL ma inną stawkę; sprawdzaj bieżący cennik. Nie ma automatycznych doładowań ani ponowień POST.

## Ręczna procedura

1. Użyj istniejącej Web App; callback `https://cofounder-morse-reflex.ngrok-free.dev/api/oauth/X/callback`. Klucze już zapisano lokalnie. Ponowne użycie Configure-X.ps1 wyłączy AllowPaidCalls — to zamierzone zabezpieczenie skryptu.
2. Po restarcie backendu odśwież PUBLICATE na domenie ngrok. Kliknij Połącz konto X i zatwierdź także przesyłanie mediów. Wybierz cel w Połączonych kontach.
3. Pierwszy test: wyłącznie wariant X, jedno niewielkie JPEG/PNG, opis np. „Test PUBLICATE — zażółć gęślą jaźń 😀”. Potwierdź w UI rzeczywistą publikację i koszt. Sprawdź link i post w X.
4. Kolejno sprawdź 4 zdjęcia z kolejnością oraz krótki MP4. Nie zaznaczaj pozostałych platform, jeśli nie chcesz powtarzać publikacji także tam.
5. Processing: użyj Sprawdź status. Unknown: sprawdź konto i diagnostykę; nie twórz kopii tego samego posta dla obejścia blokady.

## Ograniczenia i decyzje

GIF/WEBP, mieszane media, długie posty Premium, ankiety, wątki, edycja/usuwanie i wiadomości prywatne nie należą do wdrożonego zakresu kreatora. Nie deklarujemy obsługi całego API X. Nie dodano zdalnego revoke; odłączenie usuwa lokalne tokeny.

Dokumentacja video zawiera sprzeczne zalecenia i granice wymiarów (m.in. pionowe 720×1280 kontra tabela 1280×1024). Nie wprowadzamy na tej podstawie arbitralnej blokady pionowego filmu. Nie ma pełnej walidacji FPS, pixel format, GOP i profilu audio przed uploadem; wynik przetwarzania X jest rozstrzygający. Rozmiar, liczba, kontener, wymiary dodatnie i czas są sprawdzane. Nie ograniczamy kodeka do H.264 na podstawie samego zalecenia. Bez transkodowania.

Parser backendu: Jint 4.17.0 wykonuje wyłącznie wbudowany skrypt, bez CLR/HTTP/plików i bez eval danych użytkownika; tekst przekazywany jako argument. Jedna synchronizowana instancja, limity czasu/pamięci i wejścia. Bundle generuje `node scripts/build-x-text.cjs` po `npm ci` w frontendzie, jest zasobem assembly (runtime nie wymaga Node). Licencja twitter-text w Vendor. Pakiet upstream zależy od przestarzałego core-js 2; npm audit podczas instalacji nie zgłosił podatności, co nie gwarantuje ich braku. Aktualizacje parsera należy testować po obu stronach.

## Oficjalne źródła (sprawdzone 12.11.2026)

- https://docs.x.com/x-api/posts/manage-tweets/introduction
- https://docs.x.com/x-api/media/quickstart/media-upload-chunked
- https://docs.x.com/x-api/media/initialize-media-upload
- https://docs.x.com/x-api/media/append-media-upload
- https://docs.x.com/x-api/media/get-media-upload-status
- https://docs.x.com/x-api/media/quickstart/best-practices
- https://docs.x.com/fundamentals/counting-characters
- https://docs.x.com/x-api/getting-started/pricing
