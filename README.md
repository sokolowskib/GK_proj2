# Projekt 2 „Powierzchnie Béziera”

Program wyświetla powierzchnie Béziera 3. stopnia wczytane z pliku. Każdą powierzchnię dzieli na trójkąty, które wypełnia własnym algorytmem (z sortowaniem wierzchołków) i cieniuje piksel po pikselu modelem Lamberta ze składową zwierciadlaną. Scenę obraca się myszą, a źródło światła porusza się po zamkniętej krzywej Béziera. Opcjonalnie kolor obiektu pochodzi z tekstury, a wektor normalny jest modyfikowany mapą normalnych.

Cały obraz sceny liczony jest na CPU do własnego bufora pikseli, a biblioteka graficzna tylko wysyła gotowy bufor na ekran. Biblioteczne są jedynie linie pomocnicze: wielobok kontrolny, siatka trójkątów i krzywa światła.

Dokumentacja jest dostępna w programie pod klawiszem F1.

## Uruchomienie

Program kompilujemy i uruchamiamy poleceniem:

```
dotnet run -c Release --project src/Bezier3D.App
```

Konfiguracja `Release` jest ważna – w `Debug` program działa kilkukrotnie wolniej (ok. 15 zamiast ok. 100 klatek na sekundę). Testy jednostkowe uruchamiamy poleceniem `dotnet test`.

### Plik wejściowy

Przy starcie program wczytuje plik `Assets/surfaces.txt` z katalogu aplikacji. Każda powierzchnia to 16 kolejnych wierszy, po jednym dla każdego punktu kontrolnego, a wiersz zawiera trzy liczby `x y z` (z kropką dziesiętną). Liczba powierzchni to liczba wierszy podzielona przez 16. Jeśli plik nie istnieje albo jest niepoprawny, program wypisuje komunikat i kończy działanie.

Dołączony plik zawiera dwie powierzchnie, które są funkcjami nad płaszczyzną xy. Punkty kontrolne leżą w sześcianie [−200, 200]³ o środku w (0, 0, 0). Przekątna sześcianu to ok. 346 pikseli, a połowa płótna to 400, więc scena mieści się na ekranie przy każdym obrocie.

### Domyślne pliki

- `Assets/texture.png` – domyślna tekstura obiektu.
- `Assets/Tiles101_1K-PNG_NormalDX.png` – domyślna mapa normalnych (płytki, ambientCG, licencja CC0).

## Obsługa

### Mysz

- LPM + przeciągnięcie na płótnie – obrót sceny (arcball, opis niżej).
- LPM + przeciągnięcie przy brzegu płótna lub poza kołem arcballa – obrót wokół osi patrzenia (osi z ekranu).
- LPM na punkcie krzywej światła + przeciągnięcie – przesunięcie punktu kontrolnego krzywej (tylko przy włączonej „Edycji krzywej”).
- Przeciągnięcie pliku PNG na okno – wczytanie nowej tekstury albo mapy normalnych (zależnie od wyboru w panelu).

### Klawiatura

- F1 – okno pomocy (ta dokumentacja).
- Spacja – pauza / wznowienie ruchu światła.
- R – reset orientacji sceny.

### Panel boczny

Rysowanie:

- Wypełnienie, Wielobok Béziera, Siatka trójkątów – co jest rysowane.
- Dokładność n – każda powierzchnia dzielona jest na siatkę n × n kratek, czyli 2n² trójkątów (n od 1 do 40).

Oświetlenie:

- kd, ks (0–1), m (1–100) – współczynniki modelu oświetlenia.
- Kolor światła – IL, domyślnie biały.
- Kolor obiektu – jednolity (wybierany z menu) albo tekstura.
- Mapa normalnych – włączenie i wyłączenie modyfikacji wektora normalnego.

Ruch światła:

