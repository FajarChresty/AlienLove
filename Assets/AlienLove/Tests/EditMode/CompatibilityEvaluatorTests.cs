using NUnit.Framework;

namespace AlienLove.Matchmaking.Tests
{
    public class CompatibilityEvaluatorTests
    {
        private MatchmakingConfig cfg;
        private CompatibilityEvaluator eval;

        [SetUp]
        public void SetUp()
        {
            cfg = MatchmakingConfig.CreateDefault();
            eval = new CompatibilityEvaluator(cfg);
        }

        [Test]
        public void TraitMatch_FollowsBossTable_100_75_50_25()
        {
            Assert.AreEqual(100.0, eval.CalculateTraitMatch(2, 2), 0.0001);
            Assert.AreEqual(75.0, eval.CalculateTraitMatch(1, 2), 0.0001);
            Assert.AreEqual(75.0, eval.CalculateTraitMatch(3, 2), 0.0001);
            Assert.AreEqual(50.0, eval.CalculateTraitMatch(0, 2), 0.0001);
            Assert.AreEqual(25.0, eval.CalculateTraitMatch(0, 3), 0.0001);
            Assert.AreEqual(25.0, eval.CalculateTraitMatch(3, 0), 0.0001);
        }

        [Test]
        public void TraitMatch_ChangingConfigTable_ChangesResult()
        {
            cfg.traitMatchTable[3] = 0;   // atasan: 25% bisa jadi 0% setelah test
            Assert.AreEqual(0.0, eval.CalculateTraitMatch(0, 3), 0.0001);
        }

        [Test]
        public void TraitCompatibility_PerfectMutualMatch_Is100()
        {
            // A sifat 0, ingin pasangan 3. B sifat 3, ingin pasangan 0. Kedua arah selisih 0.
            var a = TestHelpers.Make("A", 0, 3);
            var b = TestHelpers.Make("B", 3, 0);
            Assert.AreEqual(100.0, eval.CalculateTraitCompatibility(a, b), 0.0001);
        }

        [Test]
        public void TraitCompatibility_WorstCase_Is25WithDefaultTable()
        {
            var a = TestHelpers.Make("A", 0, 0);
            var b = TestHelpers.Make("B", 3, 3);
            // A sifat 0 vs keinginan B (3) -> 25; B sifat 3 vs keinginan A (0) -> 25
            Assert.AreEqual(25.0, eval.CalculateTraitCompatibility(a, b), 0.0001);
        }

        [Test]
        public void Flags_PdfExample_A_toward_B_is_66_67()
        {
            var a = TestHelpers.Make("A", tags: new[] { "bad_words", "smokes", "very_loner", "loves_mommy" },
                green: new[] { "loves_kids" }, red: new[] { "smokes", "lives_alone" });
            var b = TestHelpers.Make("B", tags: new[] { "smokes", "loves_kids" },
                green: new[] { "smokes", "very_loner", "loves_mommy" }, red: new[] { "bad_words" });

            Assert.AreEqual(66.67, eval.CalculateDirectionalFlagScore(a, b), 0.01);
        }

        [Test]
        public void Flags_PdfExample_B_toward_A_is_37_5()
        {
            var a = TestHelpers.Make("A", tags: new[] { "bad_words", "smokes", "very_loner", "loves_mommy" },
                green: new[] { "loves_kids" }, red: new[] { "smokes", "lives_alone" });
            var b = TestHelpers.Make("B", tags: new[] { "smokes", "loves_kids" },
                green: new[] { "smokes", "very_loner", "loves_mommy" }, red: new[] { "bad_words" });

            Assert.AreEqual(37.5, eval.CalculateDirectionalFlagScore(b, a), 0.01);
        }

        [Test]
        public void Flags_PdfExample_Combined_is_52_08()
        {
            var a = TestHelpers.Make("A", tags: new[] { "bad_words", "smokes", "very_loner", "loves_mommy" },
                green: new[] { "loves_kids" }, red: new[] { "smokes", "lives_alone" });
            var b = TestHelpers.Make("B", tags: new[] { "smokes", "loves_kids" },
                green: new[] { "smokes", "very_loner", "loves_mommy" }, red: new[] { "bad_words" });

            Assert.AreEqual(52.08, eval.CalculateFlagCompatibility(a, b), 0.01);
        }

        [Test]
        public void FinalScore_PdfExample_80_and_52_08_is_68_83()
        {
            double flag = (200.0 / 3.0 + 37.5) / 2.0;   // 52.0833...
            Assert.AreEqual(68.83, eval.CombineScores(80.0, flag), 0.0001);
        }

        [Test]
        public void Flags_NoFlagsAtAll_Is100()
        {
            var a = TestHelpers.Make("A");
            var b = TestHelpers.Make("B", tags: new[] { "smokes" });
            Assert.AreEqual(100.0, eval.CalculateDirectionalFlagScore(a, b), 0.0001);
        }

