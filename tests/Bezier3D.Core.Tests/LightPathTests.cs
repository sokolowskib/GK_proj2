using System.Numerics;
using Bezier3D.Core.Lighting;

namespace Bezier3D.Core.Tests;

public class LightPathTests
{
    private const float Eps = 1e-4f;

    // punkty kontrolne po zamknięciu: (0,0) (0,100) (100,100) (0,0)
    private static LightPath Path() => new([new(0, 0), new(0, 100), new(100, 100)], 7);

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Eps, $"oczekiwano {expected}, jest {actual}");

    [Fact]
    public void StartsAndEndsAtFirstPoint()
    {
        AssertClose(new(0, 0, 7), Path().At(0));
        AssertClose(new(0, 0, 7), Path().At(1));
    }

    [Fact]
    public void MiddleByHand()
    {
        // runda 1: (0,50) (50,100) (50,50); runda 2: (25,75) (50,75); runda 3: (37.5,75)
        AssertClose(new(37.5f, 75, 7), Path().At(0.5f));
    }

    [Fact]
    public void LiesOnPlaneZ()
    {
        var path = Path();
        path.Z = -300;
        for (float t = 0; t <= 1; t += 0.1f)
            Assert.Equal(-300, path.At(t).Z);
    }

    [Fact]
    public void MovingPointChangesCurve()
    {
        var path = Path();
        var before = path.At(0.5f);
        path.Points[1] = new(0, 300);
        Assert.NotEqual(before, path.At(0.5f));
    }
}
