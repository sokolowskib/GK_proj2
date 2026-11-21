using System.Numerics;
using Bezier3D.Core.Rendering;

namespace Bezier3D.Core.Tests;

public class TriangleFillerTests
{
    // Zbiera wszystkie piksele; podwójny piksel w jednym trójkącie → błąd testu.
    private static HashSet<(int X, int Y)> Pixels(Vector2 a, Vector2 b, Vector2 c)
    {
        var set = new HashSet<(int, int)>();
        TriangleFiller.Fill(a, b, c, (y, x0, x1) =>
        {
            Assert.True(x0 < x1, $"pusty span w wierszu {y}: [{x0}, {x1})");
            for (int x = x0; x < x1; x++)
                Assert.True(set.Add((x, y)), $"piksel ({x}, {y}) dwa razy");
        });
        return set;
    }

    // Wzorzec: środek piksela po wewnętrznej stronie wszystkich trzech krawędzi.
    private static HashSet<(int X, int Y)> BruteForce(Vector2 a, Vector2 b, Vector2 c)
    {
        static float Cross(Vector2 p, Vector2 q, Vector2 r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
        var set = new HashSet<(int, int)>();
        for (int y = 0; y < 100; y++)
            for (int x = 0; x < 100; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d1 = Cross(a, b, p), d2 = Cross(b, c, p), d3 = Cross(c, a, p);
                if ((d1 > 0 && d2 > 0 && d3 > 0) || (d1 < 0 && d2 < 0 && d3 < 0))
                    set.Add((x, y));
            }
        return set;
    }

    [Fact]
    public void ExampleFromExplanation()
    {
        var spans = new Dictionary<int, (int, int)>();
        TriangleFiller.Fill(new(2, 0), new(0, 4), new(6, 8), (y, x0, x1) => spans[y] = (x0, x1));

        Assert.Equal((1, 3), spans[2]);
        Assert.Equal((4, 5), spans[6]);
    }

    // Wierzchołki niecałkowite, żeby żadna krawędź nie przechodziła dokładnie przez środek piksela.
    [Theory]
    [InlineData(10.3f, 5.1f, 40.7f, 22.9f, 3.2f, 30.6f)]  // zwykły
    [InlineData(50.2f, 2.3f, 90.6f, 60.1f, 20.4f, 95.7f)] // duży
    [InlineData(5.3f, 10.2f, 60.6f, 10.2f, 30.1f, 50.7f)] // płaska góra
    [InlineData(5.3f, 50.2f, 60.6f, 50.2f, 30.1f, 10.7f)] // płaski dół
    [InlineData(1.1f, 1.2f, 98.3f, 3.4f, 2.6f, 97.9f)]    // długi i chudy
    public void MatchesBruteForce(float ax, float ay, float bx, float by, float cx, float cy)
    {
        Vector2 a = new(ax, ay), b = new(bx, by), c = new(cx, cy);
        Assert.Equal(BruteForce(a, b, c), Pixels(a, b, c));
    }

    [Fact]
    public void VertexOrderDoesNotMatter()
    {
        Vector2 a = new(10.3f, 5.1f), b = new(40.7f, 22.9f), c = new(3.2f, 30.6f);
        var expected = Pixels(a, b, c);
        Assert.Equal(expected, Pixels(b, a, c));
        Assert.Equal(expected, Pixels(c, b, a));
        Assert.Equal(expected, Pixels(b, c, a));
    }

    [Theory]
    [InlineData(0f, 0f, 10f, 10f, 20f, 20f)]  // ukośna linia
    [InlineData(0f, 5f, 10f, 5f, 20f, 5f)]    // pozioma linia (dzielenie przez 0!)
    [InlineData(3f, 3f, 3f, 3f, 3f, 3f)]      // punkt
    public void DegenerateGivesNoPixels(float ax, float ay, float bx, float by, float cx, float cy)
    {
        Assert.Empty(Pixels(new(ax, ay), new(bx, by), new(cx, cy)));
    }

    [Fact]
    public void SharedEdgeNoHolesNoOverlap()
    {
        // kwadrat 0..20 przecięty po przekątnej — przekątna przechodzi przez środki pikseli,
        // więc to właśnie konwencja „lewa włącznie, prawa wyłącznie” decyduje
        Vector2 tl = new(0, 0), tr = new(20, 0), bl = new(0, 20), br = new(20, 20);
        var t1 = Pixels(tl, tr, br);
        var t2 = Pixels(tl, br, bl);

        Assert.Empty(t1.Intersect(t2));
        Assert.Equal(400, t1.Count + t2.Count);
    }
}
