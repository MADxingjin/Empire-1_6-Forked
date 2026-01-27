using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using FactionColonies.PatchNote;
using Verse.AI.Group;
using LudeonTK;

namespace FactionColonies
{
    public class FCSettings : ModSettings
    {

        /*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-* 
         *           ~  DEFAULTS  ~
         * for saving, reseting, and validation
         * Centralized for ease of editing, and to ensure that all references to these values
         *   are synced.
         *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*/
        /* Defaults by difficulty setting */
        public const int MINIMUM_TAX_INTERVAL_DAYS = 1;
        public const EmpireDifficultyLevel DEFAULT_DIFFICULTY_LEVEL = EmpireDifficultyLevel.AdventureStory;
        //Peaceful
        public const int DEFAULT_SILVER_PER_RESOURCE_PEACEFUL = 200;
        public const int DEFAULT_TAX_INTERVAL_DAYS_PEACEFUL = 2;
        public const int DEFAULT_PRODUCTION_TITHE_MOD_PEACEFUL = 50;
        public const int DEFAULT_WORKER_COST_PEACEFUL = 75;
        //Community Builder
        public const int DEFAULT_SILVER_PER_RESOURCE_COMMUNITYBUILDER = 150;
        public const int DEFAULT_TAX_INTERVAL_DAYS_COMMUNITYBUILDER = 5;
        public const int DEFAULT_PRODUCTION_TITHE_MOD_COMMUNITYBUILDER = 25;
        public const int DEFAULT_WORKER_COST_COMMUNITYBUILDER = 100;
        //Adventure Story
        public const int DEFAULT_SILVER_PER_RESOURCE_ADVENTURESTORY = 100;
        public const int DEFAULT_TAX_INTERVAL_DAYS_ADVENTURESTORY = 5;
        public const int DEFAULT_PRODUCTION_TITHE_MOD_ADVENTURESTORY = 25;
        public const int DEFAULT_WORKER_COST_ADVENTURESTORY = 100;
        //Strive to Survive
        public const int DEFAULT_SILVER_PER_RESOURCE_STRIVETOSURVIVE = 100;
        public const int DEFAULT_TAX_INTERVAL_DAYS_STRIVETOSURVIVE = 10;
        public const int DEFAULT_PRODUCTION_TITHE_MOD_STRIVETOSURVIVE = 20;
        public const int DEFAULT_WORKER_COST_STRIVETOSURVIVE = 125;
        //Blood and Dust
        public const int DEFAULT_SILVER_PER_RESOURCE_BLOODANDDUST = 80;
        public const int DEFAULT_TAX_INTERVAL_DAYS_BLOODANDDUST = 15;
        public const int DEFAULT_PRODUCTION_TITHE_MOD_BLOODANDDUST = 15;
        public const int DEFAULT_WORKER_COST_BLOODANDDUST = 125;
        //Losing is Fun
        public const int DEFAULT_SILVER_PER_RESOURCE_LOSINGISFUN = 70;
        public const int DEFAULT_TAX_INTERVAL_DAYS_LOSINGISFUN = 30;
        public const int DEFAULT_PRODUCTION_TITHE_MOD_LOSINGISFUN = 10;
        public const int DEFAULT_WORKER_COST_LOSINGISFUN = 150;
        // Global defaults
        // The default difficulty setting is Adventure Story, so set the global defaults accordingly
        public const int DEFAULT_SILVER_PER_RESOURCE = DEFAULT_SILVER_PER_RESOURCE_ADVENTURESTORY;
        public const int DEFAULT_TAX_INTERVAL_DAYS = DEFAULT_TAX_INTERVAL_DAYS_ADVENTURESTORY;
        public const int DEFAULT_PRODUCTION_TITHE_MOD = DEFAULT_PRODUCTION_TITHE_MOD_ADVENTURESTORY;
        public const int DEFAULT_WORKER_COST = DEFAULT_WORKER_COST_ADVENTURESTORY;
        /* Defaults for Research settings */
        public const bool DEFAULT_MEDIEVAL_TECH_ONLY = false;
        /* Defaults for Settlement settings */
        public const TaxDeliveryMode DEFAULT_TAX_DELIVERY_MODE = TaxDeliveryMode.None;
        public const TaxNotificationMode DEFAULT_TAX_NOTIFICATION_MODE = TaxNotificationMode.All;
        public static double DEFAULT_SETTLEMENT_FOUNDING_COST = 1000;
        public static double DEFAULT_SETTLEMENT_BASE_UPGRADE_COST = 1000;
        public static int DEFAULT_SETTLEMENT_MAX_LEVEL = 10;
        /* Defaults for Events & Military settings */
        public const bool DEFAULT_DISABLE_HOSTILE_MILITARY_ACTIONS = false;
        public const bool DEFAULT_DISABLE_RANDOM_EVENTS = false;
        public const bool DEFAULT_DISABLE_FORCED_PAUSING_DURING_EVENTS = true;
        public const bool DEFAULT_DEAD_PAWNS_INCREASE_MILITARY_COOLDOWN = true;
        public const bool DEFAULT_SETTLEMENTS_AUTO_BATTLE = true;
        public const int DEFAULT_MIN_DAYS_TIL_MILITARY_ACTION = 4;
        public const int DEFAULT_MAX_DAYS_TIL_MILITARY_ACTION = 10;
        public const int DEFAULT_MIN_DAYS_TIL_RANDOM_EVENT = 0;
        public const int DEFAULT_MAX_DAYS_TIL_RANDOM_EVENT = 6;
        /*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-* 
         *           ~  DEFAULTS END ~
         *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*/

