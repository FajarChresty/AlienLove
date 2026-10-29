using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    /// <summary>Satu sesi pemrosesan klien, dengan state machine eksplisit (Sub-GDD bab 13).</summary>
    public class MatchmakingSession
    {
        public string sessionId;
        public string clientCharacterId;
        public SessionState state = SessionState.NotStarted;
        public string selectedCandidateId;
        public MatchOutcome outcome;
        public readonly List<string> history = new List<string>();

        public MatchmakingSession(string id, string clientId)
        {
            sessionId = id;
            clientCharacterId = clientId;
        }

        public bool IsActive { get { return state != SessionState.NotStarted && state != SessionState.Completed; } }

        public static bool IsTransitionAllowed(SessionState from, SessionState to)
        {
            switch (from)
            {
                case SessionState.NotStarted:
                    return to == SessionState.ClientReview;
                case SessionState.ClientReview:
                    return to == SessionState.CandidateSelection;
                case SessionState.CandidateSelection:
                    return to == SessionState.Interviewing || to == SessionState.ReadyToRecommend;
                case SessionState.Interviewing:
                    return to == SessionState.CandidateSelection || to == SessionState.ReadyToRecommend;
                case SessionState.ReadyToRecommend:
                    // Evaluating = rekomendasi; ResolvingOutcome = keputusan tanpa rekomendasi (no-match)
                    return to == SessionState.CandidateSelection || to == SessionState.Interviewing
                        || to == SessionState.Evaluating || to == SessionState.ResolvingOutcome;
                case SessionState.Evaluating:
                    // kembali ke ReadyToRecommend kalau evaluasi gagal (data tidak valid)
                    return to == SessionState.ResolvingOutcome || to == SessionState.ReadyToRecommend;
                case SessionState.ResolvingOutcome:
                    return to == SessionState.Completed;
                default:
                    return false;
            }
        }

        public bool TryTransition(SessionState to)
        {
            if (!IsTransitionAllowed(state, to))
            {
                history.Add("INVALID transition " + state + " -> " + to);
                return false;
            }
            history.Add(state + " -> " + to);
            state = to;
            return true;
        }
    }
}
