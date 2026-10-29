using UnityEngine;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// Wrapper ScriptableObject untuk satu karakter asli (nanti dipakai penulis konten).
    /// Buat via: Create > AlienLove > Character.
    /// </summary>
    [CreateAssetMenu(fileName = "Character", menuName = "AlienLove/Character")]
    public class CharacterAsset : ScriptableObject
    {
        public CharacterRecord record = new CharacterRecord();
    }
}
