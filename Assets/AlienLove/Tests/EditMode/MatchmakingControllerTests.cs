using System.Reflection;
using NUnit.Framework;

namespace AlienLove.Matchmaking.Tests
{
    public class MatchmakingControllerTests
    {
        // Pasangan sempurna: kedua arah selisih 0, tanpa flag -> 100%.
        private static CharacterRecord PerfectClient() { return TestHelpers.Make("CLIENT", 0, 3); }
        private static CharacterRecord PerfectCandidate() { return TestHelpers.Make("CAND", 3, 0); }

        // Pasangan gagal biasa (skor 35): trait 25%, satu sisi kena Red Flag.
        private static CharacterRecord FailClient() { return TestHelpers.Make("CLIENT", 0, 0, red: new[] { "x" }); }
        private static CharacterRecord FailCandidate() { return TestHelpers.Make("CAND", 3, 3, tags: new[] { "x" }); }

        // Pasangan gagal sangat buruk (skor 15): trait 25%, kedua sisi kena Red Flag.
        private static CharacterRecord BadClient() { return TestHelpers.Make("CLIENT", 0, 0, tags: new[] { "y" }, red: new[] { "x" }); }
        private static CharacterRecord BadCandidate() { return TestHelpers.Make("CAND", 3, 3, tags: new[] { "x" }, red: new[] { "y" }); }

        [Test]
        public void PerfectMatch_PaysTriple_AndBothBecomeUnavailable()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.AddRegularCandidate(PerfectCandidate());
            h.PrepareToRecommend("CLIENT", "CAND");

            var r = h.controller.Recommend("CAND");

            Assert.IsTrue(r.Success);
            Assert.AreEqual(MatchCategory.PerfectSuccess, h.controller.LastOutcome.category);
            Assert.AreEqual(3.0, h.controller.LastOutcome.paymentMultiplier, 0.0001);
            Assert.AreEqual(AvailabilityStatus.Unavailable, h.repo.Get("CLIENT").availability);
            Assert.AreEqual(AvailabilityStatus.Unavailable, h.repo.Get("CAND").availability);
            Assert.AreEqual(SessionState.Completed, h.controller.CurrentSession.state);
            Assert.AreEqual(1, h.events.Count);
        }

        [Test]
        public void Recommend_Twice_IsRejected_NoDuplicateOutcomeOrEvent()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.AddRegularCandidate(PerfectCandidate());
            h.PrepareToRecommend("CLIENT", "CAND");

