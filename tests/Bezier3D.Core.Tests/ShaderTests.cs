using System.Numerics;
using Bezier3D.Core.Rendering;

namespace Bezier3D.Core.Tests;

public class ShaderTests
{
    private const float Eps = 1e-4f;
    private static readonly Vector3 Up = Vector3.UnitZ;           // N płaskiej powierzchni
    private static readonly Vector3 White = Vector3.One;
    private static readonly Vector3 Orange = new(1, 0.5f, 0);

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Eps, $"oczekiwano {expected}, jest {actual}");

    [Fact]
    public void LightStraightAboveGivesDiffusePlusSpecular()
    {
        // N·L = 1, V·R = 1 → kd·IL·IO + ks·IL·IO
        var c = Shader.Shade(Up, Vector3.Zero, new(0, 0, 100), White, Orange, 0.3f, 0.2f, 10);
        AssertClose(0.5f * Orange, c);
    }

    [Fact]
    public void LightAt45Degrees()
    {
        // L = (√2/2, 0, √2/2): N·L = √2/2, R = (−√2/2, 0, √2/2), V·R = √2/2
        float cos = MathF.Sqrt(2) / 2;
        var c = Shader.Shade(Up, Vector3.Zero, new(100, 0, 100), White, White, 0.5f, 0.5f, 10);
        AssertClose(new Vector3(0.5f * cos + 0.5f * MathF.Pow(cos, 10)), c);
    }

    [Fact]
    public void LightPerpendicularGivesBlack()
    {
        // N ⊥ L → N·L = 0, a R = −L leży w płaszczyźnie → V·R = 0
        var c = Shader.Shade(Up, Vector3.Zero, new(100, 0, 0), White, White, 0.5f, 0.5f, 10);
        AssertClose(Vector3.Zero, c);
    }

    [Fact]
    public void LightBelowSurfaceGivesBlackNotNegative()
    {
        var c = Shader.Shade(Up, Vector3.Zero, new(30, 0, -100), White, White, 0.5f, 0.5f, 1);
        Assert.True(c.X >= 0 && c.Y >= 0 && c.Z >= 0, $"ujemny kolor {c}");
        AssertClose(Vector3.Zero, c);
    }

    [Fact]
    public void ResultIsClampedToOne()
    {
        // kd + ks = 2 → bez obcięcia wyszłoby 2
        var c = Shader.Shade(Up, Vector3.Zero, new(0, 0, 100), White, White, 1, 1, 10);
        AssertClose(Vector3.One, c);
    }

    [Fact]
    public void ChannelsAreSeparate()
    {
        // czerwone światło na niebieskim obiekcie → nic nie przechodzi
        var c = Shader.Shade(Up, Vector3.Zero, new(0, 0, 100), new(1, 0, 0), new(0, 0, 1), 1, 1, 10);
        AssertClose(Vector3.Zero, c);
    }

    [Fact]
    public void LightDistanceDoesNotMatter()
    {
        // L jest normalizowany — odległość światła nie zmienia jasności
        var near = Shader.Shade(Up, Vector3.Zero, new(0, 0, 1), White, Orange, 0.5f, 0.5f, 10);
        var far = Shader.Shade(Up, Vector3.Zero, new(0, 0, 1000), White, Orange, 0.5f, 0.5f, 10);
        AssertClose(near, far);
    }

    [Fact]
    public void SpotOnAxisIsFull() =>
        // punkt na osi reflektora → α = 0, cos = 1
        Assert.Equal(1, Shader.Spot(new(0, 0, 100), new(0, 0, 10), 20), Eps);

    [Fact]
    public void SpotAt60DegreesIsCosPowM()
    {
        // reflektor w (0,0,100), punkt (100·√3, 0, 0) → α = 60°, cos = 1/2
        var s = Shader.Spot(new(0, 0, 100), new(100 * MathF.Sqrt(3), 0, 0), 5);
        Assert.Equal(MathF.Pow(0.5f, 5), s, Eps);
    }

    [Fact]
    public void SpotBehindIsZero() =>
        // punkt za reflektorem → α > 90°, nie świeci
        Assert.Equal(0, Shader.Spot(new(0, 0, 100), new(0, 0, 200), 5), Eps);
}
