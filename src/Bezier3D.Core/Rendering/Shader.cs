using System.Numerics;

namespace Bezier3D.Core.Rendering;

/// <summary>Kolor punktu: Lambert + odbicie zwierciadlane. Kolory jako (R, G, B) w zakresie 0–1.</summary>
public static class Shader
{
    /// <summary>
    /// I = kd·IL·IO·max(0, N·L) + ks·IL·IO·max(0, V·R)^m, gdzie L = normalize(lightPos − P), V = [0,0,1],
    /// R = 2(N·L)N − L. N musi być jednostkowy. Wynik obcięty do [0, 1].
    /// </summary>
    public static Vector3 Shade(Vector3 N, Vector3 P, Vector3 lightPos, Vector3 IL, Vector3 IO, float kd, float ks,
                                float m)
    {
        // kierunek punktu od swiatla
        var L = Vector3.Normalize(lightPos - P);

        var NL = Vector3.Dot(N, L);

        var R = 2 * NL * N - L;

        var V = new Vector3(0, 0, 1);

        var VR = Vector3.Dot(V, R);

        return Vector3.Clamp(kd * IL * IO * MathF.Max(0, NL) + ks * IL * IO * MathF.Pow(MathF.Max(0, VR), m),
                             Vector3.Zero, Vector3.One);
    }
}
