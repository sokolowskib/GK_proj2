using System.Numerics;

namespace Bezier3D.Core.Rendering;

/// <summary>Zaburzenie normalnej mapą wysokości (wysokość = składowa R, 0–1).</summary>
public static class HeightMap
{
    public static Vector3 Apply(Vector3 N, Vector3 Pu, Vector3 Pv, Texture H, float u, float v, float k)
    {
        // sąsiedni piksel = przesunięcie o 1/W (1/H); Sample obcina na brzegach
        float du = 1f / H.W, dv = 1f / H.H;
        float Hu = (H.Sample(u + du, v).X - H.Sample(u - du, v).X) / 2;
        float Hv = (H.Sample(u, v + dv).X - H.Sample(u, v - dv).X) / 2;

        var D = k * (Hu * Vector3.Cross(N, Pv) - Hv * Vector3.Cross(N, Pu));

        return Vector3.Normalize(N + D);
    }
}
