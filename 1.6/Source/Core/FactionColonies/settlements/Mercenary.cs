using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class Mercenary : IExposable, ILoadReferenceable
    {
        //init variables
        public MilUnitFC loadout;
        public MercenarySquadFC squad;
        public WorldSettlementFC settlement;
        public Mercenary handler;
        public Mercenary animal;
        public Pawn pawn;
        public bool deployable = false;
        public int loadID;
        private bool isOnMap = false;

        /// <summary>
        /// Extensible data dictionary for submods. Keyed by submod namespace to avoid collisions.
        /// Use <see cref="GetCustomData{T}"/>, <see cref="SetCustomData"/>, <see cref="RemoveCustomData"/>.
        /// </summary>
        private Dictionary<string, IExposable> customData;

        public Mercenary()
        {

        }

        public Mercenary(bool blank)
        {
            loadID = FactionCache.FactionComp.GetNextMercenaryID();
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                isOnMap = pawn?.Map != null;
            }

            Scribe_Values.Look(ref isOnMap, "isOnMap", false);
            Scribe_References.Look(ref loadout, "loadout");
            Scribe_References.Look(ref squad, "squad");
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_References.Look(ref handler, "handler");
            Scribe_References.Look(ref animal, "animal");

            if (isOnMap)
            {
                Scribe_References.Look(ref pawn, "pawn");
            }
            else
            {
                Scribe_Deep.Look(ref pawn, "pawn");
            }

            Scribe_Values.Look(ref loadID, "loadID");

            // Custom data — only expose if non-empty (backwards compatible with older saves)
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                bool hasData = customData != null && customData.Count > 0;
                Scribe_Values.Look(ref hasData, "hasCustomData", false);
                if (hasData)
                    Scribe_Collections.Look(ref customData, "customData", LookMode.Value, LookMode.Deep);
            }
            else
            {
                bool hasData = false;
                Scribe_Values.Look(ref hasData, "hasCustomData", false);
                if (hasData)
                    Scribe_Collections.Look(ref customData, "customData", LookMode.Value, LookMode.Deep);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit && pawn != null && pawn.kindDef == null)
            {
                pawn.kindDef = PawnKindDefOf.Colonist;
                LogUtil.Warning($"Mercenary pawn {pawn.LabelShort} had null kindDef on load, reset to Colonist.");
            }
        }

        public T GetCustomData<T>(string key) where T : class, IExposable
        {
            if (customData != null && customData.TryGetValue(key, out IExposable val))
                return val as T;
            return null;
        }

        public void SetCustomData(string key, IExposable data)
        {
            if (customData == null) customData = new Dictionary<string, IExposable>();
            customData[key] = data;
        }

        public void RemoveCustomData(string key)
        {
            customData?.Remove(key);
        }

        public string GetUniqueLoadID()
        {
            return "Mercenary_" + loadID;
        }
    }
}