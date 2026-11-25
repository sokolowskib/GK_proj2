using System.Numerics;

namespace Bezier3D.Core.Maths;

/// <summary>Obrót myszą: kula jednostkowa za płótnem, ruch myszy p0 → p1 obraca kulę.</summary>
public static class Arcball
{
    /// <summary>
    /// Piksel myszy → punkt na kuli jednostkowej (długość 1).    /// </summary>
    public static Vector3 MapToSphere(Vector2 mouse, Vector2 center, float radius)
    {
        var x = (mouse.X - center.X) / radius;
        var y = (center.Y - mouse.Y) / radius;

        var d_squared = x * x + y * y;
        if (d_squared <= 1)
        {
            var z = MathF.Sqrt(1 - d_squared);
            return new Vector3(x, y, z);
        }
        return Vector3.Normalize(new Vector3(x, y, 0));
    }

    public static Mat3 Increment(Vector3 p0, Vector3 p1)
    {
        var axis = Vector3.Cross(p0, p1);
        if (axis.Length() < 1e-6f)
            return Mat3.Identity;

        float cos = Math.Clamp(Vector3.Dot(p0, p1), -1f, 1f);

        return Mat3.Rotation(axis, MathF.Acos(cos));
    }
}
