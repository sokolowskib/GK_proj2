using System.Numerics;

namespace Bezier3D.Core.Maths;

/// <summary>Macierz 3×3 w konwencji kolumnowej</summary>
public sealed class Mat3
{
    private readonly float[,] m = new float[3, 3];

    /// <summary>Element w wierszu row, kolumnie col.</summary>
    public float this[int row, int col]
    {
        get => m[row, col];
        set => m[row, col] = value;
    }

    public static Mat3 Identity
    {
        get {
            var id = new Mat3();
            id[0, 0] = 1;
            id[1, 1] = 1;
            id[2, 2] = 1;
            return id;
        }
    }

    /// <summary>M = [a b c]</summary>
    public static Mat3 FromColumns(Vector3 a, Vector3 b, Vector3 c)
    {
        var cols = new List<Vector3> { a, b, c };
        var M = new Mat3();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                M[i, j] = cols[j][i];
            }
        }
        return M;
    }

    public static Mat3 Rotation(Vector3 axis, float angle)
    {
        Mat3 I = Identity;
        axis = Vector3.Normalize(axis);

        Mat3 K = new();
        // K to macierz, dla ktorej K * v = axis x v;
        K[1, 0] = axis.Z;
        K[2, 0] = -axis.Y;

        K[0, 1] = -axis.Z;
        K[2, 1] = axis.X;

        K[0, 2] = axis.Y;
        K[1, 2] = -axis.X;

        Mat3 r = I + MathF.Sin(angle) * K + (1 - MathF.Cos(angle)) * K * K;
        return r;
    }

    public static Mat3 operator*(Mat3 a, Mat3 b)
    {
        var r = new Mat3();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    r[i, j] += a[i, k] * b[k, j];
                }
            }
        }
        return r;
    }
    public static Vector3 operator*(Mat3 a, Vector3 v)
    {
        Vector3 r = new();

        for (int i = 0; i < 3; i++)
        {
            r.X += a[0, i] * v[i];
            r.Y += a[1, i] * v[i];
            r.Z += a[2, i] * v[i];
        }
        return r;
    }

    public static Mat3 operator +(Mat3 a, Mat3 b)
    {
        Mat3 r = new();

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                r[i, j] = a[i, j] + b[i, j];
            }
        }
        return r;
    }

    public static Mat3 operator*(float s, Mat3 a)
    {
        Mat3 r = new();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                r[i, j] = a[i, j] * s;
            }
        }
        return r;
    }
}
