using System.Numerics;

namespace Bezier3D.Core.Rendering;

/// <summary>Wypełnianie trójkąta z sortowaniem wierzchołków.</summary>
public static class TriangleFiller
{
    /// <summary>
    /// Dla każdego wiersza y, w którym trójkąt ma piksele, woła onSpan(y, xStart, xEnd):
    /// piksele x = xStart .. xEnd − 1. Piksel jest w środku, gdy jego środek (x + 0.5, y + 0.5) jest w trójkącie;
    /// lewa krawędź włącznie, prawa wyłącznie (wspólna krawędź dwóch trójkątów bez dziur i bez podwójnych pikseli).
    /// </summary>
    public static void Fill(Vector2 a, Vector2 b, Vector2 c, Action<int, int, int> onSpan)
    {
        // a jest na gorze, b w srodku, c na dole
        if (b.Y < a.Y)
            (a, b) = (b, a);
        if (c.Y < a.Y)
            (a, c) = (c, a);
        if (c.Y < b.Y)
            (b, c) = (c, b);

        var yStart = (int)Math.Ceiling(a.Y - 0.5f);
        var yEnd = (int)Math.Ceiling(c.Y - 0.5f);

        for (int y = yStart; y < yEnd; y++)
        {
            var yc = y + 0.5f;
            var xLong = XAt(a, c, yc);
            var xShort = yc < b.Y ? XAt(a, b, yc) : XAt(b, c, yc);

            var left = Math.Min(xLong, xShort);
            var right = Math.Max(xLong, xShort);

            var xStart = (int)Math.Ceiling(left - 0.5f);
            var xEnd = (int)Math.Ceiling(right - 0.5f);
            if (xStart < xEnd)
                onSpan(y, xStart, xEnd);
        }
    }
    private static float XAt(Vector2 p, Vector2 q, float y)
    {
        return p.X + (y - p.Y) * (q.X - p.X) / (q.Y - p.Y);
    }
}
