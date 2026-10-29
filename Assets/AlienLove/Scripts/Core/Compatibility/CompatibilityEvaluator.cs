using System;
using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// Menghitung compatibility dua karakter (Sub-GDD bab 11). Class murni C#, tanpa UI & tanpa Unity,
    /// deterministik: input sama selalu hasil sama.
    ///
    /// Skor akhir = (Trait Preference x 0.60) + (Green/Red Flag x 0.40)
    /// Semua angka berasal dari MatchmakingConfig.
    /// </summary>
    public class CompatibilityEvaluator
    {
        private readonly MatchmakingConfig cfg;

        public CompatibilityEvaluator(MatchmakingConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            cfg = config;
        }

        public CompatibilityResult Evaluate(CharacterRecord a, CharacterRecord b)
        {
            var result = new CompatibilityResult
            {
                characterAId = a != null ? a.characterId : null,
                characterBId = b != null ? b.characterId : null
            };

            var va = CharacterValidator.Validate(a);
            var vb = CharacterValidator.Validate(b);
            if (!va.IsValid || !vb.IsValid)
            {
                result.isValid = false;
                result.error = (va.IsValid ? "" : va.ErrorText) + (vb.IsValid ? "" : (va.IsValid ? "" : " | ") + vb.ErrorText);
                return result;
            }

            result.traitScore = CalculateTraitCompatibility(a, b);
            result.flagScoreAToB = CalculateDirectionalFlagScore(a, b);
            result.flagScoreBToA = CalculateDirectionalFlagScore(b, a);
            result.flagScore = (result.flagScoreAToB + result.flagScoreBToA) / 2.0;
            result.finalScore = CombineScores(result.traitScore, result.flagScore);
            result.category = Categorize(result.finalScore);
            result.multiplier = GetMultiplier(result.category);
            result.isValid = true;
            return result;
        }

        // ---------- 11.2 Trait Preference Compatibility (60%) ----------

        /// <summary>Persen cocok satu trait: tabel[|sifat - preferensi|].</summary>
        public double CalculateTraitMatch(int actualValue, int preferredValue)
        {
            int diff = Math.Abs(actualValue - preferredValue);
            return cfg.traitMatchTable[diff];
        }

        /// <summary>Rata-rata 15 trait; tiap trait = rata-rata dua arah. Semua trait berbobot sama.</summary>
        public double CalculateTraitCompatibility(CharacterRecord a, CharacterRecord b)
        {
            double sum = 0;
            for (int i = 0; i < TraitCatalog.TraitCount; i++)
            {
                double bPrefForA = CalculateTraitMatch(a.actualTraits[i], b.partnerPreferences[i]);
                double aPrefForB = CalculateTraitMatch(b.actualTraits[i], a.partnerPreferences[i]);
                sum += (bPrefForA + aPrefForB) / 2.0;
            }
            return sum / TraitCatalog.TraitCount;
        }

        // ---------- 11.3 Green Flag / Red Flag Compatibility (40%) ----------

        /// <summary>
        /// Skor satu arah: flag milik "seeker" dicek ke ciri ASLI milik "target".
        /// Mulai 100%, kurangi penalti Red Flag yang terpenuhi, tambah setengah nilai Green Flag yang terpenuhi,
        /// lalu dibatasi 0-100. Tanpa flag sama sekali = noFlagScore (100).
        /// </summary>
        public double CalculateDirectionalFlagScore(CharacterRecord seeker, CharacterRecord target)
        {
            int greenCount = seeker.greenFlags.Count;
            int redCount = seeker.redFlags.Count;
            int total = greenCount + redCount;
            if (total == 0) return cfg.noFlagScore;

            var targetTags = BuildTagSet(target.tags);

            double greenValue = 100.0 / total;
            double redPenalty = redCount > 0 ? 100.0 / redCount : 0.0;

            double score = 100.0;
            for (int i = 0; i < redCount; i++)
                if (targetTags.Contains(Normalize(seeker.redFlags[i]))) score -= redPenalty;

            double fulfilledGreenSum = 0;
            for (int i = 0; i < greenCount; i++)
                if (targetTags.Contains(Normalize(seeker.greenFlags[i]))) fulfilledGreenSum += greenValue;

            score += fulfilledGreenSum * cfg.greenFlagBonusFactor;

            if (score < 0) score = 0;
            if (score > 100) score = 100;
            return score;
        }

        public double CalculateFlagCompatibility(CharacterRecord a, CharacterRecord b)
        {
            return (CalculateDirectionalFlagScore(a, b) + CalculateDirectionalFlagScore(b, a)) / 2.0;
        }

        // ---------- 11.1 / 11.6 Skor akhir & kategori ----------

        public double CombineScores(double traitScore, double flagScore)
        {
            double s = traitScore * cfg.traitWeight + flagScore * cfg.flagWeight;
            s = Math.Round(s, cfg.scoreDecimals, MidpointRounding.AwayFromZero);
            if (s < 0) s = 0;
            if (s > 100) s = 100;
            return s;
        }

        public MatchCategory Categorize(double finalScore)
        {
            if (finalScore >= cfg.perfectMin) return MatchCategory.PerfectSuccess;
            if (finalScore >= cfg.normalSuccessMin) return MatchCategory.NormalSuccess;
            if (finalScore >= cfg.normalFailureMin) return MatchCategory.NormalFailure;
            return MatchCategory.VeryBadFailure;
        }

        public double GetMultiplier(MatchCategory category)
        {
            switch (category)
            {
                case MatchCategory.PerfectSuccess: return cfg.perfectMultiplier;
                case MatchCategory.NormalSuccess: return cfg.normalSuccessMultiplier;
                case MatchCategory.NormalFailure: return cfg.normalFailureMultiplier;
                default: return cfg.veryBadFailureMultiplier;
            }
        }

        // ---------- util ----------

        private static string Normalize(string s)
        {
            return s == null ? "" : s.Trim().ToLowerInvariant();
        }

        private static HashSet<string> BuildTagSet(List<string> tags)
        {
            var set = new HashSet<string>();
            for (int i = 0; i < tags.Count; i++) set.Add(Normalize(tags[i]));
            return set;
        }
    }
}
