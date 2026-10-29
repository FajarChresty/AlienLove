using System.Collections.Generic;

namespace AlienLove.Matchmaking
{
    /// <summary>
    /// Daftar 15 trait (Trait System V1, Sub-GDD bab 5).
    /// Semua nama & label ada di SATU tempat ini; logika lain hanya membaca dari sini.
    /// Kalau trait berubah, edit file ini saja.
    /// </summary>
    public static class TraitCatalog
    {
        public const int TraitCount = 15;
        public const int MinValue = 0;
        public const int MaxValue = 3;

        private static readonly List<TraitDefinition> all = Build();

        public static IReadOnlyList<TraitDefinition> All { get { return all; } }

        public static TraitDefinition GetByIndex(int index) { return all[index]; }

        public static bool IsValidValue(int value) { return value >= MinValue && value <= MaxValue; }

        private static TraitDefinition T(int id, string group, string name, string l0, string l1, string l2, string l3, string desc = "")
        {
            return new TraitDefinition
            {
                id = id, group = group, displayName = name,
                positionLabels = new[] { l0, l1, l2, l3 },
                description = desc
            };
        }

        private static List<TraitDefinition> Build()
        {
            const string A = "Social and Communication";
            const string B = "Emotional and Relationship Behavior";
            const string C = "Independence and Lifestyle";
            const string D = "Goals, Money, and Experiences";
            const string E = "Intellectual, Social-Bond, and Cultural Outlook";

            return new List<TraitDefinition>
            {
                T(1,  A, "Social Orientation",   "Very Social", "Social-Leaning", "Solitude-Leaning", "Very Solitary"),
                T(2,  A, "Communication Style",  "Very Direct", "Mostly Direct", "Mostly Tactful/Indirect", "Very Tactful/Indirect", "Tidak menentukan jujur atau tidaknya karakter."),
                T(3,  A, "Emotional Expressiveness", "Very Expressive", "Somewhat Expressive", "Somewhat Reserved", "Very Reserved"),
                T(4,  B, "Conflict Approach",    "Addresses Conflict Immediately", "Prefers Early Discussion", "Needs Time Before Discussion", "Tends to Avoid Conflict"),
                T(5,  B, "Emotional Intensity",  "Very Intense", "Emotionally Intense-Leaning", "Emotionally Steady-Leaning", "Very Steady", "Bukan ukuran kedewasaan atau kebaikan."),
                T(6,  B, "Affection Style",      "Very Openly Affectionate", "Generally Affectionate", "Subtle and Reserved", "Very Private with Affection"),
                T(7,  C, "Relationship Pace",    "Very Fast Commitment", "Relatively Fast", "Relatively Slow", "Very Slow and Cautious"),
                T(8,  C, "Personal Independence","Strongly Interdependent", "Leans Toward Interdependence", "Leans Toward Independence", "Strongly Independent"),
                T(9,  C, "Lifestyle Structure",  "Very Structured", "Mostly Structured", "Mostly Spontaneous", "Very Spontaneous"),
                T(10, D, "Ambition and Life Priorities", "Achievement-Driven", "Leans Toward Achievement", "Leans Toward Life Balance", "Strongly Prioritizes Life Balance", "Memilih keseimbangan bukan berarti malas."),
                T(11, D, "Financial Attitude",   "Very Security-Oriented", "Prefers Saving", "Prefers Spending on Experiences", "Very Spontaneous with Spending", "Sikap terhadap uang, bukan jumlah kekayaan."),
                T(12, D, "Adventure Orientation","Very Adventure-Seeking", "Open to Adventure", "Prefers Familiar Experiences", "Strongly Prefers Safety and Familiarity"),
                T(13, E, "Intellectual Engagement", "Loves Abstract Ideas and Debate", "Leans Toward Theoretical Exploration", "Leans Toward Practical Discussion", "Strongly Prefers Concrete, Practical Topics", "Bukan ukuran kecerdasan."),
                T(14, E, "Family and Community Orientation", "Strongly Family/Community-Centered", "Leans Toward Family/Community", "Leans Toward Couple-Centered Privacy", "Strongly Couple-Centered and Private"),
                T(15, E, "Tradition and Change Orientation", "Strongly Tradition-Oriented", "Leans Toward Tradition", "Leans Toward Change", "Strongly Change-Oriented", "Tidak boleh dipakai untuk menilai satu budaya lebih unggul.")
            };
        }
    }
}
