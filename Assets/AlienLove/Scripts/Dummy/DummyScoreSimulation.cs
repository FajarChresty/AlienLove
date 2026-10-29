using System;
using System.Text;

namespace AlienLove.Matchmaking.Dummy
{
    /// <summary>
    /// Simulasi sebaran skor pasangan acak, untuk mengecek apakah balancing terlalu mudah / terlalu sulit.
    /// Hanya alat bantu pengembangan; aman dihapus bersama folder Dummy.
    /// </summary>
    public static class DummyScoreSimulation
    {
        public static string Run(MatchmakingConfig cfg, int pairs = 10000, int seed = 42)
        {
            var eval = new CompatibilityEvaluator(cfg);
            var rng = new Random(seed);

            int[] cat = new int[4];
            int[] hist = new int[10];
            double sum = 0, min = 100, max = 0;

            for (int i = 0; i < pairs; i++)
            {
                var a = DummyDataFactory.CreateRandomCharacter("A" + i, "A", rng);
                var b = DummyDataFactory.CreateRandomCharacter("B" + i, "B", rng);
                var r = eval.Evaluate(a, b);
                sum += r.finalScore;
                if (r.finalScore < min) min = r.finalScore;
                if (r.finalScore > max) max = r.finalScore;
                cat[(int)r.category]++;
                int bucket = (int)Math.Min(9, r.finalScore / 10.0);
                hist[bucket]++;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== Simulasi " + pairs + " pasangan acak ===");
            sb.AppendLine("Rata-rata: " + (sum / pairs).ToString("0.00") + "%   Min: " + min.ToString("0.00") + "%   Max: " + max.ToString("0.00") + "%");
            sb.AppendLine("Perfect Success : " + Pct(cat[(int)MatchCategory.PerfectSuccess], pairs));
            sb.AppendLine("Normal Success  : " + Pct(cat[(int)MatchCategory.NormalSuccess], pairs));
            sb.AppendLine("Normal Failure  : " + Pct(cat[(int)MatchCategory.NormalFailure], pairs));
            sb.AppendLine("Very Bad Failure: " + Pct(cat[(int)MatchCategory.VeryBadFailure], pairs));
            sb.AppendLine("Histogram skor:");
            for (int i = 0; i < 10; i++)
                sb.AppendLine("  " + (i * 10).ToString("00") + "-" + (i == 9 ? "100" : (i * 10 + 9).ToString("00")) + "% : " + Pct(hist[i], pairs));
            return sb.ToString();
        }

        private static string Pct(int count, int total)
        {
            return (100.0 * count / total).ToString("0.0") + "% (" + count + ")";
        }
    }
}
