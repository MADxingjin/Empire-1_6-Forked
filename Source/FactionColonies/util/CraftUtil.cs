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
                if (Find.World.GetComponent<FactionFC>().techLevel < thing.techLevel)
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
    }
}
