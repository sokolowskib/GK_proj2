using System.Numerics;
using System.Runtime.InteropServices;
using Bezier3D.Core.Lighting;
using Bezier3D.Core.Maths;
using Bezier3D.Core.Rendering;
using Bezier3D.App.UI;
using Bezier3D.Core.Surfaces;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
// Raylib_cs ma własne Shader i Texture — bierzemy nasze
using Shader = Bezier3D.Core.Rendering.Shader;
using Texture = Bezier3D.Core.Rendering.Texture;

const int CanvasSize = 800;
const int PanelWidth = 300;

string surfaces_path = Path.Combine(AppContext.BaseDirectory, "Assets", "surfaces.txt");
List<BezierSurface> surfaces;
try
{
    surfaces = SurfaceFileParser.Parse(File.ReadAllText(surfaces_path));
}
catch (Exception e) when (e is FormatException or IOException)
{
    Console.Error.WriteLine($"Nie udało się wczytać pliku: {surfaces_path}: {e.Message}");
    return 1;
}

Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
Raylib.InitWindow(CanvasSize + PanelWidth, CanvasSize, "Bezier3D");
rlImGui.BeginInitImGui();
ImGui.StyleColorsDark();
LoadFont("JetBrainsMono-Regular.ttf", 16f);
rlImGui.EndInitImGui();

var help = new HelpWindow();
bool showControlNet = true;
bool showMesh = true;
bool showFill = true;

var frame = new FrameBuffer(CanvasSize, CanvasSize);
// tekstura w karcie graficznej, do której co klatkę wgrywamy frame.Color
var canvasTex = Raylib.LoadTextureFromImage(Raylib.GenImageColor(CanvasSize, CanvasSize, Color.White));
// oświetlenie — kolory jako (R, G, B) w zakresie 0–1
float kd = 0.5f, ks = 0.5f;
int m = 10;
var lightColor = Vector3.One;               // domyślnie białe
var objectColor = new Vector3(1f, 0.55f, 0.2f);
bool useTexture = false;
var lightPath = DefaultLightPath();
float lightT = 0;                           // parametr krzywej, rośnie z czasem i zawija się po 1
const float LightSpeed = 0.1f;              // okrążenie co 10 s
bool lightPaused = false;
bool editPath = false;
int draggedPoint = -1;                      // indeks przeciąganego punktu krzywej, −1 = żaden
var lightPos = lightPath.At(0);
var texture = LoadTexture(Path.Combine(AppContext.BaseDirectory, "Assets", "texture.png"));
bool useNormalMap = false;
var normalMap = LoadTexture(Path.Combine(AppContext.BaseDirectory, "Assets", "Tiles101_1K-PNG_NormalDX.png"));
bool dropAsNormalMap = false;               // czym jest plik przeciągnięty na okno
string dropMessage = "";
int n = 10;
var meshes = surfaces.Select(s => Triangulator.Triangulate(s, n)).ToList();

var R = Mat3.Identity;     // cała orientacja sceny
bool dragging = false;     // czy trzymamy LPM na płótnie
Vector3 p0 = default;      // punkt na kuli z poprzedniej klatki
var center = new Vector2(CanvasSize / 2f);
float radius = CanvasSize / 2f;

// Wierzchołki współdzielone obracają się kilka razy, ale Rotate liczy od P, więc wynik ten sam.
void RotateAll()
{
    foreach (var mesh in meshes)
        foreach (var t in mesh)
        {
            t.A.Rotate(R); t.B.Rotate(R); t.C.Rotate(R);
        }
}

RotateAll();

