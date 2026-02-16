using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies.util
{
    public static class CraftUtil
    {
        public static bool canCraftItem(ThingDef thing, bool includeSingleUse = false)
        {
            bool canCraft = true;
            if (thing.recipeMaker != null)
            {
                if (thing.recipeMaker.researchPrerequisites != null)
                {
                    foreach (ResearchProjectDef research in thing.recipeMaker.researchPrerequisites)
                    {
                        if (!(Find.ResearchManager.GetProgress(research) >= research.baseCost))
                        {
                            //research is not good
                            canCraft = false;
                        }
                    }
                }

                if (thing.recipeMaker.researchPrerequisite != null)
                {
                    if (!(Find.ResearchManager.GetProgress(thing.recipeMaker.researchPrerequisite) >=
                          thing.recipeMaker.researchPrerequisite.baseCost))
                    {
                        //research is not good
                        canCraft = false;
                    }
                }
            }
            else
            {
                if (FactionCache.FactionComp.techLevel < thing.techLevel)
                {
                    canCraft = false;
                }
            }

            if (thing.thingSetMakerTags != null && thing.thingSetMakerTags.Contains("SingleUseWeapon") &&
                !includeSingleUse)
            {
                canCraft = false;
            }


            return canCraft;
        }

        public static bool thingHasQuality(ThingDef thing)
        {
            return thing.HasComp<CompQuality>();
        }
        public static bool thingIsStuffable(ThingDef thing)
        {
            return thing.MadeFromStuff;
        }
        /// <summary>
        /// For the given <paramref name="thing"/>, returns a list of valid stuff ThingDefs.
        /// </summary>
        /// <param name="thing">ThingDef to retrieve a list of stuff for.</param>
        /// <param name="filterList">List of possible things to use for stuff.</param>
        /// <returns>The list of ThingDefs that can be used to stuff the given <paramref name="thing"/>. Returns an empty list if <paramref name="thing"/> is not stuffable.</returns>
        public static List<ThingDef> getThingStuffs(ThingDef thing, List<ThingDef> filterList)
        {
            List<ThingDef> list = new List<ThingDef>();
            if (thingIsStuffable(thing) && filterList.Count > 0)
            {
                foreach(ThingDef possible in filterList)
                {
                    if (possible.IsStuff && possible.stuffProps.CanMake(thing))
                    {
                        list.Add(possible);
                    }
                }
            }
            return list;
        }
        /*
        public static void filterResource(ThingFilter filter, ResourceType resourceType, TechLevel techLevel, SettlementFC settlement = null)
        {
            switch (resourceType)
            {
                case ResourceType.Food:
                    filter.SetAllow(ThingCategoryDefOf.Foods, true);
                    filter.SetAllow(ThingDefOf.Hay, true);
                    filter.SetAllow(ThingDefOf.Kibble, true);
                    break;
                case ResourceType.Weapons:
                    filter.SetAllow(ThingCategoryDefOf.Weapons, true);
                    filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("MortarShells"), true);
                    if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Ammo") != null)
                        filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Ammo"), true);
                    break;
                case ResourceType.Apparel:
                    filter.SetAllow(ThingCategoryDefOf.Apparel, true);
                    filter.SetAllow(ThingDefOf.Cloth, true);
                    if (ResearchUtil.returnIsResearched(
                        DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Devilstrand")))
                    {
                        filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("DevilstrandCloth"), true);
                    }
                    break;
                case ResourceType.Animals:
                    List<PawnKindDef> allAnimalDefs = FactionCache.AllPawnKindDefs;
                    foreach (PawnKindDef def in allAnimalDefs)
                    {
                        if (def.IsAnimalAndAllowed())
                        {
                            filter.SetAllow(def.race, true);
                        }
                    }
                    break;
                case ResourceType.Logging:
                    filter.SetAllow(ThingDefOf.WoodLog, true);
                    filter.SetAllow(StuffCategoryDefOf.Woody, true);
                    break;
                case ResourceType.Mining:
                    filter.SetAllow(StuffCategoryDefOf.Metallic, true);
                    filter.SetAllow(StuffCategoryDefOf.Stony, true);
                    filter.SetAllow(ThingDefOf.Silver, false);
                    //Android shit?
                    filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("Teachmat"), false);
                    //Remove RimBees Beeswax
                    filter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("RB_Waxy"), false);
                    //Remove Alpha Animals skysteel
                    filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("AA_SkySteel"), false);
                    ThingDef rawMagicyte = DefDatabase<ThingDef>.GetNamedSilentFail("RawMagicyte");
                    if (rawMagicyte != null)
                    {
                        filter.SetAllow(rawMagicyte, true);
                    }
                    filter.SetAllow(ThingDefOf.ComponentIndustrial, true);
                    filter.SetAllow(ThingCategoryDefOf.StoneBlocks, true);
                    break;
                case ResourceType.Research:
                case ResourceType.Power:
                    break;
                case ResourceType.Medicine:
                    filter.SetAllow(ThingCategoryDefOf.Medicine, true);
                    filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural"), true);
                    switch (techLevel)
                    {
                        case TechLevel.Archotech:
                        case TechLevel.Ultra:
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsUltra"), true);
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsBionic"), true);
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsProsthetic"), true);
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AdvancedProstheses") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AdvancedProstheses"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Neurotrainers") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Neurotrainers"), true);
                            break;
                        case TechLevel.Spacer:
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsBionic"), true);
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsProsthetic"), true);
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AdvancedProstheses") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AdvancedProstheses"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Neurotrainers") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Neurotrainers"), true);
                            break;
                        case TechLevel.Industrial:
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsProsthetic"), true);
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural"), true);
                            filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsBionic"), true);
                            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses") != null)
                                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses"), true);
                            break;
                    }
                    break;
                case ResourceType.Gravtech:
                    filter.SetAllow(ThingDefOf.GravlitePanel, true);
                    break;
                case ResourceType.Chemfuel:
                    filter.SetAllow(ThingDefOf.Chemfuel, true);
                    break;
            }
        }*/
    }
}
