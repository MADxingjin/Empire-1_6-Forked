using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Permissions;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class ResourceFC : IExposable
    {
        public ResourceTypeDef def;
        public string name;
        public string label;
        //public double baseProduction; //base production for resource
        //public double endProduction;  //production after modifiers
        //public double baseProductionMultiplier = 1;  //base production modifier for resource
        //public double endProductionMultiplier = 1;  //end production modifier for resource
        //public List<ProductionAdditive> baseProductionAdditives = new List<ProductionAdditive>();    // {ID, Value, Desc}
        //public List<ProductionMultiplier> baseProductionMultipliers = new List<ProductionMultiplier>();  // {ID, Value, Desc}
        //For now, 'amount' should only be used by the 'global' resources stored in the FactionFC worldcomponent, to display total faction output in the menus.
        // I'm sure there's a better solution for that, but let's refactor only three things at a time, please.
        public double amount;
        private int savedAssignedWorkers;
        public int assignedWorkers
        {
            get
            {
                return savedAssignedWorkers;
            }
            set
            {
                savedAssignedWorkers = value;
                setDirtyCache();
            }
        }
        public bool isTithe;
        public bool isTitheBool; //used to track if isTithe is changed. AGHHH

        /* All bonuses and maluses, even from biome or hilliness, should be applied through productionAdditives and productionMultipliers */
        private Dictionary<string, ProductionBonus> productionAdditives = new Dictionary<string, ProductionBonus>();
        private Dictionary<string, ProductionBonus> productionMultipliers = new Dictionary<string, ProductionBonus>();

        //A structure for the future, to hold per-item tithe specifications
        // should work on replicating current functionality before *adding* to it, though
        // TODO
        //public Dictionary<Thing, int> tithes = new Dictionary<Thing, int>();

        private bool dirtyProductionBaseCache = true;
        private bool dirtyProductionMultCache = true;
        private double cachedProductionBase = 1;
        private double cachedProductionMult = 1;
        private Texture2D iconLoaded;

        public ThingFilter filter = new ThingFilter();
        public double taxStock = 0;
        public double taxMinimumToTithe = 99999;
        public double taxPercentage = 0;
        public WorldSettlementFC settlement;

        public double production => productionBase * productionMult;
        public double productionBase
        {
            get
            {
                if (dirtyProductionBaseCache)
                {
                    cachedProductionBase = calculateProductionBase();
                    dirtyProductionBaseCache = false;
                }
                return cachedProductionBase;
            }
        }
        public double productionMult
        {
            get
            {
                if (dirtyProductionMultCache)
                {
                    cachedProductionMult = calculateProductonMult();
                    dirtyProductionMultCache = false;
                }
                return cachedProductionMult;
            }
        }

        public double totalProduction => production * assignedWorkers;
        public double totalProductionRounded => Math.Round(totalProduction);

        public Texture2D getIcon
        {
            get
            {
                if (iconLoaded != null) return iconLoaded;

                if (def != null)
                {
                    iconLoaded = def.Icon;
                }
                else
                {
                    LogUtil.Error("Failed to load icon for ResourceFC: no associated resourceDef!");
                    iconLoaded = TexLoad.questionmark;
                }
                return iconLoaded;
            }
        }

        public ResourceFC()
        {
        }

        public ResourceFC(ResourceTypeDef resourceDef, WorldSettlementFC settlement = null)
        {
            this.settlement = settlement;
            def = resourceDef;
            if (resourceDef == null)
            {
                LogUtil.Error($"Created ResourceFC with NULL resourceDef!");
            }
            {
                name = resourceDef.label;
                label = resourceDef.LabelCap;
                if (resourceDef.isPoolResource)
                {
                    isTithe = true;
                    isTitheBool = true;
                }
            }
            amount = 0;
            filter = new ThingFilter();
            productionAdditives.Clear();
            productionMultipliers.Clear();
            if (settlement != null)
            {
                //TODO: Setup the filter. Should be done with a function in *this* class, not PaymentUtil
                // Add ProductionBonuses for Biome any special modifiers in the resourceDef
                setBaseResourceAdditives();
                setBaseResourceMultipliers();
            }
            resetThingFilter();
        }


        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref amount, "amount");
            Scribe_Collections.Look(ref productionAdditives, "productionAdditives", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref productionMultipliers, "productionMultiplers", LookMode.Value, LookMode.Deep);
            //A structure for the future, to hold per-item tithe specifications
            // should work on replicating current functionality before *adding* to it, though
            // TODO
            //Scribe_Collections.Look(ref tithes, "tithes", LookMode.Deep);

            //tithe and income data
            Scribe_Values.Look(ref isTithe, "isTithe");
            Scribe_Values.Look(ref isTitheBool, "isTitheBool");
            Scribe_Values.Look(ref savedAssignedWorkers, "assignedWorkers");

            Scribe_Deep.Look(ref filter, "filter");
            //Tax Stock
            Scribe_Values.Look(ref taxStock, "taxStock");
            Scribe_Values.Look(ref taxMinimumToTithe, "taxMinimumToTithe");
            Scribe_Values.Look(ref taxPercentage, "taxPercentage");

            Scribe_References.Look(ref settlement, "settlement");
        }

        /// <summary>
        /// Calculates the total production base.
        /// </summary>
        /// <returns></returns>
        private double calculateProductionBase()
        {
            double productionBase = productionAdditives.Values.Sum(p => p.value);
            return productionBase;
        }
        /// <summary>
        /// Calculates the total production multiplier.
        /// </summary>
        /// <returns></returns>
        private double calculateProductonMult()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            double egalitarianTaxBoost = 0;
            if (faction.hasPolicy(FCPolicyDefOf.egalitarian))
            {
                egalitarianTaxBoost = Math.Floor(settlement.happiness / 10);
                if (settlement.trait_Egalitarian_TaxBreak_Enabled)
                {
                    egalitarianTaxBoost -= 30;
                }
            }

            double isolationistTaxBoost = 0;
            if (faction.hasPolicy(FCPolicyDefOf.isolationist))
                isolationistTaxBoost = 10;

            double productionMultiplier = 1;
            foreach (ProductionBonus bonus in productionMultipliers.Values)
            {
                productionMultiplier *= bonus.value;
            }
            /* The production multiplier only matters for settlement resources, but we use a barren copy of ResourceFC at the FactionFC level to track total production for all resources.
             * So if settlement == null, then we're at the faction-level resource, and don't need to actually calculate anything.
             * TODO: find a better way to store resource info at the FactionFC level */
            if (settlement != null)
            {
                productionMultiplier *= ((100 + egalitarianTaxBoost + isolationistTaxBoost + TraitUtilsFC.cycleTraits("taxBasePercentage", settlement.Traits, Operation.Addition)) / 100);
            }
            return productionMultiplier;
        }

        public void setDirtyCache()
        {
            dirtyProductionBaseCache = true;
            dirtyProductionMultCache = true;
        }
        
        public bool checkMinimum()
        {
            if (taxStock >= taxMinimumToTithe)
            {
                return true;
            }

            return false;
        }
        public double returnTaxPercentage()
        {
            taxPercentage = Math.Round(taxStock / taxMinimumToTithe, 2)*100 ;
            return taxPercentage;
        }

        public double returnLowestCost()
        {
            double minimum = filter.AllowedThingDefs.Aggregate<ThingDef, double>(999999, 
                (current, thing) => Math.Min(thing?.BaseMarketValue ?? 100, current));
            //LogUtil.Message(minimum.ToString());
            taxMinimumToTithe = minimum + FCSettings.productionTitheMod + 
                                TraitUtilsFC.cycleTraits("taxBaseRandomModifier", settlement.Traits, Operation.Addition);
            return minimum;
        }

        public ResourcePool createPool()
        {
            ResourcePool pool = new ResourcePool
            {
                resource = def,
                pool = 0
            };
            if (def.isPoolResource)
            {
                pool.pool += def.GetModExtension<ResourcePoolExtension>().createPool(production, settlement);
            }
            return pool;
        }

        /* 
         * Production Additive functions
         */
        public void setBaseResourceAdditives()
        {
            double bonus = 0;
            if (settlement != null)
            {
                /* getBiomeResource returns NULL if this resource isn't allowed in the biome. We already checked this when adding the ResourceFC to the WorldSettlementFC, though,
                 * so we should be good to go here. */
                ResourceBonuses biomeBonus = settlement.biomeDef.getBiomeResource(def);
                if (biomeBonus == null)
                {
                    LogUtil.Error($"Found NULL biomeBonus for resource {def} in settlement {settlement.Name}, despite the ResourceFC already existing");
                }
                bonus = biomeBonus?.additive ?? 0;
                if (bonus != 0)
                {
                    addProductionAdditive(def.defName + settlement.biomeDef.defName + settlement?.Name ?? "nullsettlement", bonus, $"{settlement.biomeDef.LabelCap}");
                }

                ResourceBonuses settleBonus = settlement.settlementDef.getSettlementResource(def);
                bonus = settleBonus?.additive ?? 0;
                if (bonus != 0)
                {
                    addProductionAdditive(def.defName + settlement.settlementDef.defName + settlement?.Name ?? "nullsettlement", bonus, $"{settlement.settlementDef.LabelCap}");
                }
            }
            if (def != null && def.modExtensions != null)
            {
                foreach (ResourceProductionExtension ext in def.modExtensions.OfType<ResourceProductionExtension>())
                {
                    bonus = ext.GetAdditiveBonus(settlement.Tile);
                    if (bonus != 0)
                    {
                        addProductionAdditive(def.defName + ext.extName + settlement?.Name ?? "nullsettlement", bonus, $"{ext.extName}");
                    }
                }
            }
        }
        public void addProductionAdditive(string id, double value, string desc)
        {
            TaggedString fulldesc = "RTDproductionAdditiveFrom".Translate(TextUtil.colorizeAdditiveBonus(value), def.LabelCap, desc);
            ProductionBonus additive = new ProductionBonus(value, fulldesc);
            addProductionAdditive(id, additive);
        }

        private void addProductionAdditive(string id, ProductionBonus additive)
        {
            try
            {
                productionAdditives.Add(id, additive);
            }
            catch (Exception e)
            {
                LogUtil.Error($"Failed when adding ProductionBonus additive with id {id}: {e.Message}");
            }
            dirtyProductionBaseCache = true;
        }
        public void removeProductionAdditiveById(string id)
        {
            productionAdditives.Remove(id);
            dirtyProductionBaseCache = true;
        }
        /*
         * Production Multiplier functions
         */
        public void setBaseResourceMultipliers()
        {
            double bonus = 0;
            if (settlement != null)
            {
                /* getBiomeResource returns NULL if this resource isn't allowed in the biome. We already checked this when adding the ResourceFC to the WorldSettlementFC, though,
                 * so we should be good to go here. */
                ResourceBonuses biomeBonus = settlement.biomeDef.getBiomeResource(def);
                if (biomeBonus == null)
                {
                    LogUtil.Error($"Found NULL biomeBonus for resource {def} in settlement {settlement.Name}, despite the ResourceFC already existing");
                }
                bonus = biomeBonus?.multiplier ?? 1;
                if (bonus != 1)
                {
                    addProductionMultiplier(settlement.biomeDef.defName, bonus, $"{settlement.biomeDef.LabelCap}");
                }

                ResourceBonuses settleBonus = settlement.settlementDef.getSettlementResource(def);
                bonus = settleBonus?.multiplier ?? 1;
                if (bonus != 1)
                {
                    addProductionMultiplier(def.defName + settlement.settlementDef.defName + settlement?.Name ?? "nullsettlement", bonus, $"{settlement.settlementDef.LabelCap}");
                }
            }
            if (def != null && def.modExtensions != null)
            {
                foreach (ResourceProductionExtension ext in def.modExtensions.OfType<ResourceProductionExtension>())
                {
                    bonus = ext.GetMultiplierBonus(settlement.Tile);
                    if (bonus != 1)
                    {
                        addProductionMultiplier(ext.extName, bonus, $"{ext.extDesc}");
                    }
                }
            }
        }
        public void addProductionMultiplier(string id, double value, string desc)
        {
            TaggedString fulldesc = "RTDproductionMultiplierFrom".Translate(TextUtil.colorizeMultiplierBonus(value), def.LabelCap, desc);
            ProductionBonus multiplier = new ProductionBonus(value, fulldesc);
            addProductionMultiplier(id, multiplier);
        }

        private void addProductionMultiplier(string id, ProductionBonus multiplier)
        {
            try
            {
                productionMultipliers.Add(id, multiplier);
            }
            catch (Exception e)
            {
                LogUtil.Error($"Failed when adding ProductionBonus multiplier with id {id}: {e.Message}");
            }
            dirtyProductionMultCache = true;
        }
        public void removeProductionMultiplierById(string id)
        {
            productionMultipliers.Remove(id);
            dirtyProductionMultCache = true;
        }

        /*
         * Filter functions
         */
        public void resetThingFilter()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();

            if (def == null)
                return;
            
            def.FilterResource(filter, faction.techLevel);
        }
        public List<ThingDef> generateThingDefList()
        {
            if (def.isPoolResource)
            {
                LogUtil.Error($"Attempted to generate thing list for pool resource {def.defName} in settlement {settlement.Name}");
                return null;
            }

            FactionFC faction = Find.World.GetComponent<FactionFC>();
            List<ThingDef> things = new List<ThingDef>();
            ThingSetMaker thingSetMaker = new ThingSetMaker_Count();
            ThingSetMakerParams param = new ThingSetMakerParams();
            param.filter = new ThingFilter();
            param.techLevel = ColonyUtil.getPlayerColonyFaction().def.techLevel;
            param.countRange = new IntRange(1, 1);

            TechLevel tmplevel = TechLevel.Undefined;
            ThingSetMaker tmp = def.GetModExtension<ResourceFilterExtension>()?.getThingSetMaker(out tmplevel);
            if (tmp != null)
            {
                thingSetMaker = tmp;
                param.techLevel = tmplevel;
            }

            def.FilterResource(param.filter, faction.techLevel);

            /* AllGenerateableThingsDebug(param).ToList() was taken from PaymentUtil.debugGenerateTithe(), which was used to generate the selection float menu
             * in the settlement screen. Is this really the right function to use? TODO: look into this. */
            things = thingSetMaker.AllGeneratableThingsDebug(param).ToList();
            return things;
        }
        public List<Thing> generateTithe(double valueBase, double valueDiff, int multiplier, double traitValueMod)
        {
            List<Thing> things = new List<Thing>();
            ThingSetMaker thingSetMaker = new ThingSetMaker_MarketValue();
            ThingSetMakerParams param = new ThingSetMakerParams();
            param.totalMarketValueRange = new FloatRange((float)(valueBase - (valueDiff + traitValueMod)), (float)(valueBase + (valueDiff + traitValueMod) * multiplier));
            param.filter = filter;
            param.techLevel = ColonyUtil.getPlayerColonyFaction().def.techLevel;

            if (def.isPoolResource)
            {
                LogUtil.Error($"Attempted to generate tithe for pool resource {def.defName} in settlement {settlement.Name}");
                return null;
            }

            TechLevel tmplevel = TechLevel.Undefined;
            ThingSetMaker tmp = def.GetModExtension<ResourceFilterExtension>()?.getThingSetMaker(out tmplevel);
            if (tmp != null)
            {
                thingSetMaker = tmp;
                param.techLevel = tmplevel;
            }
            else
            {
                param.countRange = new IntRange(def.titheMinCount, def.titheMaxCountBase + (def.titheMaxCountScaler * multiplier));
            }
            //TODO: when writing the defs for resources, make sure to set tithe values according to the below code
                /*switch (resourceType)
                {
                    case ResourceType.Food:
                        param.countRange = new IntRange(1, 5 + multiplier);
                        break;
                    case ResourceType.Weapons:
                        param.countRange = new IntRange(1, 4 + (2 * multiplier));
                        break;
                    case ResourceType.Apparel:
                        param.countRange = new IntRange(1, 4 + (3 * multiplier));
                        break;
                    case ResourceType.Animals:
                        thingSetMaker = new ThingSetMaker_Animals();
                        param.techLevel = TechLevel.Undefined;
                        //param.countRange = new IntRange(1,4);
                        break;
                    case ResourceType.Logging:
                        param.countRange = new IntRange(1, 5 * multiplier);
                        break;
                    case ResourceType.Mining:
                        param.countRange = new IntRange(1, 4 * multiplier);
                        break;
                    case ResourceType.Research:
                    case ResourceType.Power:
                        LogUtil.Error("generateTithe - " + resourceType + " Tithe - How did you get here?");
                        break;
                    case ResourceType.Medicine:
                        param.countRange = new IntRange(1, 2 * multiplier);
                        break;
                    case ResourceType.Gravtech:
                        param.countRange = new IntRange(1, 3 * multiplier);
                        break;
                    case ResourceType.Chemfuel:
                        param.countRange = new IntRange(1, 4 * multiplier);
                        break;
                }*/
            things = thingSetMaker.Generate(param);
            return things;
        }

        public int compareForUI(ResourceFC compareDef)
        {
            if (compareDef == null)
            {
                return -2;
            }
            if (compareDef.def == null)
            {
                return -1;
            }
            if (this.def == null)
            {
                return 1;
            }
            return ResourceTypeDef.sortForUI(this.def, compareDef.def);
        }
        public static int sortForUI(ResourceFC a, ResourceFC b)
        {
            return a.compareForUI(b);
        }
    }

    public class ProductionBonus : IExposable
    {
        public double value;
        public TaggedString desc;
        public ProductionBonus()
        {
        }

        public ProductionBonus(double value, TaggedString desc)
        {
            this.value = value;
            this.desc = desc;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref value, "value");
            Scribe_Values.Look(ref desc, "desc");
        }
    }

    public class ResourcePool : IExposable
    {
        public ResourceTypeDef resource;
        public double pool;
        public ResourcePool()
        {
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref resource, "resource");
            Scribe_Values.Look(ref pool, "pointpool");
        }
    }

    /// <summary>
    /// A small class meant for use with FactionFC to display faction-level resource production totals.
    /// </summary>
    public class ResourceDisplay : IExposable
    {
        public ResourceTypeDef resourceDef;
        public double amount;

        public Texture2D Icon => resourceDef?.Icon ?? TexLoad.questionmark;
        public string label => resourceDef?.LabelCap ?? "";
        public ResourceDisplay()
        {
        }
        public ResourceDisplay(ResourceTypeDef def)
        {
            resourceDef = def;
            amount = 0;
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref resourceDef, "resourcedef");
            Scribe_Values.Look(ref amount, "amount");
        }
        public int compareForUI(ResourceDisplay compareDef)
        {
            if (compareDef == null)
            {
                return -2;
            }
            if (compareDef.resourceDef == null)
            {
                return -1;
            }
            if (this.resourceDef == null)
            {
                return 1;
            }
            return ResourceTypeDef.sortForUI(this.resourceDef, compareDef.resourceDef);
        }
        public static int sortForUI(ResourceDisplay a, ResourceDisplay b)
        {
            return a.compareForUI(b);
        }
    }
}
