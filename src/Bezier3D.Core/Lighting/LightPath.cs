using System.Numerics;

namespace Bezier3D.Core.Lighting;

/// <summary>Tor światła: jedna zamknięta krzywa Béziera w płaszczyźnie z = Z. Zamknięcie jest automatyczne —
/// ostatni punkt kontrolny to Points[0], więc przeciąganie punktów nigdy krzywej nie otwiera.</summary>
public sealed class LightPath(List<Vector2> points, float z)
{
    public List<Vector2> Points = points;
    public float Z = z;

    /// <summary>Punkt krzywej dla t ∈ [0,1] </summary>
    public Vector3 At(float t)
    {
        var p = new Vector2[Points.Count + 1];
        Points.CopyTo(p);
        p[^1] = Points[0];
        for (int round = 1; round < p.Length; round++)
            for (int i = 0; i < p.Length - round; i++)
                p[i] = Vector2.Lerp(p[i], p[i + 1], t);
        return new Vector3(p[0], Z);
    }
}
