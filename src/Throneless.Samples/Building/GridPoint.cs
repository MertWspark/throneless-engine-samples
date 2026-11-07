using System;

namespace Throneless.Samples.Building
{
    /// <summary>
    /// Voksel ızgarasında bir hücre. Unity'nin Vector3Int tipi yerine kendi
    /// tipimizi kullanıyoruz; böylece şekil motoru Unity olmadan test edilebiliyor.
    /// </summary>
    public readonly struct GridPoint : IEquatable<GridPoint>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public GridPoint(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(GridPoint other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is GridPoint p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
