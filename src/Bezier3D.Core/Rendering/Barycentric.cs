using System.Numerics;

namespace Bezier3D.Core.Rendering;

/// <summary>Współrzędne barycentryczne (α, β, γ) punktu w trójkącie i interpolacja nimi wartości z
/// wierzchołków.</summary>
public static class Barycentric
{
    /// <summary>Iloczyn wektorowy 2D: u.X·v.Y − u.Y·v.X (podwójne pole ze znakiem).</summary>
    public static float Cross(Vector2 u, Vector2 v)
    {
        return u.X * v.Y - u.Y * v.X;
    }

    /// <summary>(α, β, γ) punktu p w trójkącie abc; α = pole(p, b, c) / pole(a, b, c) itd. Suma = 1.</summary>
    public static Vector3 Compute(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        var abc = Cross(b - a, c - a);
        var pbc = Cross(b - p, c - p);
        var pca = Cross(c - p, a - p);
        var pab = Cross(a - p, b - p);

        return new Vector3(pbc / abc, pca / abc, pab / abc);
    }

    /// <summary>α·a + β·b + γ·c, gdzie w = (α, β, γ).</summary>
    public static float Interpolate(Vector3 w, float a, float b, float c)
    {
        return w.X * a + w.Y * b + w.Z * c;
    }

    /// <summary>α·a + β·b + γ·c, gdzie w = (α, β, γ).</summary>
    public static Vector3 Interpolate(Vector3 w, Vector3 a, Vector3 b, Vector3 c)
    {
        return w.X * a + w.Y * b + w.Z * c;
    }
}