        public const int updateUiTimer = 150; // UI update interval in ticks

        public static int silverPerResource = DEFAULT_SILVER_PER_RESOURCE;
        public static double silverToCreateSettlement = DEFAULT_SETTLEMENT_FOUNDING_COST;

        private static int timeBetweenTaxes_days = DEFAULT_TAX_INTERVAL_DAYS;
        public static int timeBetweenTaxes => timeBetweenTaxes_days * GenDate.TicksPerDay;


        public static int productionTitheMod = DEFAULT_PRODUCTION_TITHE_MOD;
        public static int storeReportCount = 4;
        public static int workerCost = DEFAULT_WORKER_COST;

        public static EmpireDifficultyLevel difficultyLevel = DEFAULT_DIFFICULTY_LEVEL;

        public static double settlementBaseUpgradeCost = DEFAULT_SETTLEMENT_BASE_UPGRADE_COST;
        public static int settlementMaxLevel = DEFAULT_SETTLEMENT_MAX_LEVEL;

        public static bool medievalTechOnly = DEFAULT_MEDIEVAL_TECH_ONLY;
        public static bool disableHostileMilitaryActions = DEFAULT_DISABLE_HOSTILE_MILITARY_ACTIONS;
        public static bool disableRandomEvents = DEFAULT_DISABLE_RANDOM_EVENTS;
        public static bool disableForcedPausingDuringEvents = DEFAULT_DISABLE_FORCED_PAUSING_DURING_EVENTS;
        public static bool deadPawnsIncreaseMilitaryCooldown = DEFAULT_DEAD_PAWNS_INCREASE_MILITARY_COOLDOWN;
        public static bool settlementsAutoBattle = DEFAULT_SETTLEMENTS_AUTO_BATTLE;
        public static TaxDeliveryMode forcedTaxDeliveryMode = DEFAULT_TAX_DELIVERY_MODE;
        public static TaxNotificationMode taxNotificationMode = DEFAULT_TAX_NOTIFICATION_MODE;

        public static int minDaysTillMilitaryAction = DEFAULT_MIN_DAYS_TIL_MILITARY_ACTION;
        public static int maxDaysTillMilitaryAction = DEFAULT_MAX_DAYS_TIL_MILITARY_ACTION;
        public static IntRange minMaxDaysTillMilitaryAction = new IntRange(minDaysTillMilitaryAction, maxDaysTillMilitaryAction);

        public static int minDaysTillRandomEvent = DEFAULT_MIN_DAYS_TIL_RANDOM_EVENT;
        public static int maxDaysTillRandomEvent = DEFAULT_MAX_DAYS_TIL_RANDOM_EVENT;
        public static IntRange minMaxDaysTillRandomEvent = new IntRange(minDaysTillRandomEvent, maxDaysTillRandomEvent);

        /* TODO: might be interesting to expose these values in the settings. Might be a bit much
         * for the user though. Perhaps can add an "advanced settings" tab that lets the user
         * fine-tune a lot of the smaller values? */
        public static double unrestBaseGain = 0;
        public static double unrestBaseLost = 1;
        public static double loyaltyBaseGain = 1;
        public static double loyaltyBaseLost = 0;
        public static double happinessBaseGain = 1;
        public static double happinessBaseLost = 0;
        public static double prosperityBaseRecovery = 1;
        public static int productionResearchBase = 100;
        public static double militaryAnimalCostMultiplier = 1.5;
        public static double militaryRaceCostMultiplier = 0.15;

