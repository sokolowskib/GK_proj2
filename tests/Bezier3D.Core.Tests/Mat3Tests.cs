using System.Numerics;
using Bezier3D.Core.Maths;

namespace Bezier3D.Core.Tests;

public class Mat3Tests
{
    private const float Eps = 1e-4f;
    private const float Deg90 = MathF.PI / 2;

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Eps, $"oczekiwano {expected}, jest {actual}");

    private static void AssertClose(Mat3 expected, Mat3 actual)
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                Assert.True(MathF.Abs(expected[i, j] - actual[i, j]) < Eps,
                    $"[{i},{j}]: oczekiwano {expected[i, j]}, jest {actual[i, j]}");
    }

    private static Mat3 Transpose(Mat3 a)
    {
        var t = new Mat3();
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                t[i, j] = a[j, i];
        return t;
    }

    [Fact]
    public void IdentityLeavesVectorUnchanged()
    {
        var v = new Vector3(1, -2, 3);
        AssertClose(v, Mat3.Identity * v);
    }

    [Fact]
    public void FromColumnsTimesUnitVectorGivesColumn()
    {
        var a = new Vector3(1, 2, 3);
        var b = new Vector3(4, 5, 6);
        var c = new Vector3(7, 8, 9);
        var m = Mat3.FromColumns(a, b, c);

        AssertClose(a, m * Vector3.UnitX);
        AssertClose(b, m * Vector3.UnitY);
        AssertClose(c, m * Vector3.UnitZ);
        Assert.Equal(2, m[1, 0]); // a.Y siedzi w wierszu 1, kolumnie 0
    }

    [Fact]
    public void MatrixProductMatchesHandComputed()
    {
        // A = [[1,2,0],[0,1,0],[0,0,1]], B = [[1,0,0],[3,1,0],[0,0,2]] → A·B = [[7,2,0],[3,1,0],[0,0,2]]
        var a = Mat3.FromColumns(new(1, 0, 0), new(2, 1, 0), new(0, 0, 1));
        var b = Mat3.FromColumns(new(1, 3, 0), new(0, 1, 0), new(0, 0, 2));
        var expected = Mat3.FromColumns(new(7, 3, 0), new(2, 1, 0), new(0, 0, 2));

        AssertClose(expected, a * b);
    }

    [Fact]
    public void SumAndScale()
    {
        var a = Mat3.FromColumns(new(1, 2, 3), new(4, 5, 6), new(7, 8, 9));
        var expected = Mat3.FromColumns(new(3, 6, 9), new(12, 15, 18), new(21, 24, 27));

        AssertClose(expected, a + 2 * a);
    }

    [Theory]
    [InlineData(0, 0, 1, 1, 0, 0, 0, 1, 0)] // wokół Z: X → Y
    [InlineData(1, 0, 0, 0, 1, 0, 0, 0, 1)] // wokół X: Y → Z
    [InlineData(0, 1, 0, 0, 0, 1, 1, 0, 0)] // wokół Y: Z → X
    public void Rotation90AroundAxis(float ax, float ay, float az, float vx, float vy, float vz, float ex, float ey, float ez)
    {
        var r = Mat3.Rotation(new Vector3(ax, ay, az), Deg90);
        AssertClose(new Vector3(ex, ey, ez), r * new Vector3(vx, vy, vz));
    }

    [Fact]
    public void RotationAroundZ90IsExactMatrix()
    {
        var expected = Mat3.FromColumns(new(0, 1, 0), new(-1, 0, 0), new(0, 0, 1));
        AssertClose(expected, Mat3.Rotation(Vector3.UnitZ, Deg90));
    }

    [Fact]
    public void RotationAxisIsNormalizedInside()
    {
        AssertClose(Mat3.Rotation(Vector3.UnitZ, 0.7f), Mat3.Rotation(new Vector3(0, 0, 5), 0.7f));
    }

    [Fact]
    public void RotationIsOrthogonal()
    {
        var r = Mat3.Rotation(new Vector3(1, 2, -3), 1.234f);
        AssertClose(Mat3.Identity, r * Transpose(r));
    }

    [Fact]
    public void RotationKeepsAxisFixed()
    {
        var axis = Vector3.Normalize(new Vector3(1, 2, -3));
        AssertClose(axis, Mat3.Rotation(axis, 2.5f) * axis);
    }

    [Fact]
    public void TwoRotationsComposeIntoOne()
    {
        var axis = new Vector3(1, 1, 0);
        var half = Mat3.Rotation(axis, Deg90 / 2);
        AssertClose(Mat3.Rotation(axis, Deg90), half * half);
    }
}
