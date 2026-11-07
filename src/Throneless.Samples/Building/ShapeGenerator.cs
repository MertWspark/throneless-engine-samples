using System;
using System.Collections.Generic;

namespace Throneless.Samples.Building
{
    /// <summary>Desteklenen inşa şekilleri.</summary>
    public enum ShapeKind
    {
        Single,
        Line,
        Wall,
        Floor,
        Box,
        Circle,
        Sphere,
        Dome
    }

    /// <summary>Dolu şekil mi, yoksa sadece dış kabuk mu.</summary>
    public enum Fill
    {
        Solid,
        Hollow
    }

    public sealed class ShapeRequest
    {
        public ShapeKind Kind = ShapeKind.Single;
        public Fill Fill = Fill.Solid;

        /// <summary>Tek seferde konabilecek en fazla blok. 0 = sınırsız.</summary>
        public int MaxBlocks;
    }

    /// <summary>
    /// Oyuncunun tıkladığı noktalardan inşa edilecek blokların listesini üretir.
    ///
    /// Tasarım kararları:
    /// - Saf fonksiyon: dünyayı okumaz, değiştirmez. Önizleme ve gerçek
    ///   yerleştirme aynı fonksiyonu kullanır, böylece ekranda görülen şekil
    ///   ile konan şekil hiçbir zaman farklı olmaz.
    /// - Belirlenimci (deterministic): aynı girdi her zaman aynı sırada aynı
    ///   listeyi verir. MaxBlocks listeyi kestiği için sıranın sabit olması
    ///   önemli.
    /// - Sadece tamsayı aritmetiği: float yuvarlama farkları yüzünden farklı
    ///   cihazlarda farklı şekil çıkmasın.
    /// </summary>
    public static class ShapeGenerator
    {
        /// <param name="a">İlk tıklama (başlangıç ya da merkez).</param>
        /// <param name="b">İkinci tıklama (bitiş ya da yarıçap).</param>
        /// <param name="c">Üçüncü tıklama (yükseklik). Kutu, küre ve kubbe için.</param>
        public static List<GridPoint> Generate(ShapeRequest request, GridPoint a, GridPoint b, GridPoint c)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var result = new List<GridPoint>();
            var seen = new HashSet<GridPoint>();

            switch (request.Kind)
            {
                case ShapeKind.Single:
                    Add(result, seen, a);
                    break;
                case ShapeKind.Line:
                    Line(result, seen, a, LockToAxis(a, b));
                    break;
                case ShapeKind.Wall:
                    Cuboid(result, seen, a, LockToWall(a, b), request.Fill);
                    break;
                case ShapeKind.Floor:
                    Cuboid(result, seen, a, new GridPoint(b.X, a.Y, b.Z), request.Fill);
                    break;
                case ShapeKind.Box:
                    Cuboid(result, seen, a, new GridPoint(b.X, c.Y, b.Z), request.Fill);
                    break;
                case ShapeKind.Circle:
                    Ring(result, seen, a, Radius(a, b), a.Y, request.Fill == Fill.Hollow);
                    break;
                case ShapeKind.Sphere:
                    Ellipsoid(result, seen, a, Radius(a, b), Math.Abs(c.Y - a.Y), request.Fill, domeOnly: false);
                    break;
                case ShapeKind.Dome:
                    Ellipsoid(result, seen, a, Radius(a, b), Math.Abs(c.Y - a.Y), request.Fill, domeOnly: true);
                    break;
            }

            if (request.MaxBlocks > 0 && result.Count > request.MaxBlocks)
                result.RemoveRange(request.MaxBlocks, result.Count - request.MaxBlocks);

            return result;
        }

        public static List<GridPoint> Generate(ShapeRequest request, GridPoint a, GridPoint b)
            => Generate(request, a, b, b);

        /// <summary>Bu şekil yükseklik için üçüncü bir tıklama istiyor mu.</summary>
        public static bool NeedsHeightClick(ShapeKind kind)
            => kind == ShapeKind.Box || kind == ShapeKind.Sphere || kind == ShapeKind.Dome;

        // ─── Eksen kilitleme ────────────────────────────────────────────────

        /// <summary>
        /// Çizgi modunda oyuncu düz bir çizgi bekler. Fare biraz kaydığında
        /// çapraz bir çizgi çıkmasın diye en uzun fark hangi eksendeyse sadece
        /// o eksen korunur.
        /// </summary>
        public static GridPoint LockToAxis(GridPoint a, GridPoint b)
        {
            int dx = Math.Abs(b.X - a.X), dy = Math.Abs(b.Y - a.Y), dz = Math.Abs(b.Z - a.Z);
            if (dx >= dy && dx >= dz) return new GridPoint(b.X, a.Y, a.Z);
            if (dy >= dz) return new GridPoint(a.X, b.Y, a.Z);
            return new GridPoint(a.X, a.Y, b.Z);
        }

        /// <summary>Duvar dikey bir düzlemdir: yatayda en uzun eksen korunur, yükseklik serbest.</summary>
        public static GridPoint LockToWall(GridPoint a, GridPoint b)
        {
            int dx = Math.Abs(b.X - a.X), dz = Math.Abs(b.Z - a.Z);
            return dx >= dz ? new GridPoint(b.X, b.Y, a.Z) : new GridPoint(a.X, b.Y, b.Z);
        }

        // ─── Şekiller ───────────────────────────────────────────────────────

