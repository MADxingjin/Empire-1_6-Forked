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
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.ParticleSystem;

namespace FactionColonies
{
    public class ResourceFC : IExposable
    {
        public ResourceTypeDef def;
        public string name;
        public string label;
        public WorldSettlementFC settlement;
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
                FactionCache.FactionComp?.setDirtyResourceDisplayCache(def);
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

        /* Don't expost tithes publicly. We want values to be added or removed *only* through our special functions, so that we can dirty or set
         * cached values appropriately. */
        private Dictionary<ThingQualityTuple, int> tithes = new Dictionary<ThingQualityTuple, int>();
        private bool dirtyTitheCache = true;
        private double cachedTitheTotalValue = 0;
        /// <summary>
        /// The amount of budget available to this resource for random tithing. If the lowest-value random tithing thing is still higher in value than the available titheStock,
        /// then production is rolled over to the next tax period, until enough has accrued to actually produce the tithe.
        /// </summary>
        // TODO: alert the player when tithing has rolled over?
        public double randomTitheStock = 0;
        public bool disburseTitheStock = false;

        public bool hasRandomTithe = false;
        public ThingFilter randomTitheFilter = new ThingFilter();
        private bool dirtyRandomTitheCache = true;
        private List<ThingDef> thingsForRandomTithes = new List<ThingDef>();
        private bool dirtyFilteredRandomTitheCache = true;
        private List<ThingDef> filteredThingsForRandomTithes = new List<ThingDef>();
        public string storedRandomTitheBudgetBuffer = "";
        public int storedRandomTitheBudget = 0;
        private int oldStoredRandomTitheBudget = 0;

        // Submods can register named allocations to siphon production away from taxes and tithes.
        // Each mod registers under its own key so multiple submods compose correctly.
        // Not persisted — submods are expected to re-register their allocations on load.
        private struct StockpileEntry
        {
            public double amount;
            public Action onEvicted; // invoked if the entry is evicted at tax time; null is allowed
        }
        private Dictionary<string, StockpileEntry> stockpileAllocations = new Dictionary<string, StockpileEntry>();
        public double totalStockpileAllocation => stockpileAllocations.Values.Sum(e => e.amount);

        /// <summary>
        /// Attempts to register a named production diversion for a stockpile.
        /// Returns false without registering if the amount would push total diversions above <see cref="rawTotalProduction"/>.
        /// If the key already exists, the old entry is replaced (using the new amount in the capacity check).
        /// </summary>
        /// <param name="key">Unique identifier for the calling mod (e.g. "MyMod.MyFeature").</param>
        /// <param name="onEvicted">Optional callback invoked if this entry is later evicted at tax time due to insufficient production.</param>
        public bool SetStockpileAllocation(string key, double amount, Action onEvicted = null)
        {
            double currentForKey = stockpileAllocations.TryGetValue(key, out var existing) ? existing.amount : 0;
            if (totalStockpileAllocation - currentForKey + amount > rawTotalProduction)
                return false;
            stockpileAllocations[key] = new StockpileEntry { amount = amount, onEvicted = onEvicted };
            return true;
        }

        /// <summary>Removes a previously registered stockpile allocation. The eviction callback is NOT invoked.</summary>
        public void ClearStockpileAllocation(string key) => stockpileAllocations.Remove(key);