        public static double updateVersion = 0;

        /* Flag for debug/verbose logging. */
        private static bool printDebug = false;
        public static bool PrintDebug => printDebug;

        // Window size settings - add these fields
        public static float buildingWindowWidth = 450f;
        public static float buildingWindowHeight = 600f;

        // Static variables to remember window size during play session
        private static Vector2 savedWindowSize = new Vector2(450f, 600f);
        private static bool hasSavedSize = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref silverPerResource, "silverPerResource", DEFAULT_SILVER_PER_RESOURCE);
            Scribe_Values.Look(ref timeBetweenTaxes_days, "timeBetweenTaxes_days", DEFAULT_TAX_INTERVAL_DAYS);
            Scribe_Values.Look(ref productionTitheMod, "productionTitheMod", DEFAULT_PRODUCTION_TITHE_MOD);
            Scribe_Values.Look(ref workerCost, "workerCost", DEFAULT_WORKER_COST);
            Scribe_Values.Look(ref settlementMaxLevel, "settlementMaxLevel", DEFAULT_SETTLEMENT_MAX_LEVEL);
            Scribe_Values.Look(ref medievalTechOnly, "medievalTechOnly", DEFAULT_MEDIEVAL_TECH_ONLY);
            Scribe_Values.Look(ref disableHostileMilitaryActions, "disableHostileMilitaryActions", DEFAULT_DISABLE_HOSTILE_MILITARY_ACTIONS);
            Scribe_Values.Look(ref disableRandomEvents, "disableRandomEvents", DEFAULT_DISABLE_RANDOM_EVENTS);
            Scribe_Values.Look(ref forcedTaxDeliveryMode, "forcedTaxDeliveryMode", DEFAULT_TAX_DELIVERY_MODE);
            Scribe_Values.Look(ref taxNotificationMode, "taxNotificationMode", DEFAULT_TAX_NOTIFICATION_MODE);
            Scribe_Values.Look(ref deadPawnsIncreaseMilitaryCooldown, "deadPawnsIncreaseMilitaryCooldown", DEFAULT_DEAD_PAWNS_INCREASE_MILITARY_COOLDOWN);
            Scribe_Values.Look(ref settlementsAutoBattle, "settlementsAutoBattle", DEFAULT_SETTLEMENTS_AUTO_BATTLE);
            Scribe_Values.Look(ref minDaysTillMilitaryAction, "minDaysTillMilitaryAction", DEFAULT_MIN_DAYS_TIL_MILITARY_ACTION);
            Scribe_Values.Look(ref maxDaysTillMilitaryAction, "maxDaysTillMilitaryAction", DEFAULT_MAX_DAYS_TIL_MILITARY_ACTION);
            Scribe_Values.Look(ref minDaysTillRandomEvent, "minDaysTillRandomEvent", DEFAULT_MIN_DAYS_TIL_RANDOM_EVENT);
            Scribe_Values.Look(ref maxDaysTillRandomEvent, "maxDaysTillRandomEvent", DEFAULT_MAX_DAYS_TIL_RANDOM_EVENT);
            Scribe_Values.Look(ref updateVersion, "updateVersion");
            Scribe_Values.Look(ref buildingWindowWidth, "buildingWindowWidth", 450f);
            Scribe_Values.Look(ref buildingWindowHeight, "buildingWindowHeight", 600f);
            Scribe_Values.Look(ref difficultyLevel, "difficultyLevel", DEFAULT_DIFFICULTY_LEVEL);
            Scribe_Values.Look(ref printDebug, "printDebug", false);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Band aid - For existing users upgrading from old system, detect if they have custom values
                if (difficultyLevel == DEFAULT_DIFFICULTY_LEVEL)
                {
                    // Check if current values match Adventure Story defaults
                    if (silverPerResource != DEFAULT_SILVER_PER_RESOURCE || timeBetweenTaxes_days != DEFAULT_TAX_INTERVAL_DAYS ||
                        productionTitheMod != DEFAULT_PRODUCTION_TITHE_MOD || workerCost != DEFAULT_WORKER_COST)
                    {
                        // User had custom settings, set to Custom mode
                        difficultyLevel = EmpireDifficultyLevel.Custom;
                    }
                }
                /* Re-construct the intranges */
                minMaxDaysTillMilitaryAction = new IntRange(minDaysTillMilitaryAction, maxDaysTillMilitaryAction);
                minMaxDaysTillRandomEvent = new IntRange(minDaysTillRandomEvent, maxDaysTillRandomEvent);
            }
        }

        public static string GetModVersion()
        {
            try
            {
                var mod = LoadedModManager.GetMod<FactionColoniesMod>();
                string manifestPath = Path.Combine(mod.Content.RootDir, "About", "Manifest.xml");
                if (File.Exists(manifestPath))
                {
                    string content = File.ReadAllText(manifestPath);
                    int versionStart = content.IndexOf("<version>") + 9;
                    int versionEnd = content.IndexOf("</version>");
                    if (versionStart > 8 && versionEnd > versionStart)
                    {
                        return content.Substring(versionStart, versionEnd - versionStart);
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.Warning("Failed to read version from manifest: " + ex.Message);
            }
            return "Unknown";
        }
        public static void UpdateChanges()
        {
            FactionFC factionFC = Find.World.GetComponent<FactionFC>();
            PatchNoteSettings patchNoteSettings = LoadedModManager.GetMod<PatchNoteMod>().GetSettings<PatchNoteSettings>();

            // Store the initial state before any modifications
            bool wasAlreadyProcessed = factionFC.updateProcessed;

            // Only log once when first setting up
            if (!factionFC.updateProcessed)
            {
                LogUtil.Message("Updating Empire to Latest Version");
                // DON'T set updateProcessed = true here yet! ( ͡° ͜ʖ ͡°)
            }
            //NEW PLACE FOR UPDATE VERSIONS

            //I think this does things necessary for SOS so I'm gonna keep it
            if (factionFC.factionBackup == null)
            {
                factionFC.factionBackup = new Faction();
                factionFC.factionBackup = ColonyUtil.getPlayerColonyFaction();
                if (ColonyUtil.getPlayerColonyFaction() != null)
                {
                    LogUtil.Message("Faction created");
                    factionFC.factionCreated = true;
                }

                factionFC.capitalPlanet = Find.World.info.name;

                if (!wasAlreadyProcessed)
                {
                    LogUtil.Message("Resetting faction leaders");
                }
                SoS2HarmonyPatches.ResetFactionLeaders();
            }

            // Only run verification and alerts for new games/first time setup
            if (!wasAlreadyProcessed)
            {

                // Welcome message!
                Find.WindowStack.Add(new FCWindow_Welcome());

                LogUtil.Message("Testing for traits with no tie");
                verifyTraits();
            
                MessagePlayerAboutConfigErrors(factionFC);  // ← This will now execute!

                LogUtil.Message("Testing for update change");
                
                // Mark as processed AFTER everything is done
                factionFC.updateProcessed = true;
            }

            if (updateVersion < 0.370)
            {
                Find.LetterStack.ReceiveLetter("FCManualDefenseWarningLabel".Translate(), "FCManualDefenseWarningDesc".Translate(), LetterDefOf.NeutralEvent);
            }

            double newVersion = PatchNoteDef.GetLatestForMod("saakra.empire").ToOldEmpireVersion;
            //Add update letter/checker here!!
            if (updateVersion < newVersion)
            {
                patchNoteSettings.lastVersion = updateVersion;
                patchNoteSettings.curVersion = newVersion;
                patchNoteSettings.Write();

                DebugActionsMisc.PatchNotesDisplayWindow();

                updateVersion = newVersion;
                settlementsAutoBattle = true;
                //TODO: we original forced a write here. I don't really think that's necessary, but look into it.
                //Write();
            }
        }

        private static void MessagePlayerAboutConfigErrors(FactionFC factionFC)
        {
            LogUtil.Message("Testing for invalid capital map");
            //Check for an invalid capital map
            if (Find.WorldObjects.SettlementAt(factionFC.capitalLocation) == null && factionFC.SoSShipCapital == false)
            {
                Messages.Message("FCResetCapitalLocationWarning".Translate(), MessageTypeDefOf.NegativeEvent);
            }

            if (factionFC.taxMap == null)
            {
                Messages.Message("FCTaxMapNotSetWarning".Translate(), MessageTypeDefOf.CautionInput);
            }

            if (factionFC.policies.Count() < 2)
            {
                Find.LetterStack.ReceiveLetter("FCTraits".Translate(), "FCSelectYourTraits".Translate(), LetterDefOf.NeutralEvent);
            }

            if (!settlementsAutoBattle)
            {
                Messages.Message("FCAutoResolveDisabledWarning".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        public static void verifyTraits()
        {
            //make new list for factionfc traits
            //loop through events and add traits
            //loop through
            List<FCTraitEffectDef> factionTraits = new List<FCTraitEffectDef>();

            foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
            {
                if (evt.settlementTraitLocations.Count() <= 0)
                {
                    factionTraits.AddRange(evt.traits);
                }
            }

            Find.World.GetComponent<FactionFC>().traits = factionTraits;

            //go through each settlement and make new list for each settlement
            //loop through each active event and add settlement traits
            //loop through buildings and add traits

            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                List<FCTraitEffectDef> settlementsTraits = new List<FCTraitEffectDef>();

                foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
                {
                    if (evt.settlementTraitLocations.Any())
                    {
                        //ignore
                        if (evt.settlementTraitLocations.Contains(settlement))
                        {
                            settlementsTraits.AddRange(evt.traits);
                        }
                    }
                }

                foreach (BuildingFCDef building in settlement.buildings)
                {
                    settlementsTraits.AddRange(building.traits);
                }

                settlement.traits = settlementsTraits;
            }
        }

        public static bool IsModLoaded(string packageID) => LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing == packageID);


        public static void debugMarker(ref int i)
        {
            LogUtil.Message($"debugMarker: {i}");
            i ++;
        }

        // Difficulty preset values
        public static void ApplyDifficultyPreset(EmpireDifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case EmpireDifficultyLevel.Peaceful:
                    silverPerResource = DEFAULT_SILVER_PER_RESOURCE_PEACEFUL;
                    timeBetweenTaxes_days = DEFAULT_TAX_INTERVAL_DAYS_PEACEFUL;
                    productionTitheMod = DEFAULT_PRODUCTION_TITHE_MOD_PEACEFUL;
                    workerCost = DEFAULT_WORKER_COST_PEACEFUL;
                    break;
                case EmpireDifficultyLevel.CommunityBuilder:
                    silverPerResource = DEFAULT_SILVER_PER_RESOURCE_COMMUNITYBUILDER;
                    timeBetweenTaxes_days = DEFAULT_TAX_INTERVAL_DAYS_COMMUNITYBUILDER;
                    productionTitheMod = DEFAULT_PRODUCTION_TITHE_MOD_COMMUNITYBUILDER;
                    workerCost = DEFAULT_WORKER_COST_COMMUNITYBUILDER;
                    break;
                case EmpireDifficultyLevel.AdventureStory:
                    silverPerResource = DEFAULT_SILVER_PER_RESOURCE_ADVENTURESTORY;
                    timeBetweenTaxes_days = DEFAULT_TAX_INTERVAL_DAYS_ADVENTURESTORY;
                    productionTitheMod = DEFAULT_PRODUCTION_TITHE_MOD_ADVENTURESTORY;
                    workerCost = DEFAULT_WORKER_COST_ADVENTURESTORY;
                    break;
                case EmpireDifficultyLevel.StriveToSurvive:
                    silverPerResource = DEFAULT_SILVER_PER_RESOURCE_STRIVETOSURVIVE;
                    timeBetweenTaxes_days = DEFAULT_TAX_INTERVAL_DAYS_STRIVETOSURVIVE;
                    productionTitheMod = DEFAULT_PRODUCTION_TITHE_MOD_STRIVETOSURVIVE;
                    workerCost = DEFAULT_WORKER_COST_STRIVETOSURVIVE;
                    break;
                case EmpireDifficultyLevel.BloodAndDust:
                    silverPerResource = DEFAULT_SILVER_PER_RESOURCE_BLOODANDDUST;
                    timeBetweenTaxes_days = DEFAULT_TAX_INTERVAL_DAYS_BLOODANDDUST;
                    productionTitheMod = DEFAULT_PRODUCTION_TITHE_MOD_BLOODANDDUST;
                    workerCost = DEFAULT_WORKER_COST_BLOODANDDUST;
                    break;
                case EmpireDifficultyLevel.LosingIsFun:
                    silverPerResource = DEFAULT_SILVER_PER_RESOURCE_LOSINGISFUN;
                    timeBetweenTaxes_days = DEFAULT_TAX_INTERVAL_DAYS_LOSINGISFUN;
                    productionTitheMod = DEFAULT_PRODUCTION_TITHE_MOD_LOSINGISFUN;
                    workerCost = DEFAULT_WORKER_COST_LOSINGISFUN;
                    break;
                case EmpireDifficultyLevel.Custom:
                    // Don't change anything for custom
                    break;
            }
        }

        public static int DaysBetweenTaxesByDifficulty(EmpireDifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case EmpireDifficultyLevel.Peaceful:
                    return DEFAULT_TAX_INTERVAL_DAYS_PEACEFUL;
                case EmpireDifficultyLevel.CommunityBuilder:
                    return DEFAULT_TAX_INTERVAL_DAYS_COMMUNITYBUILDER;
                case EmpireDifficultyLevel.AdventureStory:
                    return DEFAULT_TAX_INTERVAL_DAYS_ADVENTURESTORY;
                case EmpireDifficultyLevel.StriveToSurvive:
                    return DEFAULT_TAX_INTERVAL_DAYS_STRIVETOSURVIVE;
                case EmpireDifficultyLevel.BloodAndDust:
                    return DEFAULT_TAX_INTERVAL_DAYS_BLOODANDDUST;
                case EmpireDifficultyLevel.LosingIsFun:
                    return DEFAULT_TAX_INTERVAL_DAYS_LOSINGISFUN;
                default:
                    return DEFAULT_TAX_INTERVAL_DAYS;
            }
        }
        public static int TicksBetweenTaxesByDifficulty(EmpireDifficultyLevel difficulty)
        {
            return DaysBetweenTaxesByDifficulty(difficulty) * GenDate.TicksPerDay;
        }

        string silverPerResource_buffer;
        string timeBetweenTaxes_buffer;
        string productionTitheMod_buffer;
        string workerCost_buffer;
        string settlementMaxLevel_buffer;

        private Vector2 scrollVector = new Vector2();
        private float viewRectHeight = -1f;

        private bool firstRun = true;
        private bool fixDone = false;

        /// <summary>
        /// Creates an option for the list of ForcedTaxDeliveryOptions. Shuttles may not be used if royality is inactive
        /// </summary>
        private FloatMenuOption ShuttleOption
        {
            get
            {
                if (ModsConfig.RoyaltyActive)
                {
                    return new FloatMenuOption("taxDeliveryModeShuttleDesc".Translate(), delegate () { forcedTaxDeliveryMode = TaxDeliveryMode.Shuttle; });
                }
                else
                {
                    return new FloatMenuOption("taxDeliveryModeShuttleUnavailableDesc".Translate(), null);
                }
            }
        }

        /// <summary>
        /// Creates a list of options for forced tax delivery
        /// </summary>
        private List<FloatMenuOption> ForcedTaxDeliveryOptions
        {
            get
            {
                return new List<FloatMenuOption>()
                {
                    new FloatMenuOption("taxDeliveryModeDefaultDesc".Translate(), delegate() {forcedTaxDeliveryMode = default;}),
                    new FloatMenuOption("taxDeliveryModeTaxSpotDesc".Translate(), delegate() {forcedTaxDeliveryMode = TaxDeliveryMode.TaxSpot;}),
                    new FloatMenuOption("taxDeliveryModeCaravanDesc".Translate(), delegate() {forcedTaxDeliveryMode = TaxDeliveryMode.Caravan;}),
                    new FloatMenuOption("taxDeliveryModeDropPodDesc".Translate(), delegate() {forcedTaxDeliveryMode = TaxDeliveryMode.DropPod;}),
                    ShuttleOption
                };
            }
        }

        /// <summary>
        /// Creates a list of options for tax notification mode
        /// </summary>
        private List<FloatMenuOption> TaxNotificationOptions => new List<FloatMenuOption>
        {
            new FloatMenuOption("FCTaxNotifyAll".Translate(), () => taxNotificationMode = TaxNotificationMode.All),
            new FloatMenuOption("FCTaxNotifyLetterOnly".Translate(), () => taxNotificationMode = TaxNotificationMode.LetterOnly),
            new FloatMenuOption("FCTaxNotifyMessageOnly".Translate(), () => taxNotificationMode = TaxNotificationMode.MessageOnly),
            new FloatMenuOption("FCTaxNotifyNone".Translate(), () => taxNotificationMode = TaxNotificationMode.None)
        };

        public void DoWindowContents(Rect inRect)
        {
            silverPerResource_buffer = silverPerResource.ToString();
            timeBetweenTaxes_buffer = timeBetweenTaxes_days.ToString();
            productionTitheMod_buffer = productionTitheMod.ToString();
            workerCost_buffer = workerCost.ToString();
            settlementMaxLevel_buffer = settlementMaxLevel.ToString();

            minMaxDaysTillMilitaryAction = new IntRange(minDaysTillMilitaryAction, maxDaysTillMilitaryAction);
            minMaxDaysTillRandomEvent = new IntRange(minDaysTillRandomEvent, maxDaysTillRandomEvent);

            viewRectHeight = viewRectHeight == -1f ? float.MaxValue : viewRectHeight;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width - 17f, viewRectHeight);

            Widgets.BeginScrollView(inRect, ref scrollVector, viewRect);
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(viewRect);

            // Display mod version
            ls.Label("Empire Mod Version: " + GetModVersion());
            ls.Gap(10f);

            // Empire Difficulty Selection
            ls.Label("FCSettingEmpireDifficulty".Translate());
            ls.Gap(5f);

            // Create difficulty options with descriptions
            var difficultyOptions = new List<(EmpireDifficultyLevel level, string nameKey, string descKey)>
            {
                (EmpireDifficultyLevel.Peaceful, "FCDifficultyPeaceful", "FCDifficultyPeacefulDesc"),
                (EmpireDifficultyLevel.CommunityBuilder, "FCDifficultyCommunityBuilder", "FCDifficultyCommunityBuilderDesc"),
                (EmpireDifficultyLevel.AdventureStory, "FCDifficultyAdventureStory", "FCDifficultyAdventureStoryDesc"),
                (EmpireDifficultyLevel.StriveToSurvive, "FCDifficultyStriveToSurvive", "FCDifficultyStriveToSurviveDesc"),
                (EmpireDifficultyLevel.BloodAndDust, "FCDifficultyBloodAndDust", "FCDifficultyBloodAndDustDesc"),
                (EmpireDifficultyLevel.LosingIsFun, "FCDifficultyLosingIsFun", "FCDifficultyLosingIsFunDesc"),
                (EmpireDifficultyLevel.Custom, "FCDifficultyCustom", "FCDifficultyCustomDesc")
            };

            foreach (var option in difficultyOptions)
            {
                bool isSelected = difficultyLevel == option.level;

                if (ls.RadioButton(option.nameKey.Translate(), isSelected))
                {
                    if (!isSelected) // Only change if not already selected
                    {
                        difficultyLevel = option.level;
                        if (option.level != EmpireDifficultyLevel.Custom)
                        {
                            ApplyDifficultyPreset(option.level);
                        }
                    }
                }
                // Add description as a separate indented label
                ls.Label("    " + option.descKey.Translate(), -1f);
            }

            ls.Gap(15f);

            // Show economic settings only if Custom is selected
            if (difficultyLevel == EmpireDifficultyLevel.Custom)
            {
                ls.Label("FCSettingSilverPerResource".Translate());
                ls.IntEntry(ref silverPerResource, ref silverPerResource_buffer);
                ls.Label("FCSettingDaysBetweenTax".Translate());
                ls.IntEntry(ref timeBetweenTaxes_days, ref timeBetweenTaxes_buffer);
                ls.Label("FCSettingProductionTitheMod".Translate());
                ls.IntEntry(ref productionTitheMod, ref productionTitheMod_buffer);
                ls.Label("FCSettingWorkerCost".Translate());
                ls.IntEntry(ref workerCost, ref workerCost_buffer);
            }
            else
            {
                // Show current values as read-only labels for non-custom difficulties
                ls.Label($"FCSettingSilverPerResource".Translate() + ": " + silverPerResource);
                ls.Label($"FCSettingDaysBetweenTax".Translate() + ": " + timeBetweenTaxes_days);
                ls.Label($"FCSettingProductionTitheMod".Translate() + ": " + productionTitheMod);
                ls.Label($"FCSettingWorkerCost".Translate() + ": " + workerCost);
            }

            ls.Label("FCSettingMaxSettlementLevel".Translate());
            ls.IntEntry(ref settlementMaxLevel, ref settlementMaxLevel_buffer);
            ls.CheckboxLabeled("MedievalTechOnly".Translate(), ref medievalTechOnly);
            ls.CheckboxLabeled("FCSettingDisableHostileMilActions".Translate(), ref disableHostileMilitaryActions);
            ls.CheckboxLabeled("FCSettingDisableRandomEvents".Translate(), ref disableRandomEvents);
            ls.CheckboxLabeled("FCSettingDeadPawnsIncreaseMilCooldown".Translate(), ref deadPawnsIncreaseMilitaryCooldown);
            ls.CheckboxLabeled("FCSettingForcedPausing".Translate(), ref disableForcedPausingDuringEvents);
            //TODO: uncomment when auto battle works.
            //      mostly just adding this "todo" as an easy target for searching
            //ls.CheckboxLabeled("FCSettingAutoResolveBattles".Translate(), ref settings.settlementsAutoBattle);
            if (ls.ButtonText("selectTaxDeliveryModeButton".Translate() + forcedTaxDeliveryMode)) Find.WindowStack.Add(new FloatMenu(ForcedTaxDeliveryOptions));
            if (ls.ButtonText("FCTaxNotificationModeButton".Translate() + taxNotificationMode)) Find.WindowStack.Add(new FloatMenu(TaxNotificationOptions));

            ls.Label("FCSettingMinMaxMilitaryAction".Translate());
            ls.IntRange(ref minMaxDaysTillMilitaryAction, 1, 30);
            minDaysTillMilitaryAction = minMaxDaysTillMilitaryAction.min;
            maxDaysTillMilitaryAction = Math.Max(1, minMaxDaysTillMilitaryAction.max);

            ls.Label("FCSettingMinMaxRandomEvent".Translate());
            ls.IntRange(ref minMaxDaysTillRandomEvent, 0, 30);
            minDaysTillRandomEvent = minMaxDaysTillRandomEvent.min;
            maxDaysTillRandomEvent = Math.Max(1, minMaxDaysTillRandomEvent.max);

            ls.CheckboxLabeled("FCSettingEnableDebugLogging".Translate(), ref printDebug);

            if (ls.ButtonText("FCOpenPatchNotes".Translate())) DebugActionsMisc.PatchNotesDisplayWindow();

            if (ls.ButtonText("FCSettingResetButton".Translate()))
            {
                silverPerResource = DEFAULT_SILVER_PER_RESOURCE;
                timeBetweenTaxes_days = DEFAULT_TAX_INTERVAL_DAYS;
                productionTitheMod = DEFAULT_PRODUCTION_TITHE_MOD;
                workerCost = DEFAULT_WORKER_COST;
                medievalTechOnly = DEFAULT_MEDIEVAL_TECH_ONLY;
                settlementMaxLevel = DEFAULT_SETTLEMENT_MAX_LEVEL;
                minDaysTillMilitaryAction = DEFAULT_MIN_DAYS_TIL_MILITARY_ACTION;
                maxDaysTillMilitaryAction = DEFAULT_MAX_DAYS_TIL_MILITARY_ACTION;
                minDaysTillRandomEvent = DEFAULT_MIN_DAYS_TIL_RANDOM_EVENT;
                maxDaysTillRandomEvent = DEFAULT_MAX_DAYS_TIL_RANDOM_EVENT;
                disableRandomEvents = DEFAULT_DISABLE_RANDOM_EVENTS;
                deadPawnsIncreaseMilitaryCooldown = DEFAULT_DEAD_PAWNS_INCREASE_MILITARY_COOLDOWN;
                settlementsAutoBattle = DEFAULT_SETTLEMENTS_AUTO_BATTLE;
                disableForcedPausingDuringEvents = DEFAULT_DISABLE_FORCED_PAUSING_DURING_EVENTS;
                forcedTaxDeliveryMode = DEFAULT_TAX_DELIVERY_MODE;
                taxNotificationMode = DEFAULT_TAX_NOTIFICATION_MODE;
                difficultyLevel = DEFAULT_DIFFICULTY_LEVEL;
                ApplyDifficultyPreset(difficultyLevel);
            }

            FixScrollingBug(ls);
            ls.End();

            Widgets.EndScrollView();
        }

        private void FixScrollingBug(Listing_Standard ls)
        {
            if (fixDone) return;

            if (!firstRun)
            {
                viewRectHeight = ls.CurHeight + 5f;
                fixDone = true;
            }
            else
            {
                viewRectHeight = float.MaxValue;
                firstRun = false;
            }
        }
    }

    
    public class FactionColoniesMod : Mod
    {
        public FCSettings settings = new FCSettings();

        public FactionColoniesMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<FCSettings>();
        }

        public override string SettingsCategory()
        {
            return "Empire";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}
