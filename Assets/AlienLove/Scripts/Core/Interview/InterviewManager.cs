using System;
using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// Mengelola state wawancara per sesi per karakter (Sub-GDD 7.4 dan 7.5).
    /// - Pertanyaan yang sama tidak memberi informasi baru berulang-ulang.
    /// - Tidak ada penalti buatan untuk pertanyaan ulang.
    /// - Tidak pernah mengembalikan status benar/bohong.
    /// </summary>
    public class InterviewManager
    {
        private readonly InterviewContent content;
        private readonly Dictionary<string, HashSet<string>> asked = new Dictionary<string, HashSet<string>>();

        public InterviewManager(InterviewContent interviewContent)
        {
            if (interviewContent == null) throw new ArgumentNullException("interviewContent");
            content = interviewContent;
        }

        private static string Key(string sessionId, string characterId) { return sessionId + "|" + characterId; }

        public List<InterviewTopicInfo> GetAvailableTopics(string sessionId, string characterId)
        {
            var topics = new List<InterviewTopicInfo>();
            var entries = content.GetEntries(characterId);
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (!PrerequisiteMet(sessionId, characterId, e)) continue;
                topics.Add(new InterviewTopicInfo
                {
                    topicId = e.topicId,
                    question = e.question,
                    alreadyAsked = HasAsked(sessionId, characterId, e.topicId)
                });
            }
            return topics;
        }

        public bool Ask(string sessionId, string characterId, string topicId, out InterviewResponse response, out string error)
        {
            response = null;
            InterviewEntry entry = null;
            var entries = content.GetEntries(characterId);
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].topicId == topicId) { entry = entries[i]; break; }

            if (entry == null) { error = "Topik tidak ditemukan."; return false; }
            if (!PrerequisiteMet(sessionId, characterId, entry)) { error = "Topik belum tersedia."; return false; }

            bool repeat = HasAsked(sessionId, characterId, topicId);
            string text;
            if (repeat)
            {
                // Dipakai jawaban ulang yang ditulis penulis; kalau tidak ada, jawaban lama dipakai ulang.
                text = !string.IsNullOrEmpty(entry.repeatAnswer) ? entry.repeatAnswer : Compose(entry);
            }
            else
            {
                text = Compose(entry);
                HashSet<string> set;
                if (!asked.TryGetValue(Key(sessionId, characterId), out set))
                {
                    set = new HashSet<string>();
                    asked.Add(Key(sessionId, characterId), set);
                }
                set.Add(topicId);
            }

            response = new InterviewResponse { characterId = characterId, topicId = topicId, text = text, wasRepeat = repeat };
            error = null;
            return true;
        }

        private bool HasAsked(string sessionId, string characterId, string topicId)
        {
            HashSet<string> set;
            return asked.TryGetValue(Key(sessionId, characterId), out set) && set.Contains(topicId);
        }

        private bool PrerequisiteMet(string sessionId, string characterId, InterviewEntry e)
        {
            return string.IsNullOrEmpty(e.prerequisiteTopicId) || HasAsked(sessionId, characterId, e.prerequisiteTopicId);
        }

        private static string Compose(InterviewEntry e)
        {
            return string.IsNullOrEmpty(e.clue) ? e.answer : e.answer + " " + e.clue;
        }
    }
}
