# Wybór strony Facebooka

W panelu Połączone konta, pod kontem Facebooka, wybierz „Strona do publikacji”. Wybór zapisuje się od razu w bazie danych dla zalogowanego użytkownika PUBLICATE. Jedna domyślna strona Facebooka może pochodzić z dowolnego z jego podłączonych kont.

Tester i kreator pobierają ten wybór automatycznie i pokazują nazwę strony. Zmiana celu odbywa się w panelu połączeń. Brak wyboru lub utrata dostępu nie powoduje automatycznego wybrania innej strony.

Istniejące warianty zachowują swoje TargetId. Przed publikacją okno potwierdzenia zawsze pokazuje rzeczywisty cel konkretnego wariantu. Zmiana domyślnej strony nie przenosi istniejących szkiców ani historii.

Backend sprawdza właściciela celu, platformę Facebook, stan połączenia i obecność tokenu strony. Preferencja znajduje się w Users.PreferredFacebookTargetId; migracja PreferredFacebookTarget dodaje nullable uuid bez zmiany istniejących publikacji.

Weryfikacja 2026-11-10: frontend TypeScript/Vite zbudowany; 125/125 testów backendu, w tym 5 przypadków zapisu preferencji oraz odrzucenia cudzego celu, odłączonego konta, brakującego tokenu i innej platformy. Migracja zastosowana lokalnie. Nie wykonano publikacji ani restartu działających usług. Po aktualizacji wymagany restart backendu. Ostrzeżenie NU1900: niedostępna baza podatności NuGet.
