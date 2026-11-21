using System.Numerics;
using Bezier3D.Core.Maths;
using Bezier3D.Core.Surfaces;

namespace Bezier3D.Core.Tests;

public class ArcballTests
{
    private const float Eps = 1e-3f;
    private static readonly Vector2 Center = new(400, 400);
    private const float Radius = 400;

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Eps, $"oczekiwano {expected}, jest {actual}");

    [Theory]
    [InlineData(400, 400, 0, 0, 1)]         // środek → przód kuli
    [InlineData(600, 400, 0.5f, 0, 0.866f)] // w prawo
    [InlineData(400, 200, 0, 0.5f, 0.866f)] // w górę ekranu (mniejsze my) → dodatnie y
    [InlineData(800, 400, 1, 0, 0)]         // dokładnie na brzegu
    [InlineData(900, 400, 1, 0, 0)]         // poza kołem → brzeg
    [InlineData(400, 1000, 0, -1, 0)]       // poza kołem w dół
    public void MapToSphereExamples(float mx, float my, float x, float y, float z)
    {
        AssertClose(new Vector3(x, y, z), Arcball.MapToSphere(new Vector2(mx, my), Center, Radius));
    }

    [Theory]
    [InlineData(123, 456)]
    [InlineData(0, 0)]
    [InlineData(1500, -300)]
    public void MapToSphereGivesUnitVector(float mx, float my)
    {
        Assert.Equal(1f, Arcball.MapToSphere(new Vector2(mx, my), Center, Radius).Length(), 1e-4f);
    }

    [Fact]
    public void NoMovementIsIdentity()
    {
        var p = Vector3.Normalize(new Vector3(0.3f, -0.2f, 0.9f));
        var r = Arcball.Increment(p, p);
        AssertClose(Vector3.UnitX, r * Vector3.UnitX);
        AssertClose(Vector3.UnitY, r * Vector3.UnitY);
        AssertClose(Vector3.UnitZ, r * Vector3.UnitZ);
    }

    [Fact]
    public void AlmostNoMovementDoesNotGiveNaN()
    {
        // dot może wyjść minimalnie > 1 przez zaokrąglenia → acos = NaN bez obcięcia
        var p0 = Vector3.Normalize(new Vector3(0.1f, 0.2f, 0.97f));
        var p1 = Vector3.Normalize(p0 + new Vector3(1e-7f, 0, 0));
        var v = Arcball.Increment(p0, p1) * Vector3.UnitX;
        Assert.False(float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z));
    }

    [Fact]
    public void DragRightRotatesAroundYBy30Degrees()
    {
        var p0 = new Vector3(0, 0, 1);
        var p1 = Vector3.Normalize(new Vector3(0.5f, 0, 0.866f));
        var expected = Mat3.Rotation(Vector3.UnitY, MathF.PI / 6);
        var r = Arcball.Increment(p0, p1);

        AssertClose(expected * Vector3.UnitX, r * Vector3.UnitX);
        AssertClose(expected * Vector3.UnitZ, r * Vector3.UnitZ);
    }

    [Theory]
    [InlineData(400, 400, 500, 450)]
    [InlineData(100, 300, 120, 280)]
    [InlineData(700, 700, 300, 100)]
    public void IncrementCarriesP0ToP1(float x0, float y0, float x1, float y1)
    {
        // najważniejsza własność: punkt złapany myszą jedzie za myszą
        var p0 = Arcball.MapToSphere(new Vector2(x0, y0), Center, Radius);
        var p1 = Arcball.MapToSphere(new Vector2(x1, y1), Center, Radius);
        AssertClose(p1, Arcball.Increment(p0, p1) * p0);
    }

    [Fact]
    public void DragAlongRimRotatesAroundScreenZ()
    {
        var r = Arcball.Increment(Vector3.UnitX, Vector3.UnitY);
        AssertClose(Vector3.UnitY, r * Vector3.UnitX);
        AssertClose(Vector3.UnitZ, r * Vector3.UnitZ); // oś Z stoi w miejscu
    }

    [Fact]
    public void VertexRotateUsesOriginalsAndNormalizesN()
    {
        var vertex = new MeshVertex { P = new(1, 2, 3), Pu = new(1, 0, 0), Pv = new(0, 1, 0), N = new(0, 0, 1) };
        var r = Mat3.Rotation(Vector3.UnitZ, MathF.PI / 2);

        vertex.Rotate(r);
        vertex.Rotate(r); // drugi raz z tym samym R → ten sam wynik (liczone od P, nie od Pr)

        AssertClose(new(-2, 1, 3), vertex.Pr);
        AssertClose(new(0, 1, 0), vertex.Pur);
        AssertClose(new(-1, 0, 0), vertex.Pvr);
        AssertClose(new(0, 0, 1), vertex.Nr);
        Assert.Equal(1f, vertex.Nr.Length(), 1e-4f);
    }
}
