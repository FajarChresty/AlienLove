using System.Collections.Generic;

namespace AlienLove.Matchmaking.Tests
{
    /// <summary>Pembuat karakter kecil untuk test. Tidak memakai folder Dummy.</summary>
    public static class TestHelpers
    {
        public static CharacterRecord Make(string id, int traitValue = 0, int prefValue = 0,
            string[] tags = null, string[] green = null, string[] red = null)
        {
            var c = new CharacterRecord { characterId = id, displayName = id, dealBreakerId = "x" };
            for (int i = 0; i < TraitCatalog.TraitCount; i++)
            {
                c.actualTraits[i] = traitValue;
                c.partnerPreferences[i] = prefValue;
            }
            if (tags != null) c.tags.AddRange(tags);
            if (green != null) c.greenFlags.AddRange(green);
            if (red != null) c.redFlags.AddRange(red);
            return c;
        }
    }

    /// <summary>Random palsu: selalu mengembalikan nilai tetap.</summary>
    public class FixedRandom : IRandomSource
    {
        private readonly double value;
        public FixedRandom(double v) { value = v; }
        public double NextDouble() { return value; }
    }

    public class AlwaysSevereRule : ISevereFailureRule
    {
        public bool IsSevere(CompatibilityResult result) { return true; }
    }

    /// <summary>Merakit seluruh sistem untuk test controller.</summary>
    public class Harness
    {
        public MatchmakingConfig cfg = MatchmakingConfig.CreateDefault();
        public CharacterRepository repo = new CharacterRepository();
        public CandidateDatabase db;
        public CompatibilityEvaluator eval;
        public InterviewContent content = new InterviewContent();
        public MatchmakingController controller;
        public List<MatchOutcome> events = new List<MatchOutcome>();

        public Harness(double randomValue = 0.0, ISevereFailureRule severe = null)
        {
            db = new CandidateDatabase(repo, cfg);
            eval = new CompatibilityEvaluator(cfg);
            controller = new MatchmakingController(repo, db, eval, cfg, new FixedRandom(randomValue), severe, new InterviewManager(content));
            controller.OutcomeResolved += o => events.Add(o);
        }

        public void AddClient(CharacterRecord c) { string e; repo.TryAdd(c, out e); }

        public void AddRegularCandidate(CharacterRecord c)
        {
            string e;
            repo.TryAdd(c, out e);
            db.RegisterRegular(c.characterId, out e);
        }

        /// <summary>Jalankan sesi sampai siap rekomendasi.</summary>
        public void PrepareToRecommend(string clientId, string candidateId)
        {
            controller.StartSession(clientId);
            controller.GetAvailableCandidates();
            controller.SelectCandidate(candidateId);
            controller.MarkReadyToRecommend();
        }
    }
}
