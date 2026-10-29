namespace AlienLove.Matchmaking
{
    /// <summary>Sumber kandidat. REGULAR_CANDIDATE / OPT_IN_CLIENT di GDD.</summary>
    public enum CandidateSource
    {
        RegularCandidate,
        OptInClient
    }

    /// <summary>Status ketersediaan karakter untuk matchmaking.</summary>
    public enum AvailabilityStatus
    {
        Available,
        PendingResult,
        Unavailable,   // sudah match sukses
        Removed        // tidak akan kembali (gagal berat / tidak lolos return roll)
    }

    /// <summary>Kategori hasil (Sub-GDD bab 11.6).</summary>
    public enum MatchCategory
    {
        PerfectSuccess,
        NormalSuccess,
        NormalFailure,
        VeryBadFailure
    }

    /// <summary>State machine sesi matchmaking (bab 13).</summary>
    public enum SessionState
    {
        NotStarted,
        ClientReview,
        CandidateSelection,
        Interviewing,
        ReadyToRecommend,
        Evaluating,
        ResolvingOutcome,
        Completed
    }

    /// <summary>Apa yang terjadi pada kandidat setelah sebuah hasil.</summary>
    public enum CandidateReturnDecision
    {
        NotApplicable,
        Returned,
        NotReturned,
        Removed
    }
}
