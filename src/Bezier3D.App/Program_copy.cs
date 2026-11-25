using System.Numerics;
using System.Runtime.InteropServices;
using Bezier3D.App.UI;
using Bezier3D.Core.Lighting;
using Bezier3D.Core.Maths;
using Bezier3D.Core.Rendering;
using Bezier3D.Core.Surfaces;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using Shader = Bezier3D.Core.Rendering.Shader;
using Texture = Bezier3D.Core.Rendering.Texture;

const int CanvasSize = 800;
const int PanelWidth = 300;
const float LightSpeed = 0.1f;

List<BezierSurface> surfaces;
try
{
    surfaces = SurfaceFileParser.Parse(File.ReadAllText(Asset("surfaces.txt")));
}
catch (Exception e) when (e is FormatException or IOException)
{
    Console.Error.WriteLine($"Nie udało się wczytać pliku: {Asset("surfaces.txt")}: {e.Message}");
    return 1;
}

Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
Raylib.InitWindow(CanvasSize + PanelWidth, CanvasSize, "Bezier3D");
rlImGui.BeginInitImGui();
ImGui.StyleColorsDark();
LoadFont(Asset("Fonts", "JetBrainsMono-Regular.ttf"), 16f);
rlImGui.EndInitImGui();

var help = new HelpWindow();
var frame = new FrameBuffer(CanvasSize, CanvasSize);
var canvasTex = Raylib.LoadTextureFromImage(Raylib.GenImageColor(CanvasSize, CanvasSize, Color.White));

bool showFill = true, showMesh = true, showControlNet = true;
int n = 10;
var meshes = Triangulate();

var R = Mat3.Identity;
bool dragging = false;
Vector3 p0 = default;
var center = new Vector2(CanvasSize / 2f);
float radius = CanvasSize / 2f;

float kd = 0.5f, ks = 0.5f;
int m = 10;
var lightColor = Vector3.One;
var lightType = LightType.Point;
string[] lightTypeNames = ["Kierunkowe", "Punktowe", "Reflektor"];
int lightCount = 1, spotM = 20;
var lightPath = DefaultLightPath();
float lightT = 0;
bool lightPaused = false, editPath = false;
int draggedPoint = -1;
Vector3[] lightPositions = [];

var defaultTexture = LoadTexture(Asset("texture.png"));
var looks = surfaces.Select(_ => new SurfaceLook { Color = new(1f, 0.55f, 0.2f), Texture = defaultTexture }).ToList();
string[] surfaceNames = surfaces.Select((_, i) => $"Powierzchnia {i}").ToArray();
int selected = 0;

var normalSource = NormalSource.Geometry;
var normalMap = LoadTexture(Asset("Tiles101_1K-PNG_NormalDX.png"));
var heightMap = LoadTexture(Asset("Tiles101_1K-PNG_Displacement.png"));
float heightK = 20;

var dropAs = DropTarget.Texture;
string dropMessage = "";

RotateAll();

while (!Raylib.WindowShouldClose())
{
    HandleMouse();
    HandleDroppedFile();
    HandleKeys();
    UpdateLights();

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.RayWhite);
    DrawScene();
    DrawPanel();
    Raylib.EndDrawing();
}

Raylib.UnloadTexture(canvasTex);
rlImGui.Shutdown();
Raylib.CloseWindow();
return 0;

// ---------- stan sceny ----------

List<List<Triangle>> Triangulate() => surfaces.Select(s => Triangulator.Triangulate(s, n)).ToList();

void RotateAll()
{
    foreach (var t in meshes.SelectMany(mesh => mesh))
    {
        t.A.Rotate(R);
        t.B.Rotate(R);
        t.C.Rotate(R);
    }
}

void UpdateLights()
{
    if (!lightPaused)
        lightT = (lightT + Raylib.GetFrameTime() * LightSpeed) % 1;
    lightPositions = Enumerable.Range(0, lightCount)
                               .Select(k => lightPath.At((lightT + k / (float)lightCount) % 1))
                               .ToArray();
}

// ---------- wejście ----------

