using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static System.Collections.Specialized.BitVector32;

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
        private static FactionDef _cachedFactionDef = null;
        private static List<PawnKindDef> _cachedPawnKindDefs = null;
        private static Dictionary<(Type, string), FieldInfo> _cachedFields = new Dictionary<(Type, string), FieldInfo>();
        private static List<XenotypeDef> _cachedXenotypeList = null;
        private static List<CustomXenotype> _cachedCustomXenotypeList = null;
        private static List<ThingDef> _cachedRaceList = null;
        private static List<PawnKindDef> _cachedAnimalKinds = null;
        private static List<PawnKindDef> _cachedCombatAnimalKinds = null;

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
        /// <summary>
        /// The NPC Empire faction that the player created and controls.
        /// </summary>
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
        /// <summary>
        /// The player faction itself.
        /// </summary>
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
        public static FactionDef EmpireFactionDef
        {
            get
            {
                if (_cachedFactionDef == null)
                {
                    _cachedFactionDef = DefDatabase<FactionDef>.GetNamed("PColony");
                }
                return _cachedFactionDef;
            }
        }
        public static List<XenotypeDef> XenotypeDefs
        {
            get
            {
                if (_cachedXenotypeList == null)
                {
                    _cachedXenotypeList = DefDatabase<XenotypeDef>.AllDefsListForReading;
                }
                return _cachedXenotypeList;
            }
        }
        public static List<CustomXenotype> CustomXenotypes
        {
            get
            {
                if (_cachedCustomXenotypeList == null)
                {
                    _cachedCustomXenotypeList = Current.Game?.customXenotypeDatabase?.customXenotypes;
                }
                return _cachedCustomXenotypeList;
            }
        }
        public static List<ThingDef> HumanlikeRaces
        {
            get
            {
                if (_cachedRaceList == null)
                {
                    _cachedRaceList = new List<ThingDef>();
                    foreach (PawnKindDef pawnKind in AllPawnKindDefs)
                    {
                        if (pawnKind.race != null && !_cachedRaceList.Contains(pawnKind.race) && (pawnKind.race == ThingDefOf.Human || pawnKind.IsHumanLikeRace()))
                        {
                            _cachedRaceList.Add(pawnKind.race);
                        }
                    }
                }
                return _cachedRaceList;
            }
        }
        // Technically there should *always* be at least one race: ThingDefOf.Human. But it probably can't hurt to null-check, just in case of edge cases...
        public static int HumanlikeRacesCount
        {
            get
            {
                if (HumanlikeRaces == null)
                {
                    return 0;
                }
                else
                {
                    return HumanlikeRaces.Count;
                }
            }
        }
        public static List<PawnKindDef> AllAnimalKindDefs
        {
            get
            {
                if (_cachedAnimalKinds == null)
                {
                    _cachedAnimalKinds = new List<PawnKindDef>();
                    foreach (PawnKindDef def in AllPawnKindDefs)
                    {
                        if (def.IsAnimalAndAllowed())
                        {
                            _cachedAnimalKinds.Add(def);
                        }
                    }
                }
                return _cachedAnimalKinds;
            }
        }
        public static List<PawnKindDef> AllCombatAnimalKindDefs
        {
            get
            {
                if(_cachedCombatAnimalKinds == null)
                {
                    _cachedCombatAnimalKinds = new List<PawnKindDef>();
                    foreach (PawnKindDef def in AllPawnKindDefs)
                    {
                        if (def.IsCombatAnimal())
                        {
                            _cachedCombatAnimalKinds.Add(def);
                        }
                    }
                }
                return _cachedCombatAnimalKinds;
            }
        }

        public static void InvalidateCache()
        {
            LogUtil.Message("Invalidating FactionCache...");
            _cachedColonyFaction = null;
            _cachedPlayerFaction = null;
            _cachedPawnKindDefs = null;
            _cachedFactionWorldComp = null;
            _cachedFactionDef = null;
            _cachedFields.Clear();
            _cachedRaceList = null;
            _cachedXenotypeList = null;
            _cachedAnimalKinds = null;
            _cachedCombatAnimalKinds = null;
            InvalidateCustomXenotypeCache();
        }
        /* Custom xenotypes are actually expected to change while the game is loaded, and thus we may have to refresh that specific cache more frequently than the rest.
         * Hence, it gets its own function. */
        public static void InvalidateCustomXenotypeCache()
        {
            _cachedCustomXenotypeList = null;
        }
    }
}
