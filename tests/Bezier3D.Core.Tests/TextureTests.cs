using System.Numerics;
using Bezier3D.Core.Rendering;

namespace Bezier3D.Core.Tests;

public class TextureTests
{
    // obrazek 4×2, piksel o indeksie i ma kolor (i, i, i) — łatwo sprawdzić, który został wybrany
    private static Texture Tex() => new(4, 2, Enumerable.Range(0, 8).Select(i => new Vector3(i)).ToArray());

    [Theory]
    [InlineData(0f, 0f, 0)]       // lewy górny róg
    [InlineData(0.5f, 0f, 2)]     // 0.5·4 = 2
    [InlineData(0.3f, 0.7f, 5)]   // x = 1, y = 1 → 1·4 + 1
    [InlineData(0.99f, 0.99f, 7)] // ostatni piksel od środka
    [InlineData(1f, 1f, 7)]       // prawa krawędź → obcięte do ostatniego
    [InlineData(1f, 0f, 3)]       // koniec pierwszego wiersza
    public void SamplesPixelAt(float u, float v, int index)
    {
        Assert.Equal(new Vector3(index), Tex().Sample(u, v));
    }

    [Fact]
    public void OutOfRangeIsClamped()
    {
        Assert.Equal(new Vector3(0), Tex().Sample(-0.1f, -0.1f));
        Assert.Equal(new Vector3(7), Tex().Sample(1.5f, 2f));
    }
}