- Pauza (Spacja) – zatrzymanie animacji.
- z światła – płaszczyzna, na której leży krzywa (od −500 do 500). Ujemne wartości umieszczają światło za sceną.
- Edycja krzywej – pokazuje punkty kontrolne krzywej i pozwala je przeciągać.

Pliki:

- Przeciągnij PNG na okno jako: tekstura / mapa normalnych – czym ma być upuszczony plik. Po wczytaniu odpowiednia opcja włącza się sama. Niepoprawny plik (np. JPG, którego Raylib nie obsługuje) nie zamyka programu, tylko wyświetla komunikat.

## Obrót myszą (arcball)

Ruch myszy zamieniany jest na obrót tak, jakby za płótnem stała kula, którą chwytamy kursorem i obracamy. Punkt kuli pod kursorem podąża za kursorem, więc scena obraca się „za ręką”.

### Punkt myszy na kuli

Kula ma środek w środku płótna i promień r = 400 pikseli (połowa płótna). Dla pozycji myszy (mx, my) liczymy współrzędne względem środka (cx, cy), w jednostkach promienia:

```
x = (mx − cx) / r
y = (cy − my) / r
```

Oś y jest odwrócona, bo na ekranie rośnie w dół, a w scenie w górę.

1. Jeśli x² + y² ≤ 1, kursor jest nad kulą i punkt leży na jej przedniej półkuli: z = sqrt(1 − x² − y²).
2. W przeciwnym razie kursor jest poza kulą i bierzemy najbliższy punkt jej brzegu: (x, y, 0) znormalizowany do długości 1.

Wynik jest zawsze wektorem jednostkowym.

### Obrót z dwóch punktów

Przy wciśnięciu LPM zapamiętujemy punkt p0 na kuli. W każdej klatce przeciągania wyznaczamy punkt p1 dla bieżącej pozycji kursora i obrót, który przenosi p0 w p1:

- oś obrotu: p0 × p1 (prostopadła do obu punktów), znormalizowana,
- kąt obrotu: θ = acos(p0 · p1) (kąt między punktami, iloczyn skalarny obcięty do [−1, 1] przed acos).

Jeśli mysz się nie ruszyła (iloczyn wektorowy bliski zeru), przyrost to macierz jednostkowa.

Macierz obrotu o kąt θ wokół osi jednostkowej k liczona jest wzorem Rodriguesa:

```
Rprzyrost = I + sin θ · K + (1 − cos θ) · K²
```

gdzie K to macierz iloczynu wektorowego, czyli K · v = k × v:

```
K = [  0   −kz   ky ]
    [  kz   0   −kx ]
    [ −ky   kx   0  ]
```

### Składanie obrotów

Orientacja sceny to jedna macierz R (na początku jednostkowa). Każdy przyrost składany jest z lewej strony:

```
Rnowa = Rprzyrost · Raktualna
```

po czym p0 = p1. Mnożenie z lewej oznacza, że przyrost działa w układzie ekranu, a nie w układzie już obróconej sceny – ruch myszy w prawo zawsze obraca scenę w prawo, niezależnie od jej obecnej orientacji. Ponieważ obroty są składane narastająco, kąty nie są w żaden sposób ograniczone.

Ruch kursora po brzegu kuli (lub poza nią) daje punkty p0, p1 z z = 0. Ich iloczyn wektorowy jest równoległy do osi z, więc scena obraca się wtedy wokół osi patrzenia.

### Obracanie wierzchołków

Po każdej zmianie R wszystkie wierzchołki siatki są obracane od nowa, zawsze z wartości oryginalnych:

```
Pr = R · P,   Pur = R · Pu,   Pvr = R · Pv,   Nr = normalize(R · N)
```

Punkty obracane są wokół środka sceny (0, 0, 0), a wektory styczne i normalne jako wektory kierunkowe. Ponieważ obracamy zawsze oryginały, błędy zaokrągleń nie kumulują się przy kolejnych ruchach myszy.

