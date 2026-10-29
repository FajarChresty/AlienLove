using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// Satu-satunya sumber identitas karakter. Satu characterId = satu record,
    /// jadi klien yang opt-in tidak pernah menjadi duplikat.
    /// </summary>
    public class CharacterRepository
    {
        private readonly Dictionary<string, CharacterRecord> byId = new Dictionary<string, CharacterRecord>();
        private readonly List<CharacterRecord> ordered = new List<CharacterRecord>();

        public IReadOnlyList<CharacterRecord> All { get { return ordered; } }
        public int Count { get { return ordered.Count; } }

        public bool TryAdd(CharacterRecord record, out string error)
        {
            var v = CharacterValidator.Validate(record);
            if (!v.IsValid) { error = v.ErrorText; return false; }
            if (byId.ContainsKey(record.characterId)) { error = "characterId sudah ada: " + record.characterId; return false; }
            byId.Add(record.characterId, record);
            ordered.Add(record);
            error = null;
            return true;
        }

        public bool Contains(string id) { return id != null && byId.ContainsKey(id); }

        public CharacterRecord Get(string id)
        {
            CharacterRecord r;
            return (id != null && byId.TryGetValue(id, out r)) ? r : null;
        }
    }
}
