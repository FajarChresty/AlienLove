using System.Text;
using UnityEngine;

namespace AlienLove.Matchmaking.Dummy
{
    /// <summary>
    /// Demo di Console Unity: memasang seluruh sistem dengan data dummy, menjalankan satu sesi penuh,
    /// lalu (opsional) mencetak simulasi sebaran skor.
    ///
    /// Cara pakai: taruh di GameObject kosong di scene, tekan Play, lihat Console.
    /// Aman dihapus bersama folder Scripts/Dummy.
    /// </summary>
    public class DummyMatchmakingDemo : MonoBehaviour
    {
        [Tooltip("Opsional. Kosongkan untuk memakai angka default.")]
        public MatchmakingConfigAsset configAsset;
        public int seed = 1234;
        public bool runSimulationOnStart = true;
        public int simulationPairs = 10000;

        private void Start()
        {
            RunDemo();
            if (runSimulationOnStart) RunSimulation();
        }

        [ContextMenu("Run Demo")]
        public void RunDemo()
        {
            var cfg = configAsset != null ? configAsset.config : MatchmakingConfig.CreateDefault();

            var repo = new CharacterRepository();
            var db = new CandidateDatabase(repo, cfg);
            var content = new InterviewContent();
            DummyDataFactory.Populate(repo, db, content, seed);

            var evaluator = new CompatibilityEvaluator(cfg);
            var controller = new MatchmakingController(
                repo, db, evaluator, cfg, new SeededRandomSource(seed),
                new NoSevereFailureRule(), new InterviewManager(content));

            controller.OutcomeResolved += o =>
                Debug.Log("[Demo] Outcome " + o.outcomeId + ": " + o.category + " x" + o.paymentMultiplier +
                          (o.hasCompatibilityScore ? " (skor " + o.finalScore.ToString("0.00") + "%)" : " (tanpa rekomendasi)"));

            string clientId = DummyDataFactory.IdPrefix + "CLIENT_01";
            var start = controller.StartSession(clientId);
            if (!start.Success) { Debug.LogError(start.Error); return; }

            // Wawancara
            controller.BeginInterview();
            foreach (var topic in controller.GetInterviewTopics(clientId))
            {
                InterviewResponse resp;
                if (controller.Ask(clientId, topic.topicId, out resp).Success)
                    Debug.Log("[Demo] Q: " + topic.question + "  A: " + resp.text);
            }

            // Ranking kandidat (hanya untuk melihat hasil; pemain di game TIDAK melihat skor)
            var client = repo.Get(clientId);
            var candidates = controller.GetAvailableCandidates();
            var sb = new StringBuilder("[Demo] Skor klien vs kandidat:\n");
            CharacterRecord best = null;
            double bestScore = -1;
            foreach (var c in candidates)
            {
                var r = evaluator.Evaluate(client, c);
                sb.AppendLine("  " + c.displayName + " (" + c.characterId + "): " + r.finalScore.ToString("0.00") + "% -> " + r.category);
                if (r.finalScore > bestScore) { bestScore = r.finalScore; best = c; }
            }
            Debug.Log(sb.ToString());

            // Rekomendasi kandidat terbaik
            controller.SelectCandidate(best.characterId);
            controller.MarkReadyToRecommend();
            var rec = controller.Recommend(best.characterId);
            if (!rec.Success) Debug.LogError(rec.Error);
        }

        [ContextMenu("Run Score Simulation")]
        public void RunSimulation()
        {
            var cfg = configAsset != null ? configAsset.config : MatchmakingConfig.CreateDefault();
            Debug.Log(DummyScoreSimulation.Run(cfg, simulationPairs, seed));
        }
    }
}
