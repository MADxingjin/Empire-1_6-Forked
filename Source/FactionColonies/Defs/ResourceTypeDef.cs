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
        public TechLevel minTechLevel = TechLevel.Undefined;
        public TechLevel maxTechLevel = TechLevel.Undefined;
        public List<ResearchProjectDef> researchProjectDefs = new List<ResearchProjectDef>();
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
                if (!satisfiesDefTechLevel)
                {
                    return false;
                }
            }

            if (hasResearchDefs)
            {
                foreach (ResearchProjectDef projectDef in researchProjectDefs)
                {
                    if (!projectDef.IsFinished)
                    {
                        return false;
                    }
                }
            }

            return true;
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

            if (!base.SatisfiesTechRequirements(techlevel))
            {
                return false;
            }

            if (restrictByRecipe)
            {
                if (!ThingAllowedByRecipe(thingDef))
                {
                    return false;
                }
            }

            if (restrictByThingTechLevel)
            {
                if (!ThingAllowedByThingTechLevel(thingDef, techlevel))
                {
                    return false;
                }
            }

            return true;
        }
        public override void SetFilter(ThingFilter filter, TechLevel techlevel)
        {
            if (SatisfiesTechRequirements(techlevel))
            {
                filter.SetAllow(thingDef, true);
            }
            else
            {
                /* Thing specifications override Category specifications. So if a category already allowed the thing,
                 * but the Thing's own requirements aren't satisfied, then we need to disallow it. */
                filter.SetAllow(thingDef, false);
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
                filter.SetAllow(thingCategoryDef, true);
                return;
            }

            if (SatisfiesTechRequirements(techlevel))
            {
                filter.SetAllow(thingCategoryDef, true);
            }
            else
            {
                filter.SetAllow(thingCategoryDef, false);
                return;
            }

            foreach (ThingDef thingDef in thingCategoryDef.childThingDefs)
            {
                bool allowed = true;
                if (restrictByRecipe)
                {
                    allowed &= ThingAllowedByRecipe(thingDef);
                }
                if (restrictByThingTechLevel)
                {
                    allowed &= ThingAllowedByThingTechLevel(thingDef, techlevel);
                }
                filter.SetAllow(thingDef, allowed);
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

        /* biomeAllowList and biomeBlockList are mutually exclusive */
        List<BiomeResourceDef> biomeAllowList = new List<BiomeResourceDef>();
        List<BiomeResourceDef> biomeBlockList = new List<BiomeResourceDef>();

        /// <summary>
        /// Minimum tech level to access or produce this resource type.
        /// <para>If minTechLevel is not undefined, and the empire faction does not satisfy it, then the resource cannot be produced.</para>
        /// </summary>
        public TechLevel minTechLevel = TechLevel.Undefined;
        /// <summary>
        /// Maximum tech level to access or produce this resource type.
        /// <para>If maxTechLevel is not undefined, and the empire faction does not satisfy it, then the resource cannot be produced.</para>
        /// </summary>
        public TechLevel maxTechLevel = TechLevel.Undefined;
        /// <summary>
        /// A list of research projects required to unlock access to this resource type.
        /// </summary>
        public List<ResearchProjectDef> researchProjectDefs = new List<ResearchProjectDef>();
        /// <summary>
        /// Indicates whether the resource type needs to meet both the techlevel AND researchProjectDefs requirements to become available.
        /// </summary>
        public bool needsAllResearchRequirements = true;
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

        /// <summary>
        /// Determines the order that the resource is listed in the UI. Lower values = ealier in the list. Ties are broken randomly.
        /// </summary>
        public int uiPriority = 10000;

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
        public double preAddToGlobalPool(double value)
        {
            if (!isPoolResource)
            {
                LogUtil.Error($"Called preAddToGlobalPool() for non-pool resource {this.defName}");
                return value;
            }
            /* ConfigErrors already checked that ResourcePoolExtensions exists if isPoolResource is set, so
             * we won't bother with null-checking here. */
            return GetModExtension<ResourcePoolExtension>().preAddToGlobalPool(value);
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
                foreach(ResourceProductionExtension prod in modExtensions.OfType<ResourceProductionExtension>())
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
                foreach (ResourceProductionExtension prod in modExtensions.OfType<ResourceProductionExtension>())
                {
                    mult *= prod.GetMultiplierBonus(tile);
                }
            }
            return mult;
        }
        public bool resourceAllowedForBiome(BiomeResourceDef bdef)
        {
            if (biomeAllowList.Count > 0)
            {
                /* If an allowList is specified, then the resource is *only* available in those biomes. */
                return biomeAllowList.Contains(bdef);
            }
            else if (biomeBlockList.Count > 0)
            {
                return !(biomeBlockList.Contains(bdef));
            }
            /* A resource is allowed in all Biomes by default */
            return true;
        }
        public void FilterResource(ThingFilter filter, TechLevel techlevel = TechLevel.Undefined)
        {
            /* Category Allow lists */
            foreach (ResourceThingCategoryDefRestriction thingCategoryRestriction in thingCategoryAllowList)
            {
                thingCategoryRestriction.SetFilter(filter, techlevel);
            }
            foreach (StuffCategoryDef stuffCategoryDef in stuffCategoryAllowList)
            {
                filter.SetAllow(stuffCategoryDef, true);
            }
            /* Category Block Lists */
            foreach (ThingCategoryDef thingCategoryDef in thingCategoryBlockList)
            {
                filter.SetAllow(thingCategoryDef, false);
            }
            foreach (StuffCategoryDef stuffCategoryDef in stuffCategoryBlockList)
            {
                filter.SetAllow(stuffCategoryDef, false);
            }
            // Handle Things after the categories. This allows for more fine-grained control over when
            // individual things become available
            foreach (ResourceThingDefRestriction thingDefRestriction in thingAllowList)
            {
                thingDefRestriction.SetFilter(filter, techlevel);
            }
            /* Block lists */
            foreach (ThingDef thingDef in thingBlockList)
            {
                filter.SetAllow(thingDef, false);
            }

            /* Check for any ResourceExtensions, and process them now. */
            if (modExtensions != null)
            {
                foreach (ResourceFilterExtension ext in modExtensions.OfType<ResourceFilterExtension>())
                {
                    ext.SetFilter(filter, techlevel);
                }
            }
        }

        public int compareForUI(ResourceTypeDef compareDef)
        {
            return this.uiPriority - compareDef.uiPriority;
        }
        public static int sortForUI(ResourceTypeDef a, ResourceTypeDef b)
        {
            if (a == null)
            {
                return 1;
            }
            if (b == null)
            {
                return -1;
            }
            return a.compareForUI(b);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string item in base.ConfigErrors())
            {
                yield return item;
            }
            if (thingAllowList.Count == 0  && thingCategoryAllowList.Count == 0 && stuffCategoryAllowList.Count == 0 && (modExtensions?.Count ?? 0) == 0)
            {
                yield return "ResourceTypeDef " + this.defName + " does not specify any allowed resources";
            }
            if (isPoolResource && (thingAllowList.Count > 0 || thingCategoryAllowList.Count > 0 || stuffCategoryAllowList.Count > 0))
            {
                yield return "ResourceTypeDef " + this.defName + " is set as a pool resource, but it also specifies allowlists for things, thingCategories, or stuffCategories";
            }
            if (thingAllowList.Count > 0)
            {
                foreach (ResourceThingDefRestriction thingDefRestriction in thingAllowList)
                {
                    if (thingDefRestriction.maxTechLevel != TechLevel.Undefined && thingDefRestriction.maxTechLevel < thingDefRestriction.minTechLevel)
                    {
                        yield return "maxTechLevel " + thingDefRestriction.maxTechLevel + " is earlier than minTechLevel " + thingDefRestriction.minTechLevel;
                    }
                    if (thingBlockList != null && thingBlockList.Contains(thingDefRestriction.thingDef))
                    {
                        yield return "thingDefRestriction " + thingDefRestriction.thingDef.defName + " appears in both thingAllowList and thingBlockList for ResourceTypeDef " + this.defName;
                    }
                }
            }
            if (thingCategoryAllowList.Count > 0)
            {
                foreach (ResourceThingCategoryDefRestriction thingCategoryDefRestriction in thingCategoryAllowList)
                {
                    if (thingCategoryDefRestriction.maxTechLevel != TechLevel.Undefined && thingCategoryDefRestriction.maxTechLevel < thingCategoryDefRestriction.minTechLevel)
                    {
                        yield return "maxTechLevel " + thingCategoryDefRestriction.maxTechLevel + " is earlier than minTechLevel " + thingCategoryDefRestriction.minTechLevel;
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
            if (stuffCategoryAllowList.Count > 0 && stuffCategoryBlockList.Count > 0)    
            {
                foreach (StuffCategoryDef stuffCategoryDefAllow in stuffCategoryAllowList)
                {
                    if (stuffCategoryBlockList.Contains(stuffCategoryDefAllow))
                    {
                        yield return "stuffCategoryDef " + stuffCategoryDefAllow.defName + " appears in both the stuffCategoryAllowList and stuffCategoryBlockList for ResourceTypeDef " + this.defName;
                    }
                }
            }
            if (biomeAllowList.Count > 0 && biomeBlockList.Count > 0)
            {
                yield return "biomeAllowList and biomeBlockList are both specified for ResourceTypeDef " + this.defName + ". Only one should be specified";
            }
            if (modExtensions?.Count > 0)
            {
                bool foundPoolExtension = false;
                foreach (ResourcePoolExtension ext in modExtensions.OfType<ResourcePoolExtension>())
                {
                    foundPoolExtension = true;
                    foreach (ResourcePoolExtension ext2 in modExtensions.OfType<ResourcePoolExtension>())
                    {
                        if (ext != ext2)
                        {
                            yield return "ResourcePoolExtension " + ext.ToStringSafe() + "appears more than once in defModExtensions for ResourceTypeDef " + this.defName;
                        }
                    }
                }
                foreach (ResourceFilterExtension ext in modExtensions.OfType<ResourceFilterExtension>())
                {
                    foreach (ResourceFilterExtension ext2 in modExtensions.OfType<ResourceFilterExtension>())
                    {
                        if (ext != ext2)
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

        /// <summary>
        /// Creates a string description of this resource bonus, of the following form:
        /// (+/-)[additive] base [resourceDef label]
        /// x[multiplier] [resourceDef label]
        /// </summary>
        /// <returns>A TaggedString with colorized bonus values.</returns>
        public TaggedString getBonusDesc(string tab = "")
        {
            TaggedString desc = "";
            if (additive != 0)
            {
                desc += tab + "RTDproductionAdditive".Translate(TextUtil.colorizeAdditiveBonus(additive), resourceDef.LabelCap);
                if (multiplier != 1)
                {
                    desc += "\n";
                }
            }

            if (multiplier != 1)
            {
                desc += tab + "RTDproductionMultiplier".Translate(TextUtil.colorizeMultiplierBonus(multiplier), resourceDef.LabelCap);
            }

            return desc;
        }
    }

    [DefOf]
    public class ResourceTypeDefOf
    {
        public static ResourceTypeDef RTD_Food;
        public static ResourceTypeDef RTD_Weapons;
        public static ResourceTypeDef RTD_Apparel;
        public static ResourceTypeDef RTD_Animals;
        public static ResourceTypeDef RTD_Logging;
        public static ResourceTypeDef RTD_Mining;
        public static ResourceTypeDef RTD_Research;
        public static ResourceTypeDef RTD_Power;
        public static ResourceTypeDef RTD_Medicine;
        public static ResourceTypeDef RTD_Chemfuel;
        public static ResourceTypeDef RTD_Gravtech;

        static ResourceTypeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ResourceTypeDefOf));
        }
    }
}