        [Test]
        public void Flags_NoRedFlags_NoDivisionByZero_AndClampedTo100()
        {
            var a = TestHelpers.Make("A", green: new[] { "loves_kids" });
            var b = TestHelpers.Make("B", tags: new[] { "loves_kids" });
            Assert.AreEqual(100.0, eval.CalculateDirectionalFlagScore(a, b), 0.0001);
        }

        [Test]
        public void Flags_SingleRedFlagTriggered_DropsToZeroBeforeGreenBonus()
        {
            var a = TestHelpers.Make("A", red: new[] { "smokes" });
            var b = TestHelpers.Make("B", tags: new[] { "smokes" });
            Assert.AreEqual(0.0, eval.CalculateDirectionalFlagScore(a, b), 0.0001);
        }

        [Test]
        public void Flags_MatchingIsCaseAndSpaceInsensitive()
        {
            var a = TestHelpers.Make("A", red: new[] { " Smokes " });
            var b = TestHelpers.Make("B", tags: new[] { "smokes" });
            Assert.AreEqual(0.0, eval.CalculateDirectionalFlagScore(a, b), 0.0001);
        }

        [Test]
        public void Categories_BoundariesGoToHigherTier()
        {
            Assert.AreEqual(MatchCategory.VeryBadFailure, eval.Categorize(0.0));
            Assert.AreEqual(MatchCategory.VeryBadFailure, eval.Categorize(19.99));
            Assert.AreEqual(MatchCategory.NormalFailure, eval.Categorize(20.0));
            Assert.AreEqual(MatchCategory.NormalFailure, eval.Categorize(49.99));
            Assert.AreEqual(MatchCategory.NormalSuccess, eval.Categorize(50.0));
            Assert.AreEqual(MatchCategory.NormalSuccess, eval.Categorize(89.99));
            Assert.AreEqual(MatchCategory.PerfectSuccess, eval.Categorize(90.0));
            Assert.AreEqual(MatchCategory.PerfectSuccess, eval.Categorize(100.0));
        }

        [Test]
        public void Multipliers_Are_3_1_0_Minus2()
        {
            Assert.AreEqual(3.0, eval.GetMultiplier(MatchCategory.PerfectSuccess), 0.0001);
            Assert.AreEqual(1.0, eval.GetMultiplier(MatchCategory.NormalSuccess), 0.0001);
            Assert.AreEqual(0.0, eval.GetMultiplier(MatchCategory.NormalFailure), 0.0001);
            Assert.AreEqual(-2.0, eval.GetMultiplier(MatchCategory.VeryBadFailure), 0.0001);
        }

        [Test]
        public void Evaluate_InvalidTraitValue_IsRejectedNotSilentlyClamped()
        {
            var a = TestHelpers.Make("A");
            var b = TestHelpers.Make("B");
            b.actualTraits[4] = 7;
            var r = eval.Evaluate(a, b);
            Assert.IsFalse(r.isValid);
            Assert.IsNotNull(r.error);
        }

        [Test]
        public void Evaluate_IsDeterministicAndSymmetric()
        {
            var a = TestHelpers.Make("A", 1, 2, new[] { "smokes" }, new[] { "loves_kids" }, new[] { "gamer" });
            var b = TestHelpers.Make("B", 2, 1, new[] { "gamer" }, new[] { "smokes" }, new[] { "loves_kids" });
            var r1 = eval.Evaluate(a, b);
            var r2 = eval.Evaluate(a, b);
            var r3 = eval.Evaluate(b, a);
            Assert.AreEqual(r1.finalScore, r2.finalScore, 0.0);
            Assert.AreEqual(r1.finalScore, r3.finalScore, 0.0001);
        }

        [Test]
        public void Evaluate_UsesActualData_NotTheDeceptiveCv()
        {
            var honest = TestHelpers.Make("C1", 1, 1, new[] { "smokes" }, new[] { "loves_kids" }, new[] { "gamer" });
            var liar = TestHelpers.Make("C2", 1, 1, new[] { "smokes" }, new[] { "loves_kids" }, new[] { "gamer" });
            liar.hasDeceptiveCv = true;
            liar.displayedCv = liar.GetDisplayedCv().Clone();
            liar.displayedCv.tags.Clear();
            liar.displayedCv.tags.Add("loves_pets");
            liar.displayedCv.statedTraits[0] = 3;

            var partner = TestHelpers.Make("P", 2, 2, new[] { "loves_kids" }, new[] { "smokes" }, new[] { "gamer" });
            Assert.AreEqual(eval.Evaluate(honest, partner).finalScore, eval.Evaluate(liar, partner).finalScore, 0.0001);
        }

        [Test]
        public void Evaluate_ScoreIsAlwaysWithin0And100()
        {
            var a = TestHelpers.Make("A", 0, 0, null, null, new[] { "x" });
            var b = TestHelpers.Make("B", 3, 3, new[] { "x" }, null, new[] { "x" });
            var r = eval.Evaluate(a, b);
            Assert.IsTrue(r.finalScore >= 0 && r.finalScore <= 100);
        }
    }
}
