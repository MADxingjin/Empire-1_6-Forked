using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Static cache to hold on to frequently-accessed fields that change infrequently, or never.
    /// 
    /// <para>This cache needs to be invalidated any time the game loads. Presently, this is done in the ExposeDate() function in the FactionFC WorldComponent.</para>
    /// <para>NOTE: DefDatabase[PawnKindDef].AllDefsListForReading is cached here. That means that def hotloading is a no-no.</para>
    /// </summary>
    public static class FactionCache
    {
        private static Faction _cachedColonyFaction = null;
        private static Faction _cachedPlayerFaction = null;
        private static FactionFC _cachedFactionWorldComp = null;
        private static List<PawnKindDef> _cachedPawnKindDefs = null;
        private static Dictionary<(Type, string), FieldInfo> _cachedFields = new Dictionary<(Type, string), FieldInfo>();

        public static FactionFC FactionComp
        {
            get
            {
                if (_cachedFactionWorldComp == null)
                {
                    _cachedFactionWorldComp = Find.World.GetComponent<FactionFC>();
                }
                return _cachedFactionWorldComp;
            }
        }

        public static Faction PlayerColonyFaction
        {
            get
            {
                if (_cachedColonyFaction == null)
                {
                    _cachedColonyFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("PColony"));
                }
                return _cachedColonyFaction;
            }
        }
        public static Faction PlayerFaction
        {
            get
            {
                if (_cachedPlayerFaction == null)
                {
                    _cachedPlayerFaction = Find.FactionManager.AllFactions.FirstOrDefault(faction => faction.IsPlayer);
                }
                return _cachedPlayerFaction;
            }
        }
        public static List<PawnKindDef> AllPawnKindDefs
        {
            get
            {
                if (_cachedPawnKindDefs == null || _cachedPawnKindDefs.Count == 0)
                {
                    _cachedPawnKindDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;
                }
                return _cachedPawnKindDefs;
            }
        }
        public static Dictionary<(Type, string), FieldInfo> FieldCache => _cachedFields;
        public static FieldInfo GetFieldCacheValue(Type typ, string field)
        {
            FieldInfo fieldInfo;
            if (FieldCache.TryGetValue((typ, field), out fieldInfo))
            {
                return fieldInfo;
            }
            fieldInfo = typ.GetField(field);
            FieldCache.Add((typ, field), fieldInfo);
            return fieldInfo;
        }

        public static void InvalidateCache()
        {
            LogUtil.Message("Invalidating FactionCache...");
            _cachedColonyFaction = null;
            _cachedPlayerFaction = null;
            _cachedPawnKindDefs = null;
            _cachedFactionWorldComp = null;
            _cachedFields.Clear();
        }
    }
}
