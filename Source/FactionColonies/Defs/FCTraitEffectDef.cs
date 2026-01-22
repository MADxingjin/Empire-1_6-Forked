using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCTraitEffectDef: Def
    {
        public string desc = ""; //Description of trait

        //TODO: modifying all trait defs to use this new system is going to be SO much fun. Ugh
        public List<ResourceBonuses> resourceBonuses = new List<ResourceBonuses>();

        //Military Stats  = baselevel connected
        public double militaryBaseLevel = 0;
        public double militaryMultiplierCombatEfficiency = 1;                                                                                          //#NEEDS TO BE IMPLEMENTED

        //Economic Stats
        public double taxBasePercentage = 0; //0.01 - 2.00// Affects the base tax percentage        implemented
        public double taxBaseRandomModifier = 0;  //Affects the modifier for tithe income            implemented
        public double prosperityBaseRecovery = 0; //Affects how quickly settlements recover from lost prosperity                                         #NEEDS TO BE IMPLEMENTED
        public double workerBaseCost = 0; //Affects how much a single worker costs                             implemented
        public double workerBaseMax = 0; //Affects how many workers you can have (Max) before worker costs start to rise      Implemented
        public double workerBaseOverMax = 0; //Affects how many workers past the max you can hire       Implemented

        //Social Stats Base
        public double happinessLostBase = 0; //0.0 - 2.0;     Affects how much happiness is lost            Implemented
        public double happinessGainedBase = 0; //0.0 - 2.0    Affects how much happiness is gained            Implemented
        public double loyaltyLostBase = 0; //0.0 - 2.0;         Affects how much loyalty is lost            Implemented
        public double loyaltyGainedBase = 0; //0.0 - 2.0;         Affects how much loyalty is gained            Implemented
        public double unrestLostBase = 0;  //0.0 - 2.0;         Affects how much unrest is lost            Implemented
        public double unrestGainedBase = 0; //0.0 - 2.0;         Affects how much loyalty is gained            Implemented

        //Social Stats Multipliers
        public double happinessLostMultiplier = 1; //0.0 - 2.0;     Affects how much happiness is lost            Implemented
        public double happinessGainedMultiplier = 1; //0.0 - 2.0    Affects how much happiness is gained            Implemented
        public double loyaltyLostMultiplier = 1; //0.0 - 2.0;         Affects how much loyalty is lost            Implemented
        public double loyaltyGainedMultiplier = 1; //0.0 - 2.0;         Affects how much loyalty is gained            Implemented
        public double unrestLostMultiplier = 1;  //0.0 - 2.0;         Affects how much unrest is lost            Implemented
        public double unrestGainedMultiplier = 1; //0.0 - 2.0;         Affects how much loyalty is gained            Implemented
                                              //                                        #NEEDS TO BE IMPLEMENTED

        //Create Settlement Stats
        public double createSettlementBaseCost;  //affects how much it costs to create a settlement          Implemented only in faction
        public double createSettlementMultiplier = 1; //affects how much it costs to create a settlement         Only implemented in faction

        //Faction Pawn Required Traits
        List<TraitDef> forcedFactionPawnTraits = new List<TraitDef>();   //Traits that pawns are required to have                                        #NEEDS TO BE IMPLEMENTED
        List<TraitDef> factionAllowedRaces = new List<TraitDef>();   //Traits that pawns are required to have                                        #NEEDS TO BE IMPLEMENTED
        List<Thing> factionUniform = new List<Thing>(); //List of the things pawns in the faction can wear                                        #NEEDS TO BE IMPLEMENTED

        public ResourceBonuses getTraitResource(ResourceTypeDef resourceTypeDef)
        {
            return resourceBonuses.Where((ResourceBonuses b) => b.resourceDef == resourceTypeDef).FirstOrDefault();
        }
        public bool appliesToSettlements()
        {
            //TODO: surely there's a better way to do this?
            if (taxBasePercentage == 0 && taxBaseRandomModifier == 0 && prosperityBaseRecovery == 0 && workerBaseCost == 0 && workerBaseMax == 0 &&
                workerBaseOverMax == 0 && happinessLostBase == 0 && happinessGainedBase == 0 && loyaltyLostBase == 0 && loyaltyGainedBase == 0 &&
                unrestLostBase == 0 && unrestGainedBase == 0 && happinessLostMultiplier == 1 && happinessGainedMultiplier == 1 &&
                loyaltyLostMultiplier == 1 && loyaltyGainedMultiplier == 1 && unrestLostMultiplier == 1 && unrestGainedMultiplier == 1 &&
                militaryBaseLevel == 0)
            {
                return false;
            }
            return true;
        }
    }

    [DefOf]
    public class FCTraitEffectDefOf
    {

        public static FCTraitEffectDef shuttlePort;

        static FCTraitEffectDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCTraitEffectDefOf));
        }
    }
}
