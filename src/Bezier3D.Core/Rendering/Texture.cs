using System.Numerics;

namespace Bezier3D.Core.Rendering;

/// <summary>Obraz jako tablica kolorów (R, G, B) w zakresie 0–1, wiersz po wierszu.</summary>
public sealed class Texture(int w, int h, Vector3[] pixels)
{
    public int W { get; } = w;
    public int H { get; } = h;
    public Vector3[] Pixels { get; } = pixels;

    /// <summary>Kolor w punkcie (u, v) ∈ [0,1]²:
    public Vector3 Sample(float u, float v)
    {
        var x = (int)(u * W);
        var y = (int)(v * H);
        x = Math.Clamp(x, 0, W - 1);
        y = Math.Clamp(y, 0, H - 1);
        return Pixels[y * W + x];
    }
}
