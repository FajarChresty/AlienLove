using System;
using System.Collections.Generic;

namespace AlienLove.Matchmaking.Dummy
{
    /// <summary>
    /// DATA DUMMY untuk pengembangan & testing. Seluruh folder Scripts/Dummy aman dihapus;
    /// tidak ada script di Core atau Tests yang bergantung padanya.
    ///
    /// Semua id berawalan "DUMMY_". Hasil deterministik berdasarkan seed.
    /// </summary>
    public static class DummyDataFactory
    {
        public const string IdPrefix = "DUMMY_";

        public static readonly string[] TagPool =
        {
            "smokes", "loves_kids", "lives_alone", "bad_words", "very_loner", "loves_pets",
            "night_owl", "early_bird", "wealthy", "religious", "traveler", "gamer",
            "good_cook", "athletic", "artistic", "messy", "workaholic", "talkative"
        };

        private static readonly string[] Names =
        {
            "Arven", "Belira", "Caldor", "Dessa", "Elvano", "Fenna", "Garet", "Hilya", "Iravan", "Jessara",
            "Kaelo", "Lunari", "Mavro", "Nessia", "Orvik", "Pelara", "Quenta", "Rishan", "Sorel", "Tavina",
            "Ulrek", "Vessa", "Wendar", "Xilan", "Yorva", "Zenia", "Aldric", "Brinna", "Corvin", "Dalia"
        };

        public const int ClientCount = 6;
        public const int CandidateCount = 20;

        /// <summary>Isi repository + database + konten wawancara dengan data dummy.</summary>
        public static void Populate(CharacterRepository repo, CandidateDatabase db, InterviewContent interview, int seed = 1234)
        {
            var rng = new Random(seed);
            string err;

            // Klien. #2 berbohong di CV, #6 tidak punya flag sama sekali.
            for (int i = 1; i <= ClientCount; i++)
            {
                var c = CreateRandomCharacter(IdPrefix + "CLIENT_" + i.ToString("00"), Names[i - 1], rng);
                if (i == 2) MakeDeceptive(c, rng);
                if (i == 6) { c.greenFlags.Clear(); c.redFlags.Clear(); }
                repo.TryAdd(c, out err);
                if (interview != null) AddInterview(interview, c);
            }

            // Kandidat reguler (CV selalu jujur).
            for (int i = 1; i <= CandidateCount; i++)
            {
                var c = CreateRandomCharacter(IdPrefix + "CAND_" + i.ToString("00"), Names[ClientCount + i - 1], rng);
                repo.TryAdd(c, out err);
                db.RegisterRegular(c.characterId, out err);
            }
        }

        /// <summary>Karakter acak yang valid (trait 0-3, 3-5 tag, 2 green flag, 2 red flag).</summary>
        public static CharacterRecord CreateRandomCharacter(string id, string name, Random rng)
        {
            var c = new CharacterRecord { characterId = id, displayName = name, originId = "dummy_origin" };
            for (int i = 0; i < TraitCatalog.TraitCount; i++)
            {
                c.actualTraits[i] = rng.Next(0, 4);
                c.partnerPreferences[i] = rng.Next(0, 4);
            }

            var pool = new List<string>(TagPool);
            Shuffle(pool, rng);
            int tagCount = rng.Next(3, 6);
            for (int i = 0; i < tagCount; i++) c.tags.Add(pool[i]);

            Shuffle(pool, rng);
            c.greenFlags.Add(pool[0]); c.greenFlags.Add(pool[1]);
            c.redFlags.Add(pool[2]); c.redFlags.Add(pool[3]);

            c.dealBreakerId = "dummy_dealbreaker";
            return c;
        }

        /// <summary>Beri klien CV palsu: 2 trait digeser, 1 tag asli disembunyikan, 1 tag palsu ditambahkan.</summary>
        public static void MakeDeceptive(CharacterRecord c, Random rng)
        {
            var cv = c.GetDisplayedCv().Clone();

            for (int n = 0; n < 2; n++)
            {
                int idx = rng.Next(0, TraitCatalog.TraitCount);
                int v = cv.statedTraits[idx];
                cv.statedTraits[idx] = v <= 1 ? v + 2 : v - 2;   // geser 2 posisi, tetap di 0-3
            }

            if (cv.tags.Count > 0) cv.tags.RemoveAt(0);
            foreach (var t in TagPool)
                if (!c.tags.Contains(t)) { cv.tags.Add(t); break; }

            c.hasDeceptiveCv = true;
            c.displayedCv = cv;
        }

        private static void AddInterview(InterviewContent content, CharacterRecord c)
        {
            var cv = c.GetDisplayedCv();   // pernyataan klien mengikuti CV TAMPIL (bisa palsu)
            string social = TraitCatalog.GetByIndex(0).GetLabel(cv.statedTraits[0]);
            string prefSocial = TraitCatalog.GetByIndex(0).GetLabel(cv.statedPartnerPreferences[0]);
            bool lying = c.IsDeceptive;

            content.AddEntry(c.characterId, new InterviewEntry
            {
                topicId = "intro",
                question = "Ceritakan sedikit tentang diri Anda.",
                answer = "Kalau boleh jujur, saya ini orangnya \"" + social + "\".",
                clue = lying ? "(Ia mengalihkan pandangan sebentar.)" : "",
                repeatAnswer = "Seperti yang tadi saya bilang.",
                internalStatementMatchesFact = !lying
            });

            content.AddEntry(c.characterId, new InterviewEntry
            {
                topicId = "partner",
                question = "Seperti apa pasangan yang Anda cari?",
                answer = "Saya mencari pasangan yang \"" + prefSocial + "\".",
                prerequisiteTopicId = "intro",
                repeatAnswer = "Saya sudah menjawab itu tadi, ya.",
                internalStatementMatchesFact = true
            });

            content.AddEntry(c.characterId, new InterviewEntry
            {
                topicId = "habit",
                question = "Apa kebiasaan Anda sehari-hari?",
                answer = "Biasa saja, tidak ada yang spesial.",
                clue = lying ? "(Jawabannya terdengar terlalu singkat.)" : "",
                internalStatementMatchesFact = !lying
            });
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }
    }
}
