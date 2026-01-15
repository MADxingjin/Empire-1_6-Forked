using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public abstract class ResourceResearchRestrictionDef : Def
    {
        public TechLevel minTechLevel;
        public TechLevel maxTechLevel;
        public List<ResearchProjectDef> researchProjectDefs = new List<ResearchProjectDef>();
        public bool needsAllResearchRequirements = false;
        public bool restrictByRecipe = true;
        public bool restrictByThingTechLevel = true;

        public bool hasResearchDefs => (researchProjectDefs.Count > 0);
        public bool hasDefinedMinTechLevel => (minTechLevel != TechLevel.Undefined);
        public bool hasDefinedMaxTechLevel => (maxTechLevel != TechLevel.Undefined);
        public bool hasDefinedTechLevel => hasDefinedMinTechLevel || hasDefinedMaxTechLevel;
        public virtual bool alwaysSatisfiesTechRequirements => (!hasResearchDefs && !hasDefinedTechLevel && !restrictByRecipe && !restrictByThingTechLevel);
        public virtual bool SatisfiesTechRequirements(TechLevel techlevel)
        {
            if (alwaysSatisfiesTechRequirements)
                return true;

            if (hasDefinedTechLevel)
            {
                bool satisfiesMinLevel = !hasDefinedMinTechLevel || techlevel >= minTechLevel;
                bool satisfiesMaxLevel = !hasDefinedMaxTechLevel || techlevel <= maxTechLevel;
                bool satisfiesDefTechLevel = satisfiesMinLevel && satisfiesMaxLevel;
                LogUtil.Message($"techlevel {techlevel} satisfies def tech level requirement for ResourceResearchRestrictionDef {LabelCap}: {satisfiesDefTechLevel}");
                if ((!needsAllResearchRequirements && satisfiesDefTechLevel) ||
                    (needsAllResearchRequirements && !satisfiesDefTechLevel))
                {
                    return satisfiesDefTechLevel;
                }
            }

            if (hasResearchDefs)
            {
                bool satisfiesResearchDefs = needsAllResearchRequirements;
                foreach (ResearchProjectDef projectDef in researchProjectDefs)
                {
                    if (projectDef.IsFinished && !needsAllResearchRequirements)
                    {
                        satisfiesResearchDefs = true;
                        break;
                    }
                    else if (!projectDef.IsFinished && needsAllResearchRequirements)
                    {
                        satisfiesResearchDefs = false;
                        break;
                    }
                }
                LogUtil.Message($"Completed research satisfies the def research requirements for ResourceResearchRestrictionDef {LabelCap}: {satisfiesResearchDefs}");
                if ((!needsAllResearchRequirements && satisfiesResearchDefs) ||
                    (needsAllResearchRequirements && !satisfiesResearchDefs))
                {
                    return satisfiesResearchDefs;
                }
            }

            /* If we get here and we need to meet all requirements, then that means we've passed all checks, and want to return TRUE.
             * But if we get here and we do NOT need to meet all requirements, then that means that we failed all checks, and want to return FALSE.
             * So, we can just return needsAllResearchRequirements itself, since its value is exactly what we want to return right now. */
            return needsAllResearchRequirements;
        }
        public virtual void SetFilter(ThingFilter filter, TechLevel techlevel)
        {
        }
        public static bool ThingAllowedByRecipe(ThingDef thing)
        {
            if (thing.recipeMaker != null)
            {
                if (thing.recipeMaker.researchPrerequisites != null)
                {
                    foreach (ResearchProjectDef research in thing.recipeMaker.researchPrerequisites)
                    {
                        if (!research.IsFinished)
                        {
                            //research is not good
                            return false;
                        }
                    }
                }

                if (thing.recipeMaker.researchPrerequisite != null)
                {
                    if (!thing.recipeMaker.researchPrerequisite.IsFinished)
                    {
                        //research is not good
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool ThingAllowedByThingTechLevel(ThingDef thing, TechLevel techlevel)
        {
            if (techlevel < thing.techLevel)
            {
                return false;
            }
            return true;
        }
    }
    public class ResourceThingDef : ResourceResearchRestrictionDef
    {
        public ThingDef thingDef;

        public override bool SatisfiesTechRequirements(TechLevel techlevel)
        {
            if (alwaysSatisfiesTechRequirements)
                return true;

            bool satisfiesTechLevel = base.SatisfiesTechRequirements(techlevel);

            if ((!needsAllResearchRequirements && satisfiesTechLevel) ||
                (needsAllResearchRequirements && !satisfiesTechLevel))
            {
                return satisfiesTechLevel;
            }

            if (restrictByRecipe)
            {
                bool allowedByRecipe = ThingAllowedByRecipe(thingDef);
                if ((!needsAllResearchRequirements && allowedByRecipe) ||
                    (needsAllResearchRequirements && !allowedByRecipe))
                {
                    return allowedByRecipe;
                }
            }

            if (restrictByThingTechLevel)
            {
                bool allowedByTechLevel = ThingAllowedByThingTechLevel(thingDef, techlevel);
                if ((!needsAllResearchRequirements && allowedByTechLevel) ||
                    (needsAllResearchRequirements && !allowedByTechLevel))
                {
                    return allowedByTechLevel;
                }
            }

            /* If we get here and we need to meet all requirements, then that means we've passed all checks, and want to return TRUE.
             * But if we get here and we do NOT need to meet all requirements, then that means that we failed all checks, and want to return FALSE.
             * So, we can just return needsAllResearchRequirements itself, since its value is exactly what we want to return right now. */
            return needsAllResearchRequirements;
        }
        public override void SetFilter(ThingFilter filter, TechLevel techlevel)
        {
            if (SatisfiesTechRequirements(techlevel))
            {
                filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), true);
            }
        }
    }
    public class ResourceThingCategoryDef : ResourceResearchRestrictionDef
    {
        public ThingCategoryDef thingCategoryDef;

        /* We don't override SatisfiesTechRequirements here because the base class's checks are enough for the ThingCategoryDef.
         * All further checks are on the things listed within the category. That logic has to be handled in SetFiler. */
        public override void SetFilter(ThingFilter filter, TechLevel techlevel)
        {
            if (alwaysSatisfiesTechRequirements)
            {
                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail(thingCategoryDef.defName), true);
                return;
            }

            bool satisfiesTechRequirements = SatisfiesTechRequirements(techlevel);
            if (satisfiesTechRequirements)
            {
                filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail(thingCategoryDef.defName), true);
                if (!needsAllResearchRequirements)
                {  
                    return;
                }
            }
            else if (needsAllResearchRequirements)
            {
                return;
            }

            if (restrictByRecipe)
            {
                foreach (ThingDef thingDef in thingCategoryDef.childThingDefs)
                {
                    bool allowed = ThingAllowedByRecipe(thingDef);
                    if (needsAllResearchRequirements && !allowed)
                    {
                        filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), false);
                    }
                    else if (!needsAllResearchRequirements && allowed)
                    {
                        filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), true);
                    }
                }
            }
            if (restrictByThingTechLevel)
            {
                foreach (ThingDef thingDef in thingCategoryDef.childThingDefs)
                {
                    bool allowed = ThingAllowedByThingTechLevel(thingDef, techlevel);
                    if (needsAllResearchRequirements && !allowed)
                    {
                        filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), false);
                    }
                    else if (!needsAllResearchRequirements && allowed)
                    {
                        filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), true);
                    }
                }
            }
        }
    }
    public class ResourceTypeDef : Def
    {
        public string iconPath;

        List<ResourceThingDef> thingAllowList = new List<ResourceThingDef>();
        List<ThingDef> thingBlockList = new List<ThingDef>();

        List<ResourceThingCategoryDef> thingCategoryAllowList = new List<ResourceThingCategoryDef>();
        List<ThingCategoryDef> thingCategoryBlockList = new List<ThingCategoryDef>();

        public Texture2D iconLoaded;

        public Texture2D Icon
        {
            get
            {
                if (iconLoaded != null) return iconLoaded;

                if (!iconPath.NullOrEmpty())
                {
                    iconLoaded = ContentFinder<Texture2D>.Get(iconPath);
                }
                else
                {
                    LogUtil.Error("Failed to load icon for ResourceType: " + LabelCap + " at " + (iconPath ?? "nullPath") + "!");
                    iconLoaded = TexLoad.questionmark;
                }
                return iconLoaded;
            }
        }
        public void FilterResource(ThingFilter filter, TechLevel techlevel = TechLevel.Undefined)
        {
            /* Allow lists */
            if (thingAllowList != null)
            {
                foreach (ResourceThingDef thingDef in thingAllowList)
                {
                    thingDef.SetFilter(filter, techlevel);
                }
            }
            if (thingCategoryAllowList != null)
            {
                foreach (ResourceThingCategoryDef thingCategoryDef in thingCategoryAllowList)
                {
                    thingCategoryDef.SetFilter(filter, techlevel);
                }
            }
            /* Block lists */
            if (thingBlockList != null)
            {
                foreach (ThingDef thingDef in thingBlockList)
                {
                    filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), false);
                }
            }
            if (thingCategoryBlockList != null)
            {
                foreach (ThingCategoryDef thingCategoryDef in thingCategoryBlockList)
                {
                    filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail(thingCategoryDef.defName), false);
                }
            }

            /* Check for any ResourceExtensions, and process them now. */
            if (modExtensions != null)
            {
                foreach (ResourceExtension ext in modExtensions)
                {
                    ext.SetFilter(filter, techlevel);
                }
            }
        }
    }
}
