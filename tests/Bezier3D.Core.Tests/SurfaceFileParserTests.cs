using System.Numerics;
using Bezier3D.Core.Surfaces;

namespace Bezier3D.Core.Tests;

public class SurfaceFileParserTests
{
    // 16 wierszy, w których punkt k ma współrzędne (k, 10k, -k) — łatwo sprawdzić kolejność.
    private static string Group(int offset = 0) =>
        string.Join("\n", Enumerable.Range(offset, 16).Select(k => $"{k} {10 * k}.5 {-k}"));

    [Fact]
    public void ParsesTwoSurfaces()
    {
        var surfaces = SurfaceFileParser.Parse(Group() + "\n" + Group(100));

        Assert.Equal(2, surfaces.Count);
        Assert.Equal(new Vector3(100, 1000.5f, -100), surfaces[1].V[0, 0]);
    }

    [Fact]
    public void LinesFillGridRowByRow()
    {
        var s = SurfaceFileParser.Parse(Group())[0];

        Assert.Equal(new Vector3(0, 0.5f, 0), s.V[0, 0]);
        Assert.Equal(new Vector3(1, 10.5f, -1), s.V[0, 1]);   // wiersz 1 → V01
        Assert.Equal(new Vector3(4, 40.5f, -4), s.V[1, 0]);   // wiersz 4 → V10
        Assert.Equal(new Vector3(6, 60.5f, -6), s.V[1, 2]);   // wiersz 6 → V12
        Assert.Equal(new Vector3(15, 150.5f, -15), s.V[3, 3]);
    }

    [Fact]
    public void IgnoresBlankLinesAndExtraWhitespace()
    {
        string text = "\n" + Group().Replace(" ", "   \t") + "\r\n\n";

        Assert.Single(SurfaceFileParser.Parse(text));
    }

    [Fact]
    public void UsesDotAsDecimalSeparatorRegardlessOfCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pl-PL");
        try
        {
            Assert.Equal(0.5f, SurfaceFileParser.Parse(Group())[0].V[0, 0].Y);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("")]                       // brak powierzchni
    [InlineData("1 2 3")]                  // niepełna grupa
    public void RejectsWrongLineCount(string text) =>
        Assert.Throws<FormatException>(() => SurfaceFileParser.Parse(text));

    [Theory]
    [InlineData("1 2")]
    [InlineData("1 2 3 4")]
    [InlineData("1 abc 3")]
    public void RejectsMalformedLine(string badLine)
    {
        var lines = Group().Split('\n');
        lines[5] = badLine;

        Assert.Throws<FormatException>(() => SurfaceFileParser.Parse(string.Join("\n", lines)));
    }
}
