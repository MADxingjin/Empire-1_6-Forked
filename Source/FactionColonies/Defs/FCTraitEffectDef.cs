using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCTraitEffectDef: Def
    {
        public string desc = ""; //Description of trait

        //THING + (Base/Multiplier) + STAT

        //Resource Base Production  = Connected
        public double productionBaseFood;        
        public double productionBaseWeapons;
        public double productionBaseApparel;
        public double productionBaseAnimals;
        public double productionBaseLogging;
        public double productionBaseMining;
        public double productionBaseResearch = 0;
        public double productionBasePower = 0;
        public double productionBaseMedicine = 0;


        //Resource Multiplier Production = Connected
        public double productionMultiplierFood = 1;
        public double productionMultiplierWeapons = 1;
        public double productionMultiplierApparel = 1;
        public double productionMultiplierAnimals = 1;
        public double productionMultiplierLogging = 1;
        public double productionMultiplierMining = 1;
        public double productionMultiplierResearch = 1;
        public double productionMultiplierPower = 1;
        public double productionMultiplierMedicine = 1;

        //Military Stats  = baselevel connected
        public double militaryBaseLevel;
        public double militaryMultiplierCombatEfficiency = 1;                                                                                          //#NEEDS TO BE IMPLEMENTED

        //Economic Stats
        public double taxBasePercentage; //0.01 - 2.00// Affects the base tax percentage        implemented
        public double taxBaseRandomModifier;  //Affects the modifier for tithe income            implemented
        public double prosperityBaseRecovery; //Affects how quickly settlements recover from lost prosperity                                         #NEEDS TO BE IMPLEMENTED
        public double workerBaseCost; //Affects how much a single worker costs                             implemented
        public double workerBaseMax; //Affects how many workers you can have (Max) before worker costs start to rise      Implemented
        public double workerBaseOverMax; //Affects how many workers past the max you can hire       Implemented

        //Social Stats Base
        public double happinessLostBase; //0.0 - 2.0;     Affects how much happiness is lost            Implemented
        public double happinessGainedBase; //0.0 - 2.0    Affects how much happiness is gained            Implemented
        public double loyaltyLostBase; //0.0 - 2.0;         Affects how much loyalty is lost            Implemented
        public double loyaltyGainedBase; //0.0 - 2.0;         Affects how much loyalty is gained            Implemented
        public double unrestLostBase;  //0.0 - 2.0;         Affects how much unrest is lost            Implemented
        public double unrestGainedBase; //0.0 - 2.0;         Affects how much loyalty is gained            Implemented

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
