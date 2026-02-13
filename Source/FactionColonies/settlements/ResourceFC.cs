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
        /* NOTE: need to be very careful about which of rawTotalProduction, totalProduction, and actualIncome to use.
         *  * rawTotalProduction is the TOTAL production value of the resource, before accounting for tithes.
         *  * totalProductionMarketValue is the amount of production leftover after accounting for tithes.
         *  * actualIncome reports the actual income of the resource, accounting for tithes. This can be negative if the value of the tithes is
         *    greater than the leftover production of the resource.
         */
        public double rawTotalProduction => production * assignedWorkers;
        public double rawTotalProductionMarketValue => rawTotalProduction * FCSettings.silverPerResource;
        public double totalProductionMarketValue => rawTotalProductionMarketValue - titheTotalValue;
        public double actualIncome => totalProductionMarketValue - titheTotalValue;

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
        public double getTitheModifierAdditivePerWorker()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            return faction.getFactionTitheBonusAdditivePerWorker(def) + settlement.getTitheModifierPerWorker(def) + FCSettings.productionTitheMod;
        }
        public double getTitheModifierAdditiveForTotal()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            return faction.getFactionTitheBonusAdditiveForTotal(def);
        }
        public double getTitheModifierMultPerWorker()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            return faction.getFactionTitheBonusMultPerWorker(def);
        }
        public double getTitheModifierMultForTotal()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            return faction.getFactionTitheBonusMultForTotal(def);
        }
        public double getTitheModifierPerWorker()
        {
            return getTitheModifierAdditivePerWorker() * getTitheModifierMultPerWorker();
        }
        public double getTotalTitheModifierForWorkers()
        {
            return getTitheModifierPerWorker() * assignedWorkers;
        }
        public double getTitheIncome()
        {
            return (rawTotalProductionMarketValue + getTotalTitheModifierForWorkers() + getTitheModifierAdditiveForTotal()) * getTitheModifierMultForTotal();
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
            Find.World.GetComponent<FactionFC>()?.setDirtyResourceDisplayCache(def);
        }
        public void setDirtyRandomTitheCache()
        {
            dirtyRandomTitheCache = true;
            settlement.dirtyGrantThingList();
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
                pool.pool += def.GetModExtension<ResourcePoolExtension>().createPool(rawTotalProduction, settlement);
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

                FactionFC faction = Find.World.GetComponent<FactionFC>();
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
            if (quantity < 0)
            {
                LogUtil.Error($"Tried to add a negative quantity of objects to the tithes list for resource {def.LabelCap}. You should use decrementInTitheList() instead.");
                return false;
            }
            LogUtil.Message($"Resource {def.LabelCap} adding new thing to tithe list: {thing.thingDef.LabelCap} | {TextUtil.GetQualityLabelCap(thing.quality)} | {thing.stuffDef?.LabelCap ?? "null stuff"}");

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
                LogUtil.Message($"Resource {def.LabelCap} removing thing from tithe list: {thing.thingDef.LabelCap} | {TextUtil.GetQualityLabelCap(thing.quality)} | {thing.stuffDef?.LabelCap ?? "null stuff"}");
                tithes.Remove(thing);
                dirtyTitheCache = true;
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
            float value;
            if (CraftUtil.thingHasQuality(thing.thingDef))
            {
                value = StatDefOf.MarketValue.Worker.GetValue(StatRequest.For(thing.thingDef, thing.stuffDef, thing.quality));
            }
            else
            {
                value = StatWorker_MarketValue.CalculatedBaseMarketValue(thing.thingDef, thing.stuffDef);
            }
            return value;
        }
        public float titheThingTotalValue(ThingQualityTuple thing, int quanity)
        {
            return titheThingValue(thing) * quanity;
        }
        public bool canAffordThingAmount(ThingQualityTuple thing, int quanity)
        {
            return (titheThingTotalValue(thing, quanity) <= getTitheIncome() - titheTotalValue);
        }
        public int maxThingCanAfford(ThingQualityTuple thing)
        {
            return maxThingCanAfford(thing, getTitheIncome() - titheTotalValue);
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
        public List<Thing> generateTithe(out int extraSilver)//(double valueBase, double valueDiff, int multiplier, double traitValueMod)
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
                    if (minimum >= randomBudget)
                    {
                        List<Thing> randomTitheList = new List<Thing>();
                        ThingSetMaker thingSetMaker = new ThingSetMaker_MarketValue();
                        ThingSetMakerParams param = new ThingSetMakerParams();
                        double variance = getTitheModifierPerWorker();
                        param.totalMarketValueRange = new FloatRange((float)randomBudget, (float)(randomBudget + (variance * assignedWorkers)));
                        param.filter = randomTitheFilter;
                        param.techLevel = ColonyUtil.getPlayerColonyFaction().def.techLevel;

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

                        titheItems.AddRange(randomTitheList);

                        randomTitheStock = 0;
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
            }

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
