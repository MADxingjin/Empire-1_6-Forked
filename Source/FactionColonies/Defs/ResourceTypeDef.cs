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
    public abstract class ResourceResearchRestriction
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
        public virtual bool noRequirements => (!hasResearchDefs && !hasDefinedTechLevel && !restrictByRecipe && !restrictByThingTechLevel);
        public virtual bool SatisfiesTechRequirements(TechLevel techlevel)
        {
            if (noRequirements)
                return true;

            if (hasDefinedTechLevel)
            {
                bool satisfiesMinLevel = !hasDefinedMinTechLevel || techlevel >= minTechLevel;
                bool satisfiesMaxLevel = !hasDefinedMaxTechLevel || techlevel <= maxTechLevel;
                bool satisfiesDefTechLevel = satisfiesMinLevel && satisfiesMaxLevel;
                LogUtil.Message($"techlevel {techlevel} satisfies def tech level requirement for ResourceResearchRestriction: {satisfiesDefTechLevel}");
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
                LogUtil.Message($"Completed research satisfies the def research requirements for ResourceResearchRestriction: {satisfiesResearchDefs}");
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
        // Used to be part of CraftUtil.canCraftItem. Now, we do this check as part of the overall filtering process
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
        // Used to be part of CraftUtil.canCraftItem. Now, we do this check as part of the overall filtering process
        public static bool ThingAllowedByThingTechLevel(ThingDef thing, TechLevel techlevel)
        {
            if (techlevel < thing.techLevel)
            {
                return false;
            }
            return true;
        }
    }
    public class ResourceThingDefRestriction : ResourceResearchRestriction
    {
        public ThingDef thingDef;

        public override bool SatisfiesTechRequirements(TechLevel techlevel)
        {
            if (noRequirements)
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
            else
            {
                /* Thing specifications override Category specifications. So if a category already allowed the thing,
                 * but the Thing's own requirements aren't satisfied, then we need to disallow it. */
                filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail(thingDef.defName), false);
            }
        }
    }
    public class ResourceThingCategoryDefRestriction : ResourceResearchRestriction
    {
        public ThingCategoryDef thingCategoryDef;

        /* We don't override SatisfiesTechRequirements here because the base class's checks are enough for the ThingCategoryDef.
         * All further checks are on the things listed within the category. That logic has to be handled in SetFiler. */
        public override void SetFilter(ThingFilter filter, TechLevel techlevel)
        {
            if (noRequirements)
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

        /* For all Allow-Blocklist pairs, the allowlist is processed first, and then the blocklist is used to shave off blocked elements */
        /* NOTE: The thingAllowList is treated as the ultimate source of truth for the Things it specifies. That is:
         *         - If a Thing's category isn't allowed, but the Thing satisfies all of its own requirements, then that Thing is allowed
         *         - If a Thing's category is allowed, but the Thing does *not* satisfy all of its own requirements, then that Thing is disallowed
         */
        List<ResourceThingDefRestriction> thingAllowList = new List<ResourceThingDefRestriction>();
        List<ThingDef> thingBlockList = new List<ThingDef>();

        List<ResourceThingCategoryDefRestriction> thingCategoryAllowList = new List<ResourceThingCategoryDefRestriction>();
        List<ThingCategoryDef> thingCategoryBlockList = new List<ThingCategoryDef>();

        //TODO: make sure this part actually works. Game code gets stuff categories through stuff like StuffCategoryDefOf.xxx. I'm not sure
        //      if you can just slap 'xxx' into an xml node called "StuffCategoryDef" and call it a day. Might need to do some stuff with defNames.
        List<StuffCategoryDef> stuffCategoryAllowList = new List<StuffCategoryDef>();
        List<StuffCategoryDef> stuffCategoryBlockList = new List<StuffCategoryDef>();

        /// <summary>
        /// Minimum tech level to access or produce this resource type.
        /// <para>If minTechLevel is not undefined, and the empire faction does not satisfy it, then the resource cannot be produced.</para>
        /// </summary>
        public TechLevel minTechLevel;
        /// <summary>
        /// Maximum tech level to access or produce this resource type.
        /// <para>If maxTechLevel is not undefined, and the empire faction does not satisfy it, then the resource cannot be produced.</para>
        /// </summary>
        public TechLevel maxTechLevel;
        /// <summary>
        /// A list of research projects required to unlock access to this resource type.
        /// </summary>
        public List<ResearchProjectDef> researchProjectDefs = new List<ResearchProjectDef>();
        /// <summary>
        /// Indicates whether the resource type needs to meet both the techlevel AND researchProjectDefs requirements to become available.
        /// </summary>
        public bool needsAllResearchRequirements = false;
        /// <summary>
        /// Indicates whether or not this resource should accumulate a special point pool instead of actual objects.
        /// E.g. a research point pool, or a power pool.
        /// </summary>
        public bool isPoolResource = false;

        /// <summary>
        /// When generating tithes for this resource, the count range is set to (titheMinCount, titheMaxCountBase + (titheMaxCountScaler * multiplier)) where "multiplier" is set within
        /// the code.
        /// </summary>
        public int titheMinCount = 1;
        /// <summary>
        /// When generating tithes for this resource, the count range is set to (titheMinCount, titheMaxCountBase + (titheMaxCountScaler * multiplier)) where "multiplier" is set within
        /// the code.
        /// </summary>
        public int titheMaxCountBase = 1;
        /// <summary>
        /// When generating tithes for this resource, the count range is set to (titheMinCount, titheMaxCountBase + (titheMaxCountScaler * multiplier)) where "multiplier" is set within
        /// the code.
        /// </summary>
        public int titheMaxCountScaler = 1;
        /// <summary>
        /// Determines if the production value of this resource should be considered when generating defense for a settlement.
        /// </summary>
        public bool aidsDefense = false;

        private Texture2D iconLoaded;

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

        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * Pool helper functions
         * These functions are wrappers for ResourcePoolExtension functions, meant to make it easier to invoke the extension.
         * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
        public bool poolResourceResetsAtTaxTime()
        {
            if (!isPoolResource)
            {
                LogUtil.Error($"Called poolResourceResetsAtTaxTime() for non-pool resource {this.defName}");
                return false;
            }
            /* ConfigErrors already checked that ResourcePoolExtensions exists if isPoolResource is set, so
             * we won't bother with null-checking here. */
            return GetModExtension<ResourcePoolExtension>().resetAtTaxTime();
        }
        public void addedToGlobalPool(double value)
        {
            if (!isPoolResource)
            {
                LogUtil.Error($"Called addedToGlobalPool() for non-pool resource {this.defName}");
                return;
            }
            /* ConfigErrors already checked that ResourcePoolExtensions exists if isPoolResource is set, so
             * we won't bother with null-checking here. */
            GetModExtension<ResourcePoolExtension>().addedToGlobalPool(value);
        }
        public IEnumerable<FloatMenuOption> GetFactionMenuFloatMenuOptions(ResourcePool pool)
        {
            if (!isPoolResource)
            {
                LogUtil.Error($"Called GetFactionMenuFloatMenuOptions() for non-pool resource {this.defName}");
                yield break;
            }
            /* ConfigErrors already checked that ResourcePoolExtensions exists if isPoolResource is set, so
             * we won't bother with null-checking here. */
            IEnumerable<FloatMenuOption> options = GetModExtension<ResourcePoolExtension>().GetFactionMenuFloatMenuOptions(pool);
            if (options != null)
            {
                foreach (FloatMenuOption option in options)
                {
                    yield return option;
                }
            }
        }
        public void dailyUpdate(ResourcePool pool)
        {
            if (!isPoolResource)
            {
                LogUtil.Error($"Called dailyUpdate() for non-pool resource {this.defName}");
                return;
            }
            /* ConfigErrors already checked that ResourcePoolExtensions exists if isPoolResource is set, so
             * we won't bother with null-checking here. */
            GetModExtension<ResourcePoolExtension>().dailyUpdate(pool);
        }
        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * End of Pool helper functions
         * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

        public bool ResourceTypeAllowedByTech(TechLevel techlevel)
        {
            bool meetsTechlevelReq = false;
            if ((minTechLevel == TechLevel.Undefined || techlevel >= minTechLevel) &&
                (maxTechLevel == TechLevel.Undefined || techlevel <= maxTechLevel))
            {
                meetsTechlevelReq = true;
            }
            bool meetsResearchReqs = true;
            if (researchProjectDefs.Count > 0)
            {
                foreach (ResearchProjectDef research in researchProjectDefs)
                {
                    if (!research.IsFinished)
                    {
                        meetsResearchReqs = false;
                        break;
                    }
                }
            }

            if (needsAllResearchRequirements)
            {
                return meetsResearchReqs && meetsTechlevelReq;
            }
            else
            {
                return meetsResearchReqs || meetsTechlevelReq;
            }
        }
        public double getExtensionAdditives(PlanetTile tile)
        {
            double add = 0;
            if (modExtensions?.Count > 0)
            {
                foreach(ResourceProductionExtension prod in modExtensions)
                {
                    add += prod.GetAdditiveBonus(tile);
                }
            }
            return add;
        }
        public double getExtensionMultipliers(PlanetTile tile)
        {
            double mult = 1;
            if (modExtensions?.Count > 0)
            {
                foreach (ResourceProductionExtension prod in modExtensions)
                {
                    mult *= prod.GetMultiplierBonus(tile);
                }
            }
            return mult;
        }
        public void FilterResource(ThingFilter filter, TechLevel techlevel = TechLevel.Undefined)
        {
            /* Allow lists */
            if (thingCategoryAllowList != null)
            {
                foreach (ResourceThingCategoryDefRestriction thingCategoryRestriction in thingCategoryAllowList)
                {
                    thingCategoryRestriction.SetFilter(filter, techlevel);
                }
            }
            if (stuffCategoryAllowList != null)
            {
                foreach (StuffCategoryDef stuffCategoryDef in stuffCategoryAllowList)
                {
                    filter.SetAllow(stuffCategoryDef, true);
                }
            }
            // Handle Things after the categories. This allows for more fine-grained control over when
            // individual things become available
            if (thingAllowList != null)
            {
                foreach (ResourceThingDefRestriction thingDefRestriction in thingAllowList)
                {
                    thingDefRestriction.SetFilter(filter, techlevel);
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
            if (stuffCategoryBlockList != null)
            {
                foreach (StuffCategoryDef stuffCategoryDef in stuffCategoryBlockList)
                {
                    filter.SetAllow(stuffCategoryDef, false);
                }
            }

            /* Check for any ResourceExtensions, and process them now. */
            if (modExtensions != null)
            {
                foreach (ResourceFilterExtension ext in modExtensions)
                {
                    ext.SetFilter(filter, techlevel);
                }
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string item in base.ConfigErrors())
            {
                yield return item;
            }
            if (thingAllowList == null && thingCategoryAllowList == null && stuffCategoryAllowList == null && modExtensions == null)
            {
                yield return "ResourceTypeDef " + this.defName + " does not specify any allowed resources or modExtensions";
            }
            if (thingAllowList != null)
            {
                foreach (ResourceThingDefRestriction thingDefRestriction in thingAllowList)
                {
                    if (thingDefRestriction.maxTechLevel != TechLevel.Undefined && thingDefRestriction.maxTechLevel < thingDefRestriction.minTechLevel)
                    {
                        yield return "maxTechLevel " + thingDefRestriction.maxTechLevel + " is earlier than minTechLevel " + thingDefRestriction.minTechLevel;
                    }
                    if (thingDefRestriction.noRequirements && thingDefRestriction.needsAllResearchRequirements)
                    {
                        yield return "needsAllResearchRequirements is TRUE, but there are no specified research or tech level requirements";
                    }
                    if (thingBlockList != null && thingBlockList.Contains(thingDefRestriction.thingDef))
                    {
                        yield return "thingDefRestriction " + thingDefRestriction.thingDef.defName + " appears in both thingAllowList and thingBlockList for ResourceTypeDef " + this.defName;
                    }
                }
            }
            if (thingCategoryAllowList != null)
            {
                foreach (ResourceThingCategoryDefRestriction thingCategoryDefRestriction in thingCategoryAllowList)
                {
                    if (thingCategoryDefRestriction.maxTechLevel != TechLevel.Undefined && thingCategoryDefRestriction.maxTechLevel < thingCategoryDefRestriction.minTechLevel)
                    {
                        yield return "maxTechLevel " + thingCategoryDefRestriction.maxTechLevel + " is earlier than minTechLevel " + thingCategoryDefRestriction.minTechLevel;
                    }
                    if (thingCategoryDefRestriction.noRequirements && thingCategoryDefRestriction.needsAllResearchRequirements)
                    {
                        yield return "needsAllResearchRequirements is TRUE, but there are no specified research or tech level requirements";
                    }
                    if (thingCategoryBlockList != null)
                    {
                        if (thingCategoryBlockList.Contains(thingCategoryDefRestriction.thingCategoryDef))
                        {
                            yield return "thingCategoryDefRestriction " + thingCategoryDefRestriction.thingCategoryDef.defName + " appears in both thingCategoryAllowList and thingCategoryBlockList for ResourceTypeDef " + this.defName;
                        }
                    }
                }
            }
            if (stuffCategoryAllowList != null && stuffCategoryBlockList != null)    
            {
                foreach (StuffCategoryDef stuffCategoryDefAllow in stuffCategoryAllowList)
                {
                    if (stuffCategoryBlockList.Contains(stuffCategoryDefAllow))
                    {
                        yield return "stuffCategoryDef " + stuffCategoryDefAllow.defName + " appears in both the stuffCategoryAllowList and stuffCategoryBlockList for ResourceTypeDef " + this.defName;
                    }
                }
            }
            if (modExtensions != null)
            {
                bool foundPoolExtension = false;
                foreach (ResourcePoolExtension ext in modExtensions)
                {
                    foundPoolExtension = true;
                    foreach (ResourcePoolExtension ext2 in modExtensions)
                    {
                        if (ext == ext2)
                        {
                            yield return "ResourcePoolExtension " + ext.ToStringSafe() + "appears more than once in defModExtensions for ResourceTypeDef " + this.defName;
                        }
                    }
                }
                foreach (ResourceProductionExtension ext in modExtensions)
                {
                    foreach (ResourceProductionExtension ext2 in modExtensions)
                    {
                        if (ext == ext2)
                        {
                            yield return "ResourceProductionExtension " + ext.ToStringSafe() + "appears more than once in defModExtensions for ResourceTypeDef " + this.defName;
                        }
                    }
                }
                foreach (ResourceFilterExtension ext in modExtensions)
                {
                    foreach (ResourceFilterExtension ext2 in modExtensions)
                    {
                        if (ext == ext2)
                        {
                            yield return "ResourceFilterExtension " + ext.ToStringSafe() + "appears more than once in defModExtensions for ResourceTypeDef " + this.defName;
                        }
                    }
                }
                if (isPoolResource && !foundPoolExtension)
                {
                    yield return "isPoolResource is TRUE but there is no ResourcePoolExtension in defModExtensions for ResourceTypeDef " + this.defName;
                }
                if (!isPoolResource && foundPoolExtension)
                {
                    yield return "isPoolResource is FALSE but there is a ResourcePoolExtension in defModExtensions for ResourceTypeDef " + this.defName;
                }
            }
            else
            {
                if (isPoolResource)
                {
                    yield return "isPoolResource is TRUE but there is no ResourcePoolExtension in defModExtensions for ResourceTypeDef " + this.defName;
                }
            }
        }
    }
    /// <summary>
    /// Class for use in other defs.
    /// </summary>
    public class ResourceBonuses
    {
        public ResourceTypeDef resourceDef;
        public double additive = 0;
        public double multiplier = 1;
    }
    //TODO: actually define these
    public class ResourceTypeDefOf
    {
        public static ResourceTypeDef RTD_Food;
        public static ResourceTypeDef RTD_Animals;
        public static ResourceTypeDef RTD_Apparel;
        public static ResourceTypeDef RTD_Weapons;
        public static ResourceTypeDef RTD_Logging;
        public static ResourceTypeDef RTD_Power;
        public static ResourceTypeDef RTD_Research;
        public static ResourceTypeDef RTD_Medicine;
    }
}
