using System;
using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// SEMUA angka balancing ada di sini (bukan di kode logika), supaya gampang diubah saat playtest.
    /// Nilai default = Sub-GDD bab 11 + jawaban atasan.
    /// </summary>
    [Serializable]
    public class MatchmakingConfig
    {
        // Bobot komponen akhir (bab 11.1)
        public double traitWeight = 0.60;
        public double flagWeight = 0.40;

        // Selisih posisi (0..3) -> persen cocok. Atasan: 100/75/50/25 (25 bisa jadi 0 setelah test).
        public double[] traitMatchTable = new double[] { 100, 75, 50, 25 };

        // Green Flag yang terpenuhi menambah setengah dari nilainya (bab 11.3 langkah 4).
        public double greenFlagBonusFactor = 0.5;

        // Karakter tanpa flag sama sekali -> sisi itu 100% (jawaban atasan).
        public double noFlagScore = 100;

        // Batas kategori (bab 11.6). Tepat 20/50/90 masuk tier yang lebih tinggi.
        public double perfectMin = 90;
        public double normalSuccessMin = 50;
        public double normalFailureMin = 20;

        // Pengali pembayaran dari "normal payment" milik sistem ekonomi.
        public double perfectMultiplier = 3;
        public double normalSuccessMultiplier = 1;
        public double normalFailureMultiplier = 0;
        public double veryBadFailureMultiplier = -2;

        // Peluang kandidat reguler kembali setelah gagal biasa (Main GDD 7.5).
        public double regularReturnChance = 0.5;

        // Pembulatan skor akhir sebelum dibandingkan dengan batas, supaya tidak meleset karena float.
        public int scoreDecimals = 2;

        public static MatchmakingConfig CreateDefault() { return new MatchmakingConfig(); }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (traitMatchTable == null || traitMatchTable.Length != 4)
                errors.Add("traitMatchTable harus berisi 4 angka (selisih 0..3).");
            if (Math.Abs(traitWeight + flagWeight - 1.0) > 1e-9)
                errors.Add("traitWeight + flagWeight harus = 1.");
            if (!(perfectMin > normalSuccessMin && normalSuccessMin > normalFailureMin && normalFailureMin >= 0))
                errors.Add("Batas kategori harus berurutan: perfectMin > normalSuccessMin > normalFailureMin >= 0.");
            if (regularReturnChance < 0 || regularReturnChance > 1)
                errors.Add("regularReturnChance harus antara 0 dan 1.");
            return errors;
        }
    }
}