while (!Raylib.WindowShouldClose())
{
    var mouse = Raylib.GetMousePosition();
    // tylko kliknięcie na płótnie — przesuwanie suwaka w panelu nie obraca sceny
    if (Raylib.IsMouseButtonPressed(MouseButton.Left) && mouse.X < CanvasSize)
    {
        // w trybie edycji kliknięcie blisko punktu krzywej łapie punkt; w innym miejscu nadal obracamy scenę
        draggedPoint = editPath
            ? lightPath.Points.FindIndex(q => Vector2.Distance(ToScreen(new Vector3(q, 0)), mouse) < 10)
            : -1;
        dragging = draggedPoint < 0;
        p0 = Arcball.MapToSphere(mouse, center, radius);
    }
    if (Raylib.IsMouseButtonReleased(MouseButton.Left))
    {
        dragging = false;
        draggedPoint = -1;
    }

    if (draggedPoint >= 0)
        lightPath.Points[draggedPoint] = new(mouse.X - CanvasSize / 2f, CanvasSize / 2f - mouse.Y); // odwrotność ToScreen

    if (dragging)
    {
        var p1 = Arcball.MapToSphere(mouse, center, radius);
        R = Arcball.Increment(p0, p1) * R; // przyrost z lewej: obrót w układzie ekranu
        p0 = p1;
        RotateAll();
    }

    if (Raylib.IsFileDropped())
    {
        string path = Raylib.GetDroppedFiles()[0];
        try
        {
            var loaded = LoadTexture(path);
            // od razu włączamy to, co podmieniliśmy — widać efekt bez szukania checkboxa
            if (dropAsNormalMap)
                (normalMap, useNormalMap) = (loaded, true);
            else
                (texture, useTexture) = (loaded, true);
            dropMessage = $"Wczytano: {Path.GetFileName(path)}";
        }
        catch (IOException e)
        {
            dropMessage = e.Message; // zły plik nie zamyka programu
        }
    }

    if (Raylib.IsKeyPressed(KeyboardKey.Space))
        lightPaused = !lightPaused;
    if (!lightPaused)
        lightT = (lightT + Raylib.GetFrameTime() * LightSpeed) % 1;
    lightPos = lightPath.At(lightT);

    if (Raylib.IsKeyPressed(KeyboardKey.R))
    {
        R = Mat3.Identity;
        RotateAll();
    }

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.RayWhite);

    if (showFill)
    {
        frame.Clear(FrameBuffer.Rgba(245, 245, 245));
        foreach (var mesh in meshes)
            foreach (var t in mesh)
                FillTriangle(t);
        Raylib.UpdateTexture(canvasTex, frame.Color);
        Raylib.DrawTexture(canvasTex, 0, 0, Color.White);
    }

    if (showMesh)
        foreach (var mesh in meshes)
            DrawMesh(mesh);

    if (showControlNet)
        foreach (var s in surfaces)
            DrawControlNet(s, R);

    DrawLightPath(lightPath, lightPos, editPath);

    rlImGui.Begin();
    ImGui.SetNextWindowPos(new Vector2(CanvasSize, 0));
    ImGui.SetNextWindowSize(new Vector2(PanelWidth, CanvasSize));
    ImGui.Begin("Bezier3D", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);
    ImGui.Text($"FPS: {Raylib.GetFPS()}");
    if (ImGui.Button("Pomoc (F1)"))
        help.Toggle();
    ImGui.Text($"Powierzchnie: {surfaces.Count}");
    ImGui.SeparatorText("Rysowanie");
    ImGui.Checkbox("Wypełnienie", ref showFill);
    ImGui.Checkbox("Wielobok Béziera", ref showControlNet);
    ImGui.Checkbox("Siatka trójkątów", ref showMesh);
    // SliderInt zwraca true tylko w klatce, w której wartość się zmieniła
    if (ImGui.SliderInt("Dokładność n", ref n, 1, 40))
    {
        meshes = surfaces.Select(s => Triangulator.Triangulate(s, n)).ToList();
        RotateAll(); // nowe wierzchołki mają puste Pr
    }
    ImGui.SeparatorText("Oświetlenie");
    ImGui.SliderFloat("kd", ref kd, 0, 1);
    ImGui.SliderFloat("ks", ref ks, 0, 1);
    ImGui.SliderInt("m", ref m, 1, 100);
    ImGui.ColorEdit3("Kolor światła", ref lightColor);
    ImGui.Text("Kolor obiektu:");
    if (ImGui.RadioButton("Jednolity", !useTexture))
        useTexture = false;
    ImGui.SameLine();
    if (ImGui.RadioButton("Tekstura", useTexture))
        useTexture = true;
    if (!useTexture)
        ImGui.ColorEdit3("Kolor obiektu", ref objectColor);
    ImGui.Checkbox("Mapa normalnych", ref useNormalMap);
    ImGui.SeparatorText("Pliki");
    ImGui.Text("Przeciągnij PNG na okno jako:");
    if (ImGui.RadioButton("tekstura", !dropAsNormalMap))
        dropAsNormalMap = false;
    ImGui.SameLine();
    if (ImGui.RadioButton("mapa normalnych", dropAsNormalMap))
        dropAsNormalMap = true;
    if (dropMessage != "")
        ImGui.TextWrapped(dropMessage);
    ImGui.SeparatorText("Ruch światła");
    ImGui.Checkbox("Pauza (Spacja)", ref lightPaused);
    // ujemne z: światło za sceną, oświetla spód powierzchni
    ImGui.SliderFloat("z światła", ref lightPath.Z, -500, 500);
    ImGui.Checkbox("Edycja krzywej", ref editPath);
    ImGui.End();
    help.Draw();
    rlImGui.End();

    Raylib.EndDrawing();
}

