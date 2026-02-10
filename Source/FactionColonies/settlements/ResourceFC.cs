using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Policy;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class ResourceFC : IExposable
    {
        public ResourceTypeDef def;
        public string name;
        public string label;
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
                Find.World.GetComponent<FactionFC>()?.setDirtyResourceDisplayCache(def);
            }
        }

        /* All bonuses and maluses, even from biome or hilliness, should be applied through productionAdditives and productionMultipliers */
        private Dictionary<string, ProductionBonus> productionAdditives = new Dictionary<string, ProductionBonus>();
        private Dictionary<string, ProductionBonus> productionMultipliers = new Dictionary<string, ProductionBonus>();

        private bool dirtyProductionBaseCache = true;
        private bool dirtyProductionMultCache = true;
        private bool dirtyProductionBaseDescCache = true;
        private bool dirtyProductionMultDescCache = true;
        private double cachedProductionBase = 1;
        private double cachedProductionMult = 1;
        private TaggedString cachedProdBaseDesc = "";
        private TaggedString cachedProdMultDesc = "";
        private Texture2D iconLoaded;

        public Dictionary<ThingQualityTuple, int> tithes = new Dictionary<ThingQualityTuple, int>();
        private bool dirtyTitheCache = true;
        private double cachedTitheTotalValue = 0;
        public ThingFilter filter = new ThingFilter();
        public int randomTitheBudget = 0;
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
        public double titheTotalValue
        {
            get
            {
                if (dirtyTitheCache)
                {
                    pruneTitheList();
                    cachedTitheTotalValue = calcTotalTitheValue();
                    dirtyTitheCache = false;
                }
                return cachedTitheTotalValue;
            }
        }
        /* NOTE: need to be very careful about which of rawTotalProduction, totalProduction, and actualIncome to use.
         *  * rawTotalProduction is the TOTAL production value of the resource, before accounting for tithes.
         *  * totalProduction is the amount of production leftover after accounting for tithes.
         *  * actualIncome reports the actual income of the resource, accounting for tithes. This can be negative if the value of the tithes is
         *    greater than the leftover production of the resource.
         */
        public double rawTotalProduction => production * assignedWorkers;
        public double totalProduction => rawTotalProduction - titheTotalValue;
        /* We use max to set the floor at 0, as tithe modifiers mean that the titheTotalValue can actually be greater than totalProduction. */
        public double actualIncome => Math.Max(totalProduction - titheTotalValue, 0);
        public double rawTotalProductionMarketValue => rawTotalProduction * FCSettings.silverPerResource;
        public double totalProductionMarketValue => totalProduction * FCSettings.silverPerResource;
        public double titheMarketValue => titheTotalValue * FCSettings.silverPerResource;
        public double actualIncomeMarketValue => actualIncome * FCSettings.silverPerResource;

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
                /* This is a super bad case that should never happen. Find a way to make this a bigger error? */
                LogUtil.Error($"Created ResourceFC with NULL resourceDef!");
            }
            else
            {
                name = resourceDef.label;
                label = resourceDef.LabelCap;
            }
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
            Scribe_Collections.Look(ref productionAdditives, "productionAdditives", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref productionMultipliers, "productionMultiplers", LookMode.Value, LookMode.Deep);

            //tithe and income data
            Scribe_Values.Look(ref savedAssignedWorkers, "assignedWorkers");
            Scribe_Collections.Look(ref tithes, "tithes", LookMode.Deep, LookMode.Value);
            Scribe_Deep.Look(ref filter, "filter");
            Scribe_Values.Look(ref randomTitheBudget, "randomTitheBudget");

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
            double productionMultiplier = 1;
            foreach (ProductionBonus bonus in productionMultipliers.Values)
            {
                //TODO: should multipliers be additive with each other?
                productionMultiplier *= bonus.value;
            }

            double taxBonus = settlement?.getSettlementTaxBonus() ?? 1;
            productionMultiplier *= taxBonus;

            return productionMultiplier;
        }
        public double getTitheIncome()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            double income = rawTotalProduction;

            double perWorkerAdditive = faction.getFactionTitheBonusAdditive(def) + settlement.getTitheModifier(def);
            double perWorkerMult = faction.getFactionTitheBonusMult(def);
            income += (perWorkerAdditive * perWorkerMult) * assignedWorkers;

            return income;
        }

        public void setDirtyCache()
        {
            setDirtyCacheProdBase();
            setDirtyCacheProdMult();
            dirtyTitheCache = true;
            Find.World.GetComponent<FactionFC>()?.setDirtyResourceDisplayCache(def);
        }
        public void setDirtyCacheProdBase()
        {
            dirtyProductionBaseCache = true;
            dirtyProductionBaseDescCache = true;
            dirtyTitheCache = true;
        }
        public void setDirtyCacheProdMult()
        {
            dirtyProductionMultCache = true;
            dirtyProductionMultDescCache = true;
            dirtyTitheCache = true;
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
                pool.pool += def.GetModExtension<ResourcePoolExtension>().createPool(totalProduction, settlement);
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
                else
                {
                    bonus = biomeBonus.additive;
                    if (bonus != 0)
                    {
                        addProductionAdditive(def.defName + settlement.biomeDef.defName + settlement?.Name ?? "nullsettlement", bonus, settlement.biomeDef.LabelCap);
                    }
                }

                ResourceBonuses settleBonus = settlement.settlementDef.getSettlementResource(def);
                bonus = settleBonus?.additive ?? 0;
                if (bonus != 0)
                {
                    addProductionAdditive(def.defName + settlement.settlementDef.defName + settlement?.Name ?? "nullsettlement", bonus, settlement.settlementDef.LabelCap);
                }
            }
            if (def != null && def.modExtensions != null)
            {
                foreach (ResourceProductionExtension ext in def.modExtensions.OfType<ResourceProductionExtension>())
                {
                    bonus = ext.GetAdditiveBonus(settlement.Tile);
                    if (bonus != 0)
                    {
                        addProductionAdditive(def.defName + ext.extName + settlement?.Name ?? "nullsettlement", bonus, ext.extName);
                    }
                }
            }
        }
        public void addProductionAdditive(string id, double value, string desc)
        {
            ProductionBonus additive = new ProductionBonus(value, desc);

            if (desc.NullOrEmpty())
            {
                LogUtil.Warning($"Created a production additive for resource {label} in settlement {settlement.Name} with an empty description! (id: {id})");
            }
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
            setDirtyCacheProdBase();
        }
        public void removeProductionAdditiveById(string id)
        {
            productionAdditives.Remove(id);
            setDirtyCacheProdBase();
        }
        public TaggedString getProductionAdditivesDesc()
        {
            if (dirtyProductionBaseDescCache)
            {
                TaggedString desc = "";
                foreach (ProductionBonus additive in productionAdditives.Values)
                {
                    desc += TextUtil.colorizeAdditiveBonus(additive.value) + " - " + additive.desc + "\n";
                }
                cachedProdBaseDesc = desc.Trim();
                dirtyProductionBaseDescCache = false;
            }
            return cachedProdBaseDesc;
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
                    addProductionMultiplier(settlement.biomeDef.defName, bonus, settlement.biomeDef.LabelCap);
                }

                ResourceBonuses settleBonus = settlement.settlementDef.getSettlementResource(def);
                bonus = settleBonus?.multiplier ?? 1;
                if (bonus != 1)
                {
                    addProductionMultiplier(def.defName + settlement.settlementDef.defName + settlement?.Name ?? "nullsettlement", bonus, settlement.settlementDef.LabelCap);
                }
            }
            if (def != null && def.modExtensions != null)
            {
                foreach (ResourceProductionExtension ext in def.modExtensions.OfType<ResourceProductionExtension>())
                {
                    bonus = ext.GetMultiplierBonus(settlement.Tile);
                    if (bonus != 1)
                    {
                        addProductionMultiplier(ext.extName, bonus, ext.extDesc);
                    }
                }
            }
        }
        public void addProductionMultiplier(string id, double value, string desc)
        {
            ProductionBonus multiplier = new ProductionBonus(value, desc);

            if (desc.NullOrEmpty())
            {
                LogUtil.Warning($"Created a production multiplier for resource {label} in settlement {settlement.Name} with an empty description! (id: {id})");
            }
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
            setDirtyCacheProdMult();
        }
        public void removeProductionMultiplierById(string id)
        {
            productionMultipliers.Remove(id);
            setDirtyCacheProdMult();
        }
        public TaggedString getProductionMultipliersDesc()
        {
            if (dirtyProductionMultDescCache)
            {
                TaggedString desc = "";
                foreach (ProductionBonus multiplier in productionMultipliers.Values)
                {
                    desc += TextUtil.colorizeMultiplierBonus(multiplier.value) + " - " + multiplier.desc + "\n";
                }
                desc += TextUtil.colorizeMultiplierBonus(settlement?.getSettlementTaxBonus() ?? 1) + " - " + "TaxBase".Translate();

                cachedProdMultDesc = desc.Trim();
                dirtyProductionMultDescCache = false;
            }
            return cachedProdMultDesc;
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
        /* - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - *
         *   Tithe functions                                                                                                                                             *
         * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - */
        // Braintorming time
        // On thinking about it, incremental tithing is proving to be trickier than expected, especially if you want to let the user specify the quality level or stuff
        //   that the item is made of
        // What exactly do we need?
        //  - track the thingDef, specified quality, specified stuff, and quanity. At tithe time, can use these to determine value, and then use that value to determine how many of the object are produced
        //  - need a way to determine if a thingDef CAN have a quality, or a stuff
        /// <summary>
        /// Adds a given quantity of thing to the tithes list.
        /// <para>This function does not check if the given <paramref name="quantity"/> of <paramref name="thing"/> can actually be afforded.</para>
        /// <para>This function dirties the tithe cache, forcing a recalculation of the total tithe value.</para>
        /// </summary>
        /// <param name="thing">A ThingQualityTuple specifying the ThingDef, QualityCategory, and StuffDef of the thing to add.</param>
        /// <param name="quantity">The quantity to add to the tithes list. Should always be a non-zero positive value.</param>
        /// <returns>TRUE if the thing was successfully added to the tithes dictionary, FALSE otherwise.</returns>
        public bool addToTitheList(ThingQualityTuple thing, int quantity)
        {
            if (quantity == 0)
            {
                LogUtil.Warning($"Tried to add 0 objects to the tithes list for resource {def.LabelCap}");
                return false;
            }
            if (quantity < 0)
            {
                LogUtil.Error($"Tried to add a negative quantity of objects to the tithes list for resource {def.LabelCap}. You should use decrementInTitheList() instead.");
                return false;
            }

            if (tithes.ContainsKey(thing))
            {
                int totalNum = tithes[thing] + quantity;
                tithes[thing] = totalNum;
            }
            else
            {
                tithes.Add(thing, quantity);
            }

            dirtyTitheCache = true;
            return true;
        }
        /// <summary>
        /// Removes a given <paramref name="quantity"/> of <paramref name="thing"/> from the tithes list.
        /// <para>This function dirties the tithe cache, forcing a recalculation of the total tithe value.</para>
        /// <para>This function does not remove <paramref name="thing"/> from the tithes list if its quantity reaches 0. For that, use removeFromTitheList().</para>
        /// </summary>
        /// <param name="thing">A ThingQualityTuple specifying the ThingDef, QualityCategory, and StuffDef of the thing to decrement.</param>
        /// <param name="quantity">The quantity to remove from the tithes list. Should always be a non-zero positive value.</param>
        public void decrementInTitheList(ThingQualityTuple thing, int quantity)
        {
            if (quantity == 0)
            {
                LogUtil.Warning($"Tried to remove 0 objects from the tithes list for resource {def.LabelCap}");
                return;
            }
            if (quantity < 0)
            {
                LogUtil.Error($"Tried to remove a negative quantity of objects from the tithes list for resource {def.LabelCap}. You should use addToTithesList() instead.");
                return;
            }

            if (tithes.ContainsKey(thing))
            {
                tithes[thing] -= quantity;
                if (tithes[thing] < 0)
                {
                    tithes[thing] = 0;
                }
            }
            else
            {
                LogUtil.Warning($"Tried to remove {thing.thingDef.LabelCap} from tithes list for resource {def.LabelCap}, but it doesn't exist");
            }
            dirtyTitheCache = true;
        }
        /// <summary>
        /// Fully removes the given <paramref name="thing"/> from the tithes list.
        /// <para>This function dirties the tithe cache, forcing a recalculation of the total tithe value.</para>
        /// <para>We should only fully remove an item from the tithes list if the player commands it so. Use decrementInTitheList() otherwise, so that things with a quantity of 0 remain in the tithes list.</para>
        /// </summary>
        /// <param name="thing">A ThingQualityTuple specifying the ThingDef, QualityCategory, and StuffDef of the thing to remove.</param>
        public void removeFromTitheList(ThingQualityTuple thing)
        {
            if (tithes.ContainsKey(thing))
            {
                tithes.Remove(thing);
                dirtyTitheCache = true;
            }
        }
        public bool canSetTitheQuality(out QualityCategory maxQuality)
        {
            //TODO: add a building or something that enables selecting item quality when tithing
            maxQuality = QualityCategory.Legendary;
            return true;
        }
        public List<QualityCategory> getValidTitheQualities()
        {
            List<QualityCategory> list = new List<QualityCategory>();
            QualityCategory maxQuality = QualityCategory.Legendary;
            if (canSetTitheQuality(out maxQuality))
            {
                list = QualityUtility.AllQualityCategories;
                for (int i = list.Count - 1; i > 0; i--)
                {
                    if (list[i] > maxQuality)
                    {
                        list.RemoveAt(i);
                    }
                }
            }

            return list;
        }
        public bool canSetTitheStuff()
        {
            //TODO: add a building or something that enables selecting item stuff when tithing
            return true;
        }
        public List<ThingDef> getStuffListForThingDef(ThingDef thing)
        {
            List<ThingDef> stuffs = new List<ThingDef>();
            //TODO: literally the whole function
            return stuffs;
        }
        public float titheThingValue(ThingQualityTuple thing)
        {
            float value = 0;
            //TODO: implement
            if (CraftUtil.thingHasQuality(thing.thingDef))
            {
                if (CraftUtil.thingIsStuffable(thing.thingDef))
                {
                    //TODO
                }
                else
                {
                    //TODO
                }
            }
            else
            {
                if (CraftUtil.thingIsStuffable(thing.thingDef))
                {
                    //TODO
                }
                else
                {
                    value = thing.thingDef.BaseMarketValue;
                }
            }
            return value;
        }
        public float titheThingTotalValue(ThingQualityTuple thing, int quanity)
        {
            return titheThingValue(thing) * quanity;
        }
        public bool canAffordThingAmount(ThingQualityTuple thing, int quanity)
        {
            return (titheThingTotalValue(thing, quanity) <= getTitheIncome() - titheMarketValue);
        }
        public int maxThingCanAfford(ThingQualityTuple thing)
        {
            return maxThingCanAfford(thing, getTitheIncome() - titheMarketValue);
        }
        public int maxThingCanAfford(ThingQualityTuple thing, double budget)
        {
            float value = titheThingValue(thing);
            return (int)(budget / value);
        }
        public float calcTotalTitheValue()
        {
            float total = 0;
            foreach (var (key, value) in tithes)
            {
                total += titheThingTotalValue(key, value);
            }

            return total;
        }
        public ThingQualityTuple findHighestValueTitheThing()
        {
            ThingQualityTuple maxthing = null;
            float maxval = 0;
            foreach (var (key, value) in tithes)
            {
                float val = titheThingValue(key);
                if (val > maxval)
                {
                    maxthing = key;
                    maxval = val;
                }
            }
            return maxthing;
        }
        /// <summary>
        /// Removes items from the tithes dictionary if the total value of the tithes is higher than the raw total production.
        /// <para>This function dirties the tithe cache, forcing a recalculation of the total tithe value.</para>
        /// <para>NOTE: The algorithm is heavy-handed. Calling this function with high frequency is ill-advised.</para>
        /// </summary>
        /* Should only call this function in one of two places: at tithe time, and if player clicks a button to do so on the tithing screen.
         * Otherwise, we should let the player set whatever values they want, and merely warn them that the list will be pruned at tithe time.
         * Actually, to keep income values properly in sync, the tithe list should be pruned every time its changed, or the resource production
         * changes...
         */
        // Could probably make the algorithm slightly less heavy by just subtracting values from totalValue instead of constantly re-calling
        //   calcTotalTitheValue(), but I'm paranoid about the values misaligning. So leaving as is. If optimization is necessary, that's a
        //   decent place to start.
        public void pruneTitheList()
        {
            if (tithes.Count == 0)
            {
                return;
            }

            double totalValue = 0;
            double titheIncome = getTitheIncome();
            while ((totalValue = calcTotalTitheValue()) > titheIncome && tithes.Count > 0)
            {
                ThingQualityTuple maxValueThing = findHighestValueTitheThing();
                if (maxValueThing == null)
                {
                    /* This case shouldn't be possible. But *just* in case, we'll throw an error and bail out if we get here. */
                    LogUtil.Error($"Got NULL when trying to find highest value thing in tithes list for resource {def.LabelCap}. Bailing out of pruneTitheList()");
                    return;
                }
                int quantity = tithes[maxValueThing];
                double totalThingValue = titheThingTotalValue(maxValueThing, quantity);
                if (totalValue - totalThingValue < titheIncome)
                {
                    double budget = titheIncome - (totalValue - (totalValue - totalThingValue));
                    int newQuantity = maxThingCanAfford(maxValueThing, budget);
                    int removeNum = quantity - newQuantity;
                    decrementInTitheList(maxValueThing, removeNum);
                }
                else
                {
                    decrementInTitheList(maxValueThing, quantity);
                }
            }
            dirtyTitheCache = true;
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
        /* - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - *
         *   End Tithe functions                                                                                                                                         *
         * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - */

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

    public class ThingQualityTuple : IExposable
    {
        public ThingDef thingDef;
        public QualityCategory quality;
        public ThingDef stuffDef;
        public ThingQualityTuple()
        {
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look(ref quality, "quality");
            Scribe_Defs.Look(ref stuffDef, "stuffDef");
        }
    }

    /// <summary>
    /// A small class meant for use with FactionFC to display faction-level resource production totals.
    /// </summary>
    public class ResourceDisplay : IExposable
    {
        public ResourceTypeDef resourceDef;
        private double cachedAmount = 0;
        private bool dirtyCachedAmount = true;
        public double amount
        {
            get
            {
                if (dirtyCachedAmount)
                {
                    FactionFC factionFC = Find.World.GetComponent<FactionFC>();
                    double resource = 0;
                    for (int k = 0; k < factionFC.settlements.Count(); k++)
                    {
                        resource += (int)(factionFC.settlements[k].getResource(resourceDef)?.actualIncome ?? 0);
                    }

                    cachedAmount = resource;
                    dirtyCachedAmount = false;
                }
                return cachedAmount;
            }
        }

        public Texture2D Icon => resourceDef?.Icon ?? TexLoad.questionmark;
        public string label => resourceDef?.LabelCap ?? "";
        public ResourceDisplay()
        {
        }
        public ResourceDisplay(ResourceTypeDef def)
        {
            resourceDef = def;
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref resourceDef, "resourcedef");
        }
        public void setDirtyCache()
        {
            dirtyCachedAmount = true;
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
