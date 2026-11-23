using System.Numerics;
using Bezier3D.Core.Rendering;

namespace Bezier3D.Core.Tests;

public class NormalMapTests
{
    private const float Eps = 1e-4f;
    private static readonly float S = MathF.Sqrt(2) / 2; // ≈ 0.707

    // kolor, który po 2·kolor − 1 daje (S, 0, S) — przechył o 45° w stronę u
    private static readonly Vector3 Tilt45 = new((S + 1) / 2, 0.5f, (S + 1) / 2);

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Eps, $"oczekiwano {expected}, jest {actual}");

    [Fact]
    public void FlatColorKeepsNormal()
    {
        // (0.5, 0.5, 1) → Ntex = (0, 0, 1) → wynik = N
        var N = Vector3.Normalize(new(1, 2, 3));
        var Pu = Vector3.Normalize(new(3, 0, -1));
        var Pv = Vector3.Normalize(Vector3.Cross(N, Pu));
        AssertClose(N, NormalMap.Apply(N, Pu, Pv, new(0.5f, 0.5f, 1)));
    }

    [Fact]
    public void FlatSurfaceGivesNtex()
    {
        // M = I → wynik = Ntex = (S, 0, S)
        AssertClose(new(S, 0, S), NormalMap.Apply(Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, Tilt45));
    }

    [Fact]
    public void RotatedSurfaceRotatesTilt()
    {
        // powierzchnia obrócona o 90° wokół z: Pu = (0,1,0), Pv = (−1,0,0) → przechył idzie w stronę y
        AssertClose(new(0, S, S), NormalMap.Apply(Vector3.UnitZ, Vector3.UnitY, -Vector3.UnitX, Tilt45));
    }

    [Fact]
    public void FullTiltGivesPu()
    {
        // (1, 0.5, 0.5) → Ntex = (1, 0, 0) → wynik = Pu
        var Pu = Vector3.Normalize(new(1, 1, 0));
        AssertClose(Pu, NormalMap.Apply(Vector3.UnitZ, Pu, Vector3.Normalize(new(-1, 1, 0)), new(1, 0.5f, 0.5f)));
    }

    [Fact]
    public void ResultIsUnit()
    {
        var r = NormalMap.Apply(Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, new(0.9f, 0.2f, 0.6f));
        Assert.Equal(1f, r.Length(), 1e-4f);
    }
}