Raylib.UnloadTexture(canvasTex);
rlImGui.Shutdown();
Raylib.CloseWindow();
return 0;

// Rzut prostokątny: odrzucamy z; początek układu w środku płótna, oś y w górę.
static Vector2 ToScreen(Vector3 p) => new(CanvasSize / 2f + p.X, CanvasSize / 2f - p.Y);

// nie static: korzysta z parametrów światła i koloru obiektu z panelu
void FillTriangle(Triangle t)
{
    Vector2 a = ToScreen(t.A.Pr), b = ToScreen(t.B.Pr), c = ToScreen(t.C.Pr);
    TriangleFiller.Fill(a, b, c, (y, x0, x1) =>
    {
        for (int x = x0; x < x1; x++)
        {
            // wagi w środku piksela — ta sama reguła co w Fill
            var w = Barycentric.Compute(a, b, c, new Vector2(x + 0.5f, y + 0.5f));
            float z = Barycentric.Interpolate(w, t.A.Pr.Z, t.B.Pr.Z, t.C.Pr.Z);
            // średnia wektorów jednostkowych jest krótsza niż 1 → normalizacja po interpolacji
            var N = Vector3.Normalize(Barycentric.Interpolate(w, t.A.Nr, t.B.Nr, t.C.Nr));
            // oświetlenie dwustronne: widzimy spód (N ucieka od obserwatora) → normalna spodu to −N
            if (N.Z < 0)
                N = -N;
            var P = Barycentric.Interpolate(w, t.A.Pr, t.B.Pr, t.C.Pr);
            // u, v liczone raz — potrzebne i teksturze, i mapie normalnych
            float u = Barycentric.Interpolate(w, t.A.U, t.B.U, t.C.U);
            float v = Barycentric.Interpolate(w, t.A.V, t.B.V, t.C.V);
            if (useNormalMap)
            {
                // kolumny M = [Pu, Pv, N] — po obrocie, zinterpolowane i znormalizowane
                var Pu = Vector3.Normalize(Barycentric.Interpolate(w, t.A.Pur, t.B.Pur, t.C.Pur));
                var Pv = Vector3.Normalize(Barycentric.Interpolate(w, t.A.Pvr, t.B.Pvr, t.C.Pvr));
                N = NormalMap.Apply(N, Pu, Pv, normalMap.Sample(u, v));
            }
            var IO = useTexture ? texture.Sample(u, v) : objectColor;
            var I = Shader.Shade(N, P, lightPos, lightColor, IO, kd, ks, m);
            frame.SetPixel(x, y, z, FrameBuffer.Rgba((byte)(I.X * 255), (byte)(I.Y * 255), (byte)(I.Z * 255)));
        }
    });
}

