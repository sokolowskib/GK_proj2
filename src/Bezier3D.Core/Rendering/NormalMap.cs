using System.Numerics;
using Bezier3D.Core.Maths;

namespace Bezier3D.Core.Rendering;

/// <summary>Zaburzenie normalnej kolorem z mapy normalnych.</summary>
public static class NormalMap
{
    public static Vector3 Apply(Vector3 N, Vector3 Pu, Vector3 Pv, Vector3 texColor)
    {
        Mat3 M = Mat3.FromColumns(Pu, Pv, N);

        var Ntex = 2 * texColor - Vector3.One;

        return Vector3.Normalize(M * Ntex);
    }
}