Wszystkie wektory traktowane są jako kolumnowe, a macierz mnożona z lewej (M · v). `System.Numerics.Matrix4x4` używa wektorów wierszowych, dlatego program ma własną klasę `Mat3` w jednej, spójnej konwencji.

## Model danych

- **BezierSurface** – 16 punktów kontrolnych V[i, j]. Metoda `Evaluate(u, v)` zwraca punkt P, wektory styczne Pu, Pv i wektor normalny N.
- **MeshVertex** – parametry (u, v), wektory P, Pu, Pv, N przed obrotem oraz Pr, Pur, Pvr, Nr po obrocie.
- **Triangle** – trzy wierzchołki. Sąsiednie trójkąty wskazują na te same obiekty wierzchołków, więc każdy punkt siatki jest liczony i obracany raz.
- **Siatka** – lista trójkątów, jedna dla każdej powierzchni.
- **FrameBuffer** – kolor (RGBA w `uint`) i głębokość (`float`) każdego piksela płótna 800 × 800.
- **Texture** – obraz jako tablica kolorów (R, G, B) w zakresie 0–1. Używana zarówno dla tekstury, jak i dla mapy normalnych.
- **LightPath** – punkty kontrolne krzywej światła (x, y) i wysokość z.

## Algorytmy

### Powierzchnia i triangulacja

Punkt powierzchni to suma punktów kontrolnych z wagami Bernsteina 3. stopnia:

```
P(u, v) = Σi Σj V[i, j] · B3i(u) · B3j(v)
```

Wektory styczne to pochodne cząstkowe, liczone z różnic sąsiednich punktów kontrolnych i wielomianów Bernsteina 2. stopnia:

```
Pu = 3 · Σi Σj (V[i+1, j] − V[i, j]) · B2i(u) · B3j(v)
Pv = 3 · Σi Σj (V[i, j+1] − V[i, j]) · B3i(u) · B2j(v)
N  = normalize(Pu × Pv)
```

Triangulacja tworzy siatkę (n + 1) × (n + 1) wierzchołków dla u, v = 0, 1/n, …, 1 i dzieli każdą kratkę po przekątnej na dwa trójkąty.

### Rzut

Rzut jest prostokątny na płaszczyznę xy: współrzędna z jest pomijana. Początek układu leży w środku płótna, a oś y skierowana jest w górę: sx = 400 + x, sy = 400 − y.

### Wypełnianie trójkątów (sortowanie wierzchołków)

1. Wierzchołki sortujemy po y: a (góra), b (środek), c (dół).
2. Dla każdego wiersza pikseli między a i c bierzemy środek wiersza y + 0,5 i wyznaczamy dwa przecięcia: z krawędzią długą a–c oraz z krawędzią krótką (a–b nad b, b–c pod b).
3. Mniejsze przecięcie to lewy koniec odcinka, większe – prawy. Wypełniamy piksele, których środki leżą między nimi.

Lewa krawędź jest włączona, a prawa wyłączona (tak samo w pionie), dlatego dwa trójkąty ze wspólną krawędzią nie zostawiają między sobą dziur i nie rysują żadnego piksela dwa razy.

### Kolor piksela

Dla każdego piksela wewnątrz trójkąta liczymy współrzędne barycentryczne (α, β, γ) jego środka względem rzutowanych wierzchołków. Każda waga to stosunek pola trójkąta utworzonego przez piksel i dwa pozostałe wierzchołki do pola całego trójkąta (pola z iloczynu wektorowego 2D). Wagami tymi interpolujemy wartości z wierzchołków: z, N, P, u, v (oraz Pu, Pv przy mapie normalnych). Interpolowane wektory są normalizowane.

Kolor liczony jest wzorem:

```
I = kd · IL · IO · max(0, N · L) + ks · IL · IO · max(0, V · R)^m
```