// Obraz z pliku → Texture z kolorami 0–1. GetImageColor po pikselu: wolniej, ale bez unsafe, a wczytujemy raz.
static Texture LoadTexture(string path)
{
    var img = Raylib.LoadImage(path);
    // Raylib czyta PNG (BMP, TGA), ale nie JPG — przy złym pliku zwraca pusty obraz
    if (!Raylib.IsImageValid(img))
        throw new IOException($"Nie udało się wczytać obrazu: {path}");
    var pixels = new Vector3[img.Width * img.Height];
    for (int y = 0; y < img.Height; y++)
        for (int x = 0; x < img.Width; x++)
        {
            var c = Raylib.GetImageColor(img, x, y);
            pixels[y * img.Width + x] = new Vector3(c.R, c.G, c.B) / 255f;
        }
    Raylib.UnloadImage(img);
    return new(img.Width, img.Height, pixels);
}

static void DrawControlNet(BezierSurface s, Mat3 r)
{
    for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
        {
            var p = ToScreen(r * s.V[i, j]);
            if (i < 3)
                Raylib.DrawLineV(p, ToScreen(r * s.V[i + 1, j]), Color.DarkGray);
            if (j < 3)
                Raylib.DrawLineV(p, ToScreen(r * s.V[i, j + 1]), Color.DarkGray);
            Raylib.DrawCircleV(p, 4, Color.DarkGreen);
        }
}

// Domyślny tor: 6 punktów na okręgu + punkt startowy w połowie między ostatnim a pierwszym.
// Ostatni, start i pierwszy leżą na jednej prostej → brak „rogu” w miejscu zamknięcia.
static LightPath DefaultLightPath()
{
    const int Count = 6;
    const float Radius = 380; // krzywa wychodzi wyraźnie mniejsza niż wielokąt kontrolny (~140–330)
    var ring = Enumerable.Range(0, Count)
                         .Select(i => Radius * new Vector2(MathF.Cos(MathF.Tau * i / Count), MathF.Sin(MathF.Tau * i / Count)))
                         .ToList();
    return new([(ring[^1] + ring[0]) / 2, .. ring], 400);
}

static void DrawLightPath(LightPath path, Vector3 light, bool editing)
{
    const int Segments = 100;
    for (int i = 0; i < Segments; i++)
        Raylib.DrawLineV(ToScreen(path.At(i / (float)Segments)), ToScreen(path.At((i + 1) / (float)Segments)), Color.Orange);

    if (editing)
        for (int i = 0; i < path.Points.Count; i++)
        {
            var q = ToScreen(new Vector3(path.Points[i], 0));
            Raylib.DrawLineV(q, ToScreen(new Vector3(path.Points[(i + 1) % path.Points.Count], 0)), Color.Gray);
            Raylib.DrawCircleV(q, 6, Color.Maroon);
        }

    var l = ToScreen(light);
    Raylib.DrawCircleV(l, 9, Color.Black);
    Raylib.DrawCircleV(l, 7, Color.Yellow);
}

static void DrawMesh(List<Triangle> mesh)
{
    foreach (var t in mesh)
    {
        Vector2 a = ToScreen(t.A.Pr), b = ToScreen(t.B.Pr), c = ToScreen(t.C.Pr);
        Raylib.DrawLineV(a, b, Color.SkyBlue);
        Raylib.DrawLineV(b, c, Color.SkyBlue);
        Raylib.DrawLineV(c, a, Color.SkyBlue);
    }
}

// Domyślna czcionka ImGui ma tylko ASCII — ładujemy TTF z polskimi literami (Latin Extended-A), greką, typografią, strzałkami i symbolami matematycznymi.
static void LoadFont(string fileName, float size)
{
    ushort[] glyphRanges = [0x0020, 0x017F, 0x0370, 0x03FF, 0x2000, 0x206F, 0x2190, 0x22FF, 0];
    // ImGui (C++) trzyma wskaźnik do tej tablicy, więc przypinamy ją na cały czas działania programu.
    var handle = GCHandle.Alloc(glyphRanges, GCHandleType.Pinned);
    string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", fileName);
    ImGui.GetIO().Fonts.AddFontFromFileTTF(path, size, null, handle.AddrOfPinnedObject());
}
