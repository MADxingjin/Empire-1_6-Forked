using FactionColonies.util;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    /// <summary>
    ///     WorldObject that in many ways re-implements Settlement.cs from Rimworld.Planet. May cause compatibility issues with
    ///     other mods that rely on finding Settlement objects on the world map. Recommend testing this extensively with mods
    ///     like SoS2, RimWar, or any mods that modify, collect, or deep save world objects before publishing changes
    /// </summary>
    public class WorldSettlementFC : Settlement
    {
        private string name;
        private string nameShort;
        private string nameOriginal;
        public string title = "Hamlet".Translate();
        public string description = "FCGenericError".Translate();
        private int foundingTick;
        public int FoundingTick => foundingTick;

        /*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
         * ~        Settlement Base Info         ~ *
         *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*/
        public int settlementLevel = 1;
        /* Workers */
        public double workers;
        public double workersMax;
        public double workersUltraMax;
        public double workerCost;
        public double workerTotalUpkeep;
        /* Social Stats */
        public double unrest;
        public double loyalty = 100;
        public double happiness = 100;
        public double prosperity = 100;

        //public List<BuildingFCDef> buildings = new List<BuildingFCDef>();
        /// <summary>
        /// List of traits that apply to this settlement.
        /// <para>This field should never be accessed directly. Adding or removing traits should always be done through the addTrait, addTraits, removeTrait, or removeTraits functions.</para>
        /// </summary>
        private List<FCTraitEffectDef> traits = new List<FCTraitEffectDef>();
        public List<FCTraitEffectDef> Traits => traits;
        private Dictionary<(string, Operation), double> cachedTraitValues = new Dictionary<(string, Operation), double>();
        private Dictionary<(string, Operation), string> cachedTraitDescs = new Dictionary<(string, Operation), string>();

        public List<FCPrisoner> prisonerList = new List<FCPrisoner>();

        public float oneTimeSilverIncome;
        public List<Thing> tithe = new List<Thing>();
        public int titheEstimatedIncome;

        public string biome;
        public BiomeResourceDef biomeDef;

        public bool isUpgrading = false;
        public int startUpgradeTick = -1;
        public int finishUpgradeTick = -1;

        //ui only
        public double totalUpkeep;
        public string upkeepExp = "";
        public double totalIncome;
        public string incomeExp = "";
        public double totalProfit;

        //Trait stuff
        public int trait_Egalitarian_TaxBreak_Tick;
        public bool trait_Egalitarian_TaxBreak_Enabled;

        // Jealously guard our resources. Only we can modify them!
        private List<ResourceFC> resources = new List<ResourceFC>();
        public List<ResourceFC> Resources => resources;
        private List<ThingDef> grandThingList = new List<ThingDef>();
        private bool dirtyGrandThingListFlag = true;

        // Comp caching for the most-frequently accessed comps
        private WorldObjectComp_SettlementMilitary cachedMilitaryComp = null;
        private bool checkedMilitaryComp = false;
        private WorldObjectComp_SettlementBuildings cachedBuildingsComp = null;
        private bool checkedBuildingsComp = false;

        // A private state variable
        private bool calculatingTax = false;
        public bool IsCalculatingTax => calculatingTax;
        public WorldObjectComp_SettlementMilitary MilitaryComp
        {
            get
            {
                if (!checkedMilitaryComp)
                {
                    cachedMilitaryComp = GetComponent<WorldObjectComp_SettlementMilitary>();
                    checkedMilitaryComp = true;
                    if (cachedMilitaryComp == null)
                    {
                        LogUtil.Warning($"Attempted to access settlement {Name}'s MilitaryComp, but it doesn't have one");
                    }
                }
                return cachedMilitaryComp;
            }
        }
        public WorldObjectComp_SettlementBuildings BuildingsComp
        {
            get
            {
                if (!checkedBuildingsComp)
                {
                    cachedBuildingsComp = GetComponent<WorldObjectComp_SettlementBuildings>();
                    checkedBuildingsComp = true;
                    if (cachedBuildingsComp == null)
                    {
                        LogUtil.Warning($"Attempted to access settlement {Name}'s BuildingsComp, but it doesn't have one");
                    }
                }
                return cachedBuildingsComp;
            }
        }
        public int settlementMilitaryLevel
        {
            get
            {
                if (!(MilitaryComp is null))
                {
                    return MilitaryComp.settlementMilitaryLevel;
                }
                return 0;
            }
            set
            {
                if (!(MilitaryComp is null))
                {
                    MilitaryComp.settlementMilitaryLevel = value;
                }
                else
                {
                    LogUtil.Warning($"Settlement {Name} does not have a MilitaryComp, but tried to set its settlementMilitaryLevel to {value}");
                }
            }
        }

        public string ShortName
        {
            get
            {
                if (!nameShort.NullOrEmpty()) return nameShort;

                nameShort = TextGen.ToShortName(name);

                return nameShort;
            }
            set => nameShort = value.NullOrEmpty() ? name : value;
        }

        public string OriginalName
        {
            get => nameOriginal;
            private set => nameOriginal = value;
        }

        private string cachedlocationText = string.Empty;
        public string locationText
        {
            get
            {
                if (cachedlocationText.NullOrEmpty())
                {
                    cachedlocationText = settlementDef.GetModExtension<SettlementTypeExtension>().getLocationText(this);
                }
                return cachedlocationText;
            }
            set
            {
                cachedlocationText = value;
            }
        }

        public static readonly FieldInfo traitCachedIcon = typeof(WorldObjectDef).GetField("expandingIconTextureInt",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly FieldInfo traitCachedMaterial = typeof(WorldObjectDef).GetField("material",
            BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        ///     A flag meant to indicate whether or not this settlement is meant for actual destruction; used to override
        ///     WorldObject.Destroy() for compatibility purposes
        /// </summary>
        private bool destroyFlag;

        public new WorldSettlementTraderTracker trader;

        public new string Name
        {
            get
            {
                return name ?? (name = "");
            }
            set => name = value;
        }

        public override string Label => Name;


        public new TraderKindDef TraderKind
        {
            get
            {
                if (trader.settlement == null) trader.settlement = this;
                return trader?.TraderKind;
            }
        }

        public new IEnumerable<Thing> Goods => trader?.StockListForReading;

        public new int RandomPriceFactorSeed => trader?.RandomPriceFactorSeed ?? 0;

        public new string TraderName => trader?.TraderName;

        public new bool CanTradeNow => trader != null && trader.CanTradeNow;

        public new float TradePriceImprovementOffsetForPlayer => trader?.TradePriceImprovementOffsetForPlayer ?? 0.0f;

        public new TradeCurrency TradeCurrency => TraderKind.tradeCurrency;

        public new bool EverVisited => trader.EverVisited;

        public new bool RestockedSinceLastVisit => trader.RestockedSinceLastVisit;

        public new int NextRestockTick => trader.NextRestockTick;
        public WorldSettlementDef settlementDef => def as WorldSettlementDef;

        /// <summary>
        ///     Indicate that this should be destroyed when WorldObject.Destroy() is called
        /// </summary>
        public void PrepareDestroy()
        {
            destroyFlag = true;
        }

        /// <summary>
        ///     Compatibility focused: this object should only be destroyed very deliberately, else another object is likely trying
        ///     to handle negative combat resolution against this settlement.
        /// </summary>
        public override void Destroy()
        {
            if (MilitaryComp != null)
            {
                MilitaryComp.endBattle(false, 0);
            }

            if (destroyFlag)
            {
                base.Destroy();
            }
        }

        public void InvalidateCache()
        {
            InvalidateTraitCache();
            cachedlocationText = null;
            cachedBuildingsComp = null;
            checkedBuildingsComp = false;
            cachedMilitaryComp = null;
            checkedMilitaryComp = false;
        }
        public void InvalidateTraitCache()
        {
            cachedTraitDescs.Clear();
            cachedTraitValues.Clear();
        }

        /// <summary>
        /// Handles the setting up of a settlement's resources. Allows for adding or removing resources after settlement creation (such as if the resource itself has
        /// a techlevel or research restriction)
        /// </summary>
        /// <param name="techlevel"></param>
        public void PrepareResources(TechLevel techlevel)
        {
            foreach (ResourceBonuses rtd in settlementDef.resources)
            {
                bool resourceAllowed = biomeDef.getBiomeResource(rtd.resourceDef) != null && rtd.resourceDef.ResourceTypeAllowedByTech(techlevel);
                ResourceFC res = resources.Find((ResourceFC rfc) => rfc.def == rtd.resourceDef);
                if (res is null && resourceAllowed)
                {
                    LogUtil.Message($"Adding resource {rtd.resourceDef.label} to settlement {Name}");
                    /* ResourceFC initialization takes care of biome bonuses, so no need to handle that up here */
                    resources.Add(new ResourceFC(rtd.resourceDef, this));
                }
                else if (!(res is null) && !resourceAllowed)
                {
                    LogUtil.Message($"Removing resource {rtd.resourceDef.label} from settlement {Name}");
                    resources.Remove(res);
                }
                else if (!(res is null))
                {
                    res.setDirtyCache();
                }
            }
            resources.Sort(ResourceFC.sortForUI);
        }

        public override void PostMake()
        {
            trader = new WorldSettlementTraderTracker(this);

            if (!(def is WorldSettlementDef))
            { 
                LogUtil.Error($"Created settlement {name} with an invalid def: {def}! Panic! Defaulting to base def!");
                def = WorldSettlementDefOf.WorldSettlementDef_Surface;
            }
            FactionFC faction = FactionCache.FactionComp;
            Name = settlementDef.getSettlementTypeExtension().getSettlementName();

            updateTechIcon();
            def.expandingIconTexture = "FactionIcons/" + faction.factionIconPath;
            traitCachedIcon.SetValue(def, ContentFinder<Texture2D>.Get(def.expandingIconTexture));
            base.PostMake();

            LogUtil.Message($"Created world settlement {Name} with def {def}");
        }
        /// <summary>
        /// Handles necessary post-PostMake processing that requires the Tile field to be set.
        /// </summary>
        /// <param name="tile"></param>
        public void PostPostMake(PlanetTile tile)
        {
            FactionFC faction = FactionCache.FactionComp;
            this.Tile = tile;

            settlementLevel = 1;

            //Efficiency Multiplier
            workers = 0;
            workersMax = settlementDef.workersMaxBase + (settlementLevel * settlementDef.workersMaxMult) + returnMaxWorkersFromPrisoners();
            workersUltraMax = workersMax + settlementDef.workersUltraMaxBase + (settlementLevel * settlementDef.workersUltraMaxMult) + returnOverMaxWorkersFromPrisoners();

            biome = Tile.Tile.PrimaryBiome.defName;
            bool useTileBiome = true;

            if (settlementDef.biomeResourceOverride != null)
            {
                LogUtil.Message($"Using biome {settlementDef.biomeResourceOverride.defName} as override for settlement {Name} of type {settlementDef}");
                useTileBiome = false;
                biomeDef = settlementDef.biomeResourceOverride;
                if (!DefDatabase<BiomeResourceDef>.AllDefs.Contains(biomeDef))
                {
                    LogUtil.Error($"Settlement {Name} of type {settlementDef.LabelCap} has invalid override biome. Falling back onto tile biome");
                    biomeDef = BiomeResourceDefOf.defaultBiome;
                    useTileBiome = true;
                }
            }
            if (useTileBiome)
            {
                //modded biomes handling
                biomeDef = DefDatabase<BiomeResourceDef>.GetNamed(biome, false) ?? BiomeResourceDefOf.defaultBiome;
                LogUtil.Message($"Founding settlement {Name} on biome {biomeDef.LabelCap}");
            }

            BuildingsComp?.InitBuildings();

            PrepareResources(faction.techLevel);

            /* If the settlement has inherent traits, add them here. */
            if (settlementDef.traits.Count > 0)
            {
                addTraits(settlementDef.traits);
            }

            updateProfitAndProduction();

            foundingTick = Find.TickManager.TicksGame;
        }
        public string GetFoundingDate(bool full = true)
        {
            if (full)
            {
                return GenDate.DateFullStringAt(foundingTick, FactionCache.FactionComp?.StartingLongLat ?? default(Vector2));
            }
            else
            {
                return GenDate.DateShortStringAt(foundingTick, FactionCache.FactionComp?.StartingLongLat ?? default(Vector2));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref trader, "trader", this);
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref foundingTick, "foundingTick", defaultValue: 0);
            Scribe_Values.Look(ref nameShort, "nameShort", ShortName);
            Scribe_Values.Look(ref nameOriginal, "nameOriginal", OriginalName);
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref workers, "workers");
            Scribe_Values.Look(ref workersMax, "workersMax");
            Scribe_Values.Look(ref workersUltraMax, "workersUltraMax");
            Scribe_Values.Look(ref settlementLevel, "settlementLevel");
            Scribe_Values.Look(ref unrest, "unrest");
            Scribe_Values.Look(ref loyalty, "loyalty");
            Scribe_Values.Look(ref happiness, "happiness");
            Scribe_Values.Look(ref prosperity, "prosperity");
            Scribe_Values.Look(ref workerCost, "workerCost");
            Scribe_Values.Look(ref workerTotalUpkeep, "workerTotalUpkeep");

            Scribe_Collections.Look(ref resources, "resources", LookMode.Deep);

            //Taxes
            Scribe_Collections.Look(ref tithe, "tithe", LookMode.Deep);
            Scribe_Values.Look(ref titheEstimatedIncome, "titheEstimatedIncome");
            Scribe_Values.Look(ref oneTimeSilverIncome, "silverIncome");


            //Traits
            Scribe_Collections.Look(ref traits, "traits", LookMode.Def);

            //Biome_info
            Scribe_Values.Look(ref biome, "biome");
            Scribe_Defs.Look(ref biomeDef, "biomedef");

            Scribe_Values.Look(ref isUpgrading, "isupgrading", defaultValue: false);
            Scribe_Values.Look(ref startUpgradeTick, "startupgradetick", -1);
            Scribe_Values.Look(ref finishUpgradeTick, "finishupgradetick", -1);


            //Military


            //Prisoners
            Scribe_Collections.Look(ref prisonerList, "prisonerList", LookMode.Deep);

            //Traits
            Scribe_Values.Look(ref trait_Egalitarian_TaxBreak_Tick, "trait_Egalitarian_TaxBreak_Tick");
            Scribe_Values.Look(ref trait_Egalitarian_TaxBreak_Enabled, "trait_Egalitarian_TaxBreak_Enabled");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (trader != null && trader.settlement == null) trader.settlement = this;
                updateProfitAndProduction();
            }
        }

        public void updateTechIcon()
        {
            var techLevel = FactionCache.FactionComp.techLevel;
            LogUtil.Message("Got tech level " + techLevel);
            if (techLevel == TechLevel.Animal || techLevel == TechLevel.Neolithic)
                def.texture = "World/WorldObjects/TribalSettlement";
            else
                def.texture = "World/WorldObjects/DefaultSettlement";

            traitCachedMaterial.SetValue(def, MaterialPool.MatFrom(def.texture,
                ShaderDatabase.WorldOverlayTransparentLit, WorldMaterials.WorldObjectRenderQueue));
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            foreach (Gizmo gizmo in base.GetCaravanGizmos(caravan))
            {
                yield return gizmo;
            }
            if (MilitaryComp?.isUnderAttack != true)
            {
                trader.settlement = trader.settlement ?? this;
                var kindDef = trader.TraderKind;
                var action = (Command_Action)CaravanVisitUtility.TradeCommand(caravan, Faction, kindDef);

                var bestNegotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction, kindDef);
                action.action = () =>
                {
                    if (!CanTradeNow)
                        return;
                    Find.WindowStack.Add(new Dialog_Trade(bestNegotiator, this));
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(Goods.OfType<Pawn>(),
                        "LetterRelatedPawnsTradingWithSettlement"
                            .Translate((NamedArgument)Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent);
                };

                yield return action;
            }
            foreach (WorldObjectComp comp in AllComps)
            {
                foreach (Gizmo gizmo in comp.GetCaravanGizmos(caravan))
                {
                    yield return gizmo;
                }
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            if (MilitaryComp == null || !MilitaryComp.isUnderAttack)
                foreach (var option in WorldSettlementTradeAction.GetFloatMenuOptions(caravan, this))
                    yield return option;
        }

        protected override void Tick()
        {
            base.Tick();
            trader?.TraderTrackerTick();

            if (trait_Egalitarian_TaxBreak_Enabled &&
                Find.TickManager.TicksGame >= trait_Egalitarian_TaxBreak_Tick + GenDate.TicksPerDay * 10)
                trait_Egalitarian_TaxBreak_Enabled = false;

            foreach (FCTraitEffectDef trait in traits)
                trait.GetModExtension<FCTraitEffectModExtension>()?.Tick(this);
        }

        public void PublicTick()
        {
            Tick();
        }
        /*public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            //yield return OpenSettlementWindowAction;
            //if (settlement.isUnderAttack) yield return DefendColonyAction;
            //if (settlement.isUnderAttack && !attackers.Any()) yield return ChangeDefenderAction;
            //var containsShuttlePort = settlement.buildings.Contains(BuildingFCDefOf.shuttlePort);
            //if (containsShuttlePort) yield return RequestShuttleAction;
            //if (containsShuttlePort) yield return RequestShuttleForCaravanAction;
        }*/

        public override bool ShouldRemoveMapNow(out bool removeWorldObject)
        {
            removeWorldObject = false;
            if (MilitaryComp?.isUnderAttack == true) return false;
            return MilitaryComp is null || !(MilitaryComp.defenders.Any() || MilitaryComp.attackers.Any());
        }

        public void addPrisoner(Pawn prisoner)
        {
            prisonerList.Add(new FCPrisoner(prisoner, this));
        }

        public void upgradeSettlement(int times = 1)
        {
            settlementLevel += times;
            if (settlementLevel > FCSettings.settlementMaxLevel ||
                settlementLevel > settlementDef.maxSettlementLevel)
            {
                settlementLevel = FCSettings.settlementMaxLevel;
            }
            if (settlementLevel < 0) settlementLevel = 0;
            updateStats();
        }

        public void delevelSettlement(int times = -1)
        {
            upgradeSettlement(times);
        }

        public void GainUnrestWithReason(Message message, double amount)
        {
            Messages.Message(message);
            unrest += amount * getFieldValue("unrestGainedMultiplier", Operation.Multiplication);
        }
        public void GainUnrest(double amount)
        {
            unrest += amount * getFieldValue("unrestGainedMultiplier", Operation.Multiplication);
        }

        public void GainHappiness(double amount)
        {
            happiness += amount * getFieldValue("happinessLostMultiplier", Operation.Multiplication);
        }

        public void updateProfitAndProduction() //updates both profit and production
        {
            updateProfit();
            updateStats();
        }

        // TODO: will need rework after converting faction traits to comps
        public void updateStats()
        {
            FactionFC factionFc = FactionCache.FactionComp;

            int isolationistExtraWorkers = 0;
            if (factionFc.hasPolicy(FCPolicyDefOf.isolationist))
                isolationistExtraWorkers += 3;

            int SlaverExtraWorkers = 0;
            if (factionFc.hasPolicy(FCPolicyDefOf.slaver))
                SlaverExtraWorkers += 2;

            //Military Settlement Level
            settlementMilitaryLevel = settlementLevel - 1 + Convert.ToInt32(getFieldValue("militaryBaseLevel", Operation.Addition));

            //Worker Stats
            workersMax = settlementDef.workersMaxBase + (settlementLevel * (settlementDef.workersMaxMult + isolationistExtraWorkers + SlaverExtraWorkers)) +
                         getFieldValue("workerBaseMax", Operation.Addition) + returnMaxWorkersFromPrisoners();
            workersUltraMax = workersMax + settlementDef.workersUltraMaxBase - SlaverExtraWorkers + (settlementLevel * settlementDef.workersUltraMaxMult) +
                              getFieldValue("workerBaseOverMax", Operation.Addition) + returnOverMaxWorkersFromPrisoners();

        }
        public void updateProfit() //updates profit
        {
            totalUpkeep = getTotalUpkeep();
            updateWorkerCost();
            totalIncome = getTotalIncome();
            totalProfit = Convert.ToInt32(totalIncome - totalUpkeep);
        }

        // TODO: will need rework after converting faction traits to comps
        public double getHappinessGain()
        {
            FactionFC factionfc = FactionCache.FactionComp;
            double happinessGainMultiplier = getFieldValue("happinessGainedMultiplier", Operation.Multiplication);

            double policyIncrease = 0;
            if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian) && trait_Egalitarian_TaxBreak_Enabled)
                policyIncrease = 2;


            return happinessGainMultiplier * (policyIncrease + FCSettings.happinessBaseGain + getFieldValue("happinessGainedBase", Operation.Addition));
        }
        public double getHappinessLoss()
        {
            double happinessLostMultiplier = getFieldValue("happinessLostMultiplier", Operation.Multiplication);
            return happinessLostMultiplier * (FCSettings.happinessBaseLost + getFieldValue("happinessLostBase", Operation.Addition));
        }
        public double getTotalHappinessGain()
        {
            return getHappinessGain() - getHappinessLoss();
        }
        public void updateHappiness()
        {
            happiness = SettlementFormulas.ClampStat(happiness, getTotalHappinessGain());
        }
        public string getHappinessDesc()
        {
            double happinessGain = getTotalHappinessGain();
            string desc = "";
            FactionFC factionfc = FactionCache.FactionComp;
            double policyIncrease = 0;
            if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian) && trait_Egalitarian_TaxBreak_Enabled)
                policyIncrease = 2;

            if (happinessGain >= 0)
            {
                desc = "SettlementStatGain".Translate(Math.Abs(happinessGain), "Happiness".Translate());
            }
            else
            {
                desc = "SettlementStatLoss".Translate(Math.Abs(happinessGain), "Happiness".Translate());
            }
            desc += "\n\n";
            string gain = "";
            if (FCSettings.happinessBaseGain != 0)
            {
                gain += TextUtil.colorizeAdditiveBonus(FCSettings.happinessBaseGain) + " - " + "BaseGain".Translate() + "\n";
            }
            if (policyIncrease > 0)
            {
                gain += TextUtil.colorizeAdditiveBonus(policyIncrease) + " - " + FCPolicyDefOf.egalitarian.LabelCap + "\n";
            }
            gain += getFieldDesc("happinessGainedBase", Operation.Addition);
            gain += getFieldDesc("happinessGainedMultiplier", Operation.Multiplication);
            if (!gain.NullOrEmpty())
            {
                desc += gain + "\n";
            }
            if (FCSettings.happinessBaseLost != 0)
            {
                desc += TextUtil.colorizeAdditiveBonus(FCSettings.happinessBaseLost, hardinvert: true) + " - " + "BaseLoss".Translate() + "\n";
            }
            desc += getFieldDesc("happinessLostBase", Operation.Addition, hardinvert: true);
            desc += getFieldDesc("happinessLostMultiplier", Operation.Multiplication, invert: true);

            return desc.Trim();
        }

        public double getLoyaltyGain()
        {
            double loyaltyGainMultiplier = getFieldValue("loyaltyGainedMultiplier", Operation.Multiplication);
            return loyaltyGainMultiplier * (FCSettings.loyaltyBaseGain + getFieldValue("loyaltyGainedBase", Operation.Addition));
        }
        public double getLoyaltyLoss()
        {
            double loyaltyLostMultiplier = getFieldValue("loyaltyLostMultiplier", Operation.Multiplication);
            return loyaltyLostMultiplier * (FCSettings.loyaltyBaseLost + getFieldValue("loyaltyLostBase", Operation.Addition));
        }
        public double getTotalLoyaltyGain()
        {
            return getLoyaltyGain() - getLoyaltyLoss();
        }
        public void updateLoyalty()
        {
            loyalty = SettlementFormulas.ClampStat(loyalty, getTotalLoyaltyGain());
        }
        public string getLoyaltyDesc()
        {
            double loyaltyGain = getTotalLoyaltyGain();
            string desc = "";
            if (loyaltyGain >= 0)
            {
                desc = "SettlementStatGain".Translate(Math.Abs(loyaltyGain), "Loyalty".Translate());
            }
            else
            {
                desc = "SettlementStatLoss".Translate(Math.Abs(loyaltyGain), "Loyalty".Translate());
            }
            desc += "\n\n";
            string gain = "";
            if (FCSettings.loyaltyBaseGain != 0)
            {
                gain += TextUtil.colorizeAdditiveBonus(FCSettings.loyaltyBaseGain) + " - " + "BaseGain".Translate() + "\n";
            }
            gain += getFieldDesc("loyaltyGainedBase", Operation.Addition);
            gain += getFieldDesc("loyaltyGainedMultiplier", Operation.Multiplication);
            if (!gain.NullOrEmpty())
            {
                desc += gain + "\n";
            }
            if (FCSettings.loyaltyBaseLost != 0)
            {
                desc += "\n" + TextUtil.colorizeAdditiveBonus(FCSettings.loyaltyBaseLost, hardinvert: true) + " - " + "BaseLoss".Translate() + "\n";
            }
            desc += getFieldDesc("loyaltyLostBase", Operation.Addition, hardinvert: true);
            desc += getFieldDesc("loyaltyLostMultiplier", Operation.Multiplication, invert: true);

            return desc.Trim();
        }

        // TODO: will need rework after converting faction traits to comps
        public double getProsperityGain()
        {
            FactionFC factionfc = FactionCache.FactionComp;
            double policyIncrease = 0;
            if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian) && trait_Egalitarian_TaxBreak_Enabled)
                policyIncrease = 2;

            return (policyIncrease + FCSettings.prosperityBaseRecovery + getFieldValue("prosperityBaseRecovery", Operation.Addition)); //Go through traits and add prosperity where needed
        }
        public void updateProsperity()
        {
            prosperity = SettlementFormulas.ClampStat(prosperity, getProsperityGain());
        }
        public string getProsperityDesc()
        {
            double prosperityGain = getProsperityGain();
            string desc = "";
            if (prosperityGain >= 0)
            {
                desc = "SettlementStatGain".Translate(Math.Abs(prosperityGain), "Prosperity".Translate());
            }
            else
            {
                desc = "SettlementStatLoss".Translate(Math.Abs(prosperityGain), "Prosperity".Translate());
            }
            desc += "\n\n";
            if (FCSettings.prosperityBaseRecovery != 0)
            {
                desc += TextUtil.colorizeAdditiveBonus(FCSettings.prosperityBaseRecovery) + " - " + "BaseRecovery".Translate() + "\n";
            }
            desc += getFieldDesc("prosperityBaseRecovery", Operation.Addition);

            return desc.Trim();
        }
        public double getUnrestGain()
        {
            double unrestGainMultiplier = getFieldValue("unrestGainedMultiplier", Operation.Multiplication);
            return unrestGainMultiplier * (FCSettings.unrestBaseGain + getFieldValue("unrestGainedBase", Operation.Addition)); //Go through traits and add unrest where needed
        }
        public double getUnrestLoss()
        {
            double unrestLostMultiplier = getFieldValue("unrestLostMultiplier", Operation.Multiplication);
            return unrestLostMultiplier * (FCSettings.unrestBaseLost + getFieldValue("unrestLostBase", Operation.Addition)); //Go through traits and remove unrest where needed
        }
        public double getTotalUnrestGain()
        {
            return getUnrestGain() - getUnrestLoss();
        }
        public void updateUnrest()
        {
            unrest += getTotalUnrestGain();

            unrest = Math.Round(Math.Clamp(unrest, 1, 100), 1);
        }
        public string getUnrestDesc()
        {
            double unrestGain = getTotalUnrestGain();
            string desc = "";
            if (unrestGain >= 0)
            {
                desc = "SettlementStatGain".Translate(Math.Abs(unrestGain), "Unrest".Translate());
            }
            else
            {
                desc = "SettlementStatLoss".Translate(Math.Abs(unrestGain), "Unrest".Translate());
            }
            desc += "\n\n";
            string gain = "";
            if (FCSettings.unrestBaseGain != 0)
            {
                gain += TextUtil.colorizeAdditiveBonus(FCSettings.unrestBaseGain, invert: true) + " - " + "BaseGain".Translate() + "\n";
            }
            gain += getFieldDesc("unrestGainedBase", Operation.Addition, invert: true);
            gain += getFieldDesc("unrestGainedMultiplier", Operation.Multiplication, invert: true);
            if (!gain.NullOrEmpty())
            {
                desc += gain + "\n";
            }
            if (FCSettings.unrestBaseLost != 0)
            {
                desc += TextUtil.colorizeAdditiveBonus(FCSettings.unrestBaseLost, invert: true, hardinvert: true) + " - " + "BaseLoss".Translate() + "\n";
            }
            desc += getFieldDesc("unrestLostBase", Operation.Addition, invert: true, hardinvert: true);
            desc += getFieldDesc("unrestLostMultiplier", Operation.Multiplication, invert: true);

            return desc.Trim();
        }
        public double getSettlementTaxBonus()
        {
            FactionFC faction = FactionCache.FactionComp;
            double bonus = 0;
            if (faction.hasPolicy(FCPolicyDefOf.egalitarian))
            {
                bonus += Math.Floor(happiness / 10);
                if (trait_Egalitarian_TaxBreak_Enabled)
                {
                    bonus -= 30;
                }
            }

            bonus += getFieldValue("taxBasePercentage", Operation.Addition);

            bonus += faction.getFactionWideTaxBonus();

            bonus = ((100d + bonus) / 100d);

            return bonus;
        }

        public double getTotalIncome() //return total income the of settlement
        {
            double income = 0;
            incomeExp = "";
            foreach (ResourceFC resource in resources)
            {
                if (resource.actualIncome > 0)
                {
                    income += resource.actualIncome;
                    incomeExp += "+" + Math.Round((resource.actualIncome),2).ToString() + " - " + resource.label + " " + "Income".Translate() + "\n";
                }
            }
            incomeExp = incomeExp.Trim();
            return income;
        }

        public int getTotalWorkers()
        {
            int totalWorkers = 0;
            foreach (ResourceFC resource in resources)
            {
                totalWorkers += resource.assignedWorkers;
            }

            if (totalWorkers > workersUltraMax)
            {
                while (totalWorkers > workersUltraMax)
                {
                    if (increaseWorkers(null, -1))
                    {
                        totalWorkers -= 1;
                    }
                }
            }

            return totalWorkers;
        }

        private bool CanStillModify(ResourceFC resource, int singleMod) => workers + singleMod <= workersUltraMax && workers + singleMod >= 0 && resource.assignedWorkers + singleMod <= workersUltraMax && resource.assignedWorkers + singleMod >= 0;

        public bool increaseWorkers(ResourceFC resource, int numWorkers)
        {
            int singleMod = (numWorkers > 0) ? 1 : -1;
            if (resource == null)
            {
                if (numWorkers >= 0 && workers <= workersUltraMax)
                {
                    return false;
                }

                while (workers > workersUltraMax)
                {
                    int num = Rand.RangeInclusive(0, resources.Count - 1);
                    if (resources[num].assignedWorkers > 0)
                    {
                        resources[num].assignedWorkers -= 1;
                        return true;
                    }
                }
            }
            else
            {
                while (CanStillModify(resource, singleMod))
                {
                    workers += singleMod;
                    resource.assignedWorkers += singleMod;
                    numWorkers -= singleMod;
                    if (numWorkers == 0)
                    {
                        updateProfitAndProduction();
                        FactionCache.FactionComp.updateTotalProfit();
                        return true;
                    }
                }
                updateProfitAndProduction();
                FactionCache.FactionComp.updateTotalProfit();
            }

            return false;
        }

        public double getBaseWorkerCost()
        {
            return (FCSettings.workerCost + getFieldValue("workerBaseCost", Operation.Addition));
            //add building/faction modifiers
        }

        public int buildingUpkeepModifier(BuildingFCDef building)
        {
            int reduction = 0;
            //For now, this does nothing. But if we add ways to reduce building upkeep at the settlement level, that math should go here.

            return reduction;
        }

        public double getTotalUpkeep() //returns total upkeep of the settlement
        {
            upkeepExp = "";
            workers = getTotalWorkers();
            double upkeep = 0;

            workerTotalUpkeep = SettlementFormulas.CalculateWorkerUpkeep(workers, workersMax, getBaseWorkerCost());
            if (workerTotalUpkeep > 0)
            {
                upkeepExp += "+" + Math.Round(workerTotalUpkeep,2).ToString() + " - " + "Workers".Translate() + "\n";
            }

            //add building upkeep

            upkeep += (workerTotalUpkeep);

            double buildingsUpkeep = BuildingsComp?.TotalUpkeep() ?? 0;
            if (buildingsUpkeep > 0)
            {
                upkeep += buildingsUpkeep;
                upkeepExp += "+" + Math.Round(buildingsUpkeep,2).ToString() + " - " + "Buildings".Translate() + "\n";
            }

            foreach (ResourceFC resource in resources)
            {
                if (resource.actualIncome < 0)
                {
                    upkeep += (-1) * resource.actualIncome;
                    upkeepExp += "+" + Math.Round((-1 * resource.actualIncome),2).ToString() + " - " + resource.label + " " + "Tithing".Translate() + "\n";
                }
            }

            upkeepExp = upkeepExp.Trim();
            return upkeep;
        }

        public void updateWorkerCost() //runs inside updateProfit to attach during updating
        {
            workerCost = workers == 0 ? getBaseWorkerCost() : (workerTotalUpkeep / workers);
        }

        public double getTotalProfit() //returns total profit (income - upkeep) of the settlement
        {
            return (getTotalIncome() - getTotalUpkeep());
        }
        /// <summary>
        /// Compatibility focused: this object should only be destroyed very deliberately, else another object is likely trying to handle negative combat resolution against this settlement.
        /// </summary>
        public void PrepareDestroyWorldObject()
        {
            PrepareDestroy();
        }

        public float Happiness
        {
            get { return (float)Math.Round(happiness, 1); }
        }

        public float Unrest
        {
            get { return (float)Math.Round(unrest, 1); }
        }

        public float Loyalty
        {
            get { return (float)Math.Round(loyalty, 1); }
        }

        public float Prosperity
        {
            get { return (float)Math.Round(prosperity, 1); }
        }

        public ResourceFC returnHighestResource()
        {
            double highest = -1;
            ResourceFC highestResource = null;

            foreach (ResourceFC resource in resources)
            {
                if (resource.actualIncome > highest)
                {
                    highest = resource.actualIncome;
                    highestResource = resource;
                }
            }

            return highestResource;
        }

        public double getDefenseBonus()
        {
            double defenseBonus = 0;
            foreach (ResourceFC resource in resources)
            {
                if (resource.def.aidsDefense && resource.effectiveRawTotalProduction > 0)
                {
                    defenseBonus += resource.effectiveRawTotalProduction;
                }
            }
            return defenseBonus;
        }

        //Seperated into its own function to make it easier to PostFix descriptions for potential submod-added biomes
        // There *has* to be a better way to dynamically retrieve these descriptions...
        private string getDescriptionBiome()
        {
            string desc = "";

            switch (biomeDef.defName)
            {
                case "BorealForest":
                    desc = "FCDescBorealForest".Translate();
                    break;
                case "Tundra":
                    desc = "FCDescTundra".Translate();
                    break;
                case "ColdBog":
                    desc = "FCDescColdBog".Translate();
                    break;
                case "IceSheet":
                    desc = "FCDescIceSheet".Translate();
                    break;
                case "SeaIce":
                    desc = "FCDescIceSheet".Translate();
                    break;
                case "TemperateForest":
                    desc = "FCDescTemperateForest".Translate();
                    break;
                case "TemperateSwamp":
                    desc = "FCDescTemperateSwamp".Translate();
                    break;
                case "TropicalRainforest":
                    desc = "FCDescTropicalRainforest".Translate();
                    break;
                case "AridShrubland":
                    desc = "FCDescAridShrubland".Translate();
                    break;
                case "Desert":
                    desc = "FCDescDesert".Translate();
                    break;
                case "ExtremeDesert":
                    desc = "FCDescExtremeDesert".Translate();
                    break;
                case "OrbitalSpace":
                    desc = "FCDescOrbitalSpace".Translate();
                    break;
                default:
                    desc = "FCDescUnknown".Translate();
                    break;
            }
            return desc;
        }
        private string getSettlementLevelDesc()
        {
            string desc = "";
            switch (settlementLevel)
            {
                case 1:
                    desc += "FCTownLevel1".Translate();
                    break;
                case 2:
                    desc += "FCTownLevel2".Translate();
                    break;
                case 3:
                case 4:
                    desc += "FCTownLevel3".Translate();
                    break;
                case 5:
                case 6:
                    desc += "FCTownLevel4".Translate();
                    break;
                case 7:
                case 8:
                default:
                    desc += "FCTownLevel5".Translate();
                    break;
            }
            return desc;
        }

        public void updateDescription()
        {
            //biome
            description = getDescriptionBiome() + "\n\n";

            //town size
            description += getSettlementLevelDesc();
        }

        public List<FCTraitEffectDef> returnListSettlementTraits()
        {
            List<FCTraitEffectDef> tmpList = new List<FCTraitEffectDef>();
            foreach (FCTraitEffectDef trait in traits)
            {
                tmpList.Add(trait);
            }

            return tmpList;
        }

        public void addTrait(FCTraitEffectDef trait, string id = "")
        {
            /* Add production bonuses */
            string traitId = trait.defName + id;
            foreach (ResourceBonuses resourcebonus in trait.resourceBonuses)
            {
                ResourceFC resource = getResource(resourcebonus.resourceDef);
                if (resource != null)
                {
                    if (resourcebonus.additive != 0)
                    {
                        resource.addProductionAdditive(traitId, resourcebonus.additive, trait.LabelCap);
                    }
                    if (resourcebonus.multiplier != 1)
                    {
                        resource.addProductionMultiplier(traitId, resourcebonus.multiplier, trait.LabelCap);
                    }
                }
            }
            traits.Add(trait);
            FCTraitEffectModExtension traitExt = trait.GetModExtension<FCTraitEffectModExtension>();
            if (traitExt != null)
            {
                try { traitExt.OnAppliedToSettlement(this); }
                catch (Exception e) { LogUtil.Error($"WorldSettlementFC.addTrait: OnAppliedToSettlement threw for '{trait.defName}': {e}"); }
            }
            InvalidateTraitCache();
        }

        public void addTraits(List<FCTraitEffectDef> traits, string id = "")
        {
            foreach (FCTraitEffectDef trait in traits)
            {
                addTrait(trait, id);
            }
            InvalidateTraitCache();
        }

        public bool removeTrait(FCTraitEffectDef trait, string id = "")
        {
            /* Remove production bonuses */
            string traitId = trait.defName + id;
            if (traits.Contains(trait))
            {
                foreach (ResourceBonuses resourcebonus in trait.resourceBonuses)
                {
                    ResourceFC resource = getResource(resourcebonus.resourceDef);
                    if (resource != null)
                    {
                        resource.removeProductionAdditiveById(traitId);
                        resource.removeProductionMultiplierById(traitId);
                    }
                }
                FCTraitEffectModExtension traitExt = trait.GetModExtension<FCTraitEffectModExtension>();
                if (traitExt != null)
                {
                    try { traitExt.OnRemovedFromSettlement(this); }
                    catch (Exception e) { LogUtil.Error($"WorldSettlementFC.removeTrait: OnRemovedFromSettlement threw for '{trait.defName}': {e}"); }
                }
                InvalidateTraitCache();
                return traits.Remove(trait);
            }
            else
            {
                return false;
            }
        }

        public void removeTraits(List<FCTraitEffectDef> traits, string id = "")
        {
            foreach (FCTraitEffectDef trait in traits)
            {
                removeTrait(trait, id);
            }
        }
        public void clearTraits()
        {
            List<FCTraitEffectDef> currentTraits = new List<FCTraitEffectDef>();
            currentTraits.AddRange(traits);
            removeTraits(currentTraits);
            traits.Clear();
            InvalidateTraitCache();
        }
        public double getFieldValue(string field, Operation addOrMultiply)
        {
            double value = 0;
            if (!cachedTraitValues.TryGetValue((field, addOrMultiply), out value))
            {
                value = TraitUtilsFC.cycleTraits(field, traits, addOrMultiply);
                cachedTraitValues.Add((field, addOrMultiply), value);
            }
            foreach (WorldObjectComp comp in AllComps)
            {
                if (comp is IStatModifierProvider provider)
                {
                    if (addOrMultiply == Operation.Addition)
                        value += provider.GetStatModifier(field, addOrMultiply);
                    else
                        value *= provider.GetStatModifier(field, addOrMultiply);
                }
            }
            return value;
        }
        public string getFieldDesc(string field, Operation addOrMultiply, bool invert = false, bool hardinvert = false)
        {
            string desc = "";
            if (!cachedTraitDescs.TryGetValue((field, addOrMultiply), out desc))
            {
                TraitUtilsFC.cycleTraits(field, traits, addOrMultiply, true, ref desc, invert, hardinvert);
                cachedTraitDescs.Add((field, addOrMultiply), desc);
            }
            foreach (WorldObjectComp comp in AllComps)
            {
                if (comp is IStatModifierProvider provider)
                {
                    desc += "\n" + provider.GetStatModifier(field, addOrMultiply);
                 
                }
            }
            return desc;
        }

        public void deconstructBuilding(int buildingSlot)
        {
            BuildingsComp?.DeconstructBuilding(buildingSlot);
        }

        private int returnMaxWorkersFromPrisoners()
        {
            int num = 0;
            foreach (FCPrisoner prisoner in prisonerList)
            {
                switch (prisoner.workload)
                {
                    case FCWorkLoad.Medium:
                        num++;
                        break;
                    case FCWorkLoad.Heavy:
                        num += 2;
                        break;
                }
            }

            return num;
        }

        private int returnOverMaxWorkersFromPrisoners()
        {
            //LogUtil.Message("max worker : " + num);
            return prisonerList.Count(prisoner => prisoner.workload == FCWorkLoad.Light);
        }


        public bool validConstructBuilding(BuildingFCDef building, int buildingSlot)
        {
            if (BuildingsComp == null)
            {
                return false;
            }
            return BuildingsComp.validConstructBuilding(building, buildingSlot);
        }


        public void constructBuilding(BuildingFCDef building, int buildingSlot)
        {
            if (BuildingsComp == null)
            {
                return;
            }
            BuildingsComp.ConstructBuilding(building, buildingSlot);
        }

        public ResourceFC returnResource(string defName) //used to return the correct resource based on string name
        {
            ResourceFC res = resources.Find((ResourceFC rfc) => rfc.def.defName == defName);
            if (res == null)
            {
                LogUtil.Message($"Requested resource {defName} is not in settlement {Name}'s resource list");
            }
            return res;
        }

        public ResourceFC getResource(ResourceTypeDef type) //used to return the correct resource based on string name
        {
            ResourceFC res = resources.Find((ResourceFC rfc) => rfc.def == type);
            if (res == null)
            {
                LogUtil.Message($"Requested resource {type.defName} is not in settlement {Name}'s resource list");
            }
            return res;
        }

        public ResourceFC getResourceByIndex(int index)
        {
            if (index >= resources.Count || index < 0)
            {
                return null;
            }
            for (int i = 0; i < resources.Count; i++)
            {
                if (i == index)
                    return resources[i];
            }
            LogUtil.Error($"Reached end of WorldSettmentFC.getResourceByIndex for settlement {Name} and resource index {index}. This should never happen.");
            return null;
        }
        public List<ResourceFC> getTitheableResources()
        {
            List<ResourceFC> list = new List<ResourceFC>();
            foreach (ResourceFC res in Resources)
            {
                if (res.canTithe)
                {
                    list.Add(res);
                }
            }
            return list;
        }
        /// <summary>
        /// Returns a list of *all* things that this settlement can produce.
        /// </summary>
        /// <returns></returns>
        public List<ThingDef> getGrandThingList()
        {
            if (dirtyGrandThingListFlag)
            {
                grandThingList = new List<ThingDef>();
                foreach (ResourceFC res in resources)
                {
                    if (!res.def.isPoolResource)
                    {
                        List<ThingDef> resList = res.generateThingDefList();
                        if (resList != null && resList.Count > 0)
                        {
                            grandThingList.AddRange(resList);
                        }
                    }
                }
                dirtyGrandThingListFlag = false;
            }
            return grandThingList;
        }
        public void dirtyGrandThingList()
        {
            dirtyGrandThingListFlag = true;
            FactionCache.FactionComp.dirtyGrandThingList();
        }

        public float getOneTimeSilverIncome()
        {
            return oneTimeSilverIncome;
        }

        public void resetOneTimeSilverIncome()
        {
            oneTimeSilverIncome = 0;
        }

        public void addOneTimeSilverIncome(float amount)
        {
            oneTimeSilverIncome += amount;
        }

        public float returnOneTimeSilverIncome(bool reset)
        {
            float income = oneTimeSilverIncome;

            if (reset)
            {
                resetOneTimeSilverIncome();
            }

            return income;
        }

        public void goTo()
        {
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;

            //Select Settlement Tile
            Find.WorldSelector.ClearSelection();
            Find.WorldSelector.Select(Find.WorldObjects.MapParentAt(Tile));
            if (Find.MainButtonsRoot.tabs.OpenTab != null)
            {
                Find.MainButtonsRoot.tabs.OpenTab.TabWindow.Close();
            }
        }
        public List<ResourcePool> createResourcePools()
        {
            List<ResourcePool> pools = new List<ResourcePool>();

            foreach(ResourceFC resource in resources)
            {
                if (resource.def.isPoolResource)
                {
                    ResourcePool pool = resource.createPool();
                    if (pool.pool != 0)
                    {
                        pools.Add(pool);
                    }
                }
            }

            return pools;
        }

        public double getTitheModifierPerWorker(ResourceTypeDef rdef)
        {
            double modifier = 0;

            modifier += getFieldValue("taxBaseRandomModifier", Operation.Addition);

            return modifier;
        }
        public double getTitheModifierForTotal(ResourceTypeDef rdef)
        {
            double modifier = 1;

            return modifier;
        }
        public double getTaxTimeTaxBoostFlat()
        {
            double flatBoost = 0;
            //Nothing here for now, but if we add a flat boost in the future, that code should go here
            return flatBoost;
        }
        public double getTaxTimeTaxBoostMult()
        {
            FactionFC faction = FactionCache.FactionComp;
            double multBoost = 1;

            //TODO: would like to modularize faction traits more
            double trait_Industrious_TaxPercentageBoost = 1;
            if (faction.hasTrait(FCPolicyDefOf.industrious))
            {
                int num = Rand.RangeInclusive(1, 20);
                if (num == 5)
                {
                    trait_Industrious_TaxPercentageBoost = 1f + (Rand.RangeInclusive(20, 50) / 100f);
                    multBoost *= trait_Industrious_TaxPercentageBoost;
                    Find.LetterStack.ReceiveLetter("FCIdustriousTaxBoost".Translate(), "FCIndustriousPop".Translate(Name, ((trait_Industrious_TaxPercentageBoost - 1f) * 100f) + "%"), LetterDefOf.PositiveEvent);
                }
            }

            return multBoost;
        }
        public void pruneResourceTithes()
        {
            foreach (ResourceFC res in resources)
            {
                if (res.canTithe)
                {
                    res.pruneTitheList();
                }
            }
        }
        public void dirtyResourceCache(ResourceTypeDef resDef)
        {
            ResourceFC res = getResource(resDef);
            if (!(res is null))
            {
                res.setDirtyCache();
            }
        }
        public void dirtyResourceCache(ResourceFC res)
        {
            if (!(res is null))
            {
                res.setDirtyCache();
            }
        }
        public void dirtyResourceCaches()
        {
            foreach (ResourceFC res in resources)
            {
                res.setDirtyCache();
            }
        }
        /// <summary>
        /// Handles any necessary pre-tax preparations to ensure that the tax calculation is up-to-date and accurate.
        /// </summary>
        private void preTaxPrep()
        {
            dirtyResourceCaches();
            foreach (ResourceFC res in resources)
                res.PruneStockpileAllocations();
            pruneResourceTithes();
            updateProfitAndProduction();
            calculatingTax = true;
        }
        private void postTaxPrep()
        {
            calculatingTax = false;
        }
        /// <summary>
        /// This function handles the calculations for determing this settlement's taxes at tax time. It handles both tithes and silver taxes.
        /// </summary>
        /// <param name="silverAmount">The amount of silver to tax; positive if the player gains silver, negative otherwise.</param>
        /// <returns>A list of things produced by tithing resources. May be empty if there are no tithes.</returns>
        public List<Thing> createTax(out int silverAmount)
        {
            preTaxPrep();

            FactionFC faction = FactionCache.FactionComp;
            double flatTaxBoost = getTaxTimeTaxBoostFlat();
            double multTaxBoost = getTaxTimeTaxBoostMult();
            List<Thing> titheThings = new List<Thing>();
            int tmpSilverAmount = (int)((((totalIncome + flatTaxBoost) * multTaxBoost) - totalUpkeep) + returnOneTimeSilverIncome(true));

            foreach (ResourceFC resource in resources)
            {
                if (resource.canTithe)
                {
                    int resExtraSilver = 0;
                    List<Thing> resTitheThings = resource.generateTithe(out resExtraSilver);

                    if (resTitheThings.Count > 0)
                    {
                        titheThings.AddRange(resTitheThings);
                    }
                    tmpSilverAmount += resExtraSilver;
                }
            }

            postTaxPrep();
            silverAmount = tmpSilverAmount;
            return titheThings;
        }
    }

    // NOTE: PawnGizmos patch moved to GizmosPatches.cs to avoid duplication
    // The optimized version in GizmosPatches.PawnDraftGizmos handles all pawn gizmo modifications
}