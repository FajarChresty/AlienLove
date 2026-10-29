using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    public class ValidationResult
    {
        public readonly List<string> errors = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public bool IsValid { get { return errors.Count == 0; } }
        public string ErrorText { get { return string.Join("; ", errors); } }
    }

    /// <summary>Validasi data karakter (Sub-GDD 5.1 dan 16.5). Tidak pernah menebak/clamp nilai.</summary>
    public static class CharacterValidator
    {
        public static ValidationResult Validate(CharacterRecord c)
        {
            var r = new ValidationResult();
            if (c == null) { r.errors.Add("Karakter null."); return r; }

            string who = string.IsNullOrEmpty(c.characterId) ? "(tanpa id)" : c.characterId;
            if (string.IsNullOrEmpty(c.characterId)) r.errors.Add("characterId kosong.");
            if (string.IsNullOrEmpty(c.displayName)) r.warnings.Add(who + ": displayName kosong.");

            CheckTraitArray(c.actualTraits, who + ".actualTraits", r);
            CheckTraitArray(c.partnerPreferences, who + ".partnerPreferences", r);

            if (c.tags == null) r.errors.Add(who + ": tags null.");
            if (c.greenFlags == null) r.errors.Add(who + ": greenFlags null.");
            if (c.redFlags == null) r.errors.Add(who + ": redFlags null.");

            if (string.IsNullOrEmpty(c.dealBreakerId))
                r.warnings.Add(who + ": belum punya deal-breaker (wajib tepat 1 menurut GDD, belum dipakai di rumus).");

            if (c.IsDeceptive)
            {
                if (c.displayedCv.statedTraits != null) CheckTraitArray(c.displayedCv.statedTraits, who + ".displayedCv.statedTraits", r);
                if (c.displayedCv.statedPartnerPreferences != null) CheckTraitArray(c.displayedCv.statedPartnerPreferences, who + ".displayedCv.statedPartnerPreferences", r);
            }
            return r;
        }

        private static void CheckTraitArray(int[] values, string label, ValidationResult r)
        {
            if (values == null) { r.errors.Add(label + " null."); return; }
            if (values.Length != TraitCatalog.TraitCount)
            {
                r.errors.Add(label + " harus berisi " + TraitCatalog.TraitCount + " nilai, ditemukan " + values.Length + ".");
                return;
            }
            for (int i = 0; i < values.Length; i++)
            {
                if (!TraitCatalog.IsValidValue(values[i]))
                    r.errors.Add(label + "[" + i + "] = " + values[i] + " di luar rentang 0-3.");
            }
        }
    }
}
