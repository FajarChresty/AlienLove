using System;
using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// Satu entri wawancara yang DITULIS penulis. Pisahkan: fakta asli, pernyataan, petunjuk (Sub-GDD 7.1).
    /// Field internalStatementMatchesFact hanya untuk sistem/penulis dan TIDAK PERNAH diberikan ke UI.
    /// </summary>
    [Serializable]
    public class InterviewEntry
    {
        public string topicId;
        public string question;
        public string answer;                    // pernyataan karakter
        public string clue;                      // petunjuk tambahan (boleh kosong)
        public string repeatAnswer;              // jawaban kalau ditanya ulang (boleh kosong)
        public string prerequisiteTopicId;       // topik yang harus sudah ditanyakan dulu (boleh kosong)
        public bool internalStatementMatchesFact = true;  // JANGAN ditampilkan ke pemain
    }

    /// <summary>Topik yang bisa dipilih pemain (tanpa metadata kebenaran).</summary>
    public class InterviewTopicInfo
    {
        public string topicId;
        public string question;
        public bool alreadyAsked;
    }

    /// <summary>Jawaban yang dilihat pemain. Sengaja tidak ada field benar/bohong.</summary>
    public class InterviewResponse
    {
        public string characterId;
        public string topicId;
        public string text;
        public bool wasRepeat;
    }

    /// <summary>Penyimpan konten wawancara per karakter. Data-driven: tambah karakter tanpa ubah kode.</summary>
    public class InterviewContent
    {
        private readonly Dictionary<string, List<InterviewEntry>> byCharacter = new Dictionary<string, List<InterviewEntry>>();

        public void AddEntry(string characterId, InterviewEntry entry)
        {
            List<InterviewEntry> list;
            if (!byCharacter.TryGetValue(characterId, out list))
            {
                list = new List<InterviewEntry>();
                byCharacter.Add(characterId, list);
            }
            list.Add(entry);
        }

        // internal: UI di assembly lain tidak bisa membaca entri mentah (termasuk metadata kebenaran).
        internal IReadOnlyList<InterviewEntry> GetEntries(string characterId)
        {
            List<InterviewEntry> list;
            return byCharacter.TryGetValue(characterId, out list) ? list : new List<InterviewEntry>();
        }
    }
}
