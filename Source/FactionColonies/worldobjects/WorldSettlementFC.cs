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
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.AccessControl;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Noise;
using Verse.Sound;
using static Mono.Security.X509.X520;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace FactionColonies
{
    /// <summary>
    ///     WorldObject that in many ways re-implements Settlement.cs from Rimworld.Planet. May cause compatibility issues with
    ///     other mods that rely on finding Settlement objects on the world map. Recommend testing this extensively with mods
    ///     like SoS2, RimWar, or any mods that modify, collect, or deep save world objects before publishing changes
    /// </summary>
    public class WorldSettlementFC : Settlement
    {
        // Tiles being ints is obsolete. Time to actually use PlanetTiles
        //public int mapLocation;
        private string name;
        private string nameShort;
        private string nameOriginal;
        public string title = "Hamlet".Translate();
        public string description = "FCGenericError".Translate();

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
        public List<FCPrisoner> prisonerList = new List<FCPrisoner>();

        public float silverIncome;
        public List<Thing> tithe = new List<Thing>();
        public int titheEstimatedIncome;

        public string biome;
        // we're replacing the hilliness biome def with a ResourceProductionExtension
        //public BiomeResourceDef hillinessDef;
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

        // Comp caching for the most-frequently accessed comps
        private WorldObjectComp_SettlementMilitary cachedMilitaryComp = null;
        private bool checkedMilitaryComp = false;
        private WorldObjectComp_SettlementBuildings cachedBuildingsComp = null;
        private bool checkedBuildingsComp = false;
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
                if (MilitaryComp != null)
                {
                    return MilitaryComp.settlementMilitaryLevel;
                }
                return 0;
            }
            set
            {
                if (MilitaryComp != null)
                {
                    MilitaryComp.settlementMilitaryLevel = value;
                }
                else
                {
                    LogUtil.Warning($"Settlement {Name} does not have a MilitaryComp, but tried to set its settlementMilitaryLevel to {value}");
                }
            }
        }


        //public static Biome biome;

        //Settlement Production Information
        public double productionEfficiency; //Between 0.1 - 1

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

        public bool IsBeingUpgraded => Find.World.GetComponent<FactionFC>().events.Any(evt => evt.def == FCEventDefOf.upgradeSettlement && evt.location == Tile);

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
                if (res == null && resourceAllowed)
                {
                    LogUtil.Message($"Adding resource {rtd.resourceDef.label} to settlement {Name}");
                    /* ResourceFC initialization takes care of biome bonuses, so no need to handle that up here */
                    resources.Add(new ResourceFC(rtd.resourceDef, this));
                }
                else if (res != null && !resourceAllowed)
                {
                    LogUtil.Message($"Removing resource {rtd.resourceDef.label} from settlement {Name}");
                    resources.Remove(res);
                }
                else
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
            FactionFC faction = Find.World.GetComponent<FactionFC>();
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
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            this.Tile = tile;

            settlementLevel = 1;

            //Efficiency Multiplier
            productionEfficiency = 1.0;
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
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref trader, "trader");
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref nameShort, "nameShort", ShortName);
            Scribe_Values.Look(ref nameOriginal, "nameOriginal", OriginalName);
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref productionEfficiency, "productionEfficiency");
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
            Scribe_Values.Look(ref silverIncome, "silverIncome");


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
        }

        public void updateTechIcon()
        {
            var techLevel = Find.World.GetComponent<FactionFC>().techLevel;
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
            foreach (Gizmo gizmo in base.GetGizmos())
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

            //TODO: rework faction traits to be comps or something
            if (trait_Egalitarian_TaxBreak_Enabled &&
                Find.TickManager.TicksGame >= trait_Egalitarian_TaxBreak_Tick + GenDate.TicksPerDay * 10)
                trait_Egalitarian_TaxBreak_Enabled = false;
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
            return MilitaryComp == null || !(MilitaryComp.defenders.Any() || MilitaryComp.attackers.Any());
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
            unrest += amount * TraitUtilsFC.cycleTraits("unrestGainedMultiplier", traits, Operation.Multiplication);
        }
        public void GainUnrest(double amount)
        {
            unrest += amount * TraitUtilsFC.cycleTraits("unrestGainedMultiplier", traits, Operation.Multiplication);
        }

        public void GainHappiness(double amount)
        {
            happiness += amount * TraitUtilsFC.cycleTraits("happinessLostMultiplier", traits, Operation.Multiplication);
        }

        public void updateProfitAndProduction() //updates both profit and production
        {
            updateProfit();
            updateStats();
        }

        // TODO: will need rework after converting faction traits to comps
        public void updateStats()
        {
            FactionFC factionFc = Find.World.GetComponent<FactionFC>();

            int isolationistExtraWorkers = 0;
            if (factionFc.hasPolicy(FCPolicyDefOf.isolationist))
                isolationistExtraWorkers += 3;

            int SlaverExtraWorkers = 0;
            if (factionFc.hasPolicy(FCPolicyDefOf.slaver))
                SlaverExtraWorkers += 2;

            //Military Settlement Level
            settlementMilitaryLevel = settlementLevel - 1 + Convert.ToInt32(TraitUtilsFC.cycleTraits("militaryBaseLevel", traits, Operation.Addition));

            //Worker Stats
            workersMax = settlementDef.workersMaxBase + (settlementLevel * (settlementDef.workersMaxMult + isolationistExtraWorkers + SlaverExtraWorkers)) +
                         TraitUtilsFC.cycleTraits("workerBaseMax", traits, Operation.Addition) + returnMaxWorkersFromPrisoners();
            workersUltraMax = workersMax + settlementDef.workersUltraMaxBase - SlaverExtraWorkers + (settlementLevel * settlementDef.workersUltraMaxMult) +
                              TraitUtilsFC.cycleTraits("workerBaseOverMax", traits, Operation.Addition) + returnOverMaxWorkersFromPrisoners();

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
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            double happinessGainMultiplier = TraitUtilsFC.cycleTraits("happinessGainedMultiplier", traits, Operation.Multiplication);

            double policyIncrease = 0;
            if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian) && trait_Egalitarian_TaxBreak_Enabled)
                policyIncrease = 2;


            return happinessGainMultiplier * (policyIncrease + FCSettings.happinessBaseGain + TraitUtilsFC.cycleTraits("happinessGainedBase", traits, Operation.Addition));
        }
        public double getHappinessLoss()
        {
            double happinessLostMultiplier = TraitUtilsFC.cycleTraits("happinessLostMultiplier", traits, Operation.Multiplication);
            return happinessLostMultiplier * (FCSettings.happinessBaseLost + TraitUtilsFC.cycleTraits("happinessLostBase", traits, Operation.Addition));
        }
        public double getTotalHappinessGain()
        {
            return getHappinessGain() - getHappinessLoss();
        }
        public void updateHappiness()
        {
            happiness += getTotalHappinessGain();

            happiness = Math.Round(Math.Clamp(happiness, 1, 100), 1);
        }
        public string getHappinessDesc()
        {
            double happinessGain = getTotalHappinessGain();
            string desc = "";
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
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
            TraitUtilsFC.cycleTraits("happinessGainedBase", traits, Operation.Addition, true, ref gain);
            TraitUtilsFC.cycleTraits("happinessGainedMultiplier", traits, Operation.Multiplication, true, ref gain);
            if (!gain.NullOrEmpty())
            {
                desc += gain + "\n";
            }
            if (FCSettings.happinessBaseLost != 0)
            {
                desc += TextUtil.colorizeAdditiveBonus(FCSettings.happinessBaseLost, hardinvert: true) + " - " + "BaseLoss".Translate() + "\n";
            }
            TraitUtilsFC.cycleTraits("happinessLostBase", traits, Operation.Addition, true, ref desc, hardinvert: true);
            TraitUtilsFC.cycleTraits("happinessLostMultiplier", traits, Operation.Multiplication, true, ref desc, invert: true);

            return desc.Trim();
        }

        public double getLoyaltyGain()
        {
            double loyaltyGainMultiplier = TraitUtilsFC.cycleTraits("loyaltyGainedMultiplier", traits, Operation.Multiplication);
            return loyaltyGainMultiplier * (FCSettings.loyaltyBaseGain + TraitUtilsFC.cycleTraits("loyaltyGainedBase", traits, Operation.Addition));
        }
        public double getLoyaltyLoss()
        {
            double loyaltyLostMultiplier = TraitUtilsFC.cycleTraits("loyaltyLostMultiplier", traits, Operation.Multiplication);
            return loyaltyLostMultiplier * (FCSettings.loyaltyBaseLost + TraitUtilsFC.cycleTraits("loyaltyLostBase", traits, Operation.Addition));
        }
        public double getTotalLoyaltyGain()
        {
            return getLoyaltyGain() - getLoyaltyLoss();
        }
        public void updateLoyalty()
        {
            loyalty += getTotalLoyaltyGain();

            loyalty = Math.Round(Math.Clamp(loyalty, 1, 100), 1);
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
            TraitUtilsFC.cycleTraits("loyaltyGainedBase", traits, Operation.Addition, true, ref gain);
            TraitUtilsFC.cycleTraits("loyaltyGainedMultiplier", traits, Operation.Multiplication, true, ref gain);
            if (!gain.NullOrEmpty())
            {
                desc += gain + "\n";
            }
            if (FCSettings.loyaltyBaseLost != 0)
            {
                desc += "\n" + TextUtil.colorizeAdditiveBonus(FCSettings.loyaltyBaseLost, hardinvert: true) + " - " + "BaseLoss".Translate() + "\n";
            }
            TraitUtilsFC.cycleTraits("loyaltyLostBase", traits, Operation.Addition, true, ref desc, hardinvert: true);
            TraitUtilsFC.cycleTraits("loyaltyLostMultiplier", traits, Operation.Multiplication, true, ref desc, invert: true);

            return desc.Trim();
        }

        // TODO: will need rework after converting faction traits to comps
        public double getProsperityGain()
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            double policyIncrease = 0;
            if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian) && trait_Egalitarian_TaxBreak_Enabled)
                policyIncrease = 2;

            return (policyIncrease + FCSettings.prosperityBaseRecovery + TraitUtilsFC.cycleTraits("prosperityBaseRecovery", traits, Operation.Addition)); //Go through traits and add prosperity where needed
        }
        public void updateProsperity()
        {
            prosperity += getProsperityGain();

            prosperity = Math.Round(Math.Clamp(prosperity, 1, 100), 1);
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
            TraitUtilsFC.cycleTraits("prosperityBaseRecovery", traits, Operation.Addition, true, ref desc);

            return desc.Trim();
        }
        public double getUnrestGain()
        {
            double unrestGainMultiplier = TraitUtilsFC.cycleTraits("unrestGainedMultiplier", traits, Operation.Multiplication);
            return unrestGainMultiplier * (FCSettings.unrestBaseGain + TraitUtilsFC.cycleTraits("unrestGainedBase", traits, Operation.Addition)); //Go through traits and add unrest where needed
        }
        public double getUnrestLoss()
        {
            double unrestLostMultiplier = TraitUtilsFC.cycleTraits("unrestLostMultiplier", traits, Operation.Multiplication);
            return unrestLostMultiplier * (FCSettings.unrestBaseLost + TraitUtilsFC.cycleTraits("unrestLostBase", traits, Operation.Addition)); //Go through traits and remove unrest where needed
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
            TraitUtilsFC.cycleTraits("unrestGainedBase", traits, Operation.Addition, true, ref gain, invert: true);
            TraitUtilsFC.cycleTraits("unrestGainedMultiplier", traits, Operation.Multiplication, true, ref gain, invert: true);
            if (!gain.NullOrEmpty())
            {
                desc += gain + "\n";
            }
            if (FCSettings.unrestBaseLost != 0)
            {
                desc += TextUtil.colorizeAdditiveBonus(FCSettings.unrestBaseLost, invert: true, hardinvert: true) + " - " + "BaseLoss".Translate() + "\n";
            }
            TraitUtilsFC.cycleTraits("unrestLostBase", traits, Operation.Addition, true, ref desc, invert: true, hardinvert: true);
            TraitUtilsFC.cycleTraits("unrestLostMultiplier", traits, Operation.Multiplication, true, ref desc, invert: true);

            return desc.Trim();
        }
        public double getSettlementTaxBonus()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            double bonus = 0;
            if (faction.hasPolicy(FCPolicyDefOf.egalitarian))
            {
                bonus += Math.Floor(happiness / 10);
                if (trait_Egalitarian_TaxBreak_Enabled)
                {
                    bonus -= 30;
                }
            }

            bonus += TraitUtilsFC.cycleTraits("taxBasePercentage", Traits, Operation.Addition);

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
                    incomeExp += "+" + (resource.actualIncome).ToString() + " - " + resource.label + " " + "Income".Translate() + "\n";
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
                    updateProfitAndProduction();
                    Find.World.GetComponent<FactionFC>().updateTotalProfit();
                    if (numWorkers == 0) return true;
                }
            }

            return false;
        }

        public double getBaseWorkerCost()
        {
            return (FCSettings.workerCost + TraitUtilsFC.cycleTraits("workerBaseCost", traits, Operation.Addition));
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
            double overWork;
            if (workers > workersMax)
            {
                overWork = (int)(workers - workersMax);
            }
            else
            {
                overWork = 0;
            }

            workerTotalUpkeep = (workers * getBaseWorkerCost()) + ((workers * getBaseWorkerCost()) * (overWork / 20));
            if (workerTotalUpkeep > 0)
            {
                upkeepExp += "+" + workerTotalUpkeep.ToString() + " - " + "Workers".Translate() + "\n";
            }

            //add building upkeep

            upkeep += (workerTotalUpkeep);

            double buildingsUpkeep = BuildingsComp?.TotalUpkeep() ?? 0;
            if (buildingsUpkeep > 0)
            {
                upkeep += buildingsUpkeep;
                upkeepExp += "+" + buildingsUpkeep.ToString() + " - " + "Buildings".Translate() + "\n";
            }

            foreach (ResourceFC resource in resources)
            {
                if (resource.actualIncome < 0)
                {
                    upkeep += (-1) * resource.actualIncome;
                    upkeepExp += "+" + (-1 * resource.actualIncome).ToString() + " - " + resource.label + " " + "Tithing".Translate() + "\n";
                }
            }

            upkeepExp = upkeepExp.Trim();
            LogUtil.Message("upkeep " + upkeepExp);
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
                if (resource.def.aidsDefense && resource.rawTotalProduction > 0)
                {
                    defenseBonus += resource.rawTotalProduction;
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
        }

        public void addTraits(List<FCTraitEffectDef> traits, string id = "")
        {
            foreach (FCTraitEffectDef trait in traits)
            {
                addTrait(trait, id);
            }
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
            List<FCTraitEffectDef> currentTraits = traits;
            removeTraits(currentTraits);
            traits.Clear();
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

        //TODO: what is this comment for?
        //Reference
        //0 - settlement name
        //1 - food end production
        //2 - weapon end pro
        //3 - apparel end pro
        //4 - animals end pro
        //5 - logging end pro
        //6 - mining end pro
        //7 - report button
        //8 - tithe est value
        //9 - Silver income
        //10 - location id

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

        //UNUSED FUNCTIONS
        public float getSilverIncome()
        {
            return silverIncome;
        }

        public void resetSilverIncome()
        {
            silverIncome = 0;
        }

        public void addSilverIncome(float amount)
        {
            silverIncome += amount;
        }

        public float returnSilverIncome(bool reset)
        {
            float income = silverIncome;

            if (reset)
            {
                resetSilverIncome();
            }

            return income;
        }
        //UNUSED FUNCTIONS /END

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

        public double getTitheModifier(ResourceTypeDef rdef)
        {
            double modifier = 0;

            modifier += TraitUtilsFC.cycleTraits("taxBaseRandomModifier", traits, Operation.Addition);

            return modifier;
        }

        //TODO: rewrite to work with incremental tithing
        public List<Thing> createTithe(float industriousTaxPercentageBoost)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();


            List<Thing> list = new List<Thing>();
            foreach (ResourceFC resource in resources)
            {
                if (resource.hasRandomTithe && !resource.def.isPoolResource)
                {
                    if (resource.randomTitheFilter == null)
                    {
                        resource.randomTitheFilter = new ThingFilter();
                        resource.resetThingFilter();
                    }

                    if (!resource.randomTitheFilter.AllowedThingDefs.Any())
                    {
                        Find.LetterStack.ReceiveLetter("No Tithe",
                            "There are no enabled items in the tithe" + resource + " of settlement " +
                            name, LetterDefOf.NegativeEvent);
                        continue;
                    }

                    List<Thing> tmpList;

                    double production = resource.randomTitheBudget;
                    /*production *= industriousTaxPercentageBoost * ((100 + TraitUtilsFC.cycleTraits("taxBasePercentage", traits, Operation.Addition)) / 100);
                    //int assignedWorkers = resource.assignedWorkers;

                    //Create Temp Value
                    double tmpValue = production * FCSettings.silverPerResource;*/
                    resource.taxStock += production;
                    resource.returnLowestCost();
                    if (resource.checkMinimum())
                    {
                        if (faction.hasPolicy(FCPolicyDefOf.feudal))
                            resource.taxStock *= 1.2;
                        tmpList = resource.generateTithe(resource.taxStock, FCSettings.productionTitheMod, resource.assignedWorkers, TraitUtilsFC.cycleTraits("taxBaseRandomModifier", traits, Operation.Addition));

                        foreach (Thing thing in tmpList)
                        {
                            list.Add(thing);
                        }

                        resource.taxStock = 0;
                    }

                    resource.returnTaxPercentage();
                }
            }

            return list;
        }
    }

    // NOTE: PawnGizmos patch moved to GizmosPatches.cs to avoid duplication
    // The optimized version in GizmosPatches.PawnDraftGizmos handles all pawn gizmo modifications
}