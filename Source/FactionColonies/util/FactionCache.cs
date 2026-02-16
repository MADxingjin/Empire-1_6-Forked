using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public static class FactionCache
    {
        private static Faction _cachedColonyFaction = null;
        private static Faction _cachedPlayerFaction = null;
        private static FactionFC worldcomp = null;

        public static FactionFC FactionComp
        {
            get
            {
                if (worldcomp == null)
                {
                    worldcomp = Find.World.GetComponent<FactionFC>();
                }
                return worldcomp;
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
                    _cachedPlayerFaction = Find.FactionManager.AllFactions.ToList().Find(faction => faction.IsPlayer);
                }
                return _cachedPlayerFaction;
            }
        }

        public static void InvalidateCache()
        {
            LogUtil.Message("Invalidating FactionCache...");
            _cachedColonyFaction = null;
            _cachedPlayerFaction = null;
            worldcomp = null;
        }
    }
}
