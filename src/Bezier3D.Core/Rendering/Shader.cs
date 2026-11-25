using System.Numerics;

namespace Bezier3D.Core.Rendering;

public static class Shader
{
    public static Vector3 Shade(Vector3 N, Vector3 P, Vector3 lightPos, Vector3 IL, Vector3 IO, float kd, float ks,
                                float m) => Shade(N, Vector3.Normalize(lightPos - P), IL, IO, kd, ks, m);

    public static Vector3 Shade(Vector3 N, Vector3 L, Vector3 IL, Vector3 IO, float kd, float ks, float m)
    {
        var NL = Vector3.Dot(N, L);

        var R = 2 * NL * N - L;

        var V = new Vector3(0, 0, 1);

        var VR = Vector3.Dot(V, R);

        return Vector3.Clamp(kd * IL * IO * MathF.Max(0, NL) + ks * IL * IO * MathF.Pow(MathF.Max(0, VR), m),
                             Vector3.Zero, Vector3.One);
    }

    public static float Spot(Vector3 lightPos, Vector3 P, float m)
    {
        var axis = Vector3.Normalize(-lightPos);
        var toPoint = Vector3.Normalize(P - lightPos);
        return MathF.Pow(MathF.Max(0, Vector3.Dot(axis, toPoint)), m);
    }
}
