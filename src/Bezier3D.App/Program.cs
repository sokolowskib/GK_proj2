using System.Numerics;
using System.Runtime.InteropServices;
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

while (!Raylib.WindowShouldClose())
{
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.RayWhite);

    if (showControlNet)
        foreach (var s in surfaces)
            DrawControlNet(s);

    rlImGui.Begin();
    ImGui.SetNextWindowPos(new Vector2(CanvasSize, 0));
    ImGui.SetNextWindowSize(new Vector2(PanelWidth, CanvasSize));
    ImGui.Begin("Bezier3D", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);
    ImGui.Text($"FPS: {Raylib.GetFPS()}");
    ImGui.Text($"Powierzchnie: {surfaces.Count}");
    ImGui.SeparatorText("Rysowanie");
    ImGui.Checkbox("Wielobok Béziera", ref showControlNet);
    ImGui.End();
    rlImGui.End();

    Raylib.EndDrawing();
}

rlImGui.Shutdown();
Raylib.CloseWindow();
return 0;

// Rzut prostokątny: odrzucamy z; początek układu w środku płótna, oś y w górę.
static Vector2 ToScreen(Vector3 p) => new(CanvasSize / 2f + p.X, CanvasSize / 2f - p.Y);

static void DrawControlNet(BezierSurface s)
{
    for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
        {
            var p = ToScreen(s.V[i, j]);
            if (i < 3)
                Raylib.DrawLineV(p, ToScreen(s.V[i + 1, j]), Color.DarkGray);
            if (j < 3)
                Raylib.DrawLineV(p, ToScreen(s.V[i, j + 1]), Color.DarkGray);
            Raylib.DrawCircleV(p, 4, Color.DarkGreen);
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