gdzie L = normalize(Pświatła − P), V = [0, 0, 1], R = 2(N · L)N − L. Kolory są w zakresie 0–1, równanie liczone jest osobno dla R, G i B, a wynik obcinany do [0, 1] i mnożony przez 255.

IO to kolor jednolity albo kolor tekstury w punkcie (u, v). Teksturę próbkujemy najbliższym pikselem: x = u · szerokość, y = v · wysokość, obcięte do zakresu obrazu.

### Oświetlenie dwustronne

Wektor normalny powierzchni wskazuje zawsze jej „wierzch”. Gdy po obrocie widzimy spód (interpolowane N ma z < 0), przed liczeniem koloru zamieniamy N na −N. Dzięki temu oświetlana jest zawsze ta strona powierzchni, którą widzi obserwator.

### Bufor głębokości

Bufor przechowuje dla każdego piksela z najbliższego dotąd narysowanego punktu (na początku klatki −∞). Obserwator patrzy z kierunku +z, więc większe z oznacza bliżej. Piksel jest zapisywany tylko wtedy, gdy jego interpolowane z jest większe od zapisanego.

### Mapa normalnych

Kolor mapy w punkcie (u, v) zamieniany jest na wektor: Ntex = 2 · kolor − 1 (każda składowa z 0–1 do −1–1). Zmodyfikowany wektor normalny:

```
N = normalize(M · Ntex),   M = [Pu, Pv, N]
```

Kolumny M to znormalizowane, interpolowane wektory po obrocie. M · Ntex = Ntex.x · Pu + Ntex.y · Pv + Ntex.z · N, czyli wektor zapisany w mapie względem powierzchni (x wzdłuż u, y wzdłuż v, z na zewnątrz) przenoszony jest do układu sceny. Mapa w konwencji DirectX pasuje do próbkowania, w którym v rośnie w dół obrazu.

### Ruch światła

Światło porusza się po jednej zamkniętej krzywej Béziera leżącej na płaszczyźnie z = const. Domyślnie ma 7 punktów kontrolnych: 6 wierzchołków sześciokąta o promieniu 380 i punkt startowy w połowie między ostatnim a pierwszym. Zamknięcie jest automatyczne – jako ostatni punkt kontrolny dokładany jest pierwszy, więc przeciąganie punktów nie otwiera krzywej. Punkt startowy leży na prostej łączącej sąsiednie punkty, dlatego w miejscu zamknięcia krzywa nie ma „rogu”.

Punkt krzywej dla parametru t liczony jest algorytmem de Casteljau: w każdej rundzie zastępujemy każdą parę sąsiednich punktów punktem leżącym w ułamku t odcinka między nimi, aż zostanie jeden punkt. Parametr t rośnie z czasem (jedno okrążenie na 10 s) i po 1 wraca do 0.

## Założenia

- Światło porusza się w układzie obserwatora, czyli nie obraca się razem ze sceną. Obracając scenę, można więc oglądać różne strony oświetlonej powierzchni.
- Tekstura i mapa normalnych nakładane są na każdą powierzchnię osobno: (u, v) z [0, 1]² odpowiada całemu obrazowi.
- Wielobok kontrolny, siatka i krzywa światła rysowane są nad obrazem, bez bufora głębokości.
- Wczytywane obrazy muszą być w formacie PNG (Raylib nie obsługuje JPG).

## Środowisko

- C# / .NET 10
- Raylib-cs 8.1 – okno, wysłanie bufora pikseli na ekran (`UpdateTexture`), wczytywanie PNG
- ImGui.NET + rlImGui-cs – panel boczny i okno pomocy
- xUnit – testy jednostkowe części `Bezier3D.Core` (parser, powierzchnia, macierze, arcball, triangulacja, wypełnianie, współrzędne barycentryczne, oświetlenie, tekstura, mapa normalnych, krzywa światła)

`Bezier3D.Core` nie zależy od żadnej biblioteki graficznej – całe obliczenia działają na zwykłych tablicach i są testowalne bez okna.
