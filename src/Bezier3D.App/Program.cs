using System.Numerics;
using System.Runtime.InteropServices;
using Bezier3D.Core.Maths;
using Bezier3D.Core.Rendering;
using Bezier3D.Core.Surfaces;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

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

bool showControlNet = true;
bool showMesh = true;
bool showFill = true;

var frame = new FrameBuffer(CanvasSize, CanvasSize);
// tekstura w karcie graficznej, do której co klatkę wgrywamy frame.Color
var canvasTex = Raylib.LoadTextureFromImage(Raylib.GenImageColor(CanvasSize, CanvasSize, Color.White));
// bez oświetlenia jeden kolor zlałby obie powierzchnie w plamę — każda dostaje swój (dzień 4: światło)
uint[] surfaceColors = [FrameBuffer.Rgba(230, 120, 40), FrameBuffer.Rgba(40, 150, 160)];
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
        dragging = true;
        p0 = Arcball.MapToSphere(mouse, center, radius);
    }
    if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        dragging = false;

    if (dragging)
    {
        var p1 = Arcball.MapToSphere(mouse, center, radius);
        R = Arcball.Increment(p0, p1) * R; // przyrost z lewej: obrót w układzie ekranu
        p0 = p1;
        RotateAll();
    }

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
        for (int s = 0; s < meshes.Count; s++)
            foreach (var t in meshes[s])
                FillTriangle(frame, t, surfaceColors[s % surfaceColors.Length]); // % — więcej powierzchni niż kolorów
        Raylib.UpdateTexture(canvasTex, frame.Color);
        Raylib.DrawTexture(canvasTex, 0, 0, Color.White);
    }

    if (showMesh)
        foreach (var mesh in meshes)
            DrawMesh(mesh);

    if (showControlNet)
        foreach (var s in surfaces)
            DrawControlNet(s, R);

    rlImGui.Begin();
    ImGui.SetNextWindowPos(new Vector2(CanvasSize, 0));
    ImGui.SetNextWindowSize(new Vector2(PanelWidth, CanvasSize));
    ImGui.Begin("Bezier3D", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);
    ImGui.Text($"FPS: {Raylib.GetFPS()}");
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
    ImGui.End();
    rlImGui.End();

    Raylib.EndDrawing();
}

Raylib.UnloadTexture(canvasTex);
rlImGui.Shutdown();
Raylib.CloseWindow();
return 0;

// Rzut prostokątny: odrzucamy z; początek układu w środku płótna, oś y w górę.
static Vector2 ToScreen(Vector3 p) => new(CanvasSize / 2f + p.X, CanvasSize / 2f - p.Y);

static void FillTriangle(FrameBuffer frame, Triangle t, uint color)
{
    Vector2 a = ToScreen(t.A.Pr), b = ToScreen(t.B.Pr), c = ToScreen(t.C.Pr);
    TriangleFiller.Fill(a, b, c, (y, x0, x1) =>
    {
        for (int x = x0; x < x1; x++)
        {
            // wagi w środku piksela — ta sama reguła co w Fill
            var w = Barycentric.Compute(a, b, c, new Vector2(x + 0.5f, y + 0.5f));
            float z = Barycentric.Interpolate(w, t.A.Pr.Z, t.B.Pr.Z, t.C.Pr.Z);
            frame.SetPixel(x, y, z, color);
        }
    });
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

// Domyślna czcionka ImGui ma tylko ASCII — ładujemy TTF z polskimi literami (Latin Extended-A), greką i typografią.
static void LoadFont(string fileName, float size)
{
    ushort[] glyphRanges = [0x0020, 0x017F, 0x0370, 0x03FF, 0x2000, 0x206F, 0];
    // ImGui (C++) trzyma wskaźnik do tej tablicy, więc przypinamy ją na cały czas działania programu.
    var handle = GCHandle.Alloc(glyphRanges, GCHandleType.Pinned);
    string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", fileName);
    ImGui.GetIO().Fonts.AddFontFromFileTTF(path, size, null, handle.AddrOfPinnedObject());
}