        /// <summary>
        /// Evicts stockpile entries (largest first) until the total allocation fits within <see cref="rawTotalProduction"/>.
        /// Called at tax time after resource caches are refreshed. Invokes each evicted entry's callback.
        /// </summary>
        public void PruneStockpileAllocations()
        {
            if (totalStockpileAllocation <= rawTotalProduction)
                return;
            foreach (var key in stockpileAllocations
                         .OrderByDescending(kv => kv.Value.amount)
                         .Select(kv => kv.Key)
                         .ToList())
            {
                if (totalStockpileAllocation <= rawTotalProduction) break;
                var entry = stockpileAllocations[key];
                stockpileAllocations.Remove(key);
                entry.onEvicted?.Invoke();
            }
        }
        public int randomTitheBudget
        {
            get
            {
                if (hasRandomTithe)
                {
                    return storedRandomTitheBudget;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                storedRandomTitheBudget = value;
                settlement.updateProfitAndProduction();
            }
        }

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
                // Pool resources are always counted as though they are tithing, since you can't actually get any silver from them.
                // TODO: change this? Make it possible to control how much of a pool resources's pool goes into the actual pool, and how much gets shipped as silver?
                if (def.isPoolResource)
                {
                    return taxableProductionMarketValue;
                }
                if (dirtyTitheCache)
                {
                    pruneTitheList();
                    cachedTitheTotalValue = calcTotalTitheValue();
                    dirtyTitheCache = false;
                }
                return cachedTitheTotalValue + randomTitheBudget;
            }
        }
        public double titheTotalValueNoRandom => titheTotalValue - randomTitheBudget;
        /* NOTE: the production property chain flows as follows:
         *  rawTotalProduction          — gross output (units), before any splits. Display this as "Total Production".
         *  effectiveRawTotalProduction — post-stockpile output (units); what remains after submod allocations are diverted.
         *  taxableProductionMarketValue — silver value of effectiveRawTotalProduction; the budget available to taxes and tithes.
         *  actualIncome                — taxableProductionMarketValue minus tithe costs; what the player actually receives in silver.
         *                                Can be negative if tithe modifiers push the tithe value above taxable production.
         */
        public double rawTotalProduction => production * assignedWorkers;
        public double effectiveRawTotalProduction => rawTotalProduction - totalStockpileAllocation;
        public double taxableProductionMarketValue => effectiveRawTotalProduction * FCSettings.silverPerResource;
        public double actualIncome => taxableProductionMarketValue - titheTotalValue;

        public bool canTithe => !def.isPoolResource;

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
            randomTitheFilter = new ThingFilter();
            randomTitheBudget = 0;
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
            Scribe_Deep.Look(ref randomTitheFilter, "filter");
            Scribe_Values.Look(ref storedRandomTitheBudget, "randomTitheBudget");
            Scribe_Values.Look(ref hasRandomTithe, "hasRandomTithe");

            //Tax Stock
            Scribe_Values.Look(ref randomTitheStock, "taxStock");
            Scribe_Values.Look(ref disburseTitheStock, "disburseTaxStock");

