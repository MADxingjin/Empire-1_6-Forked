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
    public class FactionColonies : ModSettings
    {

                // Constants for validation
        private const int MINIMUM_TAX_INTERVAL = GenDate.TicksPerDay;
        private const int DEFAULT_TAX_INTERVAL = 5 * GenDate.TicksPerDay; // 5 days in ticks
        public const int updateUiTimer = 150; // UI update interval in ticks

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
                Log.Warning("Empire Mod: Failed to read version from manifest: " + ex.Message);
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
                Log.Message("Updating Empire to Latest Version");
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
                    Log.Message("Faction created");
                    factionFC.factionCreated = true;
                }

                factionFC.capitalPlanet = Find.World.info.name;

                if (!wasAlreadyProcessed)
                {
                    Log.Message("Resetting faction leaders");
                }
                SoS2HarmonyPatches.ResetFactionLeaders();
            }

            // Only run verification and alerts for new games/first time setup
            if (!wasAlreadyProcessed)
            {

                // Welcome message!
                Find.WindowStack.Add(new FCWindow_Welcome());

                Log.Message("Empire - Testing for traits with no tie");
                verifyTraits();
            
                MessagePlayerAboutConfigErrors(factionFC);  // ← This will now execute!

                Log.Message("Empire - Testing for update change");
                
                // Mark as processed AFTER everything is done
                factionFC.updateProcessed = true;
            }

            if (Settings().updateVersion < 0.370)
            {
                Find.LetterStack.ReceiveLetter("FCManualDefenseWarningLabel".Translate(), "FCManualDefenseWarningDesc".Translate(), LetterDefOf.NeutralEvent);
            }

            double newVersion = PatchNoteDef.GetLatestForMod("saakra.empire").ToOldEmpireVersion;
            //Add update letter/checker here!!
            if (Settings().updateVersion < newVersion)
            {
                patchNoteSettings.lastVersion = Settings().updateVersion;
                patchNoteSettings.curVersion = newVersion;
                patchNoteSettings.Write();

                DebugActionsMisc.PatchNotesDisplayWindow();

                Settings().updateVersion = newVersion;
                Settings().settlementsAutoBattle = true;
                Settings().Write();
            }
        }

        private static void MessagePlayerAboutConfigErrors(FactionFC factionFC)
        {
            Log.Message("Empire - Testing for invalid capital map");
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

            if (!Settings().settlementsAutoBattle)
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
            Log.Message(i.ToString());
            i ++;
        }

        public static FactionColonies Settings() => LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>();

        public int silverPerResource = 100;
        public static double silverToCreateSettlement = 1000;
        // public int timeBetweenTaxes = GenDate.TicksPerTwelfth;
        // public static int updateUiTimer = 150;'

        // Tax attempt fix
        private int _timeBetweenTaxes = DEFAULT_TAX_INTERVAL;
        // TODO: replace timeBetweenTaxes with a simple (taxvariable) * GenDate.TicksPerDay
        //       all 'setting' should hit the day variable in the settings. Settings variables, if setup properly in ExposeData, will save/load with the rest of the settings.
        //       It isn't clear to me why we would want to *set* this value outside of the settings. So a proper refactor would remove the need for the set() function.
        public int timeBetweenTaxes
        {
            get
            {
                // Ensure the value is never 0 or negative
                if (_timeBetweenTaxes <= 0)
                {
                    // Restore based on current difficulty level, not always to default
                    int correctValue = GetTimeBetweenTaxesForDifficulty(difficultyLevel);
                    Log.Warning($"Empire Mod - Settings: timeBetweenTaxes getter detected invalid value ({_timeBetweenTaxes}), restoring to difficulty preset ({difficultyLevel} = {correctValue / 60000} days)");
                    _timeBetweenTaxes = correctValue;
                }
                return _timeBetweenTaxes;
            }
            set
            {
                // Ensure the value is never 0 or negative
                if (value <= 0)
                {
                    // Restore based on current difficulty level, not always to minimum
                    int correctValue = GetTimeBetweenTaxesForDifficulty(difficultyLevel);
                    Log.Warning($"Empire Mod - Settings: Attempted to set timeBetweenTaxes to invalid value ({value}), restoring to difficulty preset ({difficultyLevel} = {correctValue / 60000} days)");
                    _timeBetweenTaxes = correctValue;
                }
                else
                {
                    _timeBetweenTaxes = value;
                }
            }
        }


        public int productionTitheMod = 25;
        public static int productionResearchBase = 100;
        public static int storeReportCount = 4;
        public int workerCost = 100;

        public EmpireDifficultyLevel difficultyLevel = EmpireDifficultyLevel.AdventureStory; // Default to Adventure Story

        public static double unrestBaseGain = 0;
        public static double unrestBaseLost = 1;

        public static double loyaltyBaseGain = 1;
        public static double loyaltyBaseLost = 0;

        public static double happinessBaseGain = 1;
        public static double happinessBaseLost = 0;

        public static double prosperityBaseRecovery = 1;

        public double settlementBaseUpgradeCost = 1000;
        public int settlementMaxLevel = 10;

        public bool medievalTechOnly;
        public bool disableHostileMilitaryActions;
        public bool disableRandomEvents;
        public bool disableForcedPausingDuringEvents = true;
        public bool deadPawnsIncreaseMilitaryCooldown;
        public bool settlementsAutoBattle = true;
        public TaxDeliveryMode forcedTaxDeliveryMode;
        public TaxNotificationMode taxNotificationMode = TaxNotificationMode.All;

        public int minDaysTillMilitaryAction = 4;
        public int maxDaysTillMilitaryAction = 10;

        public int minDaysTillRandomEvent = 0;
        public int maxDaysTillRandomEvent = 6;
        public IntRange minMaxDaysTillMilitaryAction = new IntRange(4, 10);
        public static double militaryAnimalCostMultiplier = 1.5;
        public static double militaryRaceCostMultiplier = .15;

        public double updateVersion = 0;

        // Window size settings - add these fields
        public float buildingWindowWidth = 450f;
        public float buildingWindowHeight = 600f;

        // Static variables to remember window size during play session
        private static Vector2 savedWindowSize = new Vector2(450f, 600f);
        private static bool hasSavedSize = false;

        // Helper method to get the correct timeBetweenTaxes for a difficulty level
        // Used when restoring corrupted values to ensure we use the preset value, not always 1 day
        private static int GetTimeBetweenTaxesForDifficulty(EmpireDifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case EmpireDifficultyLevel.Peaceful:
                    return 2 * 60000; // 2 days in ticks
                case EmpireDifficultyLevel.CommunityBuilder:
                    return 5 * 60000; // 5 days in ticks
                case EmpireDifficultyLevel.AdventureStory:
                    return 5 * 60000; // 5 days in ticks
                case EmpireDifficultyLevel.StriveToSurvive:
                    return 10 * 60000; // 10 days in ticks
                case EmpireDifficultyLevel.BloodAndDust:
                    return 15 * 60000; // 15 days in ticks
                case EmpireDifficultyLevel.LosingIsFun:
                    return 30 * 60000; // 30 days in ticks
                case EmpireDifficultyLevel.Custom:
                default:
                    return DEFAULT_TAX_INTERVAL; // 5 days fallback for Custom or unknown
            }
        }

        // Difficulty preset values
        public void ApplyDifficultyPreset(EmpireDifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case EmpireDifficultyLevel.Peaceful:
                    silverPerResource = 200;
                    timeBetweenTaxes = 2 * 60000; // 2 days in ticks
                    productionTitheMod = 50;
                    workerCost = 75;
                    break;
                case EmpireDifficultyLevel.CommunityBuilder:
                    silverPerResource = 150;
                    timeBetweenTaxes = 5 * 60000; // 5 days in ticks
                    productionTitheMod = 25;
                    workerCost = 100;
                    break;
                case EmpireDifficultyLevel.AdventureStory:
                    silverPerResource = 100;
                    timeBetweenTaxes = 5 * 60000; // 5 days in ticks
                    productionTitheMod = 25;
                    workerCost = 100;
                    break;
                case EmpireDifficultyLevel.StriveToSurvive:
                    silverPerResource = 100;
                    timeBetweenTaxes = 10 * 60000; // 10 days in ticks
                    productionTitheMod = 20;
                    workerCost = 125;
                    break;
                case EmpireDifficultyLevel.BloodAndDust:
                    silverPerResource = 80;
                    timeBetweenTaxes = 15 * 60000; // 15 days in ticks
                    productionTitheMod = 15;
                    workerCost = 125;
                    break;
                case EmpireDifficultyLevel.LosingIsFun:
                    silverPerResource = 70;
                    timeBetweenTaxes = 30 * 60000; // 30 days in ticks
                    productionTitheMod = 10;
                    workerCost = 150;
                    break;
                case EmpireDifficultyLevel.Custom:
                    // Don't change anything for custom
                    break;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref silverPerResource, "silverPerResource");
            Scribe_Values.Look(ref _timeBetweenTaxes, "timeBetweenTaxes");
            
            // Validate timeBetweenTaxes after loading to prevent corruption issues
            if (Scribe.mode == LoadSaveMode.LoadingVars && _timeBetweenTaxes <= 0)
            {
                // Restore based on current difficulty level, not always to default
                int correctValue = GetTimeBetweenTaxesForDifficulty(difficultyLevel);
                Log.Warning($"Empire Mod - Settings: Detected corrupted timeBetweenTaxes value ({_timeBetweenTaxes}), restoring to difficulty preset ({difficultyLevel} = {correctValue / 60000} days)");
                _timeBetweenTaxes = correctValue;
            }
            Scribe_Values.Look(ref productionTitheMod, "productionTitheMod");
            Scribe_Values.Look(ref workerCost, "workerCost");
            Scribe_Values.Look(ref settlementMaxLevel, "settlementMaxLevel");
            Scribe_Values.Look(ref medievalTechOnly, "medievalTechOnly");
            Scribe_Values.Look(ref disableHostileMilitaryActions, "disableHostileMilitaryActions");
            Scribe_Values.Look(ref disableRandomEvents, "disableRandomEvents");
            Scribe_Values.Look(ref forcedTaxDeliveryMode, "forcedTaxDeliveryMode", default);
            Scribe_Values.Look(ref taxNotificationMode, "taxNotificationMode", TaxNotificationMode.All);
            Scribe_Values.Look(ref deadPawnsIncreaseMilitaryCooldown, "deadPawnsIncreaseMilitaryCooldown");
            Scribe_Values.Look(ref settlementsAutoBattle, "settlementsAutoBattle");
            Scribe_Values.Look(ref minDaysTillMilitaryAction, "minDaysTillMilitaryAction");
            Scribe_Values.Look(ref maxDaysTillMilitaryAction, "maxDaysTillMilitaryAction");
            Scribe_Values.Look(ref minDaysTillRandomEvent, "minDaysTillRandomEvent", 0);
            Scribe_Values.Look(ref maxDaysTillRandomEvent, "maxDaysTillRandomEvent", 6);
            Scribe_Values.Look(ref updateVersion, "updateVersion");
            Scribe_Values.Look(ref buildingWindowWidth, "buildingWindowWidth", 450f);
            Scribe_Values.Look(ref buildingWindowHeight, "buildingWindowHeight", 600f);
            Scribe_Values.Look(ref difficultyLevel, "difficultyLevel", EmpireDifficultyLevel.AdventureStory);
            
            // Band aid - For existing users upgrading from old system, detect if they have custom values
            if (Scribe.mode == LoadSaveMode.LoadingVars && difficultyLevel == EmpireDifficultyLevel.AdventureStory)
            {
                // Check if current values match Adventure Story defaults
                if (silverPerResource != 100 || (timeBetweenTaxes / 60000) != 5 || productionTitheMod != 25 || workerCost != 100)
                {
                    // User had custom settings, set to Custom mode
                    difficultyLevel = EmpireDifficultyLevel.Custom;
                }
            }
        }
    }

    
    public class FactionColoniesMod : Mod
    {
        public FactionColonies settings = new FactionColonies();

        public FactionColoniesMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<FactionColonies>();
        }

        string silverPerResource;
        string timeBetweenTaxes;
        string productionTitheMod;
        string workerCost;
        string settlementMaxLevel;
        int daysBetweenTaxes;
        IntRange minMaxDaysTillMilitaryAction = new IntRange(4, 10);
        IntRange minMaxDaysTillRandomEvent = new IntRange(0, 6);

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
                    return new FloatMenuOption("taxDeliveryModeShuttleDesc".Translate(), delegate () {settings.forcedTaxDeliveryMode = TaxDeliveryMode.Shuttle;});
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
                    new FloatMenuOption("taxDeliveryModeDefaultDesc".Translate(), delegate() {settings.forcedTaxDeliveryMode = default;}),
                    new FloatMenuOption("taxDeliveryModeTaxSpotDesc".Translate(), delegate() {settings.forcedTaxDeliveryMode = TaxDeliveryMode.TaxSpot;}),
                    new FloatMenuOption("taxDeliveryModeCaravanDesc".Translate(), delegate() {settings.forcedTaxDeliveryMode = TaxDeliveryMode.Caravan;}),
                    new FloatMenuOption("taxDeliveryModeDropPodDesc".Translate(), delegate() {settings.forcedTaxDeliveryMode = TaxDeliveryMode.DropPod;}),
                    ShuttleOption
                };
            }
        }

        /// <summary>
        /// Creates a list of options for tax notification mode
        /// </summary>
        private List<FloatMenuOption> TaxNotificationOptions => new List<FloatMenuOption>
        {
            new FloatMenuOption("FCTaxNotifyAll".Translate(), () => settings.taxNotificationMode = TaxNotificationMode.All),
            new FloatMenuOption("FCTaxNotifyLetterOnly".Translate(), () => settings.taxNotificationMode = TaxNotificationMode.LetterOnly),
            new FloatMenuOption("FCTaxNotifyMessageOnly".Translate(), () => settings.taxNotificationMode = TaxNotificationMode.MessageOnly),
            new FloatMenuOption("FCTaxNotifyNone".Translate(), () => settings.taxNotificationMode = TaxNotificationMode.None)
        };

        public override void DoSettingsWindowContents(Rect inRect)
        {
            silverPerResource = settings.silverPerResource.ToString();
            timeBetweenTaxes = (settings.timeBetweenTaxes / 60000).ToString();
            productionTitheMod = settings.productionTitheMod.ToString();
            workerCost = settings.workerCost.ToString();
            settlementMaxLevel = settings.settlementMaxLevel.ToString();
            daysBetweenTaxes = settings.timeBetweenTaxes / 60000;

            minMaxDaysTillMilitaryAction = new IntRange(settings.minDaysTillMilitaryAction, settings.maxDaysTillMilitaryAction);
            minMaxDaysTillRandomEvent = new IntRange(settings.minDaysTillRandomEvent, settings.maxDaysTillRandomEvent);

            viewRectHeight = viewRectHeight == -1f ? float.MaxValue : viewRectHeight;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width - 17f, viewRectHeight);

            Widgets.BeginScrollView(inRect, ref scrollVector, viewRect);
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(viewRect);

            // Display mod version
            ls.Label("Empire Mod Version: " + FactionColonies.GetModVersion());
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
                bool isSelected = settings.difficultyLevel == option.level;
                
                if (ls.RadioButton(option.nameKey.Translate(), isSelected))
                {
                    if (!isSelected) // Only change if not already selected
                    {
                        settings.difficultyLevel = option.level;
                        if (option.level != EmpireDifficultyLevel.Custom)
                        {
                            settings.ApplyDifficultyPreset(option.level);
                        }
                    }
                }
                // Add description as a separate indented label
                ls.Label("    " + option.descKey.Translate(), -1f);
            }

            ls.Gap(15f);

            // Show economic settings only if Custom is selected
            if (settings.difficultyLevel == EmpireDifficultyLevel.Custom)
            {
                ls.Label("FCSettingSilverPerResource".Translate());
                ls.IntEntry(ref settings.silverPerResource, ref silverPerResource);
                ls.Label("FCSettingDaysBetweenTax".Translate());
                ls.IntEntry(ref daysBetweenTaxes, ref timeBetweenTaxes);
                settings.timeBetweenTaxes = Math.Max(1, daysBetweenTaxes) * 60000;
                ls.Label("FCSettingProductionTitheMod".Translate());
                ls.IntEntry(ref settings.productionTitheMod, ref productionTitheMod);
                ls.Label("FCSettingWorkerCost".Translate());
                ls.IntEntry(ref settings.workerCost, ref workerCost);
            }
            else
            {
                // Show current values as read-only labels for non-custom difficulties
                ls.Label($"FCSettingSilverPerResource".Translate() + ": " + settings.silverPerResource);
                ls.Label($"FCSettingDaysBetweenTax".Translate() + ": " + (settings.timeBetweenTaxes / 60000));
                ls.Label($"FCSettingProductionTitheMod".Translate() + ": " + settings.productionTitheMod);
                ls.Label($"FCSettingWorkerCost".Translate() + ": " + settings.workerCost);
            }

            ls.Label("FCSettingMaxSettlementLevel".Translate());
            ls.IntEntry(ref settings.settlementMaxLevel, ref settlementMaxLevel);
            ls.CheckboxLabeled("MedievalTechOnly".Translate(), ref settings.medievalTechOnly);
            ls.CheckboxLabeled("FCSettingDisableHostileMilActions".Translate(), ref settings.disableHostileMilitaryActions);
            ls.CheckboxLabeled("FCSettingDisableRandomEvents".Translate(), ref settings.disableRandomEvents);
            ls.CheckboxLabeled("FCSettingDeadPawnsIncreaseMilCooldown".Translate(), ref settings.deadPawnsIncreaseMilitaryCooldown);
            ls.CheckboxLabeled("FCSettingForcedPausing".Translate(), ref settings.disableForcedPausingDuringEvents);
            //ls.CheckboxLabeled("FCSettingAutoResolveBattles".Translate(), ref settings.settlementsAutoBattle);
            if (ls.ButtonText("selectTaxDeliveryModeButton".Translate() + settings.forcedTaxDeliveryMode)) Find.WindowStack.Add(new FloatMenu(ForcedTaxDeliveryOptions));
            if (ls.ButtonText("FCTaxNotificationModeButton".Translate() + settings.taxNotificationMode)) Find.WindowStack.Add(new FloatMenu(TaxNotificationOptions));

            ls.Label("FCSettingMinMaxMilitaryAction".Translate());
            ls.IntRange(ref minMaxDaysTillMilitaryAction, 1, 30);
            settings.minDaysTillMilitaryAction = minMaxDaysTillMilitaryAction.min;
            settings.maxDaysTillMilitaryAction = Math.Max(1, minMaxDaysTillMilitaryAction.max);

            ls.Label("FCSettingMinMaxRandomEvent".Translate());
            ls.IntRange(ref minMaxDaysTillRandomEvent, 0, 30);
            settings.minDaysTillRandomEvent = minMaxDaysTillRandomEvent.min;
            settings.maxDaysTillRandomEvent = Math.Max(1, minMaxDaysTillRandomEvent.max);

            if (ls.ButtonText("FCOpenPatchNotes".Translate())) DebugActionsMisc.PatchNotesDisplayWindow();

            if (ls.ButtonText("FCSettingResetButton".Translate()))
            {
                FactionColonies blank = new FactionColonies();
                settings.silverPerResource = blank.silverPerResource;
                settings.timeBetweenTaxes = blank.timeBetweenTaxes;
                settings.productionTitheMod = blank.productionTitheMod;
                settings.workerCost = blank.workerCost;
                settings.medievalTechOnly = blank.medievalTechOnly;
                settings.settlementMaxLevel = blank.settlementMaxLevel;
                settings.minDaysTillMilitaryAction = blank.minDaysTillMilitaryAction;
                settings.maxDaysTillMilitaryAction = blank.maxDaysTillMilitaryAction;
                settings.minDaysTillRandomEvent = blank.minDaysTillRandomEvent;
                settings.maxDaysTillRandomEvent = blank.maxDaysTillRandomEvent;
                settings.disableRandomEvents = blank.disableRandomEvents;
                settings.deadPawnsIncreaseMilitaryCooldown = blank.deadPawnsIncreaseMilitaryCooldown;
                settings.settlementsAutoBattle = blank.settlementsAutoBattle;
                settings.disableForcedPausingDuringEvents = blank.disableForcedPausingDuringEvents;
                settings.forcedTaxDeliveryMode = blank.forcedTaxDeliveryMode;
                settings.taxNotificationMode = blank.taxNotificationMode;
                settings.difficultyLevel = blank.difficultyLevel;
                settings.ApplyDifficultyPreset(settings.difficultyLevel);
            }

            FixScrollingBug(ls);
            ls.End();

            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
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

        public override string SettingsCategory()
        {
            return "Empire";
        }

        public override void WriteSettings()
        {
            // Only update timeBetweenTaxes if daysBetweenTaxes has been properly initialized
            // (i.e., the settings window was actually opened during this session)
            if (daysBetweenTaxes > 0)
            {
                LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().timeBetweenTaxes = daysBetweenTaxes * 60000;
            }
            base.WriteSettings();
        }
    }
}
