using System.Numerics;

namespace Bezier3D.Core.Surfaces;

/// <summary>Powierzchnia Béziera 3. stopnia: 16 punktów kontrolnych V[i, j], i — kierunek u, j — kierunek v.</summary>
public sealed class BezierSurface(Vector3[,] v)
{
    public Vector3[,] V { get; } = v;

    /// <summary>Punkt P(u,v), wektory styczne Pu, Pv i znormalizowany wektor normalny N = Pu × Pv.</summary>
    public (Vector3 P, Vector3 Pu, Vector3 Pv, Vector3 N) Evaluate(float u, float v)
    {
        float[] bu = { (1 - u) * (1 - u) * (1 - u), 3 * u * (1 - u) * (1 - u), 3 * u * u * (1 - u), u * u * u };
        float[] bv = { (1 - v) * (1 - v) * (1 - v), 3 * v * (1 - v) * (1 - v), 3 * v * v * (1 - v), v * v * v };

        float[] bu2 = { (1 - u) * (1 - u), 2 * u * (1 - u), u * u };
        float[] bv2 = { (1 - v) * (1 - v), 2 * v * (1 - v), v * v };

        Vector3 P = Vector3.Zero;
        Vector3 Pu = Vector3.Zero;
        Vector3 Pv = Vector3.Zero;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                P += bu[i] * bv[j] * V[i, j];
            }
        }

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Pu += bu2[i] * bv[j] * (V[i + 1, j] - V[i, j]);
            }
        }
        Pu *= 3;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Pv += bv2[j] * bu[i] * (V[i, j + 1] - V[i, j]);
            }
        }
        Pv *= 3;

        var N = Vector3.Normalize(Vector3.Cross(Pu, Pv));
        return (P, Pu, Pv, N);
    }
}
