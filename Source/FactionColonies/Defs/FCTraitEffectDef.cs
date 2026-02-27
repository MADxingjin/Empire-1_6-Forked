using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FCTraitEffectDef: Def
    {
        public string desc = ""; //Description of trait

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

        private bool didCacheTraitBonusDesc = false;
        private TaggedString cachedTraitBonusDesc = "";
        /// <summary>
        /// A multi-line TaggedString listing out this trait's resource bonuses.
        /// </summary>
        public TaggedString traitBonusDesc
        {
            get
            {
                if (!didCacheTraitBonusDesc)
                {
                    if (resourceBonuses.Count > 0)
                    {
                        //cachedTraitBonusDesc += "FCTraitDesc_ResourceBonusLabel".Translate() + ":\n";
                        foreach (ResourceBonuses rb in resourceBonuses)
                        {
                            cachedTraitBonusDesc += rb.getBonusDesc("") + "\n";
                        }
                    }
                    /* Death and Taxes */
                    if (militaryBaseLevel != 0) cachedTraitBonusDesc += "FCTraitDesc_MilitaryLevel".Translate(TextUtil.colorizeAdditiveBonus(militaryBaseLevel)) + "\n";
                    if (militaryMultiplierCombatEfficiency != 1) cachedTraitBonusDesc += "FCTraitDesc_MilitaryCombatEfficiency".Translate(TextUtil.colorizeMultiplierBonus(militaryMultiplierCombatEfficiency)) + "\n";
                    if (taxBasePercentage != 0) cachedTraitBonusDesc += "FCTraitDesc_taxBasePercentage".Translate(TextUtil.colorizeAdditiveBonus(taxBasePercentage)) + "\n";
                    if (taxBaseRandomModifier != 0) cachedTraitBonusDesc += "FCTraitDesc_taxBaseRandomModifier".Translate(TextUtil.colorizeAdditiveBonus(taxBaseRandomModifier)) + "\n";
                    if (prosperityBaseRecovery != 0) cachedTraitBonusDesc += "FCTraitDesc_prosperityBaseRecovery".Translate(TextUtil.colorizeAdditiveBonus(prosperityBaseRecovery)) + "\n";
                    /* Workers */
                    if (workerBaseCost != 0) cachedTraitBonusDesc += "FCTraitDesc_workerBaseCost".Translate(TextUtil.colorizeAdditiveBonus(workerBaseCost, true)) + "\n";
                    if (workerBaseMax != 0) cachedTraitBonusDesc += "FCTraitDesc_workerBaseMax".Translate(TextUtil.colorizeAdditiveBonus(workerBaseMax)) + "\n";
                    if (workerBaseOverMax != 0) cachedTraitBonusDesc += "FCTraitDesc_workerBaseOverMax".Translate(TextUtil.colorizeAdditiveBonus(workerBaseOverMax)) + "\n";
                    /* Happiness */
                    if (happinessLostBase != 0) cachedTraitBonusDesc += "FCTraitDesc_happinessLostBase".Translate(TextUtil.colorizeAdditiveBonus(happinessLostBase, true)) + "\n";
                    if (happinessGainedBase != 0) cachedTraitBonusDesc += "FCTraitDesc_happinessGainedBase".Translate(TextUtil.colorizeAdditiveBonus(happinessGainedBase)) + "\n";
                    if (happinessLostMultiplier != 1) cachedTraitBonusDesc += "FCTraitDesc_happinessLostMultiplier".Translate(TextUtil.colorizeMultiplierBonus(happinessLostMultiplier, true)) + "\n";
                    if (happinessGainedMultiplier != 1) cachedTraitBonusDesc += "FCTraitDesc_happinessGainedMultiplier".Translate(TextUtil.colorizeMultiplierBonus(happinessGainedMultiplier)) + "\n";
                    /* Loyalty */
                    if (loyaltyLostBase != 0) cachedTraitBonusDesc += "FCTraitDesc_loyaltyLostBase".Translate(TextUtil.colorizeAdditiveBonus(loyaltyLostBase, true)) + "\n";
                    if (loyaltyGainedBase != 0) cachedTraitBonusDesc += "FCTraitDesc_loyaltyGainedBase".Translate(TextUtil.colorizeAdditiveBonus(loyaltyGainedBase)) + "\n";
                    if (loyaltyLostMultiplier != 1) cachedTraitBonusDesc += "FCTraitDesc_loyaltyLostMultiplier".Translate(TextUtil.colorizeMultiplierBonus(loyaltyLostMultiplier, true)) + "\n";
                    if (loyaltyGainedMultiplier != 1) cachedTraitBonusDesc += "FCTraitDesc_loyaltyGainedMultiplier".Translate(TextUtil.colorizeMultiplierBonus(loyaltyGainedMultiplier)) + "\n";
                    /* Unrest */
                    if (unrestLostBase != 0) cachedTraitBonusDesc += "FCTraitDesc_unrestLostBase".Translate(TextUtil.colorizeAdditiveBonus(unrestLostBase)) + "\n";
                    if (unrestGainedBase != 0) cachedTraitBonusDesc += "FCTraitDesc_unrestGainedBase".Translate(TextUtil.colorizeAdditiveBonus(unrestGainedBase, true)) + "\n";
                    if (unrestLostMultiplier != 1) cachedTraitBonusDesc += "FCTraitDesc_unrestLostMultiplier".Translate(TextUtil.colorizeMultiplierBonus(unrestLostMultiplier)) + "\n";
                    if (unrestGainedMultiplier != 1) cachedTraitBonusDesc += "FCTraitDesc_unrestGainedMultiplier".Translate(TextUtil.colorizeMultiplierBonus(unrestGainedMultiplier, true)) + "\n";
                    /* Settlement Cost */
                    if (createSettlementBaseCost != 0) cachedTraitBonusDesc += "FCTraitDesc_createSettlementBaseCost".Translate(TextUtil.colorizeAdditiveBonus(createSettlementBaseCost, true)) + "\n";
                    if (createSettlementMultiplier != 1) cachedTraitBonusDesc += "FCTraitDesc_createSettlementMultiplier".Translate(TextUtil.colorizeMultiplierBonus(createSettlementMultiplier, true)) + "\n";

                    cachedTraitBonusDesc = cachedTraitBonusDesc.Trim();

                    /* Only want to do all of this crap once. It shouldn't change during gameplay, after all. So cache it */
                    didCacheTraitBonusDesc = true;
                }
                return cachedTraitBonusDesc;
            }
        }

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
