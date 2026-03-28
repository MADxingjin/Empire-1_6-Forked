using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCEventDef : Def
    {
        public int timeTillTrigger = -1;
        public string desc;
        public FCEventCategoryDef category;

        //Random Event Information
        public bool isRandomEvent = false;
        public bool activateAtStart;
        public int requiredWealth = 0;
        public IntRange rangeSettlementsAffected = new IntRange(0, 0);
        public bool targetAllSettlements = false;
        public bool settlementsCarryOver = true;
        public bool useProximity = true;
        public float proximityFalloff = 20f;
        public int weight = 0;
        public int minimumHappiness = 0;
        public int maximumHappiness = 100;
        public int minimumLoyalty = 0;
        public int maximumLoyalty = 100;
        public int minimumUnrest = 0;
        public int maximumUnrest = 100;
        public int minimumProsperity = 0;
        public int maximumProsperity = 100;
        public ResourceTypeDef requiredResource;
        public List<WorldSettlementDef> allowedSettlementTypes = new List<WorldSettlementDef>();
        public List<WorldSettlementDef> blockedSettlementTypes = new List<WorldSettlementDef>();
        public List<FCEventDef> incompatibleEvents = new List<FCEventDef>();

        //Options
        public List<FCOptionDef> options = new List<FCOptionDef>();
        public string optionDescription = "";

        //Event chain
        public bool eventFollows = false;
        public FCEventDef followingEvent = null;
        public FCEventDef followingEvent2 = null;
        public bool splitEventFollows = false;
        public int splitEventChance = 50;

        //Rewards
        public List<ThingDef> loot = new List<ThingDef>();
        public int randomThingValue = 0;
        public ResourceEventRewardDef randomThingRewardDef;
        public int prosperityLost = 0;
        public List<string> applicableBiomes = new List<string>();
        public List<string> restrictedBiomes = new List<string>();

        //Stat modifiers during event (removed when event expires)
        public List<FCStatModifier> statModifiers = new List<FCStatModifier>();

        //Permanent stat modifiers (persist on settlement after event expires)
        public List<FCStatModifier> permanentStatModifiers = new List<FCStatModifier>();

        public bool isMilitaryEvent = false;
        public bool isNegative = false;

        public bool BiomeAllowed(string biome)
        {
            if (applicableBiomes.Count > 0)
                return applicableBiomes.Contains(biome);
            if (restrictedBiomes.Count > 0)
                return !restrictedBiomes.Contains(biome);
            return true;
        }

        /// <summary>
        /// Checks whether this event can target the given settlement type.
        /// Uses depth-based resolution matching <see cref="BuildingFCDef.CanBeBuiltForSettlementType"/>.
        /// Both allow and block lists can coexist; most specific (shallowest depth) wins, tie goes to block.
        /// </summary>
        public bool SettlementTypeAllowed(WorldSettlementDef settlementDef)
        {
            int allowDepth = (allowedSettlementTypes.Count > 0)
                ? settlementDef.DepthInList(allowedSettlementTypes)
                : -1;
            int blockDepth = (blockedSettlementTypes.Count > 0)
                ? settlementDef.DepthInList(blockedSettlementTypes)
                : -1;

            if (allowDepth >= 0 || blockDepth >= 0)
            {
                if (allowDepth >= 0 && blockDepth >= 0)
                    return allowDepth < blockDepth;
                return allowDepth >= 0;
            }

            if (allowedSettlementTypes.Count > 0)
                return false;
            return true;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string err in base.ConfigErrors())
                yield return err;
            foreach (string err in FCStatModifier.ConfigErrors(statModifiers, defName))
                yield return err;
            foreach (string err in FCStatModifier.ConfigErrors(permanentStatModifiers, defName + ".permanentStatModifiers"))
                yield return err;
            if (targetAllSettlements && rangeSettlementsAffected.max != 0)
                yield return $"{defName}: targetAllSettlements is true but rangeSettlementsAffected.max is {rangeSettlementsAffected.max}";
            foreach (string biome in applicableBiomes)
            {
                if (DefDatabase<BiomeDef>.GetNamed(biome, false) == null)
                    yield return $"{defName}: applicableBiomes contains unknown biome '{biome}'";
            }

            foreach (string biome in restrictedBiomes)
            {
                if (DefDatabase<BiomeDef>.GetNamed(biome, false) == null)
                    yield return $"{defName}: restrictedBiomes contains unknown biome '{biome}'";
            }
        }
    }
    
    [DefOf]
    public class FCEventDefOf
    {
        //List Events here - loads events at start
        public static FCEventDef Null;
        public static FCEventDef settleNewColony;
        public static FCEventDef taxColony;
        public static FCEventDef constructBuilding;
        public static FCEventDef enactSettlementPolicy;
        public static FCEventDef enactFactionPolicy;
        public static FCEventDef upgradeSettlement;
        public static FCEventDef raidEnemySettlement;
        public static FCEventDef enslaveEnemySettlement;
        public static FCEventDef captureEnemySettlement;
        public static FCEventDef cooldownMilitary;
        public static FCEventDef settlementBeingAttacked;
        public static FCEventDef deliveryArrival;

        static FCEventDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCEventDefOf));
        }
    }
}