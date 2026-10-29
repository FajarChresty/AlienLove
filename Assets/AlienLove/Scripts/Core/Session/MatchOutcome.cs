namespace AlienLove.Matchmaking
{
    /// <summary>Hasil akhir satu sesi. Dikirim ke sistem ekonomi / weekly evaluation (Sub-GDD 15.6 dan 17).</summary>
    public class MatchOutcome
    {
        public string outcomeId;                 // unik per sesi, untuk mencegah proses ganda
        public string sessionId;
        public string clientId;
        public string candidateId;               // null kalau tidak ada rekomendasi

        public bool wasNoRecommendation;
        public bool clientAcceptedOptIn;

        public MatchCategory category;
        public double paymentMultiplier;         // dikali "normal payment" oleh sistem ekonomi
        public bool isSevere;

        public bool hasCompatibilityScore;
        public double traitScore;
        public double flagScore;
        public double finalScore;

        public CandidateReturnDecision candidateReturnDecision;
        public int resolvedOrder;                // urutan penyelesaian (bisa diganti hari kalender nanti)
    }
}
