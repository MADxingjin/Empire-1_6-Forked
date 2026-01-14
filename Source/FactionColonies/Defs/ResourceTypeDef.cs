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
    public class ResourceThingDef : ThingDef
    {
        public TechLevel minTechLevel;
        public TechLevel maxTechLevel;
        public List<ResearchProjectDef> researchProjectDefs = new List<ResearchProjectDef>();
        public bool needsAllResearchRequirements = false;

        public bool hasResearchDefs => (researchProjectDefs.Count > 0);
        public bool hasDefinedMinTechLevel => (minTechLevel != TechLevel.Undefined);
        public bool hasDefinedMaxTechLevel => (maxTechLevel != TechLevel.Undefined);
        public bool hasDefinedTechLevel => hasDefinedMinTechLevel || hasDefinedMaxTechLevel;
        public bool alwaysAvailable => (!hasResearchDefs && !hasDefinedTechLevel);
        public bool IsAvailable(TechLevel techlevel)
        {
            if (alwaysAvailable)
                return true;

            bool satisfiesTechLevel = true;

            if (hasDefinedTechLevel)
            {
                bool satisfiesMinLevel = !hasDefinedMinTechLevel || techlevel >= minTechLevel;
                bool satisfiesMaxLevel = !hasDefinedMaxTechLevel || techlevel <= maxTechLevel;
                satisfiesTechLevel = satisfiesMinLevel && satisfiesMaxLevel;
                LogUtil.Message($"techlevel {techlevel} satisfies tech level requirement for ResourceThingDef {LabelCap}: {satisfiesTechLevel}");
                if (!needsAllResearchRequirements)
                {
                    return satisfiesTechLevel;
                }
            }

            if (hasResearchDefs)
            {
                foreach (ResearchProjectDef projectDef in researchProjectDefs)
                {
                    if (projectDef.IsFinished && !needsAllResearchRequirements)
                    {
                        return true;
                    }
                    else if (!projectDef.IsFinished && needsAllResearchRequirements)
                    {
                        return false;
                    }
                }
            }
            else
            {
                /* If we're here, it means that needsAllResearchRequirements is marked as true, but this ResourceThingDef only defined a min and/or max tech level.
                 * So just return the result of the tech level check. */
                /* We know that there was a defined tech level, because if there wasn't, we would have returned after the alwaysAvailable check. */
                LogUtil.Warning($"ResourceThingDef {LabelCap} has needsAllResearchRequirements={needsAllResearchRequirements}, but didn't specify any ResearchProjectDefs");
                return satisfiesTechLevel;
            }

            return false;
        }
    }
    public class ResourceThingCategoryDef : ThingCategoryDef
    {
        public TechLevel minTechLevel;
        public TechLevel maxTechLevel;
        public List<ResearchProjectDef> researchProjectDefs = new List<ResearchProjectDef>();
        public bool needsAllResearchRequirements = false;

        public bool hasResearchDefs => (researchProjectDefs.Count > 0);
        public bool hasDefinedMinTechLevel => (minTechLevel != TechLevel.Undefined);
        public bool hasDefinedMaxTechLevel => (maxTechLevel != TechLevel.Undefined);
        public bool hasDefinedTechLevel => hasDefinedMinTechLevel || hasDefinedMaxTechLevel;
        public bool alwaysAvailable => (!hasResearchDefs && !hasDefinedTechLevel);
        public bool IsAvailable(TechLevel techlevel)
        {
            if (alwaysAvailable)
                return true;

            bool satisfiesTechLevel = true;

            if (hasDefinedTechLevel)
            {
                bool satisfiesMinLevel = !hasDefinedMinTechLevel || techlevel >= minTechLevel;
                bool satisfiesMaxLevel = !hasDefinedMaxTechLevel || techlevel <= maxTechLevel;
                satisfiesTechLevel = satisfiesMinLevel && satisfiesMaxLevel;
                LogUtil.Message($"techlevel {techlevel} satisfies tech level requirement for ResourceThingCategoryDef {LabelCap}: {satisfiesTechLevel}");
                if (!needsAllResearchRequirements)
                {
                    return satisfiesTechLevel;
                }
            }

            if (hasResearchDefs)
            {
                foreach (ResearchProjectDef projectDef in researchProjectDefs)
                {
                    if (projectDef.IsFinished && !needsAllResearchRequirements)
                    {
                        return true;
                    }
                    else if (!projectDef.IsFinished && needsAllResearchRequirements)
                    {
                        return false;
                    }
                }
            }
            else
            {
                /* If we're here, it means that needsAllResearchRequirements is marked as true, but this ResourceThingCategoryDef only defined a min and/or max tech level.
                 * So just return the result of the tech level check. */
                /* We know that there was a defined tech level, because if there wasn't, we would have returned after the alwaysAvailable check. */
                LogUtil.Warning($"ResourceThingCategoryDef {LabelCap} has needsAllResearchRequirements={needsAllResearchRequirements}, but didn't specify any ResearchProjectDefs");
                return satisfiesTechLevel;
            }

            return false;
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
                    if (thingDef.IsAvailable(techlevel))
                    {
                        filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), true);
                    }
                }
            }
            if (thingCategoryAllowList != null)
            {
                foreach (ResourceThingCategoryDef thingCategoryDef in thingCategoryAllowList)
                {
                    if (thingCategoryDef.IsAvailable(techlevel))
                    {
                        filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail(thingCategoryDef.defName), true);
                    }
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
                    ext.SetFilter(filter);
                }
            }
        }
    }
}
