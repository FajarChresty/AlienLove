using System;
using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    public class OperationResult
    {
        public bool Success;
        public string Error;
        public static OperationResult Ok() { return new OperationResult { Success = true }; }
        public static OperationResult Fail(string error) { return new OperationResult { Success = false, Error = error }; }
    }

    /// <summary>
    /// Mengoordinasikan satu sesi matchmaking (Sub-GDD bab 3, 9, 10, 12, 13, 14).
    ///
    /// Aturan penting:
    /// - UI hanya memanggil method di sini; UI tidak boleh mengubah ketersediaan karakter atau uang.
    /// - Rumus compatibility TIDAK ada di sini, hanya di CompatibilityEvaluator.
    /// - Controller tidak mengubah saldo; ia hanya mengirim MatchOutcome lewat event OutcomeResolved
    ///   ke sistem ekonomi / weekly evaluation.
    /// - Satu sesi hanya bisa diselesaikan sekali (tidak ada roll ulang, tidak ada pembayaran ganda).
    /// </summary>
    public class MatchmakingController
    {
        private readonly CharacterRepository repo;
        private readonly CandidateDatabase db;
        private readonly CompatibilityEvaluator evaluator;
        private readonly MatchmakingConfig cfg;
        private readonly IRandomSource rng;
        private readonly ISevereFailureRule severeRule;
        private readonly InterviewManager interview;

        private int sessionCounter;
        private int outcomeCounter;
        private readonly List<MatchOutcome> outcomes = new List<MatchOutcome>();

        public MatchmakingSession CurrentSession { get; private set; }
        public MatchOutcome LastOutcome { get; private set; }
        public IReadOnlyList<MatchOutcome> Outcomes { get { return outcomes; } }

        /// <summary>Dipanggil tepat sekali per sesi yang selesai. Pasang sistem ekonomi / weekly evaluation di sini.</summary>
        public event Action<MatchOutcome> OutcomeResolved;

        public MatchmakingController(
            CharacterRepository repository,
            CandidateDatabase database,
            CompatibilityEvaluator compatibilityEvaluator,
            MatchmakingConfig config,
            IRandomSource randomSource,
            ISevereFailureRule severeFailureRule = null,
            InterviewManager interviewManager = null)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            if (database == null) throw new ArgumentNullException("database");
            if (compatibilityEvaluator == null) throw new ArgumentNullException("compatibilityEvaluator");
            if (config == null) throw new ArgumentNullException("config");
            if (randomSource == null) throw new ArgumentNullException("randomSource");

            repo = repository;
            db = database;
            evaluator = compatibilityEvaluator;
            cfg = config;
            rng = randomSource;
            severeRule = severeFailureRule ?? new NoSevereFailureRule();
            interview = interviewManager;
        }

        // ---------- Memulai sesi ----------

        public OperationResult StartSession(string clientId)
        {
            if (CurrentSession != null && CurrentSession.IsActive)
                return OperationResult.Fail("Masih ada sesi aktif.");

            var client = repo.Get(clientId);
            if (client == null) return OperationResult.Fail("Klien tidak ditemukan: " + clientId);

            var v = CharacterValidator.Validate(client);
            if (!v.IsValid) return OperationResult.Fail("Data klien tidak valid: " + v.ErrorText);

            if (client.availability == AvailabilityStatus.Unavailable || client.availability == AvailabilityStatus.Removed)
                return OperationResult.Fail("Klien sudah tidak tersedia.");

            sessionCounter++;
            var session = new MatchmakingSession("session_" + sessionCounter, clientId);
            session.TryTransition(SessionState.ClientReview);
            CurrentSession = session;
            return OperationResult.Ok();
        }

        // ---------- Kandidat ----------

        /// <summary>Daftar kandidat yang bisa dipilih. Pertama kali dipanggil memindahkan sesi ke CandidateSelection.</summary>
        public List<CharacterRecord> GetAvailableCandidates()
        {
            if (CurrentSession == null || !CurrentSession.IsActive) return new List<CharacterRecord>();
            if (CurrentSession.state == SessionState.ClientReview)
                CurrentSession.TryTransition(SessionState.CandidateSelection);
            return db.GetAvailableCandidates(CurrentSession.clientCharacterId);
        }

        public OperationResult SelectCandidate(string candidateId)
        {
            var err = RequireDecisionPhase();
            if (err != null) return err;

            var check = ValidateCandidate(candidateId);
            if (!check.Success) return check;

            CurrentSession.selectedCandidateId = candidateId;
            if (CurrentSession.state != SessionState.CandidateSelection)
                CurrentSession.TryTransition(SessionState.CandidateSelection);
            return OperationResult.Ok();
        }

        // ---------- Wawancara ----------

        public OperationResult BeginInterview()
        {
            var err = RequireDecisionPhase();
            if (err != null) return err;
            if (CurrentSession.state == SessionState.Interviewing) return OperationResult.Ok();
            return CurrentSession.TryTransition(SessionState.Interviewing)
                ? OperationResult.Ok() : OperationResult.Fail("Tidak bisa mulai wawancara dari state " + CurrentSession.state);
        }

        public List<InterviewTopicInfo> GetInterviewTopics(string characterId)
        {
            if (interview == null || CurrentSession == null) return new List<InterviewTopicInfo>();
            return interview.GetAvailableTopics(CurrentSession.sessionId, characterId);
        }

        public OperationResult Ask(string characterId, string topicId, out InterviewResponse response)
        {
            response = null;
            if (interview == null) return OperationResult.Fail("Sistem wawancara tidak dipasang.");
            if (CurrentSession == null || CurrentSession.state != SessionState.Interviewing)
                return OperationResult.Fail("Wawancara hanya bisa dilakukan saat state Interviewing.");

            bool isClient = characterId == CurrentSession.clientCharacterId;
            bool isSelected = characterId == CurrentSession.selectedCandidateId;
            if (!isClient && !isSelected)
                return OperationResult.Fail("Hanya klien atau kandidat terpilih yang bisa diwawancara.");

            string error;
            if (!interview.Ask(CurrentSession.sessionId, characterId, topicId, out response, out error))
                return OperationResult.Fail(error);
            return OperationResult.Ok();
        }

        public OperationResult MarkReadyToRecommend()
        {
            var err = RequireDecisionPhase();
            if (err != null) return err;
            if (CurrentSession.state == SessionState.ReadyToRecommend) return OperationResult.Ok();
            return CurrentSession.TryTransition(SessionState.ReadyToRecommend)
                ? OperationResult.Ok() : OperationResult.Fail("Tidak bisa siap merekomendasikan dari state " + CurrentSession.state);
        }

        // ---------- Rekomendasi ----------

        /// <summary>Rekomendasikan kandidat. Validasi gagal tidak mengubah apa pun (Sub-GDD 10.2).</summary>
        public OperationResult Recommend(string candidateId)
        {
            if (CurrentSession == null) return OperationResult.Fail("Tidak ada sesi.");
            if (CurrentSession.state == SessionState.Completed) return OperationResult.Fail("Sesi sudah selesai.");
            if (CurrentSession.state != SessionState.ReadyToRecommend)
                return OperationResult.Fail("Rekomendasi hanya bisa dikirim saat state ReadyToRecommend (sekarang " + CurrentSession.state + ").");

            var check = ValidateCandidate(candidateId);
            if (!check.Success) return check;

            var client = repo.Get(CurrentSession.clientCharacterId);
            var candidate = repo.Get(candidateId);

            CurrentSession.TryTransition(SessionState.Evaluating);
            var result = evaluator.Evaluate(client, candidate);
            if (!result.isValid)
            {
                CurrentSession.TryTransition(SessionState.ReadyToRecommend);
                return OperationResult.Fail("Evaluasi gagal: " + result.error);
            }

            CurrentSession.TryTransition(SessionState.ResolvingOutcome);

            bool success = result.category == MatchCategory.PerfectSuccess || result.category == MatchCategory.NormalSuccess;
            bool severe = !success && severeRule.IsSevere(result);

            var returnDecision = CandidateReturnDecision.NotApplicable;
            if (success)
            {
                // Dua-duanya jadi tidak tersedia sekaligus (atomik).
                client.availability = AvailabilityStatus.Unavailable;
                client.history.Add("Matched with " + candidateId);
                db.MarkUnavailable(candidateId);
                if (db.IsRegistered(client.characterId)) db.MarkUnavailable(client.characterId);
            }
            else if (severe)
            {
                returnDecision = db.ApplySevereFailure(candidateId);
            }
            else
            {
                returnDecision = db.ApplyOrdinaryFailure(candidateId, rng);
            }

            var outcome = new MatchOutcome
            {
                outcomeId = NextOutcomeId(),
                sessionId = CurrentSession.sessionId,
                clientId = client.characterId,
                candidateId = candidateId,
                wasNoRecommendation = false,
                category = result.category,
                paymentMultiplier = result.multiplier,
                isSevere = severe,
                hasCompatibilityScore = true,
                traitScore = result.traitScore,
                flagScore = result.flagScore,
                finalScore = result.finalScore,
                candidateReturnDecision = returnDecision
            };

            return Finalize(outcome);
        }

        // ---------- Tidak ada rekomendasi ----------

        /// <summary>
        /// Pemain memutuskan tidak ada kandidat yang cocok. Selalu NormalFailure tanpa bayaran, tanpa skor (Sub-GDD 9.5 dan 11.6).
        /// clientAcceptsOptIn = true: klien didaftarkan sebagai OPT_IN_CLIENT dengan character_id yang sama.
        /// </summary>
        public OperationResult DeclineMatch(bool clientAcceptsOptIn)
        {
            if (CurrentSession == null) return OperationResult.Fail("Tidak ada sesi.");
            if (CurrentSession.state == SessionState.Completed) return OperationResult.Fail("Sesi sudah selesai.");
            var err = RequireDecisionPhase();
            if (err != null) return err;

            if (CurrentSession.state != SessionState.ReadyToRecommend)
                CurrentSession.TryTransition(SessionState.ReadyToRecommend);

            if (clientAcceptsOptIn && !db.IsRegistered(CurrentSession.clientCharacterId))
            {
                string regError;
                if (!db.RegisterOptIn(CurrentSession.clientCharacterId, out regError))
                    return OperationResult.Fail("Gagal mendaftarkan klien: " + regError);
            }

            CurrentSession.TryTransition(SessionState.ResolvingOutcome);

            var outcome = new MatchOutcome
            {
                outcomeId = NextOutcomeId(),
                sessionId = CurrentSession.sessionId,
                clientId = CurrentSession.clientCharacterId,
                candidateId = null,
                wasNoRecommendation = true,
                clientAcceptedOptIn = clientAcceptsOptIn,
                category = MatchCategory.NormalFailure,
                paymentMultiplier = cfg.normalFailureMultiplier,
                isSevere = false,
                hasCompatibilityScore = false,
                candidateReturnDecision = CandidateReturnDecision.NotApplicable
            };

            return Finalize(outcome);
        }

        // ---------- internal ----------

        private OperationResult Finalize(MatchOutcome outcome)
        {
            outcome.resolvedOrder = outcomes.Count + 1;
            CurrentSession.outcome = outcome;
            CurrentSession.TryTransition(SessionState.Completed);
            LastOutcome = outcome;
            outcomes.Add(outcome);

            var handler = OutcomeResolved;
            if (handler != null) handler(outcome);
            return OperationResult.Ok();
        }

        private string NextOutcomeId()
        {
            outcomeCounter++;
            return "outcome_" + outcomeCounter;
        }

        private OperationResult RequireDecisionPhase()
        {
            if (CurrentSession == null) return OperationResult.Fail("Tidak ada sesi.");
            switch (CurrentSession.state)
            {
                case SessionState.CandidateSelection:
                case SessionState.Interviewing:
                case SessionState.ReadyToRecommend:
                    return null;
                case SessionState.ClientReview:
                    // Boleh langsung lanjut ke pemilihan kandidat.
                    CurrentSession.TryTransition(SessionState.CandidateSelection);
                    return null;
                default:
                    return OperationResult.Fail("Aksi tidak valid pada state " + CurrentSession.state);
            }
        }

        private OperationResult ValidateCandidate(string candidateId)
        {
            if (string.IsNullOrEmpty(candidateId)) return OperationResult.Fail("Kandidat belum dipilih.");
            if (candidateId == CurrentSession.clientCharacterId) return OperationResult.Fail("Kandidat tidak boleh sama dengan klien.");
            if (!repo.Contains(candidateId)) return OperationResult.Fail("Kandidat tidak ditemukan.");
            if (!db.IsRegistered(candidateId)) return OperationResult.Fail("Karakter belum terdaftar sebagai kandidat.");
            if (!db.IsEligible(candidateId)) return OperationResult.Fail("Kandidat tidak tersedia.");
            return OperationResult.Ok();
        }
    }
}
