namespace Bezier3D.Core.Surfaces;

public static class Triangulator
{
    /// <summary>
    /// Siatka (n+1)×(n+1) wierzchołków dla u, v = 0, 1/n, ..., 1; każdy kwadrat → 2 trójkąty, razem 2n².
    /// Wierzchołki tworzone raz i współdzielone przez trójkąty.
    /// </summary>
    public static List<Triangle> Triangulate(BezierSurface surface, int n)
    {
        var grid = new MeshVertex[n + 1, n + 1];

        for (int i = 0; i <= n; i++)
        {
            for (int j = 0; j <= n; j++)
            {
                var u = (float)i / n;
                var v = (float)j / n;

                var (P, Pu, Pv, N) = surface.Evaluate(u, v);

                var vertex = new MeshVertex { U = u, V = v, P = P, Pu = Pu, Pv = Pv, N = N };
                grid[i, j] = vertex;
            }
        }

        var list = new List<Triangle>();
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                var lower = new Triangle(grid[i, j], grid[i + 1, j], grid[i + 1, j + 1]);
                var upper = new Triangle(grid[i, j], grid[i, j + 1], grid[i + 1, j + 1]);
                list.Add(lower);
                list.Add(upper);
            }
        }
        return list;
    }
}