        static void Line(List<GridPoint> output, HashSet<GridPoint> seen, GridPoint a, GridPoint b)
        {
            int dx = b.X - a.X, dy = b.Y - a.Y, dz = b.Z - a.Z;
            int steps = Math.Max(Math.Abs(dx), Math.Max(Math.Abs(dy), Math.Abs(dz)));
            for (int i = 0; i <= steps; i++)
            {
                Add(output, seen, new GridPoint(
                    a.X + RoundDiv(dx * i, steps),
                    a.Y + RoundDiv(dy * i, steps),
                    a.Z + RoundDiv(dz * i, steps)));
            }
        }

        static void Cuboid(List<GridPoint> output, HashSet<GridPoint> seen, GridPoint a, GridPoint b, Fill fill)
        {
            int x0 = Math.Min(a.X, b.X), x1 = Math.Max(a.X, b.X);
            int y0 = Math.Min(a.Y, b.Y), y1 = Math.Max(a.Y, b.Y);
            int z0 = Math.Min(a.Z, b.Z), z1 = Math.Max(a.Z, b.Z);

            // Kaç eksen tek katmanlı? 0: hacimli kutu, 1: duvar/zemin, 2+: çizgi gibi.
            int flatAxes = (x0 == x1 ? 1 : 0) + (y0 == y1 ? 1 : 0) + (z0 == z1 ? 1 : 0);

            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            for (int z = z0; z <= z1; z++)
            {
                if (fill == Fill.Hollow)
                {
                    if (flatAxes == 0)
                    {
                        // Hacimli kutu: sadece dış yüzeyler.
                        bool onFace = x == x0 || x == x1 || y == y0 || y == y1 || z == z0 || z == z1;
                        if (!onFace) continue;
                    }
                    else if (flatAxes == 1)
                    {
                        // Düz bir duvar ya da zemin: sadece çerçevesi.
                        if (IsInsideOfFlat(x, y, z, x0, x1, y0, y1, z0, z1)) continue;
                    }
                    // 1 blok genişliğindeki şekillerde içi boşaltılacak bir alan yok.
                }
                Add(output, seen, new GridPoint(x, y, z));
            }
        }

        /// <summary>İçi boş bir duvar ya da zemin sadece çerçevesinden oluşur.</summary>
        static bool IsInsideOfFlat(int x, int y, int z, int x0, int x1, int y0, int y1, int z0, int z1)
        {
            int edges = 0;
            if (x0 != x1 && (x == x0 || x == x1)) edges++;
            if (y0 != y1 && (y == y0 || y == y1)) edges++;
            if (z0 != z1 && (z == z0 || z == z1)) edges++;
            return edges == 0;
        }

        /// <summary>Yatay bir halka. Kare uzaklık karşılaştırması: karekök ve yuvarlama yok.</summary>
        static void Ring(List<GridPoint> output, HashSet<GridPoint> seen, GridPoint center, int r, int y, bool edgeOnly)
        {
            if (r <= 0)
            {
                Add(output, seen, new GridPoint(center.X, y, center.Z));
                return;
            }

            // +r, kenarlardaki tek blokluk çıkıntıları yumuşatıyor.
            int outer = r * r + r;
            int inner = (r - 1) * (r - 1) + (r - 1);

            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                int d = dx * dx + dz * dz;
                if (d > outer) continue;
                if (edgeOnly && d <= inner) continue;
                Add(output, seen, new GridPoint(center.X + dx, y, center.Z + dz));
            }
        }

        /// <summary>
        /// Küre ve kubbe aynı fonksiyondan çıkıyor: yatay ve dikey yarıçapı
        /// ayrı olan bir elipsoid. Kubbe sadece üst yarıyı çiziyor.
        /// </summary>
        static void Ellipsoid(List<GridPoint> output, HashSet<GridPoint> seen, GridPoint center,
                              int r, int ry, Fill fill, bool domeOnly)
        {
            if (ry == 0) ry = r;
            int yStart = domeOnly ? 0 : -ry;

            for (int dy = yStart; dy <= ry; dy++)
            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                if (IsOutside(dx, dy, dz, r, ry)) continue;

                // Kabuk: altı komşusundan en az biri dışarıdaysa blok yüzeydedir.
                // (Daha basit bir yöntem kutuplarda delik bırakıyordu; testlerde yakalandı.)
                if (fill == Fill.Hollow &&
                    !IsOutside(dx + 1, dy, dz, r, ry) && !IsOutside(dx - 1, dy, dz, r, ry) &&
                    !IsOutside(dx, dy + 1, dz, r, ry) && !IsOutside(dx, dy - 1, dz, r, ry) &&
                    !IsOutside(dx, dy, dz + 1, r, ry) && !IsOutside(dx, dy, dz - 1, r, ry))
                    continue;

                Add(output, seen, new GridPoint(center.X + dx, center.Y + dy, center.Z + dz));
            }
        }

        // ─── Yardımcılar ────────────────────────────────────────────────────

        static int Radius(GridPoint a, GridPoint b) => Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Z - a.Z));

        static bool IsOutside(int dx, int dy, int dz, int r, int ry)
        {
            if (r <= 0 || ry <= 0) return !(dx == 0 && dy == 0 && dz == 0);
            double n = (double)(dx * dx + dz * dz) / (r * r) + (double)(dy * dy) / (ry * ry);
            return n > 1.0001;
        }

        /// <summary>En yakına yuvarlayan tamsayı bölme (negatif sayılarda da doğru).</summary>
        static int RoundDiv(int numerator, int denominator)
        {
            if (denominator == 0) return 0;
            return numerator >= 0
                ? (numerator + denominator / 2) / denominator
                : -((-numerator + denominator / 2) / denominator);
        }

        static void Add(List<GridPoint> output, HashSet<GridPoint> seen, GridPoint p)
        {
            if (seen.Add(p)) output.Add(p);
        }
    }
}
