namespace Bezier3D.Core.Rendering;

/// <summary>Własny „ekran” w pamięci: kolor i głębokość (z) każdego piksela, indeks = y · Width + x.</summary>
public sealed class FrameBuffer(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public uint[] Color { get; } = new uint[width * height];
    public float[] Depth { get; } = new float[width * height];

    /// <summary>Kolor RGBA upakowany w uint w kolejności bajtów R, G, B, A (format tekstury R8G8B8A8).</summary>
    public static uint Rgba(byte r, byte g, byte b) => 0xFF000000u | (uint)b << 16 | (uint)g << 8 | r;

    public void Clear(uint color)
    {
        Array.Fill(Color, color);
        Array.Fill(Depth, float.NegativeInfinity);
    }

    /// <summary>Z-test: zapis tylko, gdy z jest bliżej (większe) niż to, co już jest w pikselu. Poza płótnem → nic.</summary>
    public void SetPixel(int x, int y, float z, uint color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;
        int i = y * Width + x;
        if (z <= Depth[i])
            return;
        Depth[i] = z;
        Color[i] = color;
    }
}
