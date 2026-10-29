namespace AlienLove.Matchmaking
{
    /// <summary>Hasil evaluasi satu pasangan (Sub-GDD 10.4).</summary>
    public class CompatibilityResult
    {
        public string characterAId;
        public string characterBId;

        public bool isValid;
        public string error;

        public double traitScore;        // 0-100, komponen 60%
        public double flagScore;         // 0-100, komponen 40%
        public double flagScoreAToB;     // arah: flag A dicek ke B
        public double flagScoreBToA;     // arah: flag B dicek ke A
        public double finalScore;        // 0-100, sudah dibulatkan

        public MatchCategory category;
        public double multiplier;        // pengali pembayaran
    }
}
