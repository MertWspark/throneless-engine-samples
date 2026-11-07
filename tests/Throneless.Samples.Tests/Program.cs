using System;
using System.Collections.Generic;
using System.Linq;
using Throneless.Samples.Building;
using Throneless.Samples.Timing;

// Basit test çalıştırıcısı: harici paket gerektirmez.
//   dotnet run --project tests/Throneless.Samples.Tests
var tests = new List<(string Name, Action Body)>
{
    // ─── Şekil motoru ───────────────────────────────────────────────────
    ("Tek blok sadece tıklanan hücreyi verir", () =>
    {
        var r = Gen(ShapeKind.Single, P(1, 2, 3), P(9, 9, 9));
        Equal(1, r.Count);
        Equal(P(1, 2, 3), r[0]);
    }),

    ("Çizgi en uzun eksene kilitlenir (fare kayması çapraz çizgi yapmaz)", () =>
    {
        var r = Gen(ShapeKind.Line, P(0, 0, 0), P(5, 1, 0));
        Equal(6, r.Count);
        True(r.All(p => p.Y == 0 && p.Z == 0), "bütün bloklar X ekseninde olmalı");
    }),

    ("Çizgi negatif yönde de çalışır", () =>
    {
        var r = Gen(ShapeKind.Line, P(0, 0, 0), P(0, 0, -4));
        Equal(5, r.Count);
        Equal(P(0, 0, -4), r.Last());
    }),

    ("Zemin yatay kalır ve doğru sayıda blok üretir", () =>
    {
        var r = Gen(ShapeKind.Floor, P(0, 5, 0), P(3, 9, 2));
        Equal(4 * 3, r.Count);
        True(r.All(p => p.Y == 5), "zemin ilk tıklamanın yüksekliğinde kalmalı");
    }),

    ("İçi boş duvar sadece çerçeveden oluşur", () =>
    {
        var r = Gen(ShapeKind.Wall, P(0, 0, 0), P(4, 4, 0), fill: Fill.Hollow);
        Equal(16, r.Count); // 5x5 = 25 blok, içteki 3x3 = 9 blok boş
        False(r.Contains(P(2, 2, 0)), "duvarın ortası boş olmalı");
    }),

    ("İçi boş kutu sadece dış yüzeylerden oluşur", () =>
    {
        var r = Gen(ShapeKind.Box, P(0, 0, 0), P(4, 0, 4), P(0, 4, 0), Fill.Hollow);
        Equal(125 - 27, r.Count); // 5x5x5 küpün içindeki 3x3x3 boş
        False(r.Contains(P(2, 2, 2)), "kutunun içi boş olmalı");
    }),

    ("1 blok genişliğindeki içi boş duvar delik açmaz", () =>
    {
        var r = Gen(ShapeKind.Wall, P(0, 0, 0), P(0, 4, 0), fill: Fill.Hollow);
        Equal(5, r.Count);
    }),

    ("Çember simetriktir", () =>
    {
        var r = Gen(ShapeKind.Circle, P(0, 0, 0), P(4, 0, 0));
        var set = new HashSet<GridPoint>(r);
        foreach (var p in r)
        {
            True(set.Contains(P(-p.X, p.Y, p.Z)), $"X'e göre ayna eksik: {p}");
            True(set.Contains(P(p.X, p.Y, -p.Z)), $"Z'ye göre ayna eksik: {p}");
            True(set.Contains(P(p.Z, p.Y, p.X)), $"köşegene göre ayna eksik: {p}");
        }
    }),

    ("İçi boş kürenin kutuplarında delik yok", () =>
    {
        var r = Gen(ShapeKind.Sphere, P(0, 0, 0), P(5, 0, 0), P(0, 5, 0), Fill.Hollow);
        True(r.Contains(P(0, 5, 0)), "kuzey kutbu dolu olmalı");
        True(r.Contains(P(0, -5, 0)), "güney kutbu dolu olmalı");
        False(r.Contains(P(0, 0, 0)), "kürenin merkezi boş olmalı");
    }),

    ("Kubbe sadece üst yarıyı çizer", () =>
    {
        var r = Gen(ShapeKind.Dome, P(0, 10, 0), P(4, 10, 0), P(0, 14, 0));
        True(r.All(p => p.Y >= 10), "kubbe zeminin altına inmemeli");
        True(r.Contains(P(0, 14, 0)), "kubbenin tepesi olmalı");
    }),

    ("Aynı girdi her zaman aynı sırada aynı listeyi verir", () =>
    {
        var first = Gen(ShapeKind.Sphere, P(3, 3, 3), P(7, 3, 3), P(3, 6, 3), Fill.Hollow);
        var second = Gen(ShapeKind.Sphere, P(3, 3, 3), P(7, 3, 3), P(3, 6, 3), Fill.Hollow);
        True(first.SequenceEqual(second), "sıra değişmemeli");
    }),

    ("Hiçbir şekil aynı bloğu iki kez üretmez", () =>
    {
        foreach (ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))
        {
            var r = Gen(kind, P(0, 0, 0), P(6, 3, 4), P(0, 5, 0));
            Equal(r.Count, r.Distinct().Count());
        }
    }),

    ("MaxBlocks sınırı listeyi keser", () =>
    {
        var request = new ShapeRequest { Kind = ShapeKind.Floor, MaxBlocks = 10 };
        var r = ShapeGenerator.Generate(request, P(0, 0, 0), P(9, 0, 9));
        Equal(10, r.Count);
    }),

    // ─── Adlandırılmış zamanlayıcılar ───────────────────────────────────
    ("Koşul doğruyken sayaç verilen sürede 1'e çıkar", () =>
    {
        var timers = new NamedTimers();
        bool wearing = true;
        timers.Bind("armor_equip", 0.5f, () => wearing);
        timers.Tick(0.25f);
        Near(0.5f, timers.Value("armor_equip"));
        timers.Tick(0.25f);
        Near(1f, timers.Value("armor_equip"));
        timers.Tick(1f);
        Near(1f, timers.Value("armor_equip")); // 1'in üstüne çıkmaz
    }),

    ("Koşul yanlış olunca aynı sayaç geri iner", () =>
    {
        var timers = new NamedTimers();
        bool flying = true;
        timers.Bind("flight_pose", 1f, () => flying);
        timers.Tick(1f);
        flying = false;
        timers.Tick(0.4f);
        Near(0.6f, timers.Value("flight_pose"));
    }),

    ("Yumuşatılmış değer başta ve sonda yavaştır", () =>
    {
        var timers = new NamedTimers();
        timers.Open("door", 1f);
        timers.Tick(0.1f);
        True(timers.Smoothed("door") < timers.Value("door"), "başta doğrusal değerden küçük olmalı");
        timers.Tick(0.4f);
        Near(0.5f, timers.Smoothed("door")); // ortada eşit
    }),

    ("Tek atışlık sayaç 1'e varınca geri döner ve silinir", () =>
    {
        var timers = new NamedTimers();
        timers.Open("hit_flash", 0.1f, oneShot: true);
        timers.Tick(0.1f);
        Equal(1, timers.Completions("hit_flash"));
        timers.Tick(0.1f);
        False(timers.Exists("hit_flash"), "tek atışlık sayaç silinmiş olmalı");
    }),

    ("Olmayan sayaç 0 döner", () =>
    {
        Near(0f, new NamedTimers().Value("yok"));
    }),
};

int failed = 0;
foreach (var (name, body) in tests)
{
    try
    {
        body();
        Console.WriteLine($"  GEÇTİ  {name}");
    }
    catch (Exception e)
    {
        failed++;
        Console.WriteLine($"  KALDI  {name}\n         {e.Message}");
    }
}

Console.WriteLine(failed == 0
    ? $"\nTüm testler geçti ({tests.Count})."
    : $"\n{failed} / {tests.Count} test başarısız.");
return failed == 0 ? 0 : 1;


// ─── Yardımcılar ────────────────────────────────────────────────────────
static GridPoint P(int x, int y, int z) => new GridPoint(x, y, z);

static List<GridPoint> Gen(ShapeKind kind, GridPoint a, GridPoint b, GridPoint? c = null, Fill fill = Fill.Solid)
    => ShapeGenerator.Generate(new ShapeRequest { Kind = kind, Fill = fill }, a, b, c ?? b);

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"beklenen {expected}, gelen {actual}");
}

static void Near(float expected, float actual)
{
    if (Math.Abs(expected - actual) > 0.0001f)
        throw new Exception($"beklenen {expected}, gelen {actual}");
}

static void True(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static void False(bool condition, string message) => True(!condition, message);
