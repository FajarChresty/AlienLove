using System;

namespace AlienLove.Matchmaking
{
    /// <summary>Definisi satu trait: nama, grup, dan 4 label posisi (0-3).</summary>
    [Serializable]
    public class TraitDefinition
    {
        public int id;                 // 1..15
        public string group;
        public string displayName;
        public string[] positionLabels = new string[4];
        public string description;

        public string GetLabel(int value)
        {
            if (value < 0 || value >= positionLabels.Length) return "?";
            return positionLabels[value];
        }
    }
}
