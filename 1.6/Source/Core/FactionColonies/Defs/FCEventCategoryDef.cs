using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FCEventCategoryDef : Def
    {
        public Color color = new Color(0.65f, 0.65f, 0.65f);
        public int displayOrder = 100;
    }

    [DefOf]
    public class FCEventCategoryDefOf
    {
        public static FCEventCategoryDef EC_Settlement;
        public static FCEventCategoryDef EC_Construction;
        public static FCEventCategoryDef EC_Economy;
        public static FCEventCategoryDef EC_Policy;
        public static FCEventCategoryDef EC_Military;
        public static FCEventCategoryDef EC_Other;

        static FCEventCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCEventCategoryDefOf));
        }
    }
}