void HandleMouse()
{
    var mouse = Raylib.GetMousePosition();
    if (Raylib.IsMouseButtonPressed(MouseButton.Left) && mouse.X < CanvasSize)
    {
        draggedPoint = editPath && lightType != LightType.Directional
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
        lightPath.Points[draggedPoint] = new(mouse.X - CanvasSize / 2f, CanvasSize / 2f - mouse.Y);

    if (dragging)
    {
        var p1 = Arcball.MapToSphere(mouse, center, radius);
        R = Arcball.Increment(p0, p1) * R;
        p0 = p1;
        RotateAll();
    }
}

void HandleDroppedFile()
{
    if (!Raylib.IsFileDropped())
        return;
    string path = Raylib.GetDroppedFiles()[0];
    try
    {
        var loaded = LoadTexture(path);
        switch (dropAs)
        {
            case DropTarget.NormalMap: (normalMap, normalSource) = (loaded, NormalSource.NormalMap); break;
            case DropTarget.HeightMap: (heightMap, normalSource) = (loaded, NormalSource.HeightMap); break;
            default: (looks[selected].Texture, looks[selected].UseTexture) = (loaded, true); break;
        }
        dropMessage = $"Wczytano: {Path.GetFileName(path)}";
    }
    catch (IOException e)
    {
        dropMessage = e.Message;
    }
}

void HandleKeys()
{
    if (Raylib.IsKeyPressed(KeyboardKey.Space))
        lightPaused = !lightPaused;
    if (Raylib.IsKeyPressed(KeyboardKey.R))
    {
        R = Mat3.Identity;
        RotateAll();
    }
}

// ---------- rysowanie ----------

void DrawScene()
{
    if (showFill)
    {
        frame.Clear(FrameBuffer.Rgba(245, 245, 245));
        for (int i = 0; i < meshes.Count; i++)
            foreach (var t in meshes[i])
                FillTriangle(t, looks[i]);
        Raylib.UpdateTexture(canvasTex, frame.Color);
        Raylib.DrawTexture(canvasTex, 0, 0, Color.White);
    }
    if (showMesh)
        foreach (var mesh in meshes)
            DrawMesh(mesh);
    if (showControlNet)
        foreach (var s in surfaces)
            DrawControlNet(s, R);
    if (lightType != LightType.Directional)
        DrawLightPath(lightPath, lightPositions, editPath);
}

void FillTriangle(Triangle t, SurfaceLook look)
{
    Vector2 a = ToScreen(t.A.Pr), b = ToScreen(t.B.Pr), c = ToScreen(t.C.Pr);
    TriangleFiller.Fill(a, b, c, (y, x0, x1) =>
    {
        for (int x = x0; x < x1; x++)
        {
            var w = Barycentric.Compute(a, b, c, new Vector2(x + 0.5f, y + 0.5f));
            float z = Barycentric.Interpolate(w, t.A.Pr.Z, t.B.Pr.Z, t.C.Pr.Z);
            var I = ShadePixel(t, w, look);
            frame.SetPixel(x, y, z, FrameBuffer.Rgba((byte)(I.X * 255), (byte)(I.Y * 255), (byte)(I.Z * 255)));
        }
    });
}

// ---------- cieniowanie ----------

Vector3 ShadePixel(Triangle t, Vector3 w, SurfaceLook look)
{
    var P = Barycentric.Interpolate(w, t.A.Pr, t.B.Pr, t.C.Pr);
    float u = Barycentric.Interpolate(w, t.A.U, t.B.U, t.C.U);
    float v = Barycentric.Interpolate(w, t.A.V, t.B.V, t.C.V);
    var N = SurfaceNormal(t, w, u, v);
    var IO = look.UseTexture ? look.Texture.Sample(u, v) : look.Color;
    return Vector3.Min(Illuminate(N, P, IO), Vector3.One);
}

Vector3 SurfaceNormal(Triangle t, Vector3 w, float u, float v)
{
    var N = Vector3.Normalize(Barycentric.Interpolate(w, t.A.Nr, t.B.Nr, t.C.Nr));
    if (N.Z < 0)
        N = -N;
    if (normalSource == NormalSource.Geometry)
        return N;

    var Pu = Vector3.Normalize(Barycentric.Interpolate(w, t.A.Pur, t.B.Pur, t.C.Pur));
    var Pv = Vector3.Normalize(Barycentric.Interpolate(w, t.A.Pvr, t.B.Pvr, t.C.Pvr));
    return normalSource == NormalSource.NormalMap
        ? NormalMap.Apply(N, Pu, Pv, normalMap.Sample(u, v))
        : HeightMap.Apply(N, Pu, Pv, heightMap, u, v, heightK);
}

Vector3 Illuminate(Vector3 N, Vector3 P, Vector3 IO)
{
    if (lightType == LightType.Directional)
        return Shader.Shade(N, Vector3.UnitZ, lightColor, IO, kd, ks, m);

    var I = Vector3.Zero;
    foreach (var lightPos in lightPositions)
    {
        var IL = lightType == LightType.Spot ? lightColor * Shader.Spot(lightPos, P, spotM) : lightColor;
        I += Shader.Shade(N, P, lightPos, IL, IO, kd, ks, m);
    }
    return I;
}

// ---------- panel ----------

void DrawPanel()
{
    rlImGui.Begin();
    ImGui.SetNextWindowPos(new Vector2(CanvasSize, 0));
    ImGui.SetNextWindowSize(new Vector2(PanelWidth, CanvasSize));
    ImGui.Begin("Bezier3D", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

    ImGui.Text($"FPS: {Raylib.GetFPS()}");
    if (ImGui.Button("Pomoc (F1)"))
        help.Toggle();
    ImGui.Text($"Powierzchnie: {surfaces.Count}");

    DrawingSection();
    LightingSection();
    FilesSection();
    LightMotionSection();

    ImGui.End();
    help.Draw();
    rlImGui.End();
}

void DrawingSection()
{
    ImGui.SeparatorText("Rysowanie");
    ImGui.Checkbox("Wypełnienie", ref showFill);
    ImGui.Checkbox("Wielobok Béziera", ref showControlNet);
    ImGui.Checkbox("Siatka trójkątów", ref showMesh);
    if (ImGui.SliderInt("Dokładność n", ref n, 1, 40))
    {
        meshes = Triangulate();
        RotateAll();
    }
}

void LightingSection()
{
    ImGui.SeparatorText("Oświetlenie");
    ImGui.SliderFloat("kd", ref kd, 0, 1);
    ImGui.SliderFloat("ks", ref ks, 0, 1);
    ImGui.SliderInt("m", ref m, 1, 100);
    ImGui.ColorEdit3("Kolor światła", ref lightColor);

    ImGui.Combo("##powierzchnia", ref selected, surfaceNames, surfaceNames.Length);
    var look = looks[selected];
    ImGui.Text("Kolor obiektu:");
    if (ImGui.RadioButton("Jednolity", !look.UseTexture))
        look.UseTexture = false;
    ImGui.SameLine();
    if (ImGui.RadioButton("Tekstura", look.UseTexture))
        look.UseTexture = true;
    if (!look.UseTexture)
        ImGui.ColorEdit3("Kolor obiektu", ref look.Color);

    ImGui.Text("Normalna:");
    Radio("Z siatki", ref normalSource, NormalSource.Geometry);
    ImGui.SameLine();
    Radio("Normalne", ref normalSource, NormalSource.NormalMap);
    ImGui.SameLine();
    Radio("Wysokości", ref normalSource, NormalSource.HeightMap);
    if (normalSource == NormalSource.HeightMap)
        ImGui.SliderFloat("k", ref heightK, 0, 100);
}

void FilesSection()
{
    ImGui.SeparatorText("Pliki");
    ImGui.Text("Przeciągnij PNG na okno jako:");
    Radio("tekstura", ref dropAs, DropTarget.Texture);
    ImGui.SameLine();
    Radio("normalne", ref dropAs, DropTarget.NormalMap);
    ImGui.SameLine();
    Radio("wysokości", ref dropAs, DropTarget.HeightMap);
    if (dropMessage != "")
        ImGui.TextWrapped(dropMessage);
}

void LightMotionSection()
{
    ImGui.SeparatorText("Ruch światła");
    int lightTypeIndex = (int)lightType;
    if (ImGui.Combo("Typ światła", ref lightTypeIndex, lightTypeNames, lightTypeNames.Length))
        lightType = (LightType)lightTypeIndex;
    if (lightType == LightType.Directional)
        return;

    ImGui.Checkbox("Pauza (Spacja)", ref lightPaused);
    ImGui.SliderInt("Liczba świateł", ref lightCount, 1, 4);
    if (lightType == LightType.Spot)
        ImGui.SliderInt("m reflektora", ref spotM, 5, 100);
    ImGui.SliderFloat("z światła", ref lightPath.Z, -500, 500);
    ImGui.Checkbox("Edycja krzywej", ref editPath);
}

static void Radio<T>(string label, ref T value, T option) where T : struct, Enum
{
    if (ImGui.RadioButton(label, value.Equals(option)))
        value = option;
}

// ---------- pomocnicze ----------

static string Asset(params string[] parts) => Path.Combine([AppContext.BaseDirectory, "Assets", .. parts]);

static Vector2 ToScreen(Vector3 p) => new(CanvasSize / 2f + p.X, CanvasSize / 2f - p.Y);

static Texture LoadTexture(string path)
{
    var img = Raylib.LoadImage(path);
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

static LightPath DefaultLightPath()
{
    const int Count = 6;
    const float Radius = 380;
    var ring = Enumerable.Range(0, Count)
                         .Select(i => Radius * new Vector2(MathF.Cos(MathF.Tau * i / Count), MathF.Sin(MathF.Tau * i / Count)))
                         .ToList();
    return new([(ring[^1] + ring[0]) / 2, .. ring], 400);
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

static void DrawLightPath(LightPath path, Vector3[] lights, bool editing)
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

    foreach (var light in lights)
    {
        var l = ToScreen(light);
        Raylib.DrawCircleV(l, 9, Color.Black);
        Raylib.DrawCircleV(l, 7, Color.Yellow);
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

static void LoadFont(string path, float size)
{
    ushort[] glyphRanges = [0x0020, 0x017F, 0x0370, 0x03FF, 0x2000, 0x206F, 0x2190, 0x22FF, 0];
    var handle = GCHandle.Alloc(glyphRanges, GCHandleType.Pinned);
    ImGui.GetIO().Fonts.AddFontFromFileTTF(path, size, null, handle.AddrOfPinnedObject());
}

enum LightType { Directional, Point, Spot }
enum NormalSource { Geometry, NormalMap, HeightMap }
enum DropTarget { Texture, NormalMap, HeightMap }

class SurfaceLook
{
    public Vector3 Color;
    public required Texture Texture;
    public bool UseTexture;
}
