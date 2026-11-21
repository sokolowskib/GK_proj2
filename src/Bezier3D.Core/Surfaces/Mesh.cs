using System.Numerics;
using Bezier3D.Core.Maths;

namespace Bezier3D.Core.Surfaces;

/// <summary>Wierzchołek siatki: parametry (U, V), wektory przed obrotem i po obrocie.</summary>
public sealed class MeshVertex
{
    public float U, V;

    // przed obrotem — liczone raz w Triangulate
    public Vector3 P, Pu, Pv, N;

    // po obrocie — liczone przy każdej zmianie orientacji
    public Vector3 Pr, Pur, Pvr, Nr;

    /// <summary>Pr = R·P, Pur = R·Pu, Pvr = R·Pv, Nr = normalize(R·N) — zawsze od oryginałów.</summary>
    public void Rotate(Mat3 r)
    {
        Pr = r * P;
        Pur = r * Pu;
        Pvr = r * Pv;
        Nr = Vector3.Normalize(r * N);
    }
}

/// <summary>Trójkąt wskazuje na wierzchołki współdzielone z sąsiadami (nie kopie).</summary>
public sealed record Triangle(MeshVertex A, MeshVertex B, MeshVertex C);