            Assert.IsTrue(h.controller.Recommend("CAND").Success);
            Assert.IsFalse(h.controller.Recommend("CAND").Success);
            Assert.AreEqual(1, h.events.Count);
            Assert.AreEqual(1, h.controller.Outcomes.Count);
        }

        [Test]
        public void Recommend_BeforeReadyState_IsRejected()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.AddRegularCandidate(PerfectCandidate());
            h.controller.StartSession("CLIENT");

            Assert.IsFalse(h.controller.Recommend("CAND").Success);
            Assert.AreEqual(SessionState.ClientReview, h.controller.CurrentSession.state);
            Assert.AreEqual(0, h.events.Count);
        }

        [Test]
        public void Recommend_UnavailableCandidate_IsRejected_StatePreserved()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.AddRegularCandidate(PerfectCandidate());
            h.PrepareToRecommend("CLIENT", "CAND");
            h.db.MarkUnavailable("CAND");

            Assert.IsFalse(h.controller.Recommend("CAND").Success);
            Assert.AreEqual(SessionState.ReadyToRecommend, h.controller.CurrentSession.state);
            Assert.AreEqual(0, h.events.Count);
        }

        [Test]
        public void Recommend_ClientAsOwnCandidate_IsRejected()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.db.RegisterOptIn("CLIENT", out string e);
            h.PrepareToRecommend("CLIENT", "CAND");
            h.controller.MarkReadyToRecommend();
            Assert.IsFalse(h.controller.Recommend("CLIENT").Success);
        }

        [Test]
        public void DeclineMatch_WithoutOptIn_IsNormalFailureNoPayNoScore()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.controller.StartSession("CLIENT");

            Assert.IsTrue(h.controller.DeclineMatch(false).Success);
            var o = h.controller.LastOutcome;
            Assert.AreEqual(MatchCategory.NormalFailure, o.category);
            Assert.AreEqual(0.0, o.paymentMultiplier, 0.0001);
            Assert.IsFalse(o.hasCompatibilityScore);
            Assert.IsTrue(o.wasNoRecommendation);
            Assert.IsFalse(h.db.IsRegistered("CLIENT"));
        }

        [Test]
        public void DeclineMatch_WithOptIn_RegistersSameCharacterId_NoDuplicate()
        {
            var h = new Harness();
            var client = PerfectClient();
            h.AddClient(client);
            int before = h.repo.Count;
            h.controller.StartSession("CLIENT");

            Assert.IsTrue(h.controller.DeclineMatch(true).Success);
            Assert.AreEqual(before, h.repo.Count);
            Assert.IsTrue(h.db.IsRegistered("CLIENT"));
            Assert.AreEqual(CandidateSource.OptInClient, h.db.GetRegistration("CLIENT").source);
            Assert.AreSame(client, h.repo.Get("CLIENT"));
        }

        [Test]
        public void OrdinaryFailure_RegularCandidate_ReturnsWhenRollLow()
        {
            var h = new Harness(0.1);
            h.AddClient(FailClient());
            h.AddRegularCandidate(FailCandidate());
            h.PrepareToRecommend("CLIENT", "CAND");

            Assert.IsTrue(h.controller.Recommend("CAND").Success);
            Assert.AreEqual(MatchCategory.NormalFailure, h.controller.LastOutcome.category);
            Assert.AreEqual(0.0, h.controller.LastOutcome.paymentMultiplier, 0.0001);
            Assert.AreEqual(CandidateReturnDecision.Returned, h.controller.LastOutcome.candidateReturnDecision);
            Assert.IsTrue(h.db.IsEligible("CAND"));
        }

        [Test]
        public void OrdinaryFailure_RegularCandidate_StaysOutWhenRollHigh()
        {
            var h = new Harness(0.9);
            h.AddClient(FailClient());
            h.AddRegularCandidate(FailCandidate());
            h.PrepareToRecommend("CLIENT", "CAND");

            h.controller.Recommend("CAND");
            Assert.AreEqual(CandidateReturnDecision.NotReturned, h.controller.LastOutcome.candidateReturnDecision);
            Assert.IsFalse(h.db.IsEligible("CAND"));
        }

        [Test]
        public void VeryBadFailure_PaysNegativeDouble()
        {
            var h = new Harness(0.9);
            h.AddClient(BadClient());
            h.AddRegularCandidate(BadCandidate());
            h.PrepareToRecommend("CLIENT", "CAND");

            h.controller.Recommend("CAND");
            Assert.AreEqual(MatchCategory.VeryBadFailure, h.controller.LastOutcome.category);
            Assert.AreEqual(-2.0, h.controller.LastOutcome.paymentMultiplier, 0.0001);
        }

        [Test]
        public void SevereRule_RemovesCandidate_AndMarksOutcomeSevere()
        {
            var h = new Harness(0.0, new AlwaysSevereRule());   // roll 0.0 akan "kembali" kalau bukan severe
            h.AddClient(BadClient());
            h.AddRegularCandidate(BadCandidate());
            h.PrepareToRecommend("CLIENT", "CAND");

            h.controller.Recommend("CAND");
            Assert.IsTrue(h.controller.LastOutcome.isSevere);
            Assert.AreEqual(CandidateReturnDecision.Removed, h.controller.LastOutcome.candidateReturnDecision);
            Assert.IsFalse(h.db.IsEligible("CAND"));
        }

        [Test]
        public void StartSession_WhileAnotherActive_IsRejected()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.AddClient(TestHelpers.Make("CLIENT2"));
            Assert.IsTrue(h.controller.StartSession("CLIENT").Success);
            Assert.IsFalse(h.controller.StartSession("CLIENT2").Success);
        }

        [Test]
        public void Interview_RepeatQuestion_IsFlaggedRepeat_AndPrerequisiteIsEnforced()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.content.AddEntry("CLIENT", new InterviewEntry { topicId = "intro", question = "Q1", answer = "A1", repeatAnswer = "Sudah tadi." });
            h.content.AddEntry("CLIENT", new InterviewEntry { topicId = "next", question = "Q2", answer = "A2", prerequisiteTopicId = "intro" });
            h.controller.StartSession("CLIENT");
            h.controller.BeginInterview();

            InterviewResponse resp;
            Assert.IsFalse(h.controller.Ask("CLIENT", "next", out resp).Success);   // prasyarat belum terpenuhi

            Assert.IsTrue(h.controller.Ask("CLIENT", "intro", out resp).Success);
            Assert.IsFalse(resp.wasRepeat);

            Assert.IsTrue(h.controller.Ask("CLIENT", "intro", out resp).Success);
            Assert.IsTrue(resp.wasRepeat);
            Assert.AreEqual("Sudah tadi.", resp.text);

            Assert.IsTrue(h.controller.Ask("CLIENT", "next", out resp).Success);    // sekarang terbuka
        }

        [Test]
        public void InterviewResponse_NeverExposesTruthMetadata()
        {
            foreach (var f in typeof(InterviewResponse).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string n = f.Name.ToLowerInvariant();
                Assert.IsFalse(n.Contains("truth") || n.Contains("lie") || n.Contains("honest") || n.Contains("match"), "Field terlarang: " + f.Name);
            }
            foreach (var p in typeof(InterviewResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string n = p.Name.ToLowerInvariant();
                Assert.IsFalse(n.Contains("truth") || n.Contains("lie") || n.Contains("honest") || n.Contains("match"), "Property terlarang: " + p.Name);
            }
        }

        [Test]
        public void Ask_OutsideInterviewingState_IsRejected()
        {
            var h = new Harness();
            h.AddClient(PerfectClient());
            h.content.AddEntry("CLIENT", new InterviewEntry { topicId = "intro", question = "Q", answer = "A" });
            h.controller.StartSession("CLIENT");

            InterviewResponse resp;
            Assert.IsFalse(h.controller.Ask("CLIENT", "intro", out resp).Success);
        }
    }
}