            Scribe_References.Look(ref settlement, "settlement");
        }

        /// <summary>
        /// Calculates the total production base.
        /// </summary>
        /// <returns></returns>
        private double calculateProductionBase()
        {
            double dictBase = ResourceFormulas.CalculateProductionBase(productionAdditives.Values.Select(p => p.value));
            double statBase = (settlement != null && def.productionAdditiveStat != null)
                ? settlement.getStatValue(def.productionAdditiveStat) : 0;
            return dictBase + statBase;
        }
        /// <summary>
        /// Calculates the total production multiplier.
        /// </summary>
        /// <returns></returns>
        private double calculateProductonMult()
        {
            double taxBonus = settlement?.getSettlementTaxBonus() ?? 1;
            double dictMult = ResourceFormulas.CalculateProductionMult(productionMultipliers.Values.Select(p => p.value), 1.0);
            double statMult = (settlement != null && def.productionMultiplierStat != null)
                ? settlement.getStatValue(def.productionMultiplierStat) : 1;
            return dictMult * statMult * taxBonus;
        }
        public double getTitheModifierAdditivePerWorker()
        {
            FactionFC faction = FactionCache.FactionComp;
            return faction.getFactionTitheBonusAdditivePerWorker(def) + settlement.getTitheModifierPerWorker(def) + FCSettings.productionTitheMod;
        }
        public double getTitheModifierAdditiveForTotal()
        {
            FactionFC faction = FactionCache.FactionComp;
            return faction.getFactionTitheBonusAdditiveForTotal(def);
        }
        public double getTitheModifierMultPerWorker()
        {
            FactionFC faction = FactionCache.FactionComp;
            return faction.getFactionTitheBonusMultPerWorker(def);
        }
        public double getTitheModifierMultForTotal()
        {
            FactionFC faction = FactionCache.FactionComp;
            return faction.getFactionTitheBonusMultForTotal(def);
        }
        public double getTitheModifierPerWorker()
        {
            return ResourceFormulas.CalculateTitheModifierPerWorker(getTitheModifierAdditivePerWorker(), getTitheModifierMultPerWorker());
        }
        public double getTotalTitheModifierForWorkers()
        {
            return ResourceFormulas.CalculateTotalTitheModifierForWorkers(getTitheModifierPerWorker(), assignedWorkers);
        }
        public double getTitheIncome()
        {
            return ResourceFormulas.CalculateTitheIncome(taxableProductionMarketValue, getTotalTitheModifierForWorkers(), getTitheModifierAdditiveForTotal(), getTitheModifierMultForTotal());
        }
        public void refreshOnRandomTitheBudgetChange()
        {
            if (storedRandomTitheBudget != oldStoredRandomTitheBudget)
            {
                oldStoredRandomTitheBudget = storedRandomTitheBudget;
                settlement.updateProfitAndProduction();
            }
        }
        public void setDirtyCache()
        {
            setDirtyCacheProdBase();
            setDirtyCacheProdMult();
            dirtyTitheCache = true;
            setDirtyRandomTitheCache();
            dirtyFilteredRandomTitheCache = true;
            FactionCache.FactionComp?.setDirtyResourceDisplayCache(def);
        }
        public void setDirtyRandomTitheCache()
        {
            dirtyRandomTitheCache = true;
            settlement.dirtyGrandThingList();
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

        public ResourcePool createPool()
        {
            ResourcePool pool = new ResourcePool
            {
                resource = def,
                pool = 0
            };
            if (def.isPoolResource)
            {
                pool.pool += def.GetModExtension<ResourcePoolExtension>().createPool(effectiveRawTotalProduction, settlement);
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
                ResourceAvailability biomeBonus = settlement.biomeDef.getBiomeResource(def);
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

                ResourceAvailability settleBonus = settlement.settlementDef.getSettlementResource(def);
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
                if (def.productionAdditiveStat != null && settlement != null)
                {
                    desc += settlement.getStatDesc(def.productionAdditiveStat);
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
                ResourceAvailability biomeBonus = settlement.biomeDef.getBiomeResource(def);
                if (biomeBonus == null)
                {
                    LogUtil.Error($"Found NULL biomeBonus for resource {def} in settlement {settlement.Name}, despite the ResourceFC already existing");
                }
                bonus = biomeBonus?.multiplier ?? 1;
                if (bonus != 1)
                {
                    addProductionMultiplier(settlement.biomeDef.defName, bonus, settlement.biomeDef.LabelCap);
                }

                ResourceAvailability settleBonus = settlement.settlementDef.getSettlementResource(def);
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
                if (def.productionMultiplierStat != null && settlement != null)
                {
                    desc += settlement.getStatDesc(def.productionMultiplierStat);
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
            FactionFC faction = FactionCache.FactionComp;

            if (def == null)
                return;
            
            def.FilterResource(randomTitheFilter, faction.techLevel);
        }
        /// <summary>
        /// Generates a list of ThingDefs that can be generated as tithes for this resource.
        /// </summary>
        /// <returns></returns>
        public List<ThingDef> generateThingDefList()
        {
            if (def.isPoolResource)
            {
                LogUtil.Error($"Attempted to generate thing list for pool resource {def.defName} in settlement {settlement.Name}");
                return null;
            }
            if (dirtyRandomTitheCache)
            {
                if (randomTitheFilter == null)
                {
                    randomTitheFilter = new ThingFilter();
                    resetThingFilter();
                }

                FactionFC faction = FactionCache.FactionComp;
                ThingSetMaker thingSetMaker = new ThingSetMaker_Count();
                ThingSetMakerParams param = new ThingSetMakerParams();
                param.filter = new ThingFilter();
                param.techLevel = FactionCache.PlayerColonyFaction.def.techLevel;
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
                thingsForRandomTithes = thingSetMaker.AllGeneratableThingsDebug(param).ToList();
                dirtyRandomTitheCache = false;
            }
            return thingsForRandomTithes;
        }
        public List<ThingDef> getRandomTitheFilterThings()
        {
            if (dirtyFilteredRandomTitheCache)
            {
                if (randomTitheFilter == null)
                {
                    randomTitheFilter = new ThingFilter();
                    resetThingFilter();
                }

                filteredThingsForRandomTithes = new List<ThingDef>();
                List<ThingDef> possibleThings = generateThingDefList();

                foreach(ThingDef thingDef in possibleThings)
                {
                    if (randomTitheFilter.Allows(thingDef))
                    {
                        filteredThingsForRandomTithes.Add(thingDef);
                    }
                }

                dirtyFilteredRandomTitheCache = false;
            }
            return filteredThingsForRandomTithes;
        }
        public void clearRandomTitheFilter()
        {
            if (randomTitheFilter == null)
            {
                randomTitheFilter = new ThingFilter();
            }
            randomTitheFilter.SetDisallowAll();
            setDirtyRandomTitheCache();
            dirtyFilteredRandomTitheCache = true;
        }
        public void setAllRandomTitheFilter()
        {
            if (randomTitheFilter == null)
            {
                randomTitheFilter = new ThingFilter();
            }
            resetThingFilter();
            setDirtyRandomTitheCache();
            dirtyFilteredRandomTitheCache = true;
        }
        public void setRandomTitheFilterAllow(ThingDef thing, bool allow)
        {
            if (randomTitheFilter == null)
            {
                randomTitheFilter = new ThingFilter();
                resetThingFilter();
                setDirtyRandomTitheCache();
            }

            randomTitheFilter.SetAllow(thing, allow);
            dirtyFilteredRandomTitheCache = true;
        }
        // could cache this with a dictionary, but probably best to see if there's an actual performance problem first
        public bool getRandomTitheFilterAllow(ThingDef thing)
        {
            return randomTitheFilter.Allows(thing);
        }
        /* - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - *
         *   Tithe functions                                                                                                                                             *
         * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - * - */
        /// <summary>
        /// Adds a given quantity of thing to the tithes list.
        /// <para>This function does not check if the given <paramref name="quantity"/> of <paramref name="thing"/> can actually be afforded.</para>
        /// <para>This function dirties the tithe cache, forcing a recalculation of the total tithe value.</para>
        /// </summary>
        /// <param name="thing">A ThingQualityTuple specifying the ThingDef, QualityCategory, and StuffDef of the thing to add.</param>
        /// <param name="quantity">The quantity to add to the tithes list. Should always be a non-zero positive value.</param>
        /// <returns>TRUE if the thing was successfully added to the tithes dictionary, FALSE otherwise.</returns>
        public bool addToTitheList(ThingQualityTuple thing, int quantity, bool forceToQuantity = false)
        {
            if (quantity < 0)
            {
                LogUtil.Error($"Tried to add a negative quantity of objects to the tithes list for resource {def.LabelCap}. You should use decrementInTitheList() instead.");
                return false;
            }
            LogUtil.Message($"Resource {def.LabelCap} adding new thing to tithe list: [{thing.thingDef.LabelCap} | {TextUtil.GetQualityLabelCap(thing.quality)} | {thing.stuffDef?.LabelCap ?? "null stuff"}] with quantity {quantity}");

            if (tithes.ContainsKey(thing))
            {
                int totalNum;
                if (forceToQuantity)
                {
                    totalNum = quantity;
                }
                else
                {
                    totalNum = tithes[thing] + quantity;
                }
                tithes[thing] = totalNum;
            }
            else
            {
                tithes.Add(thing, quantity);
            }

            dirtyTitheCache = true;
            settlement.updateProfitAndProduction();
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
            settlement.updateProfitAndProduction();
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
                LogUtil.Message($"Resource {def.LabelCap} removing thing from tithe list: {thing.thingDef.LabelCap} | {TextUtil.GetQualityLabelCap(thing.quality)} | {thing.stuffDef?.LabelCap ?? "null stuff"}");
                tithes.Remove(thing);
                dirtyTitheCache = true;
                settlement.updateProfitAndProduction();
            }
        }
        public ThingQualityTuple getTitheListKey(ThingQualityTuple thing)
        {
            if (tithes.ContainsKey(thing))
            {
                return thing;
            }
            else
            {
                return null;
            }
        }
        public bool hasTitheListKey(ThingQualityTuple thing)
        {
            return tithes.ContainsKey(thing);
        }
        public int getTitheListValue(ThingQualityTuple thing)
        {
            if (tithes.ContainsKey(thing))
            {
                return tithes[thing];
            }
            else
            {
                return 0;
            }
        }
        public List<ThingQualityTuple> getTitheListKeys()
        {
            return tithes.Keys.ToList();
        }
        public List<int> getTitheListValues()
        {
            return tithes.Values.ToList();
        }
        public int getTitheListCount()
        {
            return tithes.Count;
        }
        public bool canSetTitheQuality(out QualityCategory maxQuality)
        {
            //TODO: add a building or something that enables selecting item quality when tithing
            maxQuality = QualityCategory.Legendary;
            return true;
        }
        public List<QualityCategory> getValidTitheQualities(QualityCategory maxQuality)
        {
            List<QualityCategory> list = QualityUtility.AllQualityCategories;
            for (int i = list.Count - 1; i > 0; i--)
            {
                if (list[i] > maxQuality)
                {
                    list.RemoveAt(i);
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
            return CraftUtil.getThingStuffs(thing, settlement.getGrandThingList());
        }
        public float titheThingValue(ThingQualityTuple thing)
        {
            return titheThingValue(thing.thingDef, thing.stuffDef, thing.quality);
        }
        public float titheThingValue(ThingDef thing, ThingDef stuff, QualityCategory quality)
        {
            float value;
            if (CraftUtil.thingHasQuality(thing))
            {
                value = StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(thing, stuff, quality));
            }
            else
            {
                if (CraftUtil.thingIsStuffable(thing))
                {
                    value = StatWorker_MarketValue.CalculatedBaseMarketValue(thing, stuff);
                }
                else
                {
                    value = thing.BaseMarketValue;
                }
            }
            // Prevent shenanigans
            if (value <= 0)
            {
                value = 100;
            }
            return value;
        }
        public float titheThingTotalValue(ThingQualityTuple thing, int quanity)
        {
            return titheThingValue(thing) * quanity;
        }
        public bool canAffordThingAmount(ThingQualityTuple thing, int quanity)
        {
            return ResourceFormulas.CanAffordThingAmount(titheThingTotalValue(thing, quanity), getTitheIncome() - titheTotalValue);
        }
        public int maxThingCanAfford(ThingQualityTuple thing)
        {
            return maxThingCanAfford(thing, getTitheIncome() - titheTotalValue);
        }
        public int maxThingCanAfford(ThingQualityTuple thing, double budget)
        {
            return ResourceFormulas.MaxThingCanAfford(budget, titheThingValue(thing));
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
            if (totalValue > titheIncome && tithes.Count == 0)
            {
                /* In this case, the total tithe value must consist entirely of the random tithe budget. So just cap the random tithe budget at
                 * titheIncome */
                randomTitheBudget = (int)titheIncome;
            }

            /* One final sanity check. Probably not necessary? If this impacts performance too much, then nuke it. Probably fine though */
            if ((totalValue = calcTotalTitheValue()) > titheIncome)
            {
                LogUtil.Error($"Reached end of pruneTitheList() for resource {def.LabelCap}, but total tithe value {totalValue} is still greater than tithe income {titheIncome}!");
            }
            dirtyTitheCache = true;
        }
        public List<Thing> generateTithe(out int extraSilver)
        {
            int outSilver = 0;
            List<Thing> titheItems = new List<Thing>();

            //Sanity check to make sure that this function is only being called after the proper prep
            if (!settlement.IsCalculatingTax)
            {
                LogUtil.Error($"Attempted to generate tithe for resource {label} in settlement {settlement.Name} outside of tax phase");
                extraSilver = outSilver;
                return null;
            }

            if (def.isPoolResource)
            {
                LogUtil.Error($"Attempted to generate tithe for pool resource {label} in settlement {settlement.Name}");
                extraSilver = outSilver;
                return null;
            }

            // Pre-tax generation hook
            def.GetModExtension<ResourceTaxExtension>()?.OnPreTaxGeneration(this, settlement);

            // Prepare the tithe filter. I don't think it should ever be null here, but we'll account for that, just in case.
            if (randomTitheFilter == null)
            {
                randomTitheFilter = new ThingFilter();
                resetThingFilter();
            }

            // Determine random tithing budget
            if (disburseTitheStock && randomTitheStock > 0)
            {
                outSilver += (int)randomTitheStock;
                randomTitheStock = 0;
            }
            double randomBudget = randomTitheBudget + randomTitheStock;
            if (hasRandomTithe && randomBudget > 0)
            {
                if (!randomTitheFilter.AllowedThingDefs.Any())
                {
                    randomTitheStock = randomBudget;
                    Find.LetterStack.ReceiveLetter("NoTitheLetterLabel".Translate(settlement.Name), "NoTitheLetterDesc".Translate(settlement.Name, label, randomTitheStock), LetterDefOf.NeutralEvent);
                }
                else
                {
                    // Calculate the random tithe

                    double minimum = randomTitheFilter.AllowedThingDefs.Aggregate<ThingDef, double>(999999, (current, thing) => Math.Min(thing?.BaseMarketValue ?? 100, current));
                    LogUtil.Message($"{settlement.Name}, resource {label}, minimum random tithe: {minimum}, budget: {randomBudget}");
                    if (minimum <= randomBudget)
                    {
                        List<Thing> randomTitheList = new List<Thing>();
                        ThingSetMaker thingSetMaker = new ThingSetMaker_MarketValue();
                        ThingSetMakerParams param = new ThingSetMakerParams();
                        param.totalMarketValueRange = new FloatRange((float)randomBudget, (float)(randomBudget + getTotalTitheModifierForWorkers()));
                        param.filter = randomTitheFilter;
                        param.techLevel = FactionCache.PlayerColonyFaction.def.techLevel;

                        LogUtil.Message($"  randomTitheFilter has {randomTitheFilter.AllowedDefCount} allowed items");

                        TechLevel tmplevel = TechLevel.Undefined;
                        ThingSetMaker tmp = def.GetModExtension<ResourceFilterExtension>()?.getThingSetMaker(out tmplevel);
                        if (tmp != null)
                        {
                            thingSetMaker = tmp;
                            param.techLevel = tmplevel;
                        }
                        else
                        {
                            param.countRange = new IntRange(def.titheMinCount, def.titheMaxCountBase + (def.titheMaxCountScaler * assignedWorkers));
                        }
                        randomTitheList = thingSetMaker.Generate(param);

                        if (randomTitheList is null || randomTitheList.Count == 0)
                        {
                            LogUtil.Message($"Resource {label} in settlement {settlement.Name} generated an empty random tithe list");
                        }
                        else
                        {
                            for(int i = 0; i < randomTitheList.Count; i++)
                            {
                                LogUtil.Message($"  randomTitheList[{i}]: {randomTitheList[i].LabelCap}");
                            }
                            titheItems.AddRange(randomTitheList);
                            randomTitheStock = 0;
                        }
                    }
                    else
                    {
                        randomTitheStock = randomBudget;
                        Find.LetterStack.ReceiveLetter("NoTitheLetterLabel".Translate(settlement.Name), "NoTitheLetterDesc2".Translate(settlement.Name, label, randomTitheStock), LetterDefOf.NeutralEvent);
                    }

                }
            }

            // Now handle specified tithes. We (should) have called pruneTitheList before this, so we shouldn't have to worry about the math adding up
            if (tithes.Count > 0)
            {
                /* Iterate over the dictionary, creating a new thing for each entry, and adding each such thing to the list of tithe items */
                foreach (ThingQualityTuple key in tithes.Keys)
                {
                    int quantity = tithes[key];
                    if (quantity == 0)
                    {
                        continue;
                    }

                    /* Try to generate the list through the resource's ResourceFilterExtension */
                    List<Thing> things = def.GetModExtension<ResourceFilterExtension>()?.generateSpecificThings(key.thingDef, quantity, key.quality, key.stuffDef);
                    if (things is null)
                    {
                        /* If we're here, then the resource doesn't have a special implementation for generateSpecificThings(). So try to make things the generic way. */
                        for (int i = 0; i < quantity; i++)
                        {
                            Thing thing = ThingMaker.MakeThing(key.thingDef, key.stuffDef);

                            if (CraftUtil.thingHasQuality(key.thingDef))
                            {
                                CompQuality thingQuality = thing.TryGetComp<CompQuality>();
                                thingQuality.SetQuality(key.quality, ArtGenerationContext.Outsider);
                            }
                            titheItems.Add(thing);
                        }
                    }
                    else
                    {
                        titheItems.AddRange(things);
                    }
                }
            }

            // Post-tax generation hook
            def.GetModExtension<ResourceTaxExtension>()?.OnPostTaxGeneration(this, settlement, titheItems, ref outSilver);

            extraSilver = outSilver;
            return titheItems;
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

    /// <summary>
    /// Pure calculation methods for resource production and tithe economics.
    /// These methods have zero RimWorld dependencies, making them unit-testable.
    /// </summary>
    public static class ResourceFormulas
    {
        /// <summary>
        /// Calculates the total production base from additive bonuses.
        /// </summary>
        public static double CalculateProductionBase(IEnumerable<double> additiveValues)
        {
            return additiveValues.Sum();
        }

        /// <summary>
        /// Calculates the total production multiplier from multiplier bonuses and tax bonus.
        /// </summary>
        public static double CalculateProductionMult(IEnumerable<double> multiplierValues, double taxBonus)
        {
            double result = 1;
            foreach (double value in multiplierValues)
            {
                result *= value;
            }
            return result * taxBonus;
        }

        /// <summary>
        /// Calculates total production: base × multiplier.
        /// </summary>
        public static double CalculateProduction(double productionBase, double productionMult)
        {
            return productionBase * productionMult;
        }

        /// <summary>
        /// Calculates raw total production: production per worker × assigned workers.
        /// </summary>
        public static double CalculateRawTotalProduction(double production, int assignedWorkers)
        {
            return production * assignedWorkers;
        }

        /// <summary>
        /// Converts raw production to market value.
        /// </summary>
        public static double CalculateMarketValue(double rawTotalProduction, double silverPerResource)
        {
            return rawTotalProduction * silverPerResource;
        }

        /// <summary>
        /// Calculates the per-worker tithe modifier: additive × multiplicative.
        /// </summary>
        public static double CalculateTitheModifierPerWorker(double additive, double multiplicative)
        {
            return additive * multiplicative;
        }

        /// <summary>
        /// Calculates total tithe modifier for all workers: modifier per worker × worker count.
        /// </summary>
        public static double CalculateTotalTitheModifierForWorkers(double modifierPerWorker, int assignedWorkers)
        {
            return modifierPerWorker * assignedWorkers;
        }

        /// <summary>
        /// Calculates tithe income: (rawMarketValue + workerMods + additiveForTotal) × multForTotal.
        /// </summary>
        public static double CalculateTitheIncome(double rawMarketValue, double totalWorkerMod, double additiveForTotal, double multForTotal)
        {
            return (rawMarketValue + totalWorkerMod + additiveForTotal) * multForTotal;
        }

        /// <summary>
        /// Calculates how many of a thing can be afforded within a budget.
        /// </summary>
        public static int MaxThingCanAfford(double budget, float thingValue)
        {
            return (int)(budget / thingValue);
        }

        /// <summary>
        /// Checks whether a thing amount fits within the available budget.
        /// </summary>
        public static bool CanAffordThingAmount(float thingTotalValue, double availableBudget)
        {
            return thingTotalValue <= availableBudget;
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
        public IEnumerable<FloatMenuOption> GetFactionMenuFloatMenuOptions()
        {
            return resource.GetFactionMenuFloatMenuOptions(this);
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref resource, "resource");
            Scribe_Values.Look(ref pool, "pointpool");
        }
    }

    public class ThingQualityTuple : IExposable, IEquatable<ThingQualityTuple>
    {
        public ThingDef thingDef;
        public QualityCategory quality;
        public ThingDef stuffDef;
        public ThingQualityTuple()
        {
        }
        public string listRejectionMessage()
        {
            if (CraftUtil.thingHasQuality(thingDef))
            {
                if (CraftUtil.thingIsStuffable(thingDef))
                {
                    return "TitheListRejectionStuffQuality".Translate(thingDef.LabelCap, TextUtil.GetQualityLabelCap(quality), stuffDef.LabelCap);
                }
                else
                {
                    return "TitheListRejectionQuality".Translate(thingDef.LabelCap, TextUtil.GetQualityLabelCap(quality));
                }
            }
            else
            {
                if (CraftUtil.thingIsStuffable(thingDef))
                {
                    return "TitheListRejectionStuff".Translate(thingDef.LabelCap, stuffDef.LabelCap);
                }
                else
                {
                    return "TitheListRejection".Translate(thingDef.LabelCap);
                }
            }
        }
        /* IExposable functions */
        public void ExposeData()
        {
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look(ref quality, "quality");
            Scribe_Defs.Look(ref stuffDef, "stuffDef");
        }

        /* IExposable functions end */

        /* IEquatable functions */
        public bool Equals(ThingQualityTuple other)
        {
            if (other is null)
            {
                return false;
            }
            return other.thingDef == this.thingDef && other.quality == this.quality && other.stuffDef == this.stuffDef;
        }
        public override bool Equals(object obj)
        {
            if (obj is ThingQualityTuple)
            {
                return Equals(obj as ThingQualityTuple);
            }
            return false;
        }
        public override int GetHashCode() => HashCode.Combine(thingDef, quality, stuffDef);
        public static bool operator ==(ThingQualityTuple t1, ThingQualityTuple t2)
        {
            if (t1 is null)
            {
                return t2 is null;
            }
            return t1.Equals(t2);
        }
        public static bool operator !=(ThingQualityTuple t1, ThingQualityTuple t2)
        {
            if (t1 is null)
            {
                return t2 is null;
            }
            return !t1.Equals(t2);
        }
        /* IEquatable functions end */
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
                    FactionFC factionFC = FactionCache.FactionComp;
                    double resource = 0;
                    for (int k = 0; k < factionFC.settlements.Count; k++)
                    {
                        resource += (int)(factionFC.settlements[k].getResource(resourceDef)?.rawTotalProduction ?? 0);
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
