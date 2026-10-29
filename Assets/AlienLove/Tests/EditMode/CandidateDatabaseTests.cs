using NUnit.Framework;

namespace AlienLove.Matchmaking.Tests
{
    public class CandidateDatabaseTests
    {
        private MatchmakingConfig cfg;
        private CharacterRepository repo;
        private CandidateDatabase db;
        private string err;

        [SetUp]
        public void SetUp()
        {
            cfg = MatchmakingConfig.CreateDefault();
            repo = new CharacterRepository();
            db = new CandidateDatabase(repo, cfg);
        }

        private CharacterRecord Add(string id)
        {
            var c = TestHelpers.Make(id);
            Assert.IsTrue(repo.TryAdd(c, out err));
            return c;
        }

        [Test]
        public void RegisterRegular_Works_AndRecordsSource()
        {
            Add("R1");
            Assert.IsTrue(db.RegisterRegular("R1", out err));
            Assert.AreEqual(CandidateSource.RegularCandidate, db.GetRegistration("R1").source);
            Assert.IsTrue(db.IsEligible("R1"));
        }

        [Test]
        public void Register_Duplicate_IsRejected_NoSecondEntry()
        {
            Add("R1");
            Assert.IsTrue(db.RegisterRegular("R1", out err));
            Assert.IsFalse(db.RegisterRegular("R1", out err));
            Assert.IsFalse(db.RegisterOptIn("R1", out err));
            Assert.AreEqual(1, db.RegistrationCount);
            Assert.AreEqual(1, db.GetAvailableCandidates().Count);
        }

        [Test]
        public void Repository_RejectsDuplicateCharacterId()
        {
            Add("X");
            Assert.IsFalse(repo.TryAdd(TestHelpers.Make("X"), out err));
            Assert.AreEqual(1, repo.Count);
        }

        [Test]
        public void OptIn_KeepsSameCharacterIdAndNoNewRecord()
        {
            var client = Add("C1");
            int before = repo.Count;
            Assert.IsTrue(db.RegisterOptIn("C1", out err));
            Assert.AreEqual(before, repo.Count);
            Assert.AreSame(client, repo.Get("C1"));
            Assert.AreEqual(CandidateSource.OptInClient, db.GetRegistration("C1").source);
        }

        [Test]
        public void Register_UnknownCharacter_IsRejected()
        {
            Assert.IsFalse(db.RegisterRegular("NOPE", out err));
        }

        [Test]
        public void OrdinaryFailure_Regular_ReturnsWhenRollBelow50()
        {
            Add("R1"); db.RegisterRegular("R1", out err);
            var d = db.ApplyOrdinaryFailure("R1", new FixedRandom(0.49));
            Assert.AreEqual(CandidateReturnDecision.Returned, d);
            Assert.AreEqual(AvailabilityStatus.Available, repo.Get("R1").availability);
        }

        [Test]
        public void OrdinaryFailure_Regular_DoesNotReturnWhenRollAtOrAbove50()
        {
            Add("R1"); db.RegisterRegular("R1", out err);
            var d = db.ApplyOrdinaryFailure("R1", new FixedRandom(0.5));
            Assert.AreEqual(CandidateReturnDecision.NotReturned, d);
            Assert.IsFalse(db.IsEligible("R1"));
        }

        [Test]
        public void OrdinaryFailure_OptIn_NeverReturns_EvenWithBestRoll()
        {
            Add("C1"); db.RegisterOptIn("C1", out err);
            var d = db.ApplyOrdinaryFailure("C1", new FixedRandom(0.0));
            Assert.AreEqual(CandidateReturnDecision.NotReturned, d);
            Assert.IsFalse(db.IsEligible("C1"));
        }

        [Test]
        public void SevereFailure_Removes_RegardlessOfSource()
        {
            Add("R1"); db.RegisterRegular("R1", out err);
            Add("C1"); db.RegisterOptIn("C1", out err);
            Assert.AreEqual(CandidateReturnDecision.Removed, db.ApplySevereFailure("R1"));
            Assert.AreEqual(CandidateReturnDecision.Removed, db.ApplySevereFailure("C1"));
            Assert.IsFalse(db.IsEligible("R1"));
            Assert.IsFalse(db.IsEligible("C1"));
        }

        [Test]
        public void UnavailableCandidates_AreNotListed()
        {
            Add("R1"); db.RegisterRegular("R1", out err);
            Add("R2"); db.RegisterRegular("R2", out err);
            db.MarkUnavailable("R1");
            var list = db.GetAvailableCandidates();
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("R2", list[0].characterId);
        }

        [Test]
        public void GetAvailableCandidates_ExcludesGivenId()
        {
            Add("C1"); db.RegisterOptIn("C1", out err);
            Add("R1"); db.RegisterRegular("R1", out err);
            var list = db.GetAvailableCandidates("C1");
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("R1", list[0].characterId);
        }
    }
}
