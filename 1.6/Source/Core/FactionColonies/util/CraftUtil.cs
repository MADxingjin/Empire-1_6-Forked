using RimWorld;
using System.Collections.Generic;
using Verse;

namespace FactionColonies.util
{
    public static class CraftUtil
    {
        public static bool CanCraftItem(ThingDef thing, bool includeSingleUse = false)
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

        public static bool ThingHasQuality(ThingDef thing)
        {
            return thing.HasComp<CompQuality>();
        }
        public static bool ThingIsStuffable(ThingDef thing)
        {
            return thing.MadeFromStuff;
        }
        /// <summary>
        /// For the given <paramref name="thing"/>, returns a list of valid stuff ThingDefs.
        /// </summary>
        /// <param name="thing">ThingDef to retrieve a list of stuff for.</param>
        /// <param name="filterList">List of possible things to use for stuff.</param>
        /// <returns>The list of ThingDefs that can be used to stuff the given <paramref name="thing"/>. Returns an empty list if <paramref name="thing"/> is not stuffable.</returns>
        public static List<ThingDef> GetThingStuffs(ThingDef thing, List<ThingDef> filterList)
        {
            List<ThingDef> list = new List<ThingDef>();
            if (ThingIsStuffable(thing) && filterList.Count > 0)
            {
                foreach (ThingDef possible in filterList)
                {
                    if (possible.IsStuff && possible.stuffProps.CanMake(thing))
                    {
                        list.Add(possible);
                    }
                }
            }
            return list;
        }
    }
}
