using System;
using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// CV yang TAMPIL ke pemain. Untuk klien yang berbohong, isinya bisa berbeda dari data asli.
    /// Kandidat di database selalu jujur, jadi tidak memakai ini.
    /// </summary>
    [Serializable]
    public class CvData
    {
        public List<string> tags = new List<string>();
        public int[] statedTraits;              // null = tidak dinyatakan
        public int[] statedPartnerPreferences;  // null = tidak dinyatakan

        public CvData Clone()
        {
            return new CvData
            {
                tags = new List<string>(tags),
                statedTraits = statedTraits != null ? (int[])statedTraits.Clone() : null,
                statedPartnerPreferences = statedPartnerPreferences != null ? (int[])statedPartnerPreferences.Clone() : null
            };
        }
    }

    /// <summary>
    /// Data lengkap satu karakter. "Actual" = kebenaran (dipakai untuk hitungan).
    /// Trait asli dan preferensi pasangan DISIMPAN TERPISAH (Sub-GDD 4.2).
    /// </summary>
    [Serializable]
    public class CharacterRecord
    {
        public string characterId;
        public string displayName;
        public string originId;

        // --- Data asli (authoritative) ---
        public int[] actualTraits = new int[TraitCatalog.TraitCount];
        public int[] partnerPreferences = new int[TraitCatalog.TraitCount];
        public List<string> tags = new List<string>();        // ciri asli karakter (dicek oleh flag lawan)
        public List<string> greenFlags = new List<string>();  // yang dicari
        public List<string> redFlags = new List<string>();    // yang dihindari

        // --- Disimpan untuk nanti, BELUM dipakai di rumus bab 11 ---
        public string dealBreakerId;
        public int[] traitImportance;
        public int[] traitFlexibility;

        public string interviewContentId;

        public AvailabilityStatus availability = AvailabilityStatus.Available;

        // --- Kebohongan klien: CV tampil yang berbeda dari data asli ---
        public bool hasDeceptiveCv;
        public CvData displayedCv;

        public List<string> history = new List<string>();

        public bool IsDeceptive { get { return hasDeceptiveCv && displayedCv != null; } }

        /// <summary>CV yang dilihat pemain. Jujur (sama dengan data asli) kalau tidak ada CV palsu.</summary>
        public CvData GetDisplayedCv()
        {
            if (IsDeceptive) return displayedCv;
            return new CvData
            {
                tags = new List<string>(tags),
                statedTraits = (int[])actualTraits.Clone(),
                statedPartnerPreferences = (int[])partnerPreferences.Clone()
            };
        }
    }
}
