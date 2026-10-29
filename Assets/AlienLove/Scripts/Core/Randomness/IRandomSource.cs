using System;

namespace AlienLove.Matchmaking
{
    /// <summary>Sumber acak yang bisa di-seed, supaya hasil bisa direproduksi saat testing.</summary>
    public interface IRandomSource
    {
        /// <summary>Angka acak 0.0 (termasuk) sampai 1.0 (tidak termasuk).</summary>
        double NextDouble();
    }

    public class SeededRandomSource : IRandomSource
    {
        private readonly Random rng;
        public SeededRandomSource(int seed) { rng = new Random(seed); }
        public double NextDouble() { return rng.NextDouble(); }
    }
}
