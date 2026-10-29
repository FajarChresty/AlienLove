using UnityEngine;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// Wrapper ScriptableObject untuk MatchmakingConfig.
    /// Buat via: klik kanan di Project > Create > AlienLove > Matchmaking Config.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchmakingConfig", menuName = "AlienLove/Matchmaking Config")]
    public class MatchmakingConfigAsset : ScriptableObject
    {
        public MatchmakingConfig config = new MatchmakingConfig();
    }
}
