using System.Globalization;
using System.Numerics;

namespace Bezier3D.Core.Surfaces;

/// <summary>
/// Plik: kolejne grupy po 16 wierszy "x y z", jedna grupa = jedna powierzchnia.
/// Wiersz k w grupie to punkt V[k / 4, k % 4].
/// </summary>
public static class SurfaceFileParser
{
    /// <exception cref="FormatException">Zła liczba wierszy lub wiersz, który nie jest trzema liczbami.</exception>
    public static List<BezierSurface> Parse(string text)
    {
        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

        if (lines.Count == 0 || lines.Count % 16 != 0)
            throw new FormatException("Ilość linii w pliku konfiguracyjnym jest nieodpowiednia");

        int k = 0;

        List<BezierSurface> result = [];
        Vector3[,] v = new Vector3[4, 4];

        foreach (var line in lines)
        {
            var parts =
                line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);  // kilka spacji pod rzad bez problemu

            if (parts.Length != 3)
                throw new FormatException("Nieodpowiednia ilość współrzędnych w linii");

            Vector3 point = Vector3.Zero;
            for (int i = 0; i < 3; i++)
            {
                point[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
            }
            int p = k % 16;
            v[p / 4, p % 4] = point;
            if ((k + 1) % 16 == 0)
            {
                result.Add(new BezierSurface(v));
                v = new Vector3[4, 4];
            }
            k++;
        }

        return result;
    }
}
