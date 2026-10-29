using System;
using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    public class CandidateRegistration
    {
        public string characterId;
        public CandidateSource source;
    }

    /// <summary>
    /// Database kandidat + aturan ketersediaan (Main GDD bab 7, Sub-GDD bab 8 dan 12).
    /// Satu-satunya tempat yang boleh mengubah status ketersediaan kandidat.
    /// Tidak ada duplikat: satu characterId hanya boleh punya satu registrasi.
    /// </summary>
    public class CandidateDatabase
    {
        private readonly CharacterRepository repo;
        private readonly MatchmakingConfig cfg;
        private readonly Dictionary<string, CandidateRegistration> registrations = new Dictionary<string, CandidateRegistration>();
        private readonly List<string> order = new List<string>();

        public CandidateDatabase(CharacterRepository repository, MatchmakingConfig config)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            if (config == null) throw new ArgumentNullException("config");
            repo = repository;
            cfg = config;
        }

        public int RegistrationCount { get { return order.Count; } }

        // ---------- Registrasi ----------

        /// <summary>Kandidat reguler masuk lewat jadwal kedatangan.</summary>
        public bool RegisterRegular(string characterId, out string error)
        {
            return Register(characterId, CandidateSource.RegularCandidate, out error);
        }

        /// <summary>Klien yang setuju masuk database. Memakai character_id yang sama, tanpa membuat karakter baru.</summary>
        public bool RegisterOptIn(string clientId, out string error)
        {
            return Register(clientId, CandidateSource.OptInClient, out error);
        }

        private bool Register(string characterId, CandidateSource source, out string error)
        {
            var c = repo.Get(characterId);
            if (c == null) { error = "Karakter tidak ditemukan: " + characterId; return false; }

            var v = CharacterValidator.Validate(c);
            if (!v.IsValid) { error = v.ErrorText; return false; }

            if (registrations.ContainsKey(characterId))
            {
                error = "Karakter sudah terdaftar sebagai kandidat: " + characterId;
                return false;
            }

            registrations.Add(characterId, new CandidateRegistration { characterId = characterId, source = source });
            order.Add(characterId);
            c.availability = AvailabilityStatus.Available;
            c.history.Add("Registered as " + source);
            error = null;
            return true;
        }

        // ---------- Query ----------

        public bool IsRegistered(string characterId)
        {
            return characterId != null && registrations.ContainsKey(characterId);
        }

        public CandidateRegistration GetRegistration(string characterId)
        {
            CandidateRegistration r;
            return (characterId != null && registrations.TryGetValue(characterId, out r)) ? r : null;
        }

        /// <summary>Terdaftar sebagai kandidat DAN statusnya Available.</summary>
        public bool IsEligible(string characterId)
        {
            if (!IsRegistered(characterId)) return false;
            var c = repo.Get(characterId);
            return c != null && c.availability == AvailabilityStatus.Available;
        }

        /// <summary>Daftar kandidat yang bisa dipilih (tanpa duplikat). excludeId = biasanya klien yang sedang diproses.</summary>
        public List<CharacterRecord> GetAvailableCandidates(string excludeId = null)
        {
            var list = new List<CharacterRecord>();
            for (int i = 0; i < order.Count; i++)
            {
                string id = order[i];
                if (id == excludeId) continue;
                if (IsEligible(id)) list.Add(repo.Get(id));
            }
            return list;
        }

        // ---------- Perubahan status (dipanggil oleh MatchmakingController) ----------

        /// <summary>Match sukses: karakter tidak bisa dipilih lagi.</summary>
        public void MarkUnavailable(string characterId)
        {
            var c = repo.Get(characterId);
            if (c == null) return;
            c.availability = AvailabilityStatus.Unavailable;
            c.history.Add("Unavailable after successful match");
        }

        /// <summary>Gagal berat: tidak boleh kembali, apa pun sumbernya.</summary>
        public CandidateReturnDecision ApplySevereFailure(string characterId)
        {
            var c = repo.Get(characterId);
            if (c != null)
            {
                c.availability = AvailabilityStatus.Removed;
                c.history.Add("Removed after severe failure");
            }
            return CandidateReturnDecision.Removed;
        }

        /// <summary>
        /// Gagal biasa. Kandidat reguler: peluang kembali = cfg.regularReturnChance (default 50%).
        /// Klien opt-in: tidak kembali. Dipanggil SEKALI per outcome (controller mencegah roll ulang).
        /// </summary>
        public CandidateReturnDecision ApplyOrdinaryFailure(string characterId, IRandomSource rng)
        {
            var c = repo.Get(characterId);
            var reg = GetRegistration(characterId);
            if (c == null || reg == null) return CandidateReturnDecision.NotApplicable;

            if (reg.source == CandidateSource.RegularCandidate)
            {
                bool returns = rng.NextDouble() < cfg.regularReturnChance;
                if (returns)
                {
                    c.availability = AvailabilityStatus.Available;
                    c.history.Add("Returned to database after ordinary failure");
                    return CandidateReturnDecision.Returned;
                }
                c.availability = AvailabilityStatus.Removed;
                c.history.Add("Did not return after ordinary failure");
                return CandidateReturnDecision.NotReturned;
            }

            c.availability = AvailabilityStatus.Removed;
            c.history.Add("Opt-in client does not return after ordinary failure");
            return CandidateReturnDecision.NotReturned;
        }
    }
}
